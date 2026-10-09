using System;
using AntigravityTokens.Services;
using AntigravityTokens.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace AntigravityTokens
{
    public partial class App : Application
    {
        private Window? _window;

        public static IServiceProvider Services { get; private set; } = null!;

        public static T GetService<T>() where T : class
        {
            return Services.GetRequiredService<T>();
        }

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            var services = new ServiceCollection();

            // Services
            services.AddSingleton<PricingService>();
            services.AddSingleton<SummariesService>();
            services.AddSingleton<DatabaseService>();
            services.AddSingleton<TokenDataService>();
            services.AddSingleton<LiveSyncService>(sp =>
            {
                var tokenData = sp.GetRequiredService<TokenDataService>();
                var dispatcher = DispatcherQueue.GetForCurrentThread();
                return new LiveSyncService(tokenData, dispatcher);
            });

            // ViewModels
            services.AddSingleton<DashboardViewModel>();
            services.AddSingleton<LogsViewModel>();
            services.AddSingleton<AnalyticsViewModel>();
            services.AddSingleton<PricingViewModel>();
            services.AddSingleton<SettingsViewModel>();

            Services = services.BuildServiceProvider();

            // Start 1-second live heartbeat sync
            var liveSync = GetService<LiveSyncService>();
            liveSync.Start();

            _window = new MainWindow();
            _window.Activate();
        }
    }
}
