using System;

namespace AntigravityTokens.Models
{
    public class TokenCallItem
    {
        public string Id { get; set; } = string.Empty;
        public string ConversationId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Workspace { get; set; } = string.Empty;
        public string WorkspaceFull { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string FormattedTime => Timestamp == DateTime.MinValue ? "未知" : Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        public string Model { get; set; } = string.Empty;
        public long PromptTokens { get; set; }
        public long OutputTokens { get; set; }
        public long ThinkingTokens { get; set; }
        public long CandidateTokens { get; set; }
        public long TotalTokens => PromptTokens + OutputTokens;

        public string FormattedPromptTokens => $"{PromptTokens:N0}";
        public string FormattedOutputTokens => $"{OutputTokens:N0}";
        public string FormattedThinkingTokens => $"{ThinkingTokens:N0}";
        public string FormattedCandidateTokens => $"{CandidateTokens:N0}";
        public string FormattedTotalTokens => $"{TotalTokens:N0}";

        public long LatencyMs { get; set; }
        public double LatencySeconds => Math.Round(LatencyMs / 1000.0, 2);
        public string FormattedLatency => $"{LatencySeconds:F2}s";

        public double Cost { get; set; }
        public string Currency { get; set; } = "USD";
        public string FormattedCost => Currency == "CNY" ? $"¥{Cost:F4}" : $"${Cost:F4}";
        public int? StepIndex { get; set; }

        public string ModelBadgeColor => Model.ToLowerInvariant() switch
        {
            var m when m.Contains("flash") => "#0EA5E9",      // Sky Blue
            var m when m.Contains("pro") => "#8B5CF6",        // Purple
            var m when m.Contains("claude") => "#D97706",     // Amber
            _ => "#6B7280"                                    // Gray
        };
    }
}
