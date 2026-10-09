using AntigravityTokens.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AntigravityTokens.Views
{
    public sealed partial class AnalyticsPage : Page
    {
        public AnalyticsViewModel ViewModel { get; }

        public AnalyticsPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<AnalyticsViewModel>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.LoadAnalytics();
        }
    }
}
