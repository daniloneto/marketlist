# Suporte a Múltiplos Providers de Banco de Dados

Este documento descreve como usar o MarketList com PostgreSQL ou SQLite.

## 📋 Resumo das Mudanças

### Dependências Adicionadas

- **Microsoft.EntityFrameworkCore.Sqlite 9.0.4** - Suporte a SQLite no EF Core
- **Hangfire.InMemory 0.6.0** - Storage em memória para jobs (usado com SQLite)

### Novos Arquivos

- `src/MarketList.Infrastructure/Configurations/DatabaseOptions.cs` - Classe de configuração fortemente tipada
- `docker-compose.sqlite.yml` - Orquestração com SQLite
- `MULTI_DB_SETUP.md` - Este arquivo

### Arquivos Modificados

- `src/MarketList.Infrastructure/MarketList.Infrastructure.csproj` - Adicionado SQLite
- `src/MarketList.API/MarketList.API.csproj` - Adicionado Hangfire.InMemory
- `src/MarketList.Infrastructure/DependencyInjection.cs` - Lógica de seleção de provider
- `src/MarketList.API/Program.cs` - Configuração condicional de Hangfire
- `src/MarketList.API/appsettings.json` - Seção Database com ambos providers
- `src/MarketList.API/appsettings.Development.json` - Suporta alternância de provider
- `docker-compose.yml` - Variáveis de ambiente atualizadas para novo formato
- `README.md` - Documentação atualizada

---

## 🚀 Como Usar

### Cenário 1: PostgreSQL (Recomendado para Produção)

#### 1.1 Com Docker Compose

```bash
# Subir com PostgreSQL
docker-compose up --build

# API em http://localhost:5000
# Postgres em localhost:5432
# Hangfire Dashboard em http://localhost:5000/hangfire
```

#### 1.2 Localmente

Edite `appsettings.Development.json`:
```json
{
  "Database": {
    "Provider": "Postgres"
  }
}
```

Inicie o PostgreSQL:
```bash
docker-compose up -d postgres
```

Execute a API:
```bash
cd src/MarketList.API
dotnet run
```

#### 1.3 Via Variáveis de Ambiente

```bash
# PowerShell
$env:Database__Provider = "Postgres"
$env:Database__ConnectionStrings__Postgres = "Host=localhost;Port=5432;Database=marketlist;Username=postgres;Password=postgres"

# Bash
export Database__Provider=Postgres
export Database__ConnectionStrings__Postgres="Host=localhost;Port=5432;Database=marketlist;Username=postgres;Password=postgres"

cd src/MarketList.API
dotnet run
```

---

### Cenário 2: SQLite (Desenvolvimento Local Simplificado)

#### 2.1 Com Docker Compose

```bash
# Subir com SQLite (sem PostgreSQL)
docker-compose -f docker-compose.sqlite.yml up --build

# API em http://localhost:5000
# Banco em ./data/marketlist.db
# Hangfire Dashboard em http://localhost:5000/hangfire (storage em memória)
```

#### 2.2 Localmente

Edite `appsettings.Development.json`:
```json
{
  "Database": {
    "Provider": "Sqlite"
  }
}
```

Execute a API:
```bash
cd src/MarketList.API
dotnet run
```

Banco será criado em: `marketlist.db`

#### 2.3 Via Variáveis de Ambiente

```bash
# PowerShell
$env:Database__Provider = "Sqlite"
$env:Database__ConnectionStrings__Sqlite = "Data Source=marketlist.db"

# Bash
export Database__Provider=Sqlite
export Database__ConnectionStrings__Sqlite="Data Source=marketlist.db"

cd src/MarketList.API
dotnet run
```

---

## ⚙️ Configuração

### Arquivo de Configuração Principal

`src/MarketList.API/appsettings.json`:

```json
{
  "Database": {
    "Provider": "Postgres",  // "Postgres" ou "Sqlite"
    "ConnectionStrings": {
      "Postgres": "Host=localhost;Port=5432;Database=marketlist;Username=postgres;Password=postgres",
      "Sqlite": "Data Source=marketlist.db"
    }
  }
}
```

### Classe de Configuração

`src/MarketList.Infrastructure/Configurations/DatabaseOptions.cs`:

```csharp
public class DatabaseOptions
{
    public string Provider { get; set; } = "Postgres";
    public DatabaseConnectionStrings ConnectionStrings { get; set; } = new();
}

public class DatabaseConnectionStrings
{
    public string? Postgres { get; set; }
    public string? Sqlite { get; set; }
}
```

### Lógica de Seleção de Provider

Em `src/MarketList.Infrastructure/DependencyInjection.cs`:

```csharp
private static void ConfigureDbContext(IServiceCollection services, DatabaseOptions databaseOptions)
{
    services.AddDbContext<AppDbContext>(options =>
    {
        var provider = databaseOptions.Provider.ToLower();
        
        if (provider == "sqlite")
        {
            options.UseSqlite(connectionString);
        }
        else if (provider == "postgres")
        {
            options.UseNpgsql(connectionString);
        }
        else
        {
            throw new InvalidOperationException("Provider inválido");
        }
    });
}
```

---

## 🗄️ Migrações

O Entity Framework Core funciona normalmente com ambos os providers:

### Criar Nova Migração

```bash
cd src/MarketList.Infrastructure
dotnet ef migrations add MeuNome --startup-project ../MarketList.API
```

### Aplicar Migrações

As migrações são aplicadas automaticamente ao iniciar a API:

```bash
cd src/MarketList.API
dotnet run
```

Ou manualmente:
```bash
cd src/MarketList.Infrastructure
dotnet ef database update --startup-project ../MarketList.API
```

---

## 🔧 Hangfire Jobs

### Com PostgreSQL

- Storage persistente em PostgreSQL
- Jobs são mantidos entre reinicializações
- Suporta múltiplas instâncias

### Com SQLite

- Storage em memória (via `Hangfire.InMemory`)
- Jobs são perdidos ao reiniciar
- Adequado apenas para desenvolvimento

Para produção com SQLite, considere usar um storage persistente diferente.

---

## 🐳 Docker Compose

### docker-compose.yml (PostgreSQL)

Inclui:
- PostgreSQL 16
- API .NET
- Ollama (LLM)

Variáveis de ambiente:
```yaml
environment:
  - Database__Provider=Postgres
  - Database__ConnectionStrings__Postgres=Host=postgres;Port=5432;...
```

### docker-compose.sqlite.yml (SQLite)

Inclui:
- API .NET com SQLite
- Ollama (LLM)

Variáveis de ambiente:
```yaml
environment:
  - Database__Provider=Sqlite
  - Database__ConnectionStrings__Sqlite=Data Source=/data/marketlist.db
volumes:
  - ./data:/data
```

---

## ✅ Verificação

### Testar PostgreSQL

```bash
# Build
cd src/MarketList.API
dotnet build

# Restaurar pacotes
dotnet restore

# Rodar com variáveis de ambiente
$env:Database__Provider = "Postgres"
$env:Database__ConnectionStrings__Postgres = "Host=localhost;Port=5432;Database=test;Username=postgres;Password=postgres"
dotnet run
```

Esperado:
- ✅ App inicia sem erros
- ✅ Migrations aplicadas
- ✅ API disponível em http://localhost:5000
- ✅ Swagger em http://localhost:5000/swagger

### Testar SQLite

```bash
# Build (se não feito)
cd src/MarketList.API
dotnet build

# Rodar com SQLite
$env:Database__Provider = "Sqlite"
$env:Database__ConnectionStrings__Sqlite = "Data Source=test.db"
dotnet run
```

Esperado:
- ✅ App inicia sem erros
- ✅ Arquivo `test.db` é criado
- ✅ Migrations aplicadas
- ✅ API disponível em http://localhost:5000
- ✅ Swagger em http://localhost:5000/swagger

---

## 🔄 Mudança de Provider em Tempo de Desenvolvimento

### Opção 1: Editar appsettings.Development.json

```json
{
  "Database": {
    "Provider": "Sqlite"  // Mude conforme necessário
  }
}
```

### Opção 2: Variáveis de Ambiente (PowerShell)

```powershell
# Para PostgreSQL
$env:Database__Provider = "Postgres"
$env:Database__ConnectionStrings__Postgres = "Host=localhost;Port=5432;..."

# Para SQLite
$env:Database__Provider = "Sqlite"
$env:Database__ConnectionStrings__Sqlite = "Data Source=marketlist.db"

dotnet run
```

### Opção 3: Variáveis de Ambiente (Bash)

```bash
# Para PostgreSQL
export Database__Provider=Postgres
export Database__ConnectionStrings__Postgres="Host=localhost;Port=5432;..."

# Para SQLite
export Database__Provider=Sqlite
export Database__ConnectionStrings__Sqlite="Data Source=marketlist.db"

dotnet run
```

---

## 📝 Notas Importantes

1. **Entidades não mudam**: As classes de domínio funcionam igual para ambos os providers
2. **Repositórios não mudam**: A lógica de acesso a dados permanece a mesma
3. **Migrações funcionam para ambos**: O EF Core cria SQL apropriado para cada provider
4. **Hangfire com SQLite**: Usa storage em memória (não persistente) - adequado só para dev
5. **Clean Architecture preservada**: Toda a lógica de negócio fica independente do banco

---

## 🚨 Troubleshooting

### Erro: "Database provider not found"

**Causa**: Provider inválido na configuração

**Solução**:
```json
{
  "Database": {
    "Provider": "Postgres"  // ou "Sqlite"
  }
}
```

### Erro: "Connection string not configured"

**Causa**: Connection string vazia para o provider

**Solução**: Verifique `appsettings.json`:
```json
{
  "Database": {
    "ConnectionStrings": {
      "Postgres": "Host=localhost;...",  // Não pode estar vazio
      "Sqlite": "Data Source=marketlist.db"
    }
  }
}
```

### Hangfire Dashboard não mostra jobs (SQLite)

**Causa**: Storage em memória perde jobs ao reiniciar

**Solução esperada**: Isso é normal com SQLite. Use PostgreSQL para persistência.

---

## 📚 Referências

- [Entity Framework Core - SQLite](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/)
- [Entity Framework Core - PostgreSQL](https://learn.microsoft.com/en-us/ef/core/providers/postgresql/)
- [Hangfire - Storage Options](https://docs.hangfire.io/en/latest/background-methods/index.html)
- [Microsoft Configuration - Environment Variables](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
