using System.Collections.ObjectModel;
using AntigravityTokens.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AntigravityTokens.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly TokenDataService _tokenDataService;
        private readonly LiveSyncService _liveSyncService;

        [ObservableProperty]
        private bool _isLiveSyncEnabled = true;

        [ObservableProperty]
        private int _selectedIntervalIndex = 0; // 0: 1s, 1: 2s, 2: 5s

        [ObservableProperty]
        private string _appVersion = "v1.0.0 (WinUI 3 Native)";

        public ObservableCollection<string> MonitoredPaths { get; } = new();

        public SettingsViewModel(TokenDataService tokenDataService, LiveSyncService liveSyncService)
        {
            _tokenDataService = tokenDataService;
            _liveSyncService = liveSyncService;

            IsLiveSyncEnabled = _liveSyncService.IsRunning;

            foreach (var p in _tokenDataService.MonitoredDirectories)
            {
                MonitoredPaths.Add(p);
            }
        }

        [RelayCommand]
        public void ToggleLiveSync()
        {
            if (IsLiveSyncEnabled)
            {
                _liveSyncService.Start();
            }
            else
            {
                _liveSyncService.Stop();
            }
        }

        [RelayCommand]
        public void ChangeInterval(int seconds)
        {
            _liveSyncService.IntervalSeconds = seconds;
        }
    }
}
