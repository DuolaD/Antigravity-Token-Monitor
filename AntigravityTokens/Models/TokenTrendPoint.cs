using System;

namespace AntigravityTokens.Models
{
    public class TokenTrendPoint
    {
        public DateTime Date { get; set; }
        public string DateLabel => Date.ToString("MM-dd");
        public string FullDateLabel => Date.ToString("yyyy-MM-dd");

        public long TotalTokens => PromptTokens + OutputTokens;
        public long PromptTokens { get; set; }
        public long OutputTokens { get; set; }
        public long ThinkingTokens { get; set; }
        public int Calls { get; set; }
        public double Cost { get; set; }

        public string FormattedTotalTokens => $"{TotalTokens:N0}";
        public string FormattedPromptTokens => $"{PromptTokens:N0}";
        public string FormattedOutputTokens => $"{OutputTokens:N0}";
        public string FormattedThinkingTokens => $"{ThinkingTokens:N0}";
        public string FormattedCalls => $"{Calls:N0}";
        public string FormattedCostUSD => $"${Cost:F4}";
        public string FormattedCostCNY => $"¥{Cost:F4}";
    }
}
