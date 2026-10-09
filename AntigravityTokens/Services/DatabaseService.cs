using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using AntigravityTokens.Models;
using Microsoft.Data.Sqlite;

namespace AntigravityTokens.Services
{
    public class DatabaseService
    {
        private readonly PricingService _pricingService;

        public DatabaseService(PricingService pricingService)
        {
            _pricingService = pricingService;
        }

        public (List<TokenCallItem> Calls, string Workspace) ReadCallsFromDatabase(
            string dbPath, string convId, string title, string workspace, DateTime defaultTime)
        {
            var calls = new List<TokenCallItem>();

            if (!File.Exists(dbPath))
                return (calls, workspace);

            string connStr = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadOnly,
                DefaultTimeout = 3
            }.ToString();

            try
            {
                using var conn = new SqliteConnection(connStr);
                conn.Open();

                // 1. Resolve workspace from trajectory_metadata_blob if not already known
                if (string.IsNullOrEmpty(workspace))
                {
                    string? wsFromDb = ReadWorkspaceFromDatabase(conn);
                    if (!string.IsNullOrEmpty(wsFromDb))
                    {
                        workspace = wsFromDb;
                    }
                }

                // 2. Read exact step timestamps from steps table
                var stepTimestamps = ReadStepTimestamps(conn);

                // 3. Verify gen_metadata table exists
                using (var checkCmd = conn.CreateCommand())
                {
                    checkCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='gen_metadata';";
                    var tableName = checkCmd.ExecuteScalar();
                    if (tableName == null)
                        return (calls, workspace);
                }

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT idx, data FROM gen_metadata ORDER BY idx ASC;";
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    int idx = reader.GetInt32(0);
                    if (reader.IsDBNull(1))
                        continue;

                    long length = reader.GetBytes(1, 0, null, 0, 0);
                    byte[] buffer = new byte[length];
                    reader.GetBytes(1, 0, buffer, 0, (int)length);

                    var topFields = ProtobufReader.Parse(buffer);
                    byte[]? f1Data = null;
                    foreach (var f in topFields)
                    {
                        if (f.FieldNumber == 1 && f.Data.Length > 0)
                        {
                            f1Data = f.Data;
                            break;
                        }
                    }

                    if (f1Data == null)
                        continue;

                    var sub1Fields = ProtobufReader.Parse(f1Data);
                    byte[]? f4Data = null;
                    string modelName = "unknown";
                    int? lastStepIndex = null;

                    foreach (var f in sub1Fields)
                    {
                        if (f.FieldNumber == 4 && f.Data.Length > 0)
                        {
                            f4Data = f.Data;
                        }
                        else if (f.FieldNumber == 19 && f.Data.Length > 0)
                        {
                            modelName = Encoding.UTF8.GetString(f.Data);
                        }
                        else if (f.FieldNumber == 20 && f.Data.Length > 0)
                        {
                            // kv pairs
                            var kvFields = ProtobufReader.Parse(f.Data);
                            for (int kvi = 0; kvi < kvFields.Count - 1; kvi += 2)
                            {
                                string k = Encoding.UTF8.GetString(kvFields[kvi].Data);
                                string v = Encoding.UTF8.GetString(kvFields[kvi + 1].Data);
                                if (k == "last_step_index" && int.TryParse(v, out int sidx))
                                {
                                    lastStepIndex = sidx;
                                }
                            }
                        }
                    }

                    if (f4Data != null)
                    {
                        var sub4Fields = ProtobufReader.Parse(f4Data);
                        var varints = new Dictionary<int, ulong>();
                        foreach (var f in sub4Fields)
                        {
                            if (f.WireType == WireType.Varint)
                            {
                                varints[f.FieldNumber] = f.Varint;
                            }
                        }

                        long promptTokens = varints.TryGetValue(5, out ulong pVal) ? (long)pVal : 0;
                        long totalOut = varints.TryGetValue(3, out ulong oVal) ? (long)oVal : 0;
                        long thinkingTokens = varints.TryGetValue(9, out ulong thVal) ? (long)thVal : 0;
                        long candidateTokens = varints.TryGetValue(10, out ulong cVal) ? (long)cVal : (totalOut - thinkingTokens);
                        long latencyMs = varints.TryGetValue(2, out ulong latVal) ? (long)latVal : 0;

                        double cost = _pricingService.CalculateCost(promptTokens, totalOut, modelName);

                        string wsShort = "默认工作区";
                        if (!string.IsNullOrWhiteSpace(workspace))
                        {
                            string cleanWs = workspace.TrimEnd('/', '\\');
                            wsShort = Path.GetFileName(cleanWs);
                            if (string.IsNullOrEmpty(wsShort))
                                wsShort = cleanWs;
                        }

                        // Determine timestamp: step timestamp > idx timestamp > defaultTime
                        DateTime callTime = defaultTime;
                        if (lastStepIndex.HasValue && stepTimestamps.TryGetValue(lastStepIndex.Value, out var stTime))
                        {
                            callTime = stTime;
                        }
                        else if (stepTimestamps.TryGetValue(idx, out var idxTime))
                        {
                            callTime = idxTime;
                        }

                        calls.Add(new TokenCallItem
                        {
                            Id = $"{convId}_{idx}",
                            ConversationId = convId,
                            Title = title,
                            Workspace = wsShort,
                            WorkspaceFull = workspace,
                            Timestamp = callTime,
                            Model = modelName,
                            PromptTokens = promptTokens,
                            OutputTokens = totalOut,
                            ThinkingTokens = thinkingTokens,
                            CandidateTokens = candidateTokens,
                            LatencyMs = latencyMs,
                            Cost = Math.Round(cost, 5),
                            Currency = _pricingService.Settings.Currency,
                            StepIndex = lastStepIndex
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DatabaseService] Error reading {dbPath}: {ex.Message}");
            }

            return (calls, workspace);
        }

        private string? ReadWorkspaceFromDatabase(SqliteConnection conn)
        {
            try
            {
                using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='trajectory_metadata_blob';";
                if (checkCmd.ExecuteScalar() == null)
                    return null;

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT data FROM trajectory_metadata_blob LIMIT 1;";
                using var reader = cmd.ExecuteReader();
                if (reader.Read() && !reader.IsDBNull(0))
                {
                    long length = reader.GetBytes(0, 0, null, 0, 0);
                    byte[] buffer = new byte[length];
                    reader.GetBytes(0, 0, buffer, 0, (int)length);
                    string raw = Encoding.Latin1.GetString(buffer);
                    var match = Regex.Match(raw, @"file:///([^\x00-\x1f\x7f-\x9f\s""\'\)\*\`]+)");
                    if (match.Success)
                    {
                        return Uri.UnescapeDataString(match.Groups[1].Value);
                    }
                }
            }
            catch
            {
            }
            return null;
        }

        private Dictionary<int, DateTime> ReadStepTimestamps(SqliteConnection conn)
        {
            var timestamps = new Dictionary<int, DateTime>();
            try
            {
                using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='steps';";
                if (checkCmd.ExecuteScalar() == null)
                    return timestamps;

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT idx, metadata FROM steps WHERE metadata IS NOT NULL;";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int sIdx = reader.GetInt32(0);
                    if (!reader.IsDBNull(1))
                    {
                        long length = reader.GetBytes(1, 0, null, 0, 0);
                        byte[] buffer = new byte[length];
                        reader.GetBytes(1, 0, buffer, 0, (int)length);

                        var fields = ProtobufReader.Parse(buffer);
                        foreach (var f in fields)
                        {
                            if (f.FieldNumber == 1 && f.Data.Length > 0)
                            {
                                var subFields = ProtobufReader.Parse(f.Data);
                                foreach (var sf in subFields)
                                {
                                    if (sf.FieldNumber == 1 && sf.WireType == WireType.Varint)
                                    {
                                        long sec = (long)sf.Varint;
                                        if (sec > 1000000000 && sec < 2500000000)
                                        {
                                            timestamps[sIdx] = DateTimeOffset.FromUnixTimeSeconds(sec).LocalDateTime;
                                            break;
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
            }
            return timestamps;
        }
    }
}
