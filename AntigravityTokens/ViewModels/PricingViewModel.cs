using System.Collections.ObjectModel;
using AntigravityTokens.Models;
using AntigravityTokens.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AntigravityTokens.ViewModels
{
    public partial class PricingViewModel : ObservableObject
    {
        private readonly PricingService _pricingService;
        private readonly TokenDataService _tokenDataService;

        [ObservableProperty]
        public partial string Currency { get; set; } = "USD";

        [ObservableProperty]
        public partial double UsdToCnyRate { get; set; } = 7.25;

        [ObservableProperty]
        public partial string StatusMessage { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool HasStatusMessage { get; set; } = false;

        public ObservableCollection<string> CurrencyOptions { get; } = new() { "USD", "CNY" };
        public ObservableCollection<ModelPricing> PricingItems { get; } = new();

        public PricingViewModel(PricingService pricingService, TokenDataService tokenDataService)
        {
            _pricingService = pricingService;
            _tokenDataService = tokenDataService;
            LoadPricing();
        }

        public void LoadPricing()
        {
            PricingItems.Clear();
            foreach (var item in _pricingService.GetAllPricing())
            {
                PricingItems.Add(item);
            }
            Currency = _pricingService.Settings.Currency;
            UsdToCnyRate = _pricingService.Settings.UsdToCnyRate;
            HasStatusMessage = false;
        }

        [RelayCommand]
        public void SavePricing()
        {
            _pricingService.Settings.Currency = Currency;
            _pricingService.Settings.UsdToCnyRate = UsdToCnyRate;
            _pricingService.SaveToFile();
            StatusMessage = "费率与币种设置已保存并生效！";
            HasStatusMessage = true;

            // Trigger recalculation in TokenDataService
            _tokenDataService.GetOverviewStats();
        }

        [RelayCommand]
        public void ResetDefaults()
        {
            _pricingService.ResetDefaults();
            LoadPricing();
            StatusMessage = "费率已重置为系统默认价格。";
            HasStatusMessage = true;
        }
    }
}
