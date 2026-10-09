using System;
using AntigravityTokens.Models;
using AntigravityTokens.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AntigravityTokens.Views
{
    public sealed partial class LogsPage : Page
    {
        public LogsViewModel ViewModel { get; }

        public LogsPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<LogsViewModel>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.LoadFilters();
            ViewModel.LoadLogs();
        }

        private async void OnDetailButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is TokenCallItem item)
            {
                var dialog = new ContentDialog
                {
                    XamlRoot = this.XamlRoot,
                    Title = "调用明细详情",
                    CloseButtonText = "关闭",
                    DefaultButton = ContentDialogButton.Close
                };

                var panel = new StackPanel { Spacing = 10, Padding = new Thickness(0, 10, 0, 0), Width = 460 };

                void AddRow(string label, string val)
                {
                    var g = new Grid();
                    g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                    g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    var lbl = new TextBlock { Text = label, Opacity = 0.6, FontSize = 13 };
                    var value = new TextBlock { Text = val, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, FontSize = 13, TextWrapping = TextWrapping.Wrap };

                    Grid.SetColumn(lbl, 0);
                    Grid.SetColumn(value, 1);
                    g.Children.Add(lbl);
                    g.Children.Add(value);
                    panel.Children.Add(g);
                }

                AddRow("调用标识 ID:", item.Id);
                AddRow("会话 UUID:", item.ConversationId);
                AddRow("对话标题:", item.Title);
                AddRow("工作区路径:", string.IsNullOrEmpty(item.WorkspaceFull) ? item.Workspace : item.WorkspaceFull);
                AddRow("使用模型:", item.Model);
                AddRow("请求时间:", item.FormattedTime);
                AddRow("输入 Tokens:", $"{item.PromptTokens:N0}");
                AddRow("补全 Tokens:", $"{item.CandidateTokens:N0}");
                AddRow("思考 Tokens:", $"{item.ThinkingTokens:N0}");
                AddRow("总计 Tokens:", $"{item.TotalTokens:N0}");
                AddRow("执行耗时:", $"{item.LatencySeconds:F2} 秒 ({item.LatencyMs} ms)");
                AddRow("预估费用:", item.FormattedCost);

                dialog.Content = panel;
                await dialog.ShowAsync();
            }
        }
    }
}
