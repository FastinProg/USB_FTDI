using Microsoft.Win32;
using ScottPlot;
using SDReaderBinaryConvector;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace USB_FTDI.View
{
    public class FixedTickGenerator : ITickGenerator
    {
        private Tick[] _ticks;

        // Максимальное число меток (можно любое число, не обязательно фиксированное)
        public int MaxTickCount { get; set; } = int.MaxValue;

        // Свойство, чтобы внешний код мог получить текущие метки
        public Tick[] Ticks => _ticks;

        public FixedTickGenerator(Tick[] ticks)
        {
            _ticks = ticks;
        }

        // Обязательный метод регенерации меток
        public void Regenerate(CoordinateRange range, Edge edge, PixelLength size, SKPaint paint, LabelStyle labelStyle)
        {
            // Мы не меняем метки динамически, всегда возвращаем фиксированный набор
            // Можно здесь отсекать метки вне диапазона, если нужно — сделаем так:

            List<Tick> visibleTicks = new List<Tick>();
            foreach (var tick in _ticks)
            {
                if (tick.Position >= range.Min && tick.Position <= range.Max)
                    visibleTicks.Add(tick);
            }

            _ticks = visibleTicks.ToArray();
        }
    }

    /// <summary>
    /// Логика взаимодействия для DataShow.xaml
    /// </summary>
    public partial class DataShow : UserControl
    {
        public ScottPlot.WPF.WpfPlot[] WpfPlotArr = new ScottPlot.WPF.WpfPlot[4];
        private UInt32 maxChaneelQuantity = 64;
        private UInt32 currentChaneelQuantity = 64;
        private UInt32 numberOfColumn = 4;
        private UInt32 numberOfRows = 16;
        public double[][] input_data = new double[64][];

        public int indexData = 0;
        private double y_space = 15;
        DispatcherTimer tim;

        double[] data1 = new double[5000];

        private bool startConversation;

        public DataShow()
        {
            InitializeComponent();
            Loaded += DataShow_Loaded;
            Unloaded += DataShow_Unloaded;
            WpfPlotArr[0] = MyPlot1;
            WpfPlotArr[1] = MyPlot2;
            WpfPlotArr[2] = MyPlot3;
            WpfPlotArr[3] = MyPlot4;

            for (int i = 0; i < maxChaneelQuantity; i++)
            {
                input_data[i] = new double[5000];
            }
    }

        private void DataShow_Unloaded(object sender, RoutedEventArgs e)
        {
            tim.Stop();
        }

        uint MakeArgb(byte alpha, byte red, byte green, byte blue)
        {
            return ((uint)alpha << 24) | ((uint)red << 16) | ((uint)green << 8) | blue;
        }

        private void DataShow_Loaded(object sender, RoutedEventArgs e)
        {
            // Настройка таймера
            tim = new DispatcherTimer();
            tim.Interval = TimeSpan.FromMilliseconds(100);
            tim.Tick += UpdateForm;
            tim.Start();

            for (UInt32 col = 0; col < this.numberOfColumn; col++)
            {
                var plt = WpfPlotArr[col].Plot;

                // Генератор делений для оси X (Time, ms)
                var tickGenX = new ScottPlot.TickGenerators.NumericAutomatic()
                {
                    MinimumTickSpacing = 1,    // минимальный шаг 1 (миллиметр)
                    IntegerTicksOnly = true
                };

                // Генератор делений для оси Y
                var tickGenY = new ScottPlot.TickGenerators.NumericAutomatic()
                {
                    MinimumTickSpacing = 1,
                    IntegerTicksOnly = true
                };

                plt.XLabel("Time, ms");
                plt.Axes.Bottom.TickGenerator = tickGenX;
                plt.Axes.Left.TickGenerator = tickGenY;
                plt.Axes.Left.TickLabelStyle.IsVisible = false;

                // Включаем сетку
                //plt.Grid(true);

                // Настраиваем цвета и толщину линий сетки
                plt.Grid.MajorLineColor = ScottPlot.Color.FromARGB(MakeArgb(50, 204, 0, 0));
                plt.Grid.MajorLineWidth = 2;
                plt.Grid.MinorLineColor = ScottPlot.Color.FromARGB(MakeArgb(30, 255, 182, 182)); // Розовый — тонкие линии (minor)
                plt.Grid.MinorLineWidth = 1;
                for (UInt32 row = 0; row < this.numberOfRows; row++)
                {
                    var sig = plt.Add.Signal(input_data[(col * this.numberOfRows) + row], period: 2);
                    sig.LineWidth = 2;
                    plt.Axes.Bottom.TickLabelStyle.FontSize = 14;

                    // Устанавливаем ограничения осей (X: от 0 до длины данных, Y: по каналам)
                    plt.Axes.SetLimits(0, input_data[0].Length, -15, y_space * this.numberOfRows);

                    // Добавляем горизонтальную линию с подписью канала
                    var line = plt.Add.HorizontalLine(0);
                    line.LineColor = ScottPlot.Color.FromHex("#008B8B");
                    line.LineWidth = 1;
                    string s = string.Format("Chaneel {0}", (col * this.numberOfRows) + row);
                    line.Text = s;
                    line.TextRotation = 0;
                    line.TextAlignment = Alignment.MiddleLeft;
                    line.LabelOppositeAxis = false;
                    line.LinePattern = LinePattern.Solid;
                    line.Position = row * y_space;
                }
            }
        }




        // Every 100 ms update form
        private void UpdateForm(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                for (UInt32 col = 0; col < this.numberOfColumn; col++)
                {
                    WpfPlotArr[col].Refresh();
                }
                // MyPlot.Plot.AxisAuto();
                //MyPlot1.Refresh();
            }));
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            ReadFileAsunc();
        }

        public async void ReadFileAsunc()
        {


            await Task.Run(() =>
            {            // Узнаем путь к файлу
                OpenFileDialog openFileDialog = new OpenFileDialog();
                FileProperty myFile = new FileProperty();

                if (openFileDialog.ShowDialog() == true)
                {
                    myFile.InputFileName = openFileDialog.SafeFileName;
                    myFile.InputFilePath = openFileDialog.FileName;
                    myFile.InputFileSize = Convert.ToString(new FileInfo(openFileDialog.FileName).Length) + "\tбайт";
                }

                if (myFile.InputFilePath == null)
                    return;

                // Создаем поток для чтения
                using (FileStream inputFileRead = new FileStream(myFile.InputFilePath, FileMode.Open, FileAccess.Read))
                {
                    using (StreamReader reader = new StreamReader(inputFileRead))
                    {
                        string str = String.Empty;
                        while (str != null)
                        {
                            try
                            {
                                str = reader.ReadLine();
                                if (str == null)
                                    break;
                                string[] array = str.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                str = String.Empty;
                                UInt32 offset = 0;
                                for (UInt32 i = 0; i < this.currentChaneelQuantity; i++)
                                {
                                    offset = i % this.numberOfRows;
                                    input_data[i][indexData] = Convert.ToDouble(array[i]) + (offset * y_space);
                                }

                                if (indexData < data1.Length)
                                {
                                    indexData++;
                                }

                            }
                            catch
                            {
                            }
                        }
                        for (UInt32 col = 0; col < this.numberOfColumn; col++)
                        {
                            WpfPlotArr[col].Plot.Axes.AutoScale();
                        }
                        //MyPlot1.Plot.Axes.AutoScale();
                        //MyPlot1.Plot.AxisAuto();
                    }

                }
            });
            
        }

        private void MyPlot_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
