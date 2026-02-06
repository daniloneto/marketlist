using MarketList.Domain.Entities;
using MarketList.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MarketList.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BackupController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<BackupController> _logger;

    public BackupController(AppDbContext context, ILogger<BackupController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Obtém todos os DbSets do contexto ordenados por dependências (tabelas pai primeiro)
    /// </summary>
    private List<(string Name, Type EntityType)> GetOrderedDbSets()
    {
        var dbSetProperties = _context.GetType()
            .GetProperties()
            .Where(p => p.PropertyType.IsGenericType &&
                       p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
                       p.PropertyType.GetGenericArguments()[0].IsSubclassOf(typeof(BaseEntity)))
            .Select(p => (
                Name: p.Name,
                EntityType: p.PropertyType.GetGenericArguments()[0]
            ))
            .ToList();

        // Ordena por dependências usando o metadata do EF Core
        var entityTypes = _context.Model.GetEntityTypes().ToList();
        var ordered = new List<(string Name, Type EntityType)>();
        var remaining = new HashSet<Type>(dbSetProperties.Select(x => x.EntityType));

        while (remaining.Count > 0)
        {
            var added = false;
            foreach (var item in dbSetProperties.Where(x => remaining.Contains(x.EntityType)))
            {
                var efEntityType = entityTypes.FirstOrDefault(e => e.ClrType == item.EntityType);
                if (efEntityType == null) continue;

                // Verifica se todas as dependências (foreign keys) já foram adicionadas
                var foreignKeys = efEntityType.GetForeignKeys();
                var dependencies = foreignKeys
                    .Select(fk => fk.PrincipalEntityType.ClrType)
                    .Where(t => t != item.EntityType) // Ignora auto-referências
                    .ToList();

                if (dependencies.All(d => !remaining.Contains(d)))
                {
                    ordered.Add(item);
                    remaining.Remove(item.EntityType);
                    added = true;
                }
            }

            // Se não conseguiu adicionar nenhum, adiciona o primeiro restante (ciclo ou erro)
            if (!added && remaining.Count > 0)
            {
                var first = dbSetProperties.First(x => remaining.Contains(x.EntityType));
                ordered.Add(first);
                remaining.Remove(first.EntityType);
            }
        }

        return ordered;
    }

    /// <summary>
    /// Método auxiliar genérico para obter todos os registros de uma entidade
    /// </summary>
    private async Task<List<T>> GetAllEntitiesAsync<T>(CancellationToken cancellationToken) where T : BaseEntity
    {
        return await _context.Set<T>().AsNoTracking().ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Método auxiliar genérico para contar registros de uma entidade
    /// </summary>
    private async Task<int> CountEntitiesAsync<T>(CancellationToken cancellationToken) where T : BaseEntity
    {
        return await _context.Set<T>().CountAsync(cancellationToken);
    }

    /// <summary>
    /// Exporta todos os dados do sistema em um arquivo JSON (descoberta dinâmica de entidades)
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        try
        {
            var backup = new Dictionary<string, object>
            {
                ["exportedAt"] = DateTime.UtcNow,
                ["version"] = "2.0"
            };

            var dbSets = GetOrderedDbSets();

            foreach (var (name, entityType) in dbSets)
            {
                // Usa reflection para chamar o método genérico GetAllEntitiesAsync<T>
                var method = typeof(BackupController)
                    .GetMethod(nameof(GetAllEntitiesAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(entityType);

                var task = (Task)method.Invoke(this, new object[] { cancellationToken })!;
                await task.ConfigureAwait(false);

                var resultProperty = task.GetType().GetProperty("Result");
                var data = resultProperty!.GetValue(task);

                backup[name] = data!;
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            };

            var json = JsonSerializer.Serialize(backup, options);
            var fileName = $"marketlist_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";

            return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar backup");
            return StatusCode(500, new { error = "Erro ao exportar dados", details = ex.Message });
        }
    }

    /// <summary>
    /// Importa dados de um arquivo JSON de backup (descoberta dinâmica de entidades)
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> Import(IFormFile file, [FromQuery] bool clearExisting = false, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Nenhum arquivo enviado" });

        if (!file.FileName.EndsWith(".json"))
            return BadRequest(new { error = "Apenas arquivos JSON são suportados" });

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            using var stream = new StreamReader(file.OpenReadStream());
            var json = await stream.ReadToEndAsync(cancellationToken);

            var jsonNode = JsonNode.Parse(json);
            if (jsonNode == null)
                return BadRequest(new { error = "Arquivo de backup inválido" });

            var result = new Dictionary<string, EntityImportResult>();
            var dbSets = GetOrderedDbSets();

            // Se solicitado, limpa os dados existentes na ordem inversa (dependentes primeiro)
            if (clearExisting)
            {
                foreach (var (name, entityType) in dbSets.AsEnumerable().Reverse())
                {
                    // Usa reflection para chamar o método genérico que limpa a entidade
                    var clearMethod = typeof(BackupController)
                        .GetMethod(nameof(ClearEntitiesAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
                        .MakeGenericMethod(entityType);

                    var task = (Task)clearMethod.Invoke(this, new object[] { cancellationToken })!;
                    await task.ConfigureAwait(false);
                }
                await _context.SaveChangesAsync(cancellationToken);
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            // Importa na ordem correta (tabelas pai primeiro)
            foreach (var (name, entityType) in dbSets)
            {
                var entityResult = new EntityImportResult();
                result[name] = entityResult;

                var dataNode = jsonNode[name] ?? jsonNode[char.ToLower(name[0]) + name[1..]];
                if (dataNode == null) continue;

                var listType = typeof(List<>).MakeGenericType(entityType);
                var items = JsonSerializer.Deserialize(dataNode.ToJsonString(), listType, options) as System.Collections.IList;

                if (items == null || items.Count == 0) continue;

                foreach (var item in items)
                {
                    var entity = (BaseEntity)item;

                    // Verifica se já existe
                    var existsMethod = typeof(BackupController)
                        .GetMethod(nameof(EntityExistsAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
                        .MakeGenericMethod(entityType);

                    var existsTask = (Task<bool>)existsMethod.Invoke(this, new object[] { entity.Id, cancellationToken })!;
                    var exists = await existsTask;

                    if (!exists)
                    {
                        _context.Add(item);
                        entityResult.Imported++;
                    }
                    else
                    {
                        entityResult.Skipped++;
                    }
                }

                // Salva após cada entidade para respeitar foreign keys
                await _context.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            var importResult = new ImportResult
            {
                ClearedExisting = clearExisting,
                Details = result,
                TotalImported = result.Values.Sum(x => x.Imported),
                TotalSkipped = result.Values.Sum(x => x.Skipped)
            };

            _logger.LogInformation("Backup importado com sucesso: {@Result}", importResult);

            return Ok(importResult);
        }
        catch (JsonException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Erro ao deserializar arquivo de backup");
            return BadRequest(new { error = "Formato de arquivo inválido", details = ex.Message });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Erro ao importar backup");
            return StatusCode(500, new { error = "Erro ao importar dados", details = ex.Message });
        }
    }

    private async Task<bool> EntityExistsAsync<T>(Guid id, CancellationToken cancellationToken) where T : BaseEntity
    {
        return await _context.Set<T>().AnyAsync(x => x.Id == id, cancellationToken);
    }

    /// <summary>
    /// Método auxiliar genérico para limpar todos os registros de uma entidade
    /// </summary>
    private async Task ClearEntitiesAsync<T>(CancellationToken cancellationToken) where T : BaseEntity
    {
        var entities = await _context.Set<T>().ToListAsync(cancellationToken);
        _context.Set<T>().RemoveRange(entities);
    }

    /// <summary>
    /// Retorna informações sobre o estado atual dos dados (descoberta dinâmica)
    /// </summary>
    [HttpGet("info")]
    public async Task<ActionResult<BackupInfo>> GetInfo(CancellationToken cancellationToken)
    {
        var counts = new Dictionary<string, int>();
        var dbSets = GetOrderedDbSets();

        foreach (var (name, entityType) in dbSets)
        {
            // Usa reflection para chamar o método genérico CountEntitiesAsync<T>
            var method = typeof(BackupController)
                .GetMethod(nameof(CountEntitiesAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(entityType);

            var task = (Task<int>)method.Invoke(this, new object[] { cancellationToken })!;
            counts[name] = await task;
        }

        var info = new BackupInfo
        {
            Entities = counts,
            TotalRegistros = counts.Values.Sum()
        };

        return Ok(info);
    }
}

#region DTOs

public class EntityImportResult
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
}

public class ImportResult
{
    public bool ClearedExisting { get; set; }
    public Dictionary<string, EntityImportResult> Details { get; set; } = new();
    public int TotalImported { get; set; }
    public int TotalSkipped { get; set; }
}

public class BackupInfo
{
    public Dictionary<string, int> Entities { get; set; } = new();
    public int TotalRegistros { get; set; }
}

#endregion
