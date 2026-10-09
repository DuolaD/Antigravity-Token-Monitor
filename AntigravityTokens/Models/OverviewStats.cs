using System.Collections.Generic;

namespace AntigravityTokens.Models
{
    public class OverviewStats
    {
        public long TotalTokens { get; set; }
        public long TotalPromptTokens { get; set; }
        public long TotalOutputTokens { get; set; }
        public long TotalThinkingTokens { get; set; }
        public long TotalCandidateTokens { get; set; }
        public int TotalCalls { get; set; }
        public int TotalConversations { get; set; }
        public double TotalCost { get; set; }
        public double AvgLatencySeconds { get; set; }
        public string Currency { get; set; } = "USD";

        // Today metrics
        public long TodayTokens { get; set; }
        public long TodayPromptTokens { get; set; }
        public long TodayOutputTokens { get; set; }
        public long TodayThinkingTokens { get; set; }
        public int TodayCalls { get; set; }
        public double TodayCost { get; set; }

        public string FormattedTotalCost => Currency == "CNY" ? $"¥{TotalCost:F4}" : $"${TotalCost:F4}";
        public string FormattedTodayCost => Currency == "CNY" ? $"¥{TodayCost:F4}" : $"${TodayCost:F4}";

        public double ThinkingRatio => TotalOutputTokens > 0 ? (double)TotalThinkingTokens / TotalOutputTokens * 100.0 : 0.0;
        public string FormattedThinkingRatio => $"{ThinkingRatio:F1}%";

        public List<ModelUsageStat> TopModels { get; set; } = new();
        public List<WorkspaceUsageStat> TopWorkspaces { get; set; } = new();
    }

    public class ModelUsageStat
    {
        public string Model { get; set; } = string.Empty;
        public long Tokens { get; set; }
        public int Calls { get; set; }
        public double Cost { get; set; }
        public double Percentage { get; set; }
        public string FormattedTokens => $"{Tokens:N0}";
        public string FormattedPercentage => $"{Percentage:F1}%";
        public string CallsText => $"{Calls:N0} 次调用";
    }

    public class WorkspaceUsageStat
    {
        public string Workspace { get; set; } = string.Empty;
        public long Tokens { get; set; }
        public int Calls { get; set; }
        public double Cost { get; set; }
        public double Percentage { get; set; }
        public string FormattedTokens => $"{Tokens:N0}";
        public string FormattedPercentage => $"{Percentage:F1}%";
        public string CallsText => $"{Calls:N0} 次";
    }

    public class DailyTokenStat
    {
        public string Date { get; set; } = string.Empty;
        public long PromptTokens { get; set; }
        public long OutputTokens { get; set; }
        public long ThinkingTokens { get; set; }
        public long TotalTokens => PromptTokens + OutputTokens;
        public int Calls { get; set; }
        public double Cost { get; set; }

        public string FormattedPromptTokens => $"{PromptTokens:N0}";
        public string FormattedOutputTokens => $"{OutputTokens:N0}";
        public string FormattedThinkingTokens => $"{ThinkingTokens:N0}";
        public string FormattedTotalTokens => $"{TotalTokens:N0}";
        public string CallsText => $"{Calls:N0} 次";
        public string FormattedCost => $"${Cost:F4}";

        public double ThinkingRatio => OutputTokens > 0 ? (double)ThinkingTokens / OutputTokens * 100.0 : 0.0;
        public string FormattedThinkingRatio => $"{ThinkingRatio:F1}%";
    }
}
