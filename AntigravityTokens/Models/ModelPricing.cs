using CommunityToolkit.Mvvm.ComponentModel;

namespace AntigravityTokens.Models
{
    public partial class ModelPricing : ObservableObject
    {
        [ObservableProperty]
        public partial string ModelId { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string DisplayName { get; set; } = string.Empty;

        [ObservableProperty]
        public partial double PromptPricePerMillion { get; set; }

        [ObservableProperty]
        public partial double OutputPricePerMillion { get; set; }

        public string FormattedPromptPrice => $"${PromptPricePerMillion:F2} / 1M";
        public string FormattedOutputPrice => $"${OutputPricePerMillion:F2} / 1M";
    }

    public class PricingSettings
    {
        public string Currency { get; set; } = "USD"; // USD or CNY
        public double UsdToCnyRate { get; set; } = 7.25;
    }
}
