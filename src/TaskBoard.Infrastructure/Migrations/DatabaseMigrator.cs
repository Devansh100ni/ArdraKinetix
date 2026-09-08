using System.Data;
using EvolveDb;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TaskBoard.Infrastructure.Migrations;

public class DatabaseMigrator
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseMigrator> _logger;

    public DatabaseMigrator(IConfiguration configuration, ILogger<DatabaseMigrator> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=(localdb)\\mssqllocaldb;Database=TaskBoardDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
        _logger = logger;
    }

    public void MigrateDatabase()
    {
        try
        {
            EnsureDatabaseExists(_connectionString);

            using var cnx = new SqlConnection(_connectionString);

            // Determine migrations directory
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var scriptsLocation = Path.Combine(baseDirectory, "Migrations", "Scripts");

            if (!Directory.Exists(scriptsLocation))
            {
                // Fallback to source directory if running in local debug
                var altLocation = Path.Combine(Directory.GetCurrentDirectory(), "..", "TaskBoard.Infrastructure", "Migrations", "Scripts");
                if (Directory.Exists(altLocation))
                {
                    scriptsLocation = altLocation;
                }
            }

            _logger.LogInformation("Running Evolve migrations from: {Location}", scriptsLocation);

            var evolve = new Evolve(cnx, msg => _logger.LogInformation("Evolve: {Message}", msg))
            {
                Locations = [scriptsLocation],
                IsEraseDisabled = true,
                CommandTimeout = 60,
                MetadataTableSchema = "dbo",
                MetadataTableName = "changelog"
            };

            evolve.Migrate();
            _logger.LogInformation("Evolve migrations completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during Evolve database migration.");
            throw;
        }
    }

    private void EnsureDatabaseExists(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var targetDb = builder.InitialCatalog;

        if (string.IsNullOrWhiteSpace(targetDb))
        {
            return;
        }

        builder.InitialCatalog = "master";
        using var masterCnx = new SqlConnection(builder.ConnectionString);
        masterCnx.Open();

        using var cmd = masterCnx.CreateCommand();
        cmd.CommandText = $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{targetDb}') CREATE DATABASE [{targetDb}];";
        cmd.ExecuteNonQuery();

        _logger.LogInformation("Database '{DatabaseName}' verified/created.", targetDb);
    }
}
