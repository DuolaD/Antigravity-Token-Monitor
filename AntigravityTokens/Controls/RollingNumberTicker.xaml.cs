using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace AntigravityTokens.Controls
{
    public sealed partial class RollingNumberTicker : UserControl
    {
        private DispatcherTimer? _animationTimer;
        private double _displayedValue = 0;
        private double _targetValue = 0;
        private double _startValue = 0;
        private int _currentStep = 0;
        private const int TotalSteps = 15;

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(RollingNumberTicker),
                new PropertyMetadata(0.0, OnValueChanged));

        public static readonly DependencyProperty PrefixProperty =
            DependencyProperty.Register(nameof(Prefix), typeof(string), typeof(RollingNumberTicker),
                new PropertyMetadata(string.Empty, OnFormatChanged));

        public static readonly DependencyProperty SuffixProperty =
            DependencyProperty.Register(nameof(Suffix), typeof(string), typeof(RollingNumberTicker),
                new PropertyMetadata(string.Empty, OnFormatChanged));

        public static readonly DependencyProperty FormatStringProperty =
            DependencyProperty.Register(nameof(FormatString), typeof(string), typeof(RollingNumberTicker),
                new PropertyMetadata("N0", OnFormatChanged));

        public static readonly DependencyProperty ValueFontSizeProperty =
            DependencyProperty.Register(nameof(ValueFontSize), typeof(double), typeof(RollingNumberTicker),
                new PropertyMetadata(24.0, (d, e) =>
                {
                    if (d is RollingNumberTicker ticker && ticker.ValueTextBlock != null)
                        ticker.ValueTextBlock.FontSize = (double)e.NewValue;
                }));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public string Prefix
        {
            get => (string)GetValue(PrefixProperty);
            set => SetValue(PrefixProperty, value);
        }

        public string Suffix
        {
            get => (string)GetValue(SuffixProperty);
            set => SetValue(SuffixProperty, value);
        }

        public string FormatString
        {
            get => (string)GetValue(FormatStringProperty);
            set => SetValue(FormatStringProperty, value);
        }

        public double ValueFontSize
        {
            get => (double)GetValue(ValueFontSizeProperty);
            set => SetValue(ValueFontSizeProperty, value);
        }

        public RollingNumberTicker()
        {
            InitializeComponent();
            Loaded += (s, e) => UpdateText();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RollingNumberTicker ticker)
            {
                ticker.AnimateTo((double)e.NewValue);
            }
        }

        private static void OnFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RollingNumberTicker ticker)
            {
                ticker.UpdateText();
            }
        }

        public void AnimateTo(double target)
        {
            _targetValue = target;
            if (Math.Abs(_targetValue - _displayedValue) < 0.0001)
            {
                _displayedValue = target;
                UpdateText();
                return;
            }

            _startValue = _displayedValue;
            _currentStep = 0;

            // Trigger glow flash
            PulseGlow();

            if (_animationTimer == null)
            {
                _animationTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(20) // 50 FPS
                };
                _animationTimer.Tick += OnTimerTick;
            }

            _animationTimer.Stop();
            _animationTimer.Start();
        }

        private void OnTimerTick(object? sender, object e)
        {
            _currentStep++;
            double progress = (double)_currentStep / TotalSteps;

            if (progress >= 1.0)
            {
                _displayedValue = _targetValue;
                _animationTimer?.Stop();
                UpdateText();
                return;
            }

            // Ease-out cubic formula
            double ease = 1.0 - Math.Pow(1.0 - progress, 3);
            _displayedValue = _startValue + (_targetValue - _startValue) * ease;
            UpdateText();
        }

        private void UpdateText()
        {
            if (ValueTextBlock == null) return;
            string formatted = _displayedValue.ToString(FormatString);
            ValueTextBlock.Text = $"{Prefix}{formatted}{Suffix}";
        }

        private void PulseGlow()
        {
            if (GlowBorder == null) return;

            // Subtle green/cyan flash on increment
            GlowBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(50, 16, 185, 129));

            var resetTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
            resetTimer.Tick += (s, ev) =>
            {
                resetTimer.Stop();
                GlowBorder.Background = new SolidColorBrush(Colors.Transparent);
            };
            resetTimer.Start();
        }
    }
}
