using AntigravityTokens.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AntigravityTokens.Views
{
    public sealed partial class DashboardPage : Page
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<DashboardViewModel>();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.LoadDataAsync();
        }
    }
}
