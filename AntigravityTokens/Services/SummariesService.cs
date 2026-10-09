using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AntigravityTokens.Services
{
    public class SummariesService
    {
        private static readonly Regex UuidRegex = new(@"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex WorkspaceRegex = new(@"file:///([^\x00-\x1f\x7f-\x9f]+)", RegexOptions.Compiled);

        public Dictionary<string, (string Title, string Workspace)> ParseSummaries(string summariesProtoPath)
        {
            var result = new Dictionary<string, (string Title, string Workspace)>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(summariesProtoPath))
                return result;

            try
            {
                byte[] data = File.ReadAllBytes(summariesProtoPath);
                string text = Encoding.Latin1.GetString(data);

                var matches = UuidRegex.Matches(text);
                for (int i = 0; i < matches.Count; i++)
                {
                    var match = matches[i];
                    string uuid = match.Value;
                    int start = match.Index + match.Length;
                    int nextStart = (i + 1 < matches.Count) ? matches[i + 1].Index : Math.Min(text.Length, start + 800);
                    int len = Math.Max(0, nextStart - start);

                    if (len <= 0) continue;

                    string chunk = text.Substring(start, len);
                    string ws = string.Empty;

                    var wsMatch = WorkspaceRegex.Match(chunk);
                    if (wsMatch.Success)
                    {
                        ws = Uri.UnescapeDataString(wsMatch.Groups[1].Value);
                    }

                    // Extract Title: Look for UTF-8 readable text segments
                    string title = string.Empty;
                    byte[] chunkBytes = Encoding.Latin1.GetBytes(chunk);
                    // Search for length-prefixed strings
                    for (int bi = 0; bi < chunkBytes.Length - 4; bi++)
                    {
                        byte b = chunkBytes[bi];
                        if (b == 0x12 || b == 0x0A) // proto tag
                        {
                            int strLen = chunkBytes[bi + 1];
                            if (strLen >= 3 && strLen <= 100 && bi + 2 + strLen <= chunkBytes.Length)
                            {
                                try
                                {
                                    string s = Encoding.UTF8.GetString(chunkBytes, bi + 2, strLen).Trim();
                                    if (!s.StartsWith("file://", StringComparison.OrdinalIgnoreCase) &&
                                        !UuidRegex.IsMatch(s) &&
                                        s.Length >= 2)
                                    {
                                        title = s;
                                        break;
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }

                    if (!result.ContainsKey(uuid) || (!string.IsNullOrEmpty(title) && string.IsNullOrEmpty(result[uuid].Title)))
                    {
                        result[uuid] = (title, ws);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SummariesService] Parse error: {ex.Message}");
            }

            return result;
        }

        public (string Title, DateTime? CreatedAt) ParseTranscriptStep0(string transcriptPath)
        {
            if (!File.Exists(transcriptPath))
                return (string.Empty, null);

            try
            {
                using var fs = new FileStream(transcriptPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs, Encoding.UTF8);

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;

                    int stepIndex = root.TryGetProperty("step_index", out var sIdx) ? sIdx.GetInt32() : -1;
                    DateTime? createdAt = null;
                    if (root.TryGetProperty("created_at", out var catProp))
                    {
                        if (DateTime.TryParse(catProp.GetString(), out var dt))
                            createdAt = dt;
                    }

                    if (stepIndex == 0)
                    {
                        string content = root.TryGetProperty("content", out var cProp) ? cProp.GetString() ?? "" : "";
                        var reqMatch = Regex.Match(content, @"<USER_REQUEST>(.*?)</USER_REQUEST>", RegexOptions.Singleline);
                        string rawText = reqMatch.Success ? reqMatch.Groups[1].Value.Trim() : content.Trim();
                        string firstLine = string.Empty;
                        using var sr = new StringReader(rawText);
                        string? l;
                        while ((l = sr.ReadLine()) != null)
                        {
                            if (!string.IsNullOrWhiteSpace(l))
                            {
                                firstLine = l.Trim();
                                break;
                            }
                        }

                        string title = firstLine.Length > 50 ? firstLine.Substring(0, 50) + "..." : firstLine;
                        return (title, createdAt);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SummariesService] Transcript parse error: {ex.Message}");
            }

            return (string.Empty, null);
        }
    }
}
