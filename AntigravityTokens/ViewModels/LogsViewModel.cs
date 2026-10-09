using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using AntigravityTokens.Models;
using AntigravityTokens.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;

namespace AntigravityTokens.ViewModels
{
    public partial class LogsViewModel : ObservableObject
    {
        private readonly TokenDataService _tokenDataService;
        private readonly DispatcherQueue _dispatcherQueue;

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _pageSize = 25;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private int _totalCount = 0;

        [ObservableProperty]
        private string _pageInfo = "第 1 / 1 页";

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private string _selectedModel = "全部模型";

        [ObservableProperty]
        private string _selectedWorkspace = "全部工作区";

        [ObservableProperty]
        private string _selectedDateRange = "全部";

        [ObservableProperty]
        private bool _isLoading = false;

        public ObservableCollection<TokenCallItem> Items { get; } = new();
        public ObservableCollection<string> AvailableModels { get; } = new();
        public ObservableCollection<string> AvailableWorkspaces { get; } = new();
        public ObservableCollection<string> AvailableDateRanges { get; } = new() { "全部", "今天", "近7天", "近30天" };

        public LogsViewModel(TokenDataService tokenDataService)
        {
            _tokenDataService = tokenDataService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            _tokenDataService.OverviewUpdated += (s, e) =>
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    LoadFilters();
                    LoadLogs();
                });
            };
        }

        public void LoadFilters()
        {
            var models = _tokenDataService.GetAllDistinctModels();
            AvailableModels.Clear();
            AvailableModels.Add("全部模型");
            foreach (var m in models)
                AvailableModels.Add(m);

            var workspaces = _tokenDataService.GetAllDistinctWorkspaces();
            AvailableWorkspaces.Clear();
            AvailableWorkspaces.Add("全部工作区");
            foreach (var w in workspaces)
                AvailableWorkspaces.Add(w);
        }

        public void LoadLogs()
        {
            IsLoading = true;
            try
            {
                var (pagedItems, totalCount, totalPages) = _tokenDataService.GetPagedLogs(
                    CurrentPage,
                    PageSize,
                    SelectedModel,
                    SelectedWorkspace,
                    SearchQuery,
                    SelectedDateRange);

                TotalCount = totalCount;
                TotalPages = totalPages;
                PageInfo = $"第 {CurrentPage} / {TotalPages} 页 (共 {TotalCount:N0} 条)";

                Items.Clear();
                foreach (var item in pagedItems)
                {
                    Items.Add(item);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public void ApplyFilter()
        {
            CurrentPage = 1;
            LoadLogs();
        }

        [RelayCommand]
        public void ResetFilter()
        {
            SearchQuery = string.Empty;
            SelectedModel = "全部模型";
            SelectedWorkspace = "全部工作区";
            SelectedDateRange = "全部";
            CurrentPage = 1;
            LoadLogs();
        }

        [RelayCommand]
        public void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadLogs();
            }
        }

        [RelayCommand]
        public void NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                LoadLogs();
            }
        }

        [RelayCommand]
        public async Task ExportCsvAsync()
        {
            try
            {
                var (allItems, _, _) = _tokenDataService.GetPagedLogs(
                    1,
                    int.MaxValue,
                    SelectedModel,
                    SelectedWorkspace,
                    SearchQuery,
                    SelectedDateRange);

                var sb = new StringBuilder();
                sb.AppendLine("ID,时间,对话标题,工作区,模型,提示Tokens,补全Tokens,思考Tokens,总Tokens,耗时(ms),费用");

                foreach (var it in allItems)
                {
                    string cleanTitle = (it.Title ?? "").Replace("\"", "\"\"");
                    string cleanWs = (it.Workspace ?? "").Replace("\"", "\"\"");
                    sb.AppendLine($"\"{it.Id}\",\"{it.FormattedTime}\",\"{cleanTitle}\",\"{cleanWs}\",\"{it.Model}\",{it.PromptTokens},{it.CandidateTokens},{it.ThinkingTokens},{it.TotalTokens},{it.LatencyMs},{it.Cost}");
                }

                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string fileName = $"AntigravityTokens_Logs_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                string outPath = Path.Combine(desktop, fileName);

                await File.WriteAllTextAsync(outPath, sb.ToString(), Encoding.UTF8);

                // Show notification / launch folder
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{outPath}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LogsViewModel] Export CSV error: {ex.Message}");
            }
        }
    }
}
