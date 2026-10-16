using Npgsql;
using DashboardWolverine.Models;
using System.Text;

namespace DashboardWolverine.Repositories;

public class WolverineRepository
{
    private readonly string _connectionString;
    private readonly string? _schema;

    public WolverineRepository(string connectionString, string? schema = null)
    {
        _connectionString = connectionString;
        _schema = schema;
    }

    private string GetTableName(string tableName)
    {
        return string.IsNullOrWhiteSpace(_schema) 
            ? tableName 
            : $"{QuoteIdentifier(_schema)}.{tableName}";
    }

    private static string QuoteIdentifier(string identifier)
        => $"\"{identifier.Replace("\"", "\"\"")}\"";

    #region Dead Letters CRUD

    public async Task<DeadLetterResult> GetAllDeadLettersAsync(
        string? messageType = null,
        string? exceptionType = null,
        string? bodySearch = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 10)
    {
        var deadLetters = new List<WolverineDeadLetter>();
        var availableMessageTypes = new List<string>();
        var availableExceptionTypes = new List<string>();
        
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Build WHERE clause dynamically
        var whereConditions = new List<string>();
        var parameters = new List<NpgsqlParameter>();
        var paramIndex = 0;

        if (!string.IsNullOrEmpty(messageType))
        {
            whereConditions.Add($"message_type = @p{paramIndex}");
            parameters.Add(new NpgsqlParameter($"p{paramIndex}", messageType));
            paramIndex++;
        }

        if (!string.IsNullOrEmpty(exceptionType))
        {
            whereConditions.Add($"exception_type = @p{paramIndex}");
            parameters.Add(new NpgsqlParameter($"p{paramIndex}", exceptionType));
            paramIndex++;
        }

        if (!string.IsNullOrEmpty(bodySearch))
        {
            whereConditions.Add($"encode(body, 'escape') ILIKE @p{paramIndex}");
            parameters.Add(new NpgsqlParameter($"p{paramIndex}", $"%{bodySearch}%"));
            paramIndex++;
        }

        if (startDate.HasValue)
        {
            whereConditions.Add($"sent_at >= @p{paramIndex}");
            parameters.Add(new NpgsqlParameter($"p{paramIndex}", startDate.Value));
            paramIndex++;
        }

        if (endDate.HasValue)
        {
            whereConditions.Add($"sent_at <= @p{paramIndex}");
            parameters.Add(new NpgsqlParameter($"p{paramIndex}", endDate.Value));
            paramIndex++;
        }

        var whereClause = whereConditions.Count > 0 ? "WHERE " + string.Join(" AND ", whereConditions) : "";

        // Get total count
        var countQuery = $"SELECT COUNT(*) FROM {GetTableName("wolverine_dead_letters")} {whereClause}";
        int totalCount;
        await using (var countCommand = new NpgsqlCommand(countQuery, connection))
        {
            foreach (var param in parameters)
            {
                countCommand.Parameters.Add(new NpgsqlParameter(param.ParameterName, param.Value));
            }
            totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
        }

        // Get paginated data
        var offset = (page - 1) * pageSize;
        var query = $@"
            SELECT id, execution_time, body, message_type, received_at, 
                   source, exception_type, exception_message, sent_at, 
                   replayable,
                   substring(
                       encode(body, 'escape')
                       FROM position('{{' IN encode(body, 'escape'))
                   ) AS json_body
            FROM {GetTableName("wolverine_dead_letters")}
            {whereClause}
            ORDER BY execution_time DESC NULLS LAST
            LIMIT @limit OFFSET @offset";

        await using (var command = new NpgsqlCommand(query, connection))
        {
            foreach (var param in parameters)
            {
                command.Parameters.Add(new NpgsqlParameter(param.ParameterName, param.Value));
            }
            command.Parameters.AddWithValue("limit", pageSize);
            command.Parameters.AddWithValue("offset", offset);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                deadLetters.Add(new WolverineDeadLetter
                {
                    Id = reader.GetGuid(0),
                    ExecutionTime = reader.IsDBNull(1) ? null : reader.GetDateTime(1),
                    Body = (byte[])reader.GetValue(2),
                    MessageType = reader.GetString(3),
                    ReceivedAt = reader.GetString(4),
                    Source = reader.IsDBNull(5) ? null : reader.GetString(5),
                    ExceptionType = reader.IsDBNull(6) ? null : reader.GetString(6),
                    ExceptionMessage = reader.IsDBNull(7) ? null : reader.GetString(7),
                    SentAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                    Replayable = reader.IsDBNull(9) ? null : reader.GetBoolean(9),
                    JsonBody = reader.IsDBNull(10) ? null : reader.GetString(10)
                });
            }
        }

        // Get available filter options
        var messageTypesQuery = $"SELECT DISTINCT message_type FROM {GetTableName("wolverine_dead_letters")} WHERE message_type IS NOT NULL ORDER BY message_type";
        await using (var cmd = new NpgsqlCommand(messageTypesQuery, connection))
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                availableMessageTypes.Add(reader.GetString(0));
            }
        }

        var exceptionTypesQuery = $"SELECT DISTINCT exception_type FROM {GetTableName("wolverine_dead_letters")} WHERE exception_type IS NOT NULL ORDER BY exception_type";
        await using (var cmd = new NpgsqlCommand(exceptionTypesQuery, connection))
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                availableExceptionTypes.Add(reader.GetString(0));
            }
        }

        return new DeadLetterResult
        {
            Data = deadLetters,
            TotalCount = totalCount,
            AvailableMessageTypes = availableMessageTypes,
            AvailableExceptionTypes = availableExceptionTypes
        };
    }

    public async Task<WolverineDeadLetter?> GetDeadLetterByIdAsync(Guid id, string receivedAt)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = $@"
            SELECT id, execution_time, body, message_type, received_at, 
                   source, exception_type, exception_message, sent_at, 
                   replayable,
                   substring(
                       encode(body, 'escape')
                       FROM position('{{' IN encode(body, 'escape'))
                   ) AS json_body
            FROM {GetTableName("wolverine_dead_letters")}
            WHERE id = @id AND received_at = @receivedAt";

        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("receivedAt", receivedAt);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new WolverineDeadLetter
            {
                Id = reader.GetGuid(0),
                ExecutionTime = reader.IsDBNull(1) ? null : reader.GetDateTime(1),
                Body = (byte[])reader.GetValue(2),
                MessageType = reader.GetString(3),
                ReceivedAt = reader.GetString(4),
                Source = reader.IsDBNull(5) ? null : reader.GetString(5),
                ExceptionType = reader.IsDBNull(6) ? null : reader.GetString(6),
                ExceptionMessage = reader.IsDBNull(7) ? null : reader.GetString(7),
                SentAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                Replayable = reader.IsDBNull(9) ? null : reader.GetBoolean(9),
                JsonBody = reader.IsDBNull(10) ? null : reader.GetString(10)
            };
        }

        return null;
    }

    public async Task<int> SetDeadLetterReplayableAsync(Guid id, string receivedAt, bool replayable)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = $@"
            UPDATE {GetTableName("wolverine_dead_letters")} 
            SET replayable = @replayable 
            WHERE id = @id AND received_at = @receivedAt";

        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("receivedAt", receivedAt);
        command.Parameters.AddWithValue("replayable", replayable);

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<int> SetMultipleDeadLettersReplayableAsync(List<(Guid id, string receivedAt)> deadLetters, bool replayable)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var totalUpdated = 0;
            var query = $@"
                UPDATE {GetTableName("wolverine_dead_letters")} 
                SET replayable = @replayable 
                WHERE id = @id AND received_at = @receivedAt";

            foreach (var (id, receivedAt) in deadLetters)
            {
                await using var command = new NpgsqlCommand(query, connection, transaction);
                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("receivedAt", receivedAt);
                command.Parameters.AddWithValue("replayable", replayable);

                totalUpdated += await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return totalUpdated;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<int> DeleteDeadLetterAsync(Guid id, string receivedAt)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = $"DELETE FROM {GetTableName("wolverine_dead_letters")} WHERE id = @id AND received_at = @receivedAt";

        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("receivedAt", receivedAt);

        return await command.ExecuteNonQueryAsync();
    }

    #endregion

    #region Incoming Envelopes CRUD

    public async Task<IncomingEnvelopeResult> GetAllIncomingEnvelopesAsync(
        string? messageType = null,
        string? status = null,
        string? bodySearch = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 10)
    {
        var envelopes = new List<WolverineIncomingEnvelope>();
        var availableMessageTypes = new List<string>();
        var availableStatuses = new List<string>();
        
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Build WHERE clause dynamically
        var whereConditions = new List<string>();
        var parameters = new List<NpgsqlParameter>();
        var paramIndex = 0;

        if (!string.IsNullOrEmpty(messageType))
        {
            whereConditions.Add($"message_type = @p{paramIndex}");
            parameters.Add(new NpgsqlParameter($"p{paramIndex}", messageType));
            paramIndex++;
        }

        if (!string.IsNullOrEmpty(status))
        {
            whereConditions.Add($"status = @p{paramIndex}");
            parameters.Add(new NpgsqlParameter($"p{paramIndex}", status));
            paramIndex++;
        }

        if (!string.IsNullOrEmpty(bodySearch))
        {
            whereConditions.Add($"encode(body, 'escape') ILIKE @p{paramIndex}");
            parameters.Add(new NpgsqlParameter($"p{paramIndex}", $"%{bodySearch}%"));
            paramIndex++;
        }

        if (startDate.HasValue)
        {
            whereConditions.Add($"execution_time >= @p{paramIndex}");
            parameters.Add(new NpgsqlParameter($"p{paramIndex}", startDate.Value));
            paramIndex++;
        }

        if (endDate.HasValue)
        {
            whereConditions.Add($"execution_time <= @p{paramIndex}");
            parameters.Add(new NpgsqlParameter($"p{paramIndex}", endDate.Value));
            paramIndex++;
        }

        var whereClause = whereConditions.Count > 0 ? "WHERE " + string.Join(" AND ", whereConditions) : "";

        // Get total count
        var countQuery = $"SELECT COUNT(*) FROM {GetTableName("wolverine_incoming_envelopes")} {whereClause}";
        int totalCount;
        await using (var countCommand = new NpgsqlCommand(countQuery, connection))
        {
            foreach (var param in parameters)
            {
                countCommand.Parameters.Add(new NpgsqlParameter(param.ParameterName, param.Value));
            }
            totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
        }

        // Get paginated data
        var offset = (page - 1) * pageSize;
        var query = $@"
            SELECT id, status, owner_id, execution_time, attempts, 
                   body, message_type, received_at, keep_until
            FROM {GetTableName("wolverine_incoming_envelopes")}
            {whereClause}
            ORDER BY execution_time DESC NULLS LAST
            LIMIT @limit OFFSET @offset";

        await using (var command = new NpgsqlCommand(query, connection))
        {
            foreach (var param in parameters)
            {
                command.Parameters.Add(new NpgsqlParameter(param.ParameterName, param.Value));
            }
            command.Parameters.AddWithValue("limit", pageSize);
            command.Parameters.AddWithValue("offset", offset);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                envelopes.Add(new WolverineIncomingEnvelope
                {
                    Id = reader.GetGuid(0),
                    Status = reader.GetString(1),
                    OwnerId = reader.GetInt32(2),
                    ExecutionTime = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                    Attempts = reader.GetInt32(4),
                    Body = (byte[])reader.GetValue(5),
                    MessageType = reader.GetString(6),
                    ReceivedAt = reader.GetString(7),
                    KeepUntil = reader.IsDBNull(8) ? null : reader.GetDateTime(8)
                });
            }
        }

        // Get available filter options
        var messageTypesQuery = $"SELECT DISTINCT message_type FROM {GetTableName("wolverine_incoming_envelopes")} WHERE message_type IS NOT NULL ORDER BY message_type";
        await using (var cmd = new NpgsqlCommand(messageTypesQuery, connection))
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                availableMessageTypes.Add(reader.GetString(0));
            }
        }

        var statusesQuery = $"SELECT DISTINCT status FROM {GetTableName("wolverine_incoming_envelopes")} WHERE status IS NOT NULL ORDER BY status";
        await using (var cmd = new NpgsqlCommand(statusesQuery, connection))
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                availableStatuses.Add(reader.GetString(0));
            }
        }

        return new IncomingEnvelopeResult
        {
            Data = envelopes,
            TotalCount = totalCount,
            AvailableMessageTypes = availableMessageTypes,
            AvailableStatuses = availableStatuses
        };
    }

    public async Task<WolverineIncomingEnvelope?> GetIncomingEnvelopeByIdAsync(Guid id, string receivedAt)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = $@"
            SELECT id, status, owner_id, execution_time, attempts, 
                   body, message_type, received_at, keep_until
            FROM {GetTableName("wolverine_incoming_envelopes")}
            WHERE id = @id AND received_at = @receivedAt";

        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("receivedAt", receivedAt);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new WolverineIncomingEnvelope
            {
                Id = reader.GetGuid(0),
                Status = reader.GetString(1),
                OwnerId = reader.GetInt32(2),
                ExecutionTime = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                Attempts = reader.GetInt32(4),
                Body = (byte[])reader.GetValue(5),
                MessageType = reader.GetString(6),
                ReceivedAt = reader.GetString(7),
                KeepUntil = reader.IsDBNull(8) ? null : reader.GetDateTime(8)
            };
        }

        return null;
    }

    public async Task<int> DeleteIncomingEnvelopeAsync(Guid id, string receivedAt)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = $"DELETE FROM {GetTableName("wolverine_incoming_envelopes")} WHERE id = @id AND received_at = @receivedAt";

        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("receivedAt", receivedAt);

        return await command.ExecuteNonQueryAsync();
    }

    #endregion

    #region Nodes CRUD

    public async Task<List<WolverineNode>> GetAllNodesAsync()
    {
        var nodes = new List<WolverineNode>();
        
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = $@"
            SELECT id, node_number, description, uri, started, 
                   health_check, capabilities
            FROM {GetTableName("wolverine_nodes")}
            ORDER BY node_number";

        await using var command = new NpgsqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            nodes.Add(new WolverineNode
            {
                Id = reader.GetGuid(0),
                NodeNumber = reader.GetInt32(1),
                Description = reader.GetString(2),
                Uri = reader.GetString(3),
                Started = reader.GetDateTime(4),
                HealthCheck = reader.GetDateTime(5),
                Capabilities = reader.IsDBNull(6) ? null : (string[])reader.GetValue(6)
            });
        }

        return nodes;
    }

    public async Task<WolverineNode?> GetNodeByIdAsync(Guid id)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = $@"
            SELECT id, node_number, description, uri, started, 
                   health_check, capabilities
            FROM {GetTableName("wolverine_nodes")}
            WHERE id = @id";

        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new WolverineNode
            {
                Id = reader.GetGuid(0),
                NodeNumber = reader.GetInt32(1),
                Description = reader.GetString(2),
                Uri = reader.GetString(3),
                Started = reader.GetDateTime(4),
                HealthCheck = reader.GetDateTime(5),
                Capabilities = reader.IsDBNull(6) ? null : (string[])reader.GetValue(6)
            };
        }

        return null;
    }

    public async Task<int> DeleteNodeAsync(Guid id)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = $"DELETE FROM {GetTableName("wolverine_nodes")} WHERE id = @id";

        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", id);

        return await command.ExecuteNonQueryAsync();
    }

    #endregion

    #region Node Assignments CRUD

    public async Task<List<WolverineNodeAssignment>> GetAllNodeAssignmentsAsync()
    {
        var assignments = new List<WolverineNodeAssignment>();
        
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = $@"
            SELECT id, node_id, started
            FROM {GetTableName("wolverine_node_assignments")}
            ORDER BY started DESC";

        await using var command = new NpgsqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            assignments.Add(new WolverineNodeAssignment
            {
                Id = reader.GetString(0),
                NodeId = reader.IsDBNull(1) ? null : reader.GetGuid(1),
                Started = reader.GetDateTime(2)
            });
        }

        return assignments;
    }

    public async Task<WolverineNodeAssignment?> GetNodeAssignmentByIdAsync(string id)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = $@"
            SELECT id, node_id, started
            FROM {GetTableName("wolverine_node_assignments")}
            WHERE id = @id";

        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new WolverineNodeAssignment
            {
                Id = reader.GetString(0),
                NodeId = reader.IsDBNull(1) ? null : reader.GetGuid(1),
                Started = reader.GetDateTime(2)
            };
        }

        return null;
    }

    public async Task<int> DeleteNodeAssignmentAsync(string id)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = $"DELETE FROM {GetTableName("wolverine_node_assignments")} WHERE id = @id";

        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", id);

        return await command.ExecuteNonQueryAsync();
    }

    #endregion

    #region Statistics

    public async Task<object> GetDashboardStatsAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var deadLettersCount = 0;
        var incomingEnvelopesCount = 0;
        var activeNodesCount = 0;
        var replayableDeadLettersCount = 0;

        // Count dead letters
        var query1 = $"SELECT COUNT(*) FROM {GetTableName("wolverine_dead_letters")}";
        await using (var cmd = new NpgsqlCommand(query1, connection))
        {
            deadLettersCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        // Count replayable dead letters
        var query2 = $"SELECT COUNT(*) FROM {GetTableName("wolverine_dead_letters")} WHERE replayable = true";
        await using (var cmd = new NpgsqlCommand(query2, connection))
        {
            replayableDeadLettersCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        // Count incoming envelopes
        var query3 = $"SELECT COUNT(*) FROM {GetTableName("wolverine_incoming_envelopes")}";
        await using (var cmd = new NpgsqlCommand(query3, connection))
        {
            incomingEnvelopesCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        // Count active nodes (health check within last 5 minutes)
        var query4 = $"SELECT COUNT(*) FROM {GetTableName("wolverine_nodes")} WHERE health_check > @threshold";
        await using (var cmd = new NpgsqlCommand(query4, connection))
        {
            cmd.Parameters.AddWithValue("threshold", DateTime.UtcNow.AddMinutes(-5));
            activeNodesCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        return new
        {
            totalDeadLetters = deadLettersCount,
            replayableDeadLetters = replayableDeadLettersCount,
            totalIncomingEnvelopes = incomingEnvelopesCount,
            activeNodes = activeNodesCount,
            timestamp = DateTime.UtcNow
        };
    }

    #endregion
}
