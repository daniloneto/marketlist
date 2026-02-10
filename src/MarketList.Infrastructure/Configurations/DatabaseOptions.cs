namespace MarketList.Infrastructure.Configurations;

/// <summary>
/// Configurações do banco de dados
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// Provider do banco de dados: "Postgres" ou "Sqlite"
    /// </summary>
    public string Provider { get; set; } = "Postgres";

    /// <summary>
    /// Connection strings para diferentes providers
    /// </summary>
    public DatabaseConnectionStrings ConnectionStrings { get; set; } = new();
}

/// <summary>
/// Connection strings para diferentes providers de banco de dados
/// </summary>
public class DatabaseConnectionStrings
{
    /// <summary>
    /// Connection string PostgreSQL
    /// </summary>
    public string? Postgres { get; set; }

    /// <summary>
    /// Connection string SQLite
    /// </summary>
    public string? Sqlite { get; set; }
}
