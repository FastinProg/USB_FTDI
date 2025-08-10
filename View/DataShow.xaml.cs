using Microsoft.Win32;
using ScottPlot;
using ScottPlot.Plottable;
using SDReaderBinaryConvector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Drawing;
using ScottPlot.Plottables;
using System.Windows.Interop;

namespace USB_FTDI.View
{
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

        private void DataShow_Loaded(object sender, RoutedEventArgs e)
        {
            // Настройка таймера
            tim = new DispatcherTimer();
            tim.Interval = TimeSpan.FromMilliseconds(100); ;
            tim.Tick += UpdateForm;
            tim.Start();

            for (UInt32 col = 0; col < this.numberOfColumn; col++)
            {
                ScottPlot.TickGenerators.NumericAutomatic tickGenY = new ScottPlot.TickGenerators.NumericAutomatic();
                tickGenY.MinimumTickSpacing = 1000;
                tickGenY.IntegerTicksOnly = true;
                WpfPlotArr[col].Plot.XLabel("Time, ms");
                WpfPlotArr[col].Plot.Axes.Left.TickGenerator = tickGenY;
                for (UInt32 row = 0; row < this.numberOfRows; row++)
                {
                    WpfPlotArr[col].Plot.Add.Signal(input_data[(col * this.numberOfRows) + row], period: 2);
                    WpfPlotArr[col].Plot.Axes.Bottom.TickLabelStyle.FontSize = 14;
                    WpfPlotArr[col].Plot.Axes.SetLimits(0, double.NaN, double.NaN, double.NaN);
                    /* Add horizontal line */
                    var line = WpfPlotArr[col].Plot.Add.HorizontalLine(0);
                    line.LineColor = ScottPlot.Color.FromHex("#008B8B");
                    line.LineWidth = 1;
                    string s = string.Format("Chaneel {0}", (col * this.numberOfRows) + row);
                    line.Text = s;
                    line.TextRotation = 0;
                    line.TextAlignment = Alignment.MiddleLeft;
                    line.LabelOppositeAxis = false;
                    line.LinePattern = LinePattern.Solid;
                    line.Position = row * y_space;
                    WpfPlotArr[col].Plot.Axes.SetLimits(0, input_data.Length, -15, 15); // Пример: Y от -1 до 1
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
