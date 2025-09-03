using ScottPlot.AxisLimitManagers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace USB_FTDI.View
{
  
    public partial class WindowVisualization : Window
    {
        // Диапазоны осей (можно менять под свою модель)
        private const double XMin = 0;     // мс
        private const double XMax = 1000;  // мс
        private const double YMin = -2.0;  // мВ
        private const double YMax = 2.0;  // мВ

        public WindowVisualization()
        {
            InitializeComponent();
            SizeChanged += (_, __) => RedrawAll();
            Loaded += (_, __) => RedrawAll();
        }

        private void RedrawAll()
        {
            DrawGrid();
            DrawAxes();
            UpdatePoint();
        }

        private void DrawGrid()
        {
            GridCanvas.Children.Clear();

            double w = PlotCanvas.ActualWidth;
            double h = PlotCanvas.ActualHeight;

            if (w <= 0 || h <= 0)
            {
                // задержка до инициализации
                Dispatcher.InvokeAsync(() => RedrawAll());
                return;
            }

            // вертикальные линии (каждые 100 мс)
            int vCount = 10;
            for (int i = 0; i <= vCount; i++)
            {
                double x = i * w / vCount;
                var line = new Line
                {
                    X1 = x,
                    X2 = x,
                    Y1 = 0,
                    Y2 = h,
                    Stroke = (Brush)FindResource("GridLine"),
                    StrokeThickness = (i % 5 == 0) ? 1.2 : 0.6,
                    SnapsToDevicePixels = true
                };
                GridCanvas.Children.Add(line);
            }

            // горизонтальные линии (каждые 0.5 мВ)
            int hCount = 8;
            for (int i = 0; i <= hCount; i++)
            {
                double y = i * h / hCount;
                var line = new Line
                {
                    X1 = 0,
                    X2 = w,
                    Y1 = y,
                    Y2 = y,
                    Stroke = (Brush)FindResource("GridLine"),
                    StrokeThickness = (i == hCount / 2) ? 1.2 : 0.6,
                    SnapsToDevicePixels = true
                };
                GridCanvas.Children.Add(line);
            }
        }

        private void DrawAxes()
        {
            AxisCanvas.Children.Clear();

            double w = PlotCanvas.ActualWidth;
            double h = PlotCanvas.ActualHeight;

            // Ось X
            var xAxis = new Line
            {
                X1 = 40,
                X2 = w,
                Y1 = h - 30,
                Y2 = h - 30,
                Stroke = (Brush)FindResource("AxisLine"),
                StrokeThickness = 1.5
            };
            AxisCanvas.Children.Add(xAxis);

            // Подписи по X (каждые 100 мс)
            int stepX = 100;
            for (int t = (int)XMin; t <= XMax; t += stepX)
            {
                double x = MapX(t);
                var tb = new TextBlock
                {
                    Text = t.ToString(),
                    FontSize = 11,
                    Foreground = Brushes.Black
                };
                AxisCanvas.Children.Add(tb);
                Canvas.SetLeft(tb, x - 10);
                Canvas.SetTop(tb, h - 25);
            }

            // Название оси X
            var xLabel = new TextBlock
            {
                Text = "t, мс",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black
            };
            AxisCanvas.Children.Add(xLabel);
            Canvas.SetLeft(xLabel, w / 2 - 15);
            Canvas.SetTop(xLabel, h - 10);

            // Ось Y
            var yAxis = new Line
            {
                X1 = 40,
                X2 = 40,
                Y1 = 0,
                Y2 = h - 30,
                Stroke = (Brush)FindResource("AxisLine"),
                StrokeThickness = 1.5
            };
            AxisCanvas.Children.Add(yAxis);

            // Подписи по Y (каждые 0.5 мВ)
            double stepY = 0.5;
            for (double a = YMin; a <= YMax; a += stepY)
            {
                double y = MapY(a);
                var tb = new TextBlock
                {
                    Text = a.ToString("0.0"),
                    FontSize = 11,
                    Foreground = Brushes.Black
                };
                AxisCanvas.Children.Add(tb);
                Canvas.SetLeft(tb, 5);
                Canvas.SetTop(tb, y - 8);
            }

            // Название оси Y (вертикально)
            var yLabel = new TextBlock
            {
                Text = "U, мВ",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,
                LayoutTransform = new RotateTransform(-90) // Повернуть
            };
            AxisCanvas.Children.Add(yLabel);
            Canvas.SetLeft(yLabel, 10);
            Canvas.SetTop(yLabel, h / 2 - 40);
        }


        private double MapX(double x)  // мс -> пиксели
        {
            double w = PlotCanvas.ActualWidth;
            return (x - XMin) / (XMax - XMin) * w;
        }

        private double MapY(double y)  // мВ -> пиксели (инверсия)
        {
            double h = PlotCanvas.ActualHeight;
            double t = (y - YMin) / (YMax - YMin);
            return (1 - t) * h;
        }

        private void UpdatePoint()
        {
            if (PointEl == null) return;
            double cx = MapX(SliderX.Value) - PointEl.Width / 2.0;
            double cy = MapY(SliderY.Value) - PointEl.Height / 2.0;

            Canvas.SetLeft(PointEl, cx);
            Canvas.SetTop(PointEl, cy);
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //UpdatePoint();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            SliderX.Value = 220;
            SliderY.Value = 0.6;
            UpdatePoint();
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            // Заглушка: «запуск» мог бы пересчитать координаты точки.
            // Здесь просто немного сдвигаем значение для наглядности.
            SliderX.Value = (SliderX.Value + 30) % XMax;
            UpdatePoint();
        }
    }
}
