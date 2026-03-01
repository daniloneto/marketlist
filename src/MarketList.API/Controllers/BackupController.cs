using MarketList.Domain.Entities;
using MarketList.Domain.Helpers;
using MarketList.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
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
    /// A ordem é FIXA para garantir que as FKs sejam respeitadas na importação
    /// </summary>
    private List<(string Name, Type EntityType)> GetOrderedDbSets()
    {
        // Ordem fixa garantida por dependências:
        // 1. Tabelas sem dependências
        // 2. Tabelas que dependem das anteriores
        var orderedTypes = new List<(string Name, Type EntityType)>
        {
            ("Categorias", typeof(Categoria)),
            ("Empresas", typeof(Empresa)),
            ("Produtos", typeof(Produto)),
            ("SinonimosProduto", typeof(SinonimoProduto)),
            ("HistoricoPrecos", typeof(HistoricoPreco)),
            ("RegrasClassificacaoCategoria", typeof(RegraClassificacaoCategoria)),
            ("ListasDeCompras", typeof(ListaDeCompras)),
            ("ItensListaDeCompras", typeof(ItemListaDeCompras)),
        };

        return orderedTypes;
    }

    /// <summary>
    /// Método auxiliar genérico para obter todos os registros de uma entidade com suas relações (até 2 níveis)
    /// </summary>
    private async Task<List<T>> GetAllEntitiesAsync<T>(CancellationToken cancellationToken) where T : BaseEntity
    {
        // Para entidades específicas que precisam de relações aninhadas, usa métodos dedicados
        if (typeof(T) == typeof(Categoria))
        {
            var categorias = await _context.Categorias
                .AsNoTracking()
                .Include(c => c.Produtos)
                    .ThenInclude(p => p.Sinonimos)
                .Include(c => c.Produtos)
                    .ThenInclude(p => p.HistoricoPrecos)
                .ToListAsync(cancellationToken);
            return (categorias as List<T>)!;
        }

        if (typeof(T) == typeof(Produto))
        {
            var produtos = await _context.Produtos
                .AsNoTracking()
                .Include(p => p.Sinonimos)
                .Include(p => p.HistoricoPrecos)
                .Include(p => p.ItensLista)
                .ToListAsync(cancellationToken);
            return (produtos as List<T>)!;
        }

        if (typeof(T) == typeof(ListaDeCompras))
        {
            var listas = await _context.ListasDeCompras
                .AsNoTracking()
                .Include(l => l.Itens)
                .ToListAsync(cancellationToken);
            return (listas as List<T>)!;
        }

        // Para outras entidades, carrega com navegações de primeiro nível
        IQueryable<T> query = _context.Set<T>().AsNoTracking();
        
        var entityType = _context.Model.FindEntityType(typeof(T));
        if (entityType != null)
        {
            var navigations = entityType.GetNavigations()
                .Where(n => n.IsCollection)
                .Select(n => n.Name)
                .ToList();
            
            foreach (var navigation in navigations)
            {
                query = EntityFrameworkQueryableExtensions.Include(query, navigation);
            }
        }
        
        return await query.ToListAsync(cancellationToken);
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
            var fileName = $"fincontrol_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";

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
            _logger.LogInformation("Ordem de importação: {Ordem}", string.Join(" -> ", dbSets.Select(d => d.Name)));
            
            // Log das chaves encontradas no JSON
            var jsonKeys = jsonNode.AsObject().Select(p => p.Key).ToList();
            _logger.LogInformation("Chaves encontradas no JSON: {Keys}", string.Join(", ", jsonKeys));
            
            foreach (var (name, entityType) in dbSets)
            {
                var entityResult = new EntityImportResult();
                result[name] = entityResult;

                // Tenta encontrar a chave no JSON (camelCase ou PascalCase)
                var nameCamelCase = char.ToLower(name[0]) + name[1..];
                var dataNode = jsonNode[nameCamelCase] ?? jsonNode[name];
                if (dataNode == null)
                {
                    _logger.LogWarning("Entidade {Name} (ou {CamelCase}) não encontrada no backup. Chaves disponíveis: {Keys}", 
                        name, nameCamelCase, string.Join(", ", jsonKeys));
                    continue;
                }

                var listType = typeof(List<>).MakeGenericType(entityType);
                var items = JsonSerializer.Deserialize(dataNode.ToJsonString(), listType, options) as System.Collections.IList;

                if (items == null || items.Count == 0) 
                {
                    _logger.LogInformation("Entidade {Name}: 0 registros", name);
                    continue;
                }
                
                _logger.LogInformation("Importando {Name}: {Count} registros", name, items.Count);

                foreach (var item in items)
                {
                    var entity = (BaseEntity)item;

                    // Verifica se já existe no banco
                    var existsMethod = typeof(BackupController)
                        .GetMethod(nameof(EntityExistsAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
                        .MakeGenericMethod(entityType);

                    var existsTask = (Task<bool>)existsMethod.Invoke(this, new object[] { entity.Id, cancellationToken })!;
                    var exists = await existsTask;

                    if (!exists)
                    {
                        // Insere via SQL direto para TODAS as entidades
                        // Isso preserva os IDs originais e evita problemas com
                        // cascade inserts de navigation properties
                        await InsertEntityViaSqlAsync(item, entityType, cancellationToken);
                        entityResult.Imported++;
                    }
                    else
                    {
                        entityResult.Skipped++;
                    }
                }

                _logger.LogInformation("Entidade {Name}: {Imported} importados, {Skipped} ignorados", 
                    name, entityResult.Imported, entityResult.Skipped);
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
    /// Insere uma entidade diretamente via SQL, usando metadados do EF Core para descobrir
    /// nomes de tabela/colunas. Ignora navigation properties completamente, preserva IDs originais.
    /// </summary>
    private async Task InsertEntityViaSqlAsync(object entity, Type entityType, CancellationToken ct)
    {
        var efEntityType = _context.Model.FindEntityType(entityType)
            ?? throw new InvalidOperationException($"Tipo {entityType.Name} não encontrado no modelo EF Core");

        var tableName = efEntityType.GetTableName()!;
        var schema = efEntityType.GetSchema();
        var storeObject = StoreObjectIdentifier.Table(tableName, schema);

        // GetProperties() retorna apenas propriedades escalares (não navigations)
        var properties = efEntityType.GetProperties()
            .Where(p => !p.IsShadowProperty() && entityType.GetProperty(p.Name) != null)
            .ToList();

        var columns = new List<string>();
        var paramPlaceholders = new List<string>();
        var values = new List<object>();
        int paramIndex = 0;

        for (int i = 0; i < properties.Count; i++)
        {
            var prop = properties[i];
            var columnName = prop.GetColumnName(storeObject) ?? prop.Name;
            var value = entityType.GetProperty(prop.Name)!.GetValue(entity);

            // Corrige DateTimeKind para UTC se necessário
            if (value is DateTime dt)
            {
                value = DateTimeHelper.EnsureUtc(dt);
            }

            columns.Add(columnName);

            if (value == null)
            {
                // Null values go directly in SQL, no parameter needed
                paramPlaceholders.Add("NULL");
            }
            else
            {
                paramPlaceholders.Add($"{{{paramIndex}}}");
                values.Add(value);
                paramIndex++;
            }
        }

        var sql = $"INSERT INTO {tableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", paramPlaceholders)})";
        await _context.Database.ExecuteSqlRawAsync(sql, values.ToArray(), ct);
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

    /// <summary>
    /// Analisa um arquivo de backup sem importar (diagnóstico)
    /// </summary>
    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeBackup(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Nenhum arquivo enviado" });

        try
        {
            using var stream = new StreamReader(file.OpenReadStream());
            var json = await stream.ReadToEndAsync(cancellationToken);

            var jsonNode = JsonNode.Parse(json);
            if (jsonNode == null)
                return BadRequest(new { error = "Arquivo de backup inválido" });

            var analysis = new Dictionary<string, object>();
            var dbSets = GetOrderedDbSets();

            foreach (var (name, entityType) in dbSets)
            {
                var nameCamelCase = char.ToLower(name[0]) + name[1..];
                var dataNode = jsonNode[nameCamelCase] ?? jsonNode[name];
                
                if (dataNode is JsonArray arr)
                {
                    analysis[name] = new
                    {
                        found = true,
                        count = arr.Count,
                        keyInJson = jsonNode[nameCamelCase] != null ? nameCamelCase : name
                    };
                }
                else
                {
                    analysis[name] = new
                    {
                        found = false,
                        count = 0,
                        keyInJson = (string?)null
                    };
                }
            }

            // Mostra todas as chaves encontradas no JSON
            var allKeys = jsonNode.AsObject().Select(p => p.Key).ToList();

            return Ok(new
            {
                expectedOrder = dbSets.Select(d => d.Name).ToList(),
                keysInJson = allKeys,
                analysis
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Erro ao analisar backup", details = ex.Message });
        }
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
