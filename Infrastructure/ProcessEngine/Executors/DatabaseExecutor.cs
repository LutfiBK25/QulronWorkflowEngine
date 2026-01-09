using Domain.ProcessEngine.Entities;
using Infrastructure.ProcessEngine.Execution;
using Infrastructure.ProcessEngine.Parsing;
using Npgsql;
using System;
using System.Text.RegularExpressions;

namespace Infrastructure.ProcessEngine.Executors;

public class DatabaseExecutor
{
    private readonly Dictionary<string, string> _connectionStrings;
    private readonly ModuleCache _moduleCache;
    private readonly FieldParser _fieldParser;
    private readonly ReturnParser _returnParser;

    // Pattern to match CONNECT statement at the beginning
    private static readonly Regex ConnectPattern = new Regex(
        @"^\s*CONNECT\s+([A-Za-z0-9_]+)\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    public DatabaseExecutor(
        Dictionary<string, string> connectionStrings,
        ModuleCache moduleCache)
    {
        _moduleCache = moduleCache;
        _connectionStrings = connectionStrings; 
        _fieldParser = new FieldParser(moduleCache);
        _returnParser = new ReturnParser(moduleCache);
    }

    public async Task<ActionResult> ExecuteAsync(
        DatabaseActionModule dbAction,
        ExecutionSession session)
    {
        try
        {
            
            // Parse return fields
            var returnFields = _returnParser.ParseReturnFields(dbAction.Statement);

            // Remove Return clause and Subsitute fields
            var cleanStatement = _returnParser.RemoveReturnsClause(dbAction.Statement);

            // Check if statement starts with CONNECT
            var connectMatch = ConnectPattern.Match(cleanStatement);
            string sqlToExecute;

            if (connectMatch.Success)
            {
                // Extract database name from CONNECT statement
                var databaseName = connectMatch.Groups[1].Value;

                // Remove CONNECT statement from the SQL
                sqlToExecute = ConnectPattern.Replace(cleanStatement, "").Trim();

                // Close existing connection if switching databases
                if (session.CurrentDatabase != databaseName)
                {
                    await session.CloseConnectionAsync();

                    // Get connection string for the specified database
                    if (!_connectionStrings.TryGetValue(databaseName, out var connectionString))
                    {
                        return ActionResult.Fail($"No connection string configured for database: {databaseName}");
                    }

                    // Create new connection
                    var connection = new NpgsqlConnection(connectionString);
                    await connection.OpenAsync();

                    session.CurrentConnection = connection;
                    session.CurrentDatabase = databaseName;
                }
                
                // If no SQL after CONNECT, just return success
                if (string.IsNullOrWhiteSpace(sqlToExecute))
                {
                    return ActionResult.Pass($"Connected to database: {databaseName}");
                }
            }
            else
            {
                // No CONNECT statement, use existing connection or default
                sqlToExecute = cleanStatement;

                if (session.CurrentConnection == null)
                {
                    // Use default connection (first in dictionary or one named "WMS")
                    string defaultConnectionString = null;

                    if (_connectionStrings.ContainsKey("WMS"))
                    {
                        defaultConnectionString = _connectionStrings["WMS"];
                    }
                    else if (_connectionStrings.Count > 0)
                    {
                        defaultConnectionString = _connectionStrings.Values.First();
                    }

                    if (defaultConnectionString == null)
                    {
                        return ActionResult.Fail("No database connection available");
                    }

                    var connection = new NpgsqlConnection(defaultConnectionString);
                    await connection.OpenAsync();
                    session.CurrentConnection = connection;
                    session.CurrentDatabase = _connectionStrings.FirstOrDefault(x => x.Value == defaultConnectionString).Key ?? "WMS";
                }
            }

            var substitutedStatement = _fieldParser.SubstituteFields(sqlToExecute, session);

            Console.WriteLine(substitutedStatement);

            // Execute SQL using the session's persistent connection
            using var command = session.CurrentConnection.CreateCommand();
            command.CommandText = substitutedStatement;

            await using var reader = await ((NpgsqlCommand)command).ExecuteReaderAsync();


            var result = ActionResult.Pass($"Database execution completed on {session.CurrentDatabase}");

            // Map returned values to fields
            if (reader.Read() && returnFields.Count > 0)
            {
                for (int i = 0; i < Math.Min(reader.FieldCount, returnFields.Count); i++)
                {
                    var fieldId = returnFields[i];
                    var value = reader.GetValue(i);

                    // DEBUG: Log what we're storing
                    //Console.WriteLine($"DEBUG: Storing field {fieldId}");
                    //Console.WriteLine($"  Value: {value}");
                    //Console.WriteLine($"  Type: {value?.GetType().Name ?? "null"}");
                    //Console.WriteLine($"  IsArray: {value?.GetType().IsArray}");

                    if (value == DBNull.Value)
                    {
                        value = null;
                    }

                    session.SetFieldValue(fieldId, value);
                    result.ReturnedFields[fieldId] = value;
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            return ActionResult.Fail($"Database execution failed: {ex.Message}", ex);
        }
    }
}
