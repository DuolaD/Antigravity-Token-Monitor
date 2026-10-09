using System.Collections.ObjectModel;
using System.Linq;
using AntigravityTokens.Models;
using AntigravityTokens.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;

namespace AntigravityTokens.ViewModels
{
    public partial class AnalyticsViewModel : ObservableObject
    {
        private readonly TokenDataService _tokenDataService;
        private readonly DispatcherQueue _dispatcherQueue;

        [ObservableProperty]
        private string _totalDaysText = "0 天";

        [ObservableProperty]
        private string _avgDailyTokensText = "0";

        [ObservableProperty]
        private string _overallThinkingRatioText = "0.0%";

        public ObservableCollection<DailyTokenStat> DailyItems { get; } = new();

        public AnalyticsViewModel(TokenDataService tokenDataService)
        {
            _tokenDataService = tokenDataService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            _tokenDataService.OverviewUpdated += (s, e) =>
            {
                _dispatcherQueue.TryEnqueue(LoadAnalytics);
            };
        }

        public void LoadAnalytics()
        {
            var dailyList = _tokenDataService.GetDailyStats();
            DailyItems.Clear();

            long grandTotal = 0;
            long grandThinking = 0;
            long grandOutput = 0;

            foreach (var item in dailyList)
            {
                DailyItems.Add(item);
                grandTotal += item.TotalTokens;
                grandThinking += item.ThinkingTokens;
                grandOutput += item.OutputTokens;
            }

            int count = dailyList.Count;
            TotalDaysText = $"{count} 个活跃日";
            long avg = count > 0 ? grandTotal / count : 0;
            AvgDailyTokensText = $"{avg:N0}";

            double ratio = grandOutput > 0 ? (double)grandThinking / grandOutput * 100.0 : 0.0;
            OverallThinkingRatioText = $"{ratio:F1}%";
        }

        [RelayCommand]
        public void Refresh()
        {
            LoadAnalytics();
        }
    }
}
