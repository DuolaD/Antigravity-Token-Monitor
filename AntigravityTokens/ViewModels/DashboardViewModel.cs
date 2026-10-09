using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AntigravityTokens.Models;
using AntigravityTokens.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;

namespace AntigravityTokens.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly TokenDataService _tokenDataService;
        private readonly PricingService _pricingService;
        private readonly LiveSyncService _liveSyncService;
        private readonly DispatcherQueue _dispatcherQueue;

        [ObservableProperty]
        private double _totalCost;

        [ObservableProperty]
        private double _totalTokens;

        [ObservableProperty]
        private double _totalCalls;

        [ObservableProperty]
        private double _avgLatency;

        [ObservableProperty]
        private double _promptTokens;

        [ObservableProperty]
        private double _outputTokens;

        [ObservableProperty]
        private double _thinkingTokens;

        [ObservableProperty]
        private string _thinkingRatioText = "0.0%";

        [ObservableProperty]
        private double _todayCost;

        [ObservableProperty]
        private double _todayTokens;

        [ObservableProperty]
        private double _todayCalls;

        [ObservableProperty]
        private string _currency = "USD";

        [ObservableProperty]
        private string _currencySymbol = "$";

        [ObservableProperty]
        private string _lastSyncTime = "正在初始化...";

        [ObservableProperty]
        private string _heartbeatStatus = "🟢 实时 1s 心跳同步中";

        [ObservableProperty]
        private bool _isLoading = false;

        // Chart Range Selection
        [ObservableProperty]
        private string _selectedPreset = "月"; // Options: "周", "月", "季度", "半年", "-1年", "全部", "自定义"

        [ObservableProperty]
        private string _selectedPresetText = "近30天";

        [ObservableProperty]
        private DateTimeOffset? _customStartDate = DateTimeOffset.Now.AddDays(-30);

        [ObservableProperty]
        private DateTimeOffset? _customEndDate = DateTimeOffset.Now;

        [ObservableProperty]
        private bool _isCustomDateRangeOpen = false;

        [ObservableProperty]
        private string _rangeTotalTokensText = "0 Tokens";

        [ObservableProperty]
        private string _rangeTotalCostText = "$0.0000";

        [ObservableProperty]
        private string _rangeTotalCallsText = "0 次";

        [ObservableProperty]
        private string _rangeDateRangeText = "";

        [ObservableProperty]
        private ObservableCollection<TokenTrendPoint> _chartPoints = new();
        public ObservableCollection<ModelUsageStat> TopModels { get; } = new();
        public ObservableCollection<WorkspaceUsageStat> TopWorkspaces { get; } = new();

        public DashboardViewModel(TokenDataService tokenDataService, PricingService pricingService, LiveSyncService liveSyncService)
        {
            _tokenDataService = tokenDataService;
            _pricingService = pricingService;
            _liveSyncService = liveSyncService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            _tokenDataService.OverviewUpdated += OnOverviewUpdated;
            _liveSyncService.HeartbeatTicked += OnHeartbeatTicked;

            Currency = _pricingService.Settings.Currency;
            CurrencySymbol = Currency == "CNY" ? "¥" : "$";
        }

        private void OnOverviewUpdated(object? sender, OverviewStats stats)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ApplyStats(stats);
                UpdateChartData();
            });
        }

        private void OnHeartbeatTicked(object? sender, DateTime time)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                LastSyncTime = $"{time:HH:mm:ss} 同步成功";
            });
        }

        public async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                await _tokenDataService.RefreshAllAsync();
                var stats = _tokenDataService.GetOverviewStats();
                ApplyStats(stats);
                UpdateChartData();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyStats(OverviewStats stats)
        {
            TotalCost = stats.TotalCost;
            TotalTokens = stats.TotalTokens;
            TotalCalls = stats.TotalCalls;
            AvgLatency = stats.AvgLatencySeconds;

            PromptTokens = stats.TotalPromptTokens;
            OutputTokens = stats.TotalOutputTokens;
            ThinkingTokens = stats.TotalThinkingTokens;
            ThinkingRatioText = stats.FormattedThinkingRatio;

            TodayCost = stats.TodayCost;
            TodayTokens = stats.TodayTokens;
            TodayCalls = stats.TodayCalls;

            Currency = stats.Currency;
            CurrencySymbol = Currency == "CNY" ? "¥" : "$";

            TopModels.Clear();
            foreach (var m in stats.TopModels)
                TopModels.Add(m);

            TopWorkspaces.Clear();
            foreach (var w in stats.TopWorkspaces)
                TopWorkspaces.Add(w);
        }

        public void UpdateChartData()
        {
            DateTime today = DateTime.Today;
            DateTime startDate;
            DateTime endDate = today;

            switch (SelectedPreset)
            {
                case "周":
                    startDate = today.AddDays(-6);
                    break;
                case "月":
                    startDate = today.AddDays(-29);
                    break;
                case "季度":
                    startDate = today.AddDays(-89);
                    break;
                case "半年":
                    startDate = today.AddDays(-179);
                    break;
                case "-1年":
                    startDate = today.AddDays(-364);
                    break;
                case "全部":
                    var (earliest, latest) = _tokenDataService.GetDateRange();
                    startDate = earliest;
                    endDate = latest;
                    break;
                case "自定义":
                    startDate = CustomStartDate?.DateTime.Date ?? today.AddDays(-30);
                    endDate = CustomEndDate?.DateTime.Date ?? today;
                    break;
                default:
                    startDate = today.AddDays(-29);
                    break;
            }

            SelectedPresetText = SelectedPreset switch
            {
                "周" => "近7天",
                "月" => "近30天",
                "季度" => "近90天",
                "半年" => "近180天",
                "-1年" => "近1年",
                "全部" => "全部历史",
                "自定义" => "自定义范围",
                _ => SelectedPreset
            };

            RangeDateRangeText = $"{startDate:yyyy-MM-dd} ~ {endDate:yyyy-MM-dd}";

            var points = _tokenDataService.GetTrendData(startDate, endDate);

            long sumTokens = points.Sum(p => p.TotalTokens);
            long sumCalls = points.Sum(p => p.Calls);
            double sumCost = points.Sum(p => p.Cost);

            RangeTotalTokensText = $"{sumTokens:N0} Tokens";
            RangeTotalCallsText = $"{sumCalls:N0} 次请求";
            RangeTotalCostText = CurrencySymbol == "¥" ? $"¥{sumCost:F4}" : $"${sumCost:F4}";

            ChartPoints = new ObservableCollection<TokenTrendPoint>(points);
        }

        [RelayCommand]
        public void SelectPreset(string preset)
        {
            SelectedPreset = preset;
            IsCustomDateRangeOpen = (preset == "自定义");
            UpdateChartData();
        }

        [RelayCommand]
        public void ApplyCustomRange()
        {
            SelectedPreset = "自定义";
            IsCustomDateRangeOpen = true;
            UpdateChartData();
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            await LoadDataAsync();
        }

        [RelayCommand]
        public void ToggleCurrency()
        {
            string newCur = Currency == "USD" ? "CNY" : "USD";
            _pricingService.Settings.Currency = newCur;
            _pricingService.SaveToFile();
            Currency = newCur;
            CurrencySymbol = Currency == "CNY" ? "¥" : "$";

            var stats = _tokenDataService.GetOverviewStats();
            ApplyStats(stats);
            UpdateChartData();
        }
    }
}
