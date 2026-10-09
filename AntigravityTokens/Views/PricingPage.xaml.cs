using AntigravityTokens.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AntigravityTokens.Views
{
    public sealed partial class PricingPage : Page
    {
        public PricingViewModel ViewModel { get; }

        public PricingPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<PricingViewModel>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.LoadPricing();
        }
    }
}
