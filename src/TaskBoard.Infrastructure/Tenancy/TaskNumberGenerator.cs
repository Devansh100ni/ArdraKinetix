using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Security;
using TaskBoard.Domain.Entities;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.Infrastructure.Tenancy;

public class TaskNumberGenerator : ITaskNumberGenerator
{
    private readonly TaskBoardDbContext _context;

    public TaskNumberGenerator(TaskBoardDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateNextTaskNumberAsync(Guid tenantId, string prefix, CancellationToken cancellationToken = default)
    {
        var cleanPrefix = string.IsNullOrWhiteSpace(prefix) ? "TASK" : prefix.Trim().ToUpperInvariant();

        // Safe concurrency handling: if using SQL Server, execute atomic update with UPDLOCK
        if (_context.Database.IsSqlServer())
        {
            var sql = @"
                MERGE INTO TenantTaskSequences WITH (HOLDLOCK) AS target
                USING (SELECT {0} AS TenantId) AS source
                ON (target.TenantId = source.TenantId)
                WHEN MATCHED THEN
                    UPDATE SET LastNumber = target.LastNumber + 1
                WHEN NOT MATCHED THEN
                    INSERT (TenantId, LastNumber) VALUES (source.TenantId, 1)
                OUTPUT INSERTED.LastNumber;";

            var connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            
            var param = cmd.CreateParameter();
            param.ParameterName = "@p0";
            param.Value = tenantId;
            cmd.Parameters.Add(param);

            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            long nextNumber = result != null && result != DBNull.Value ? Convert.ToInt64(result) : 1;
            return $"{cleanPrefix}-{nextNumber:D6}";
        }
        else
        {
            // In-Memory fallback for testing
            var sequence = await _context.TenantTaskSequences
                .FirstOrDefaultAsync(ts => ts.TenantId == tenantId, cancellationToken);

            if (sequence == null)
            {
                sequence = new TenantTaskSequence
                {
                    TenantId = tenantId,
                    LastNumber = 1
                };
                await _context.TenantTaskSequences.AddAsync(sequence, cancellationToken);
            }
            else
            {
                sequence.LastNumber++;
                _context.TenantTaskSequences.Update(sequence);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return $"{cleanPrefix}-{sequence.LastNumber:D6}";
        }
    }
}
