using AntigravityTokens.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AntigravityTokens.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsViewModel ViewModel { get; }

        public SettingsPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<SettingsViewModel>();
        }

        private void OnLiveSyncToggled(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleLiveSync();
        }
    }
}
