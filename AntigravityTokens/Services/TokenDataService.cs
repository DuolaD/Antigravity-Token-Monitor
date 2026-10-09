using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntigravityTokens.Models;

namespace AntigravityTokens.Services
{
    public class TokenDataService
    {
        private readonly DatabaseService _databaseService;
        private readonly SummariesService _summariesService;
        private readonly PricingService _pricingService;

        private readonly ConcurrentDictionary<string, ConversationSummary> _conversations = new();
        private readonly ConcurrentDictionary<string, DateTime> _fileMtimes = new();

        public event EventHandler<OverviewStats>? OverviewUpdated;
        public event EventHandler<long>? TokensIncremented;

        private long _lastTotalTokens = 0;

        public List<string> MonitoredDirectories { get; } = new();

        public TokenDataService(DatabaseService databaseService, SummariesService summariesService, PricingService pricingService)
        {
            _databaseService = databaseService;
            _summariesService = summariesService;
            _pricingService = pricingService;

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string agyConv = Path.Combine(home, ".gemini", "antigravity", "conversations");
            string ideConv = Path.Combine(home, ".gemini", "antigravity-ide", "conversations");

            if (Directory.Exists(agyConv)) MonitoredDirectories.Add(agyConv);
            if (Directory.Exists(ideConv)) MonitoredDirectories.Add(ideConv);
        }

        public async Task RefreshAllAsync(bool force = false)
        {
            await Task.Run(() =>
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string summariesPath = Path.Combine(home, ".gemini", "antigravity", "agyhub_summaries_proto.pb");
                var summariesMeta = _summariesService.ParseSummaries(summariesPath);

                bool hasAnyUpdate = false;

                foreach (var dir in MonitoredDirectories)
                {
                    if (!Directory.Exists(dir)) continue;

                    var dbFiles = Directory.GetFiles(dir, "*.db");
                    foreach (var dbPath in dbFiles)
                    {
                        string convId = Path.GetFileNameWithoutExtension(dbPath);
                        DateTime curMtime = File.GetLastWriteTimeUtc(dbPath);

                        if (!force && _conversations.ContainsKey(convId) && _fileMtimes.TryGetValue(dbPath, out var cachedMtime) && cachedMtime == curMtime)
                        {
                            continue;
                        }

                        // Determine title and workspace
                        string title = string.Empty;
                        string workspace = string.Empty;

                        if (summariesMeta.TryGetValue(convId, out var meta))
                        {
                            title = meta.Title;
                            workspace = meta.Workspace;
                        }

                        // Check transcript for title / created time if needed
                        string brainDir = Path.Combine(home, ".gemini", "antigravity", "brain", convId, ".system_generated", "logs", "transcript.jsonl");
                        var (transTitle, transTime) = _summariesService.ParseTranscriptStep0(brainDir);

                        if (string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(transTitle))
                        {
                            title = transTitle;
                        }
                        if (string.IsNullOrEmpty(title))
                        {
                            title = $"对话 {convId.Substring(0, Math.Min(8, convId.Length))}";
                        }

                        DateTime defaultTime = transTime ?? curMtime;

                        var (calls, resolvedWs) = _databaseService.ReadCallsFromDatabase(dbPath, convId, title, workspace, defaultTime);
                        if (!string.IsNullOrEmpty(resolvedWs))
                        {
                            workspace = resolvedWs;
                        }

                        long totalPrompt = calls.Sum(c => c.PromptTokens);
                        long totalOut = calls.Sum(c => c.OutputTokens);
                        long totalThinking = calls.Sum(c => c.ThinkingTokens);
                        long totalLat = calls.Sum(c => c.LatencyMs);
                        double totalCost = calls.Sum(c => c.Cost);
                        var models = calls.Select(c => c.Model).Distinct().ToList();

                        var summary = new ConversationSummary
                        {
                            Id = convId,
                            Title = title,
                            Workspace = workspace,
                            CreatedAt = defaultTime,
                            UpdatedAt = curMtime,
                            DbPath = dbPath,
                            TotalCalls = calls.Count,
                            TotalPromptTokens = totalPrompt,
                            TotalOutputTokens = totalOut,
                            TotalThinkingTokens = totalThinking,
                            TotalCandidateTokens = totalOut - totalThinking,
                            TotalLatencyMs = totalLat,
                            TotalCost = totalCost,
                            Models = models,
                            Calls = calls
                        };

                        _conversations[convId] = summary;
                        _fileMtimes[dbPath] = curMtime;
                        hasAnyUpdate = true;
                    }
                }

                var stats = GetOverviewStats();

                if (_lastTotalTokens > 0 && stats.TotalTokens > _lastTotalTokens)
                {
                    long diff = stats.TotalTokens - _lastTotalTokens;
                    TokensIncremented?.Invoke(this, diff);
                }
                _lastTotalTokens = stats.TotalTokens;

                if (hasAnyUpdate)
                {
                    OverviewUpdated?.Invoke(this, stats);
                }
            });
        }

        public OverviewStats GetOverviewStats()
        {
            var convs = _conversations.Values.ToList();
            long totalPrompt = 0;
            long totalOutput = 0;
            long totalThinking = 0;
            long totalCandidate = 0;
            int totalCalls = 0;
            double totalCost = 0.0;
            long totalLatency = 0;

            DateTime today = DateTime.Today;
            long todayPrompt = 0;
            long todayOutput = 0;
            long todayThinking = 0;
            int todayCalls = 0;
            double todayCost = 0.0;

            var modelDict = new Dictionary<string, (long Tokens, int Calls, double Cost)>(StringComparer.OrdinalIgnoreCase);
            var wsDict = new Dictionary<string, (long Tokens, int Calls, double Cost)>(StringComparer.OrdinalIgnoreCase);

            foreach (var c in convs)
            {
                string wsShort = "默认工作区";
                if (!string.IsNullOrWhiteSpace(c.Workspace))
                {
                    string clean = c.Workspace.TrimEnd('/', '\\');
                    wsShort = Path.GetFileName(clean);
                    if (string.IsNullOrEmpty(wsShort))
                        wsShort = clean;
                }

                foreach (var call in c.Calls)
                {
                    long p = call.PromptTokens;
                    long o = call.OutputTokens;
                    long th = call.ThinkingTokens;
                    long tot = p + o;
                    double cost = _pricingService.CalculateCost(p, o, call.Model);

                    totalPrompt += p;
                    totalOutput += o;
                    totalThinking += th;
                    totalCandidate += call.CandidateTokens;
                    totalCalls++;
                    totalCost += cost;
                    totalLatency += call.LatencyMs;

                    if (call.Timestamp.Date == today)
                    {
                        todayPrompt += p;
                        todayOutput += o;
                        todayThinking += th;
                        todayCalls++;
                        todayCost += cost;
                    }

                    // Model stats
                    if (!modelDict.ContainsKey(call.Model))
                        modelDict[call.Model] = (0, 0, 0);
                    var (mToks, mCalls, mCost) = modelDict[call.Model];
                    modelDict[call.Model] = (mToks + tot, mCalls + 1, mCost + cost);

                    // Workspace stats
                    if (!wsDict.ContainsKey(wsShort))
                        wsDict[wsShort] = (0, 0, 0);
                    var (wToks, wCalls, wCost) = wsDict[wsShort];
                    wsDict[wsShort] = (wToks + tot, wCalls + 1, wCost + cost);
                }
            }

            long grandTotal = totalPrompt + totalOutput;
            double avgLat = totalCalls > 0 ? (totalLatency / (double)totalCalls / 1000.0) : 0.0;

            var topModels = modelDict.OrderByDescending(x => x.Value.Tokens)
                .Select(x => new ModelUsageStat
                {
                    Model = x.Key,
                    Tokens = x.Value.Tokens,
                    Calls = x.Value.Calls,
                    Cost = Math.Round(x.Value.Cost, 4),
                    Percentage = grandTotal > 0 ? ((double)x.Value.Tokens / grandTotal * 100.0) : 0.0
                }).ToList();

            var topWorkspaces = wsDict.OrderByDescending(x => x.Value.Tokens)
                .Select(x => new WorkspaceUsageStat
                {
                    Workspace = x.Key,
                    Tokens = x.Value.Tokens,
                    Calls = x.Value.Calls,
                    Cost = Math.Round(x.Value.Cost, 4),
                    Percentage = grandTotal > 0 ? ((double)x.Value.Tokens / grandTotal * 100.0) : 0.0
                }).ToList();

            return new OverviewStats
            {
                TotalTokens = grandTotal,
                TotalPromptTokens = totalPrompt,
                TotalOutputTokens = totalOutput,
                TotalThinkingTokens = totalThinking,
                TotalCandidateTokens = totalCandidate,
                TotalCalls = totalCalls,
                TotalConversations = convs.Count,
                TotalCost = Math.Round(totalCost, 4),
                AvgLatencySeconds = Math.Round(avgLat, 2),
                Currency = _pricingService.Settings.Currency,

                TodayTokens = todayPrompt + todayOutput,
                TodayPromptTokens = todayPrompt,
                TodayOutputTokens = todayOutput,
                TodayThinkingTokens = todayThinking,
                TodayCalls = todayCalls,
                TodayCost = Math.Round(todayCost, 4),

                TopModels = topModels,
                TopWorkspaces = topWorkspaces
            };
        }

        public List<TokenTrendPoint> GetTrendData(DateTime startDate, DateTime endDate)
        {
            startDate = startDate.Date;
            endDate = endDate.Date;
            if (startDate > endDate)
            {
                var tmp = startDate;
                startDate = endDate;
                endDate = tmp;
            }

            var dict = new Dictionary<DateTime, TokenTrendPoint>();
            DateTime curr = startDate;
            while (curr <= endDate)
            {
                dict[curr] = new TokenTrendPoint { Date = curr };
                curr = curr.AddDays(1);
            }

            foreach (var c in _conversations.Values)
            {
                foreach (var call in c.Calls)
                {
                    var callDate = call.Timestamp.Date;
                    if (callDate >= startDate && callDate <= endDate && dict.TryGetValue(callDate, out var point))
                    {
                        point.PromptTokens += call.PromptTokens;
                        point.OutputTokens += call.OutputTokens;
                        point.ThinkingTokens += call.ThinkingTokens;
                        point.Calls++;
                        point.Cost += _pricingService.CalculateCost(call.PromptTokens, call.OutputTokens, call.Model);
                    }
                }
            }

            return dict.Values.OrderBy(p => p.Date).ToList();
        }

        public (DateTime Earliest, DateTime Latest) GetDateRange()
        {
            DateTime min = DateTime.MaxValue;
            DateTime max = DateTime.MinValue;

            foreach (var c in _conversations.Values)
            {
                foreach (var call in c.Calls)
                {
                    if (call.Timestamp != DateTime.MinValue)
                    {
                        if (call.Timestamp < min) min = call.Timestamp;
                        if (call.Timestamp > max) max = call.Timestamp;
                    }
                }
            }

            if (min == DateTime.MaxValue) min = DateTime.Today.AddDays(-30);
            if (max == DateTime.MinValue) max = DateTime.Today;

            return (min.Date, max.Date);
        }

        public (List<TokenCallItem> Items, int TotalCount, int TotalPages) GetPagedLogs(
            int page, int pageSize, string? modelFilter, string? wsFilter, string? searchQuery, string? dateRange)
        {
            var query = _conversations.Values.SelectMany(c => c.Calls);

            DateTime now = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(dateRange) && dateRange != "全部")
            {
                if (dateRange == "今天")
                    query = query.Where(c => c.Timestamp.Date == DateTime.Today);
                else if (dateRange == "近7天")
                    query = query.Where(c => (now - c.Timestamp).TotalDays <= 7);
                else if (dateRange == "近30天")
                    query = query.Where(c => (now - c.Timestamp).TotalDays <= 30);
            }

            if (!string.IsNullOrEmpty(modelFilter) && modelFilter != "全部模型")
            {
                query = query.Where(c => string.Equals(c.Model, modelFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(wsFilter) && wsFilter != "全部工作区")
            {
                query = query.Where(c => string.Equals(c.Workspace, wsFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(searchQuery))
            {
                string q = searchQuery.Trim().ToLowerInvariant();
                query = query.Where(c =>
                    c.Title.ToLowerInvariant().Contains(q) ||
                    c.Model.ToLowerInvariant().Contains(q) ||
                    c.Workspace.ToLowerInvariant().Contains(q) ||
                    c.ConversationId.ToLowerInvariant().Contains(q));
            }

            var sorted = query.OrderByDescending(c => c.Timestamp).ToList();
            int totalCount = sorted.Count;
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;

            int skip = Math.Max(0, (page - 1) * pageSize);
            var pageItems = sorted.Skip(skip).Take(pageSize).ToList();

            // Refresh costs with current currency
            foreach (var item in pageItems)
            {
                item.Cost = Math.Round(_pricingService.CalculateCost(item.PromptTokens, item.OutputTokens, item.Model), 5);
                item.Currency = _pricingService.Settings.Currency;
            }

            return (pageItems, totalCount, totalPages);
        }

        public List<DailyTokenStat> GetDailyStats()
        {
            var dailyDict = new Dictionary<string, DailyTokenStat>();

            foreach (var c in _conversations.Values)
            {
                foreach (var call in c.Calls)
                {
                    string dateKey = call.Timestamp != DateTime.MinValue ? call.Timestamp.ToString("yyyy-MM-dd") : "未知日期";
                    if (!dailyDict.ContainsKey(dateKey))
                    {
                        dailyDict[dateKey] = new DailyTokenStat { Date = dateKey };
                    }
                    var s = dailyDict[dateKey];
                    s.PromptTokens += call.PromptTokens;
                    s.OutputTokens += call.OutputTokens;
                    s.ThinkingTokens += call.ThinkingTokens;
                    s.Calls++;
                    s.Cost += _pricingService.CalculateCost(call.PromptTokens, call.OutputTokens, call.Model);
                }
            }

            return dailyDict.Values.OrderByDescending(x => x.Date).ToList();
        }

        public List<string> GetAllDistinctModels()
        {
            return _conversations.Values
                .SelectMany(c => c.Models)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(m => m)
                .ToList();
        }

        public List<string> GetAllDistinctWorkspaces()
        {
            return _conversations.Values
                .Select(c => {
                    if (string.IsNullOrWhiteSpace(c.Workspace)) return "默认工作区";
                    string clean = c.Workspace.TrimEnd('/', '\\');
                    string shortName = Path.GetFileName(clean);
                    return string.IsNullOrEmpty(shortName) ? clean : shortName;
                })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(w => w)
                .ToList();
        }
    }
}
