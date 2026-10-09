using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace AntigravityTokens.Services
{
    public class LiveSyncService : IDisposable
    {
        private readonly TokenDataService _tokenDataService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly List<FileSystemWatcher> _watchers = new();
        private System.Threading.Timer? _heartbeatTimer;
        private readonly Dictionary<string, DateTime> _lastKnownMtimes = new();
        private bool _isChecking = false;

        public bool IsRunning { get; private set; } = false;
        public int IntervalSeconds { get; set; } = 1;

        public event EventHandler<DateTime>? HeartbeatTicked;

        public LiveSyncService(TokenDataService tokenDataService, DispatcherQueue dispatcherQueue)
        {
            _tokenDataService = tokenDataService;
            _dispatcherQueue = dispatcherQueue;
        }

        public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;

            // Setup FileSystemWatchers
            foreach (var dir in _tokenDataService.MonitoredDirectories)
            {
                if (!Directory.Exists(dir)) continue;

                try
                {
                    var watcher = new FileSystemWatcher(dir)
                    {
                        Filter = "*.*",
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                        EnableRaisingEvents = true
                    };

                    watcher.Changed += OnFileChanged;
                    watcher.Created += OnFileChanged;
                    _watchers.Add(watcher);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LiveSyncService] Watcher error on {dir}: {ex.Message}");
                }
            }

            // Setup 1-second dynamic heartbeat timer
            _heartbeatTimer = new System.Threading.Timer(async _ =>
            {
                await CheckForUpdatesAsync();
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(IntervalSeconds));
        }

        public void Stop()
        {
            IsRunning = false;
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = null;

            foreach (var w in _watchers)
            {
                w.EnableRaisingEvents = false;
                w.Dispose();
            }
            _watchers.Clear();
        }

        private async void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.EndsWith(".db", StringComparison.OrdinalIgnoreCase) ||
                e.FullPath.EndsWith(".db-wal", StringComparison.OrdinalIgnoreCase))
            {
                await CheckForUpdatesAsync();
            }
        }

        private async Task CheckForUpdatesAsync()
        {
            if (_isChecking) return;
            _isChecking = true;

            try
            {
                bool hasChange = false;

                foreach (var dir in _tokenDataService.MonitoredDirectories)
                {
                    if (!Directory.Exists(dir)) continue;

                    var files = Directory.GetFiles(dir, "*.db*");
                    foreach (var file in files)
                    {
                        try
                        {
                            var mtime = File.GetLastWriteTimeUtc(file);
                            if (!_lastKnownMtimes.TryGetValue(file, out var oldTime) || oldTime != mtime)
                            {
                                _lastKnownMtimes[file] = mtime;
                                hasChange = true;
                            }
                        }
                        catch
                        {
                        }
                    }
                }

                if (hasChange)
                {
                    await _tokenDataService.RefreshAllAsync();
                }

                // Notify UI on dispatcher thread
                _dispatcherQueue.TryEnqueue(() =>
                {
                    HeartbeatTicked?.Invoke(this, DateTime.Now);
                });
            }
            finally
            {
                _isChecking = false;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
