using System;
using System.Collections.Generic;

namespace AntigravityTokens.Models
{
    public class ConversationSummary
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Workspace { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public long Mtime { get; set; }
        public string DbPath { get; set; } = string.Empty;
        public int TotalCalls { get; set; }
        public long TotalPromptTokens { get; set; }
        public long TotalOutputTokens { get; set; }
        public long TotalThinkingTokens { get; set; }
        public long TotalCandidateTokens { get; set; }
        public long TotalTokens => TotalPromptTokens + TotalOutputTokens;
        public long TotalLatencyMs { get; set; }
        public double TotalCost { get; set; }
        public List<string> Models { get; set; } = new();
        public List<TokenCallItem> Calls { get; set; } = new();
    }
}
