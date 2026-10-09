using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using AntigravityTokens.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace AntigravityTokens.Controls
{
    public sealed partial class TokenTrendChart : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(IEnumerable<TokenTrendPoint>),
                typeof(TokenTrendChart),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public static readonly DependencyProperty CurrencySymbolProperty =
            DependencyProperty.Register(
                nameof(CurrencySymbol),
                typeof(string),
                typeof(TokenTrendChart),
                new PropertyMetadata("$", (d, e) => ((TokenTrendChart)d).Redraw()));

        public IEnumerable<TokenTrendPoint>? ItemsSource
        {
            get => (IEnumerable<TokenTrendPoint>?)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public string CurrencySymbol
        {
            get => (string)GetValue(CurrencySymbolProperty);
            set => SetValue(CurrencySymbolProperty, value);
        }

        private readonly List<(Point Pos, TokenTrendPoint Data)> _cachedPoints = new();

        public TokenTrendChart()
        {
            this.InitializeComponent();
            this.Loaded += (s, e) => Redraw();
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TokenTrendChart chart)
            {
                if (e.OldValue is INotifyCollectionChanged oldColl)
                {
                    oldColl.CollectionChanged -= chart.OnCollectionChanged;
                }
                if (e.NewValue is INotifyCollectionChanged newColl)
                {
                    newColl.CollectionChanged += chart.OnCollectionChanged;
                }
                chart.Redraw();
            }
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Redraw();
        }

        private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width > 0 && e.NewSize.Height > 0)
            {
                ChartCanvas.Width = e.NewSize.Width;
                ChartCanvas.Height = e.NewSize.Height;
                OverlayCanvas.Width = e.NewSize.Width;
                OverlayCanvas.Height = e.NewSize.Height;
                Redraw();
            }
        }

        public void Redraw()
        {
            if (ChartCanvas == null) return;

            var points = ItemsSource?.ToList();
            bool hasData = points != null && points.Count > 0 && points.Any(p => p.TotalTokens > 0);

            if (!hasData)
            {
                ChartCanvas.Children.Clear();
                _cachedPoints.Clear();
                EmptyStatePanel.Visibility = Visibility.Visible;
                CrosshairLine.Visibility = Visibility.Collapsed;
                PointDot.Visibility = Visibility.Collapsed;
                TooltipCard.Visibility = Visibility.Collapsed;
                return;
            }

            double width = RootGrid.ActualWidth > 0 ? RootGrid.ActualWidth : ChartCanvas.ActualWidth;
            double height = RootGrid.ActualHeight > 0 ? RootGrid.ActualHeight : ChartCanvas.ActualHeight;

            if (width < 50 || height < 50)
            {
                DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, Redraw);
                return;
            }

            ChartCanvas.Width = width;
            ChartCanvas.Height = height;
            OverlayCanvas.Width = width;
            OverlayCanvas.Height = height;

            EmptyStatePanel.Visibility = Visibility.Collapsed;
            ChartCanvas.Children.Clear();
            _cachedPoints.Clear();

            double marginLeft = 65;
            double marginRight = 20;
            double marginTop = 20;
            double marginBottom = 35;

            double plotWidth = width - marginLeft - marginRight;
            double plotHeight = height - marginTop - marginBottom;

            if (plotWidth <= 0 || plotHeight <= 0) return;

            long maxVal = points!.Max(p => p.TotalTokens);
            if (maxVal <= 0) maxVal = 1000;

            // Round maxVal up to nice friendly number
            maxVal = NiceCeiling(maxVal);

            // 1. Draw 4 horizontal gridlines & Y-axis labels
            int gridLineCount = 4;
            for (int i = 0; i <= gridLineCount; i++)
            {
                double fraction = (double)i / gridLineCount;
                double y = marginTop + plotHeight - (fraction * plotHeight);
                long val = (long)(fraction * maxVal);

                // Grid line
                var line = new Line
                {
                    X1 = marginLeft,
                    Y1 = y,
                    X2 = marginLeft + plotWidth,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(40, 128, 128, 128)),
                    StrokeThickness = 1
                };
                ChartCanvas.Children.Add(line);

                // Y-axis label
                var label = new TextBlock
                {
                    Text = FormatNumberShort(val),
                    FontSize = 10,
                    Opacity = 0.55,
                    HorizontalTextAlignment = TextAlignment.Right,
                    Width = marginLeft - 8
                };
                Canvas.SetLeft(label, 0);
                Canvas.SetTop(label, y - 8);
                ChartCanvas.Children.Add(label);
            }

            // 2. Compute point coordinates
            var pointCollection = new PointCollection();
            var pts = points!;
            double stepX = pts.Count > 1 ? plotWidth / (pts.Count - 1) : plotWidth;

            for (int i = 0; i < pts.Count; i++)
            {
                var ptData = pts[i];
                double x = pts.Count > 1 ? marginLeft + (i * stepX) : marginLeft + (plotWidth / 2);
                double yRatio = (double)ptData.TotalTokens / maxVal;
                double y = marginTop + plotHeight - (yRatio * plotHeight);

                var point = new Point(x, y);
                pointCollection.Add(point);
                _cachedPoints.Add((point, ptData));
            }

            // 3. Draw gradient area underneath
            if (pointCollection.Count > 1)
            {
                var areaPolygon = new Polygon
                {
                    Fill = CreateAreaBrush()
                };

                var areaPoints = new PointCollection();
                // Bottom left start
                areaPoints.Add(new Point(pointCollection[0].X, marginTop + plotHeight));
                foreach (var pt in pointCollection)
                {
                    areaPoints.Add(pt);
                }
                // Bottom right end
                areaPoints.Add(new Point(pointCollection[^1].X, marginTop + plotHeight));

                areaPolygon.Points = areaPoints;
                ChartCanvas.Children.Add(areaPolygon);
            }

            // 4. Draw Polyline
            var polyline = new Polyline
            {
                Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 59, 130, 246)), // #3B82F6
                StrokeThickness = 2.5,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Points = pointCollection
            };
            ChartCanvas.Children.Add(polyline);

            // 5. Draw X-axis date labels (sample ~5 to 7 labels)
            int labelCount = Math.Min(6, pts.Count);
            if (labelCount > 1)
            {
                int step = (int)Math.Ceiling((double)(pts.Count - 1) / (labelCount - 1));
                for (int i = 0; i < pts.Count; i += step)
                {
                    AddDateLabel(pts[i].DateLabel, _cachedPoints[i].Pos.X, marginTop + plotHeight + 8);
                }
                // Always ensure last label is shown
                if ((pts.Count - 1) % step != 0)
                {
                    AddDateLabel(pts[^1].DateLabel, _cachedPoints[^1].Pos.X, marginTop + plotHeight + 8);
                }
            }
            else if (pts.Count == 1)
            {
                AddDateLabel(pts[0].DateLabel, _cachedPoints[0].Pos.X, marginTop + plotHeight + 8);
            }
        }

        private void AddDateLabel(string text, double centerX, double topY)
        {
            var label = new TextBlock
            {
                Text = text,
                FontSize = 10,
                Opacity = 0.55,
                HorizontalTextAlignment = TextAlignment.Center
            };
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double lblWidth = label.DesiredSize.Width;

            Canvas.SetLeft(label, centerX - (lblWidth / 2));
            Canvas.SetTop(label, topY);
            ChartCanvas.Children.Add(label);
        }

        private LinearGradientBrush CreateAreaBrush()
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };
            brush.GradientStops.Add(new GradientStop
            {
                Color = Windows.UI.Color.FromArgb(85, 59, 130, 246), // 33% opacity #3B82F6
                Offset = 0.0
            });
            brush.GradientStops.Add(new GradientStop
            {
                Color = Windows.UI.Color.FromArgb(0, 59, 130, 246),  // 0% opacity
                Offset = 1.0
            });
            return brush;
        }

        private void ChartCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_cachedPoints.Count == 0) return;

            var currentPoint = e.GetCurrentPoint(ChartCanvas).Position;

            // Find closest cached point along X
            var closest = _cachedPoints.OrderBy(p => Math.Abs(p.Pos.X - currentPoint.X)).First();

            // Update Crosshair Line
            CrosshairLine.X1 = closest.Pos.X;
            CrosshairLine.X2 = closest.Pos.X;
            CrosshairLine.Y1 = 20;
            CrosshairLine.Y2 = ChartCanvas.ActualHeight - 35;
            CrosshairLine.Visibility = Visibility.Visible;

            // Update Point Dot
            Canvas.SetLeft(PointDot, closest.Pos.X - (PointDot.Width / 2));
            Canvas.SetTop(PointDot, closest.Pos.Y - (PointDot.Height / 2));
            PointDot.Visibility = Visibility.Visible;

            // Update Tooltip Card
            var d = closest.Data;
            TooltipDate.Text = d.FullDateLabel;
            TooltipTotal.Text = $"{d.FormattedTotalTokens} Tokens";
            TooltipPrompt.Text = $"{d.FormattedPromptTokens} Tokens";
            TooltipOutput.Text = $"{d.FormattedOutputTokens} Tokens";
            TooltipCalls.Text = $"{d.FormattedCalls} 次";
            TooltipCost.Text = CurrencySymbol == "¥" ? d.FormattedCostCNY : d.FormattedCostUSD;

            TooltipCard.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double tipW = TooltipCard.DesiredSize.Width > 0 ? TooltipCard.DesiredSize.Width : 180;
            double tipH = TooltipCard.DesiredSize.Height > 0 ? TooltipCard.DesiredSize.Height : 160;

            double tipX = closest.Pos.X + 16;
            if (tipX + tipW > ChartCanvas.ActualWidth - 10)
            {
                tipX = closest.Pos.X - tipW - 16;
            }

            double tipY = closest.Pos.Y - (tipH / 2);
            if (tipY < 10) tipY = 10;
            if (tipY + tipH > ChartCanvas.ActualHeight - 10) tipY = ChartCanvas.ActualHeight - tipH - 10;

            Canvas.SetLeft(TooltipCard, tipX);
            Canvas.SetTop(TooltipCard, tipY);
            TooltipCard.Visibility = Visibility.Visible;
        }

        private void ChartCanvas_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            CrosshairLine.Visibility = Visibility.Collapsed;
            PointDot.Visibility = Visibility.Collapsed;
            TooltipCard.Visibility = Visibility.Collapsed;
        }

        private static long NiceCeiling(long val)
        {
            if (val <= 0) return 1000;
            double exp = Math.Floor(Math.Log10(val));
            double factor = Math.Pow(10, exp);
            double normalized = val / factor;

            double nice;
            if (normalized <= 1.0) nice = 1.0;
            else if (normalized <= 2.0) nice = 2.0;
            else if (normalized <= 2.5) nice = 2.5;
            else if (normalized <= 5.0) nice = 5.0;
            else nice = 10.0;

            return (long)(nice * factor);
        }

        private static string FormatNumberShort(long num)
        {
            if (num >= 1_000_000_000) return $"{num / 1_000_000_000.0:0.#}B";
            if (num >= 1_000_000) return $"{num / 1_000_000.0:0.#}M";
            if (num >= 1_000) return $"{num / 1_000.0:0.#}K";
            return num.ToString();
        }
    }
}
