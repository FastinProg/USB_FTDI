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

namespace USB_FTDI.View
{
    /// <summary>
    /// Логика взаимодействия для DataShow.xaml
    /// </summary>
    public partial class DataShow : UserControl
    {
        private UInt32 chaneelQuantity = 64;
        public double[][] input_data = new double[64][];

        public int indexData = 0;
        DispatcherTimer tim;

        double[] data1 = new double[5000];

        private bool startConversation;

        public DataShow()
        {
            InitializeComponent();
            Loaded += DataShow_Loaded;
            Unloaded += DataShow_Unloaded;


            for (int i = 0; i < chaneelQuantity; i++)
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

            
            for (int i = 0; i < chaneelQuantity; i++)
            {
                MyPlot1.Plot.Add.Signal(input_data[i], period:2);

                MyPlot1.Plot.Axes.Bottom.TickLabelStyle.FontSize = 14;
              
                MyPlot1.Plot.Axes.SetLimits(0, double.NaN, double.NaN, double.NaN);
                /* Add horizontal line */
                var hl2 = MyPlot1.Plot.Add.HorizontalLine(0);
                hl2.LineColor = ScottPlot.Color.FromHex("#008B8B");
                hl2.LineWidth = 1;
                string s = string.Format("Chaneel {0}", i);
                hl2.Text = s;
                hl2.TextRotation = 0;
                hl2.TextAlignment = Alignment.MiddleLeft;
                hl2.LabelOppositeAxis = false;
                hl2.LinePattern = LinePattern.Solid;
                hl2.Position = i * 0.6;
                //MyPlot1.Plot.YLabel("Value, mV");
            }
            // Настройка графика
            MyPlot1.Plot.XLabel("Time, ms");
            
            ScottPlot.TickGenerators.NumericAutomatic tickGenY = new ScottPlot.TickGenerators.NumericAutomatic();
            tickGenY.MinimumTickSpacing = 1000;

            tickGenY.IntegerTicksOnly = true;
            MyPlot1.Plot.Axes.Left.TickGenerator = tickGenY;

            var a = MyPlot1.Plot.Axes;
            var b = a.Left;
            //MyPlot1.Plot.Add.HorizontalLine(1);
            // MyPlot1.Plot.Add.HorizontalLine(2);
            //MyPlot1.Plot.Add.HorizontalLine(3);



        }

        // Every 100 ms update form
        private void UpdateForm(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
               // MyPlot.Plot.AxisAuto();
                MyPlot1.Refresh();
                //MyPlot1.Plot.Axes.AutoScale();
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

                                for (int i = 0; i < array.Length; i++)
                                {
                                    input_data[i][indexData] = (Convert.ToDouble(array[i]) / 1000) + (0.6 * i);
                                }

                                double[] doubleArr = new double[8];
                                for (int i = 0; i < 8; i++)
                                {
                                    doubleArr[i] = Convert.ToDouble(array[i]);
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
                        MyPlot1.Plot.Axes.AutoScale();
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
