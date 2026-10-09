using System;
using AntigravityTokens.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AntigravityTokens
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Extend content into titlebar for translucent acrylic effect
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            UpdateTitleBarTheme();

            if (Content is FrameworkElement root)
            {
                root.ActualThemeChanged += (s, e) => UpdateTitleBarTheme();
            }
        }

        private void UpdateTitleBarTheme()
        {
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                var titleBar = AppWindow.TitleBar;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

                var theme = (Content as FrameworkElement)?.ActualTheme ?? ElementTheme.Default;
                if (theme == ElementTheme.Dark)
                {
                    titleBar.ButtonForegroundColor = Colors.White;
                    titleBar.ButtonHoverForegroundColor = Colors.White;
                    titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(40, 255, 255, 255);
                    titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(60, 255, 255, 255);
                    titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(120, 255, 255, 255);
                }
                else
                {
                    titleBar.ButtonForegroundColor = Colors.Black;
                    titleBar.ButtonHoverForegroundColor = Colors.Black;
                    titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(40, 0, 0, 0);
                    titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(60, 0, 0, 0);
                    titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(120, 0, 0, 0);
                }
            }
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            // Default to Dashboard
            NavView.SelectedItem = NavView.MenuItems[0];
            ContentFrame.Navigate(typeof(DashboardPage));
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer is NavigationViewItem item && item.Tag is string tag)
            {
                Type pageType = tag switch
                {
                    "dashboard" => typeof(DashboardPage),
                    "logs" => typeof(LogsPage),
                    "analytics" => typeof(AnalyticsPage),
                    "pricing" => typeof(PricingPage),
                    "settings" => typeof(SettingsPage),
                    _ => typeof(DashboardPage)
                };

                if (ContentFrame.CurrentSourcePageType != pageType)
                {
                    ContentFrame.Navigate(pageType);
                }
            }
        }
    }
}
