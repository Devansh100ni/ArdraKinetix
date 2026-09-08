using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TaskBoard.Infrastructure.Migrations;
using Xunit;

namespace TaskBoard.Infrastructure.Tests;

public class DatabaseMigrationTests
{
    private const string ConnectionString = "Server=(localdb)\\mssqllocaldb;Database=TaskBoardDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    [Fact]
    public void MigrateDatabase_ExecutesSuccessfully_CreatesTablesAndSeeds()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "ConnectionStrings:DefaultConnection", ConnectionString }
        });
        var configuration = configBuilder.Build();

        var migrator = new DatabaseMigrator(configuration, NullLogger<DatabaseMigrator>.Instance);
        migrator.MigrateDatabase();

        // Verify tables exist and have seed data
        using var cnx = new SqlConnection(ConnectionString);
        cnx.Open();

        using var cmd = cnx.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM [dbo].[Roles];";
        var roleCount = Convert.ToInt32(cmd.ExecuteScalar());
        roleCount.Should().BeGreaterThanOrEqualTo(3);

        cmd.CommandText = "SELECT COUNT(*) FROM [dbo].[Users];";
        var userCount = Convert.ToInt32(cmd.ExecuteScalar());
        userCount.Should().BeGreaterThanOrEqualTo(4);

        cmd.CommandText = "SELECT COUNT(*) FROM [dbo].[Tenants];";
        var tenantCount = Convert.ToInt32(cmd.ExecuteScalar());
        tenantCount.Should().BeGreaterThanOrEqualTo(2);

        cmd.CommandText = "SELECT COUNT(*) FROM [dbo].[Tasks];";
        var taskCount = Convert.ToInt32(cmd.ExecuteScalar());
        taskCount.Should().BeGreaterThanOrEqualTo(5);

        cmd.CommandText = "SELECT COUNT(*) FROM [dbo].[TaskStatuses];";
        var statusCount = Convert.ToInt32(cmd.ExecuteScalar());
        statusCount.Should().BeGreaterThanOrEqualTo(6);

        cmd.CommandText = "SELECT COUNT(*) FROM [dbo].[TaskPriorities];";
        var priorityCount = Convert.ToInt32(cmd.ExecuteScalar());
        priorityCount.Should().BeGreaterThanOrEqualTo(4);

        cmd.CommandText = "SELECT COUNT(*) FROM [dbo].[ScrumBoards];";
        var boardCount = Convert.ToInt32(cmd.ExecuteScalar());
        boardCount.Should().BeGreaterThanOrEqualTo(1);
    }
}
