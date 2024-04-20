//#define PLOT

using FTDI;
using ScottPlot.Plottable;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using USB_FTDI.FTDI_Logic;

namespace USB_FTDI.View
{
    /// <summary>
    /// Логика взаимодействия для ConectToDevice.xaml
    /// </summary>
    public partial class ConectToDevice : UserControl
    {

        // ТЕСТ
        public UInt32 p_index = 0;
        public UInt32 Italue = 0;

        Queue<FTDI_Data_t> Rxqueue = new Queue<FTDI_Data_t>();

        // Для приема данных
        private Thread updateReciveBuf;
        private object locker = new object();
        private bool updateReciveBufIsAlive = true;
        EventWaitHandle rxWait = new AutoResetEvent(false);

        public double[] data = new double[100_000];
        int nextDataIndex = 1;
        //SignalPlot signalPlot;
        Random rand = new Random(0);

        DispatcherTimer tim;
        FTDI_Hardware fTDI;

        private DispatcherTimer _updateDataTimer;
        private DispatcherTimer _renderTimer;
        // Экземпляры привзяки
        ConnectUpdate ConnectUpdate = new ConnectUpdate();


        public ConectToDevice()
        {
            InitializeComponent();
            Loaded += ConectToDevice_Loaded;
        }

        private void ConectToDevice_Loaded(object sender, RoutedEventArgs e)
        {

            // Привязка
            tblConnectStatus.DataContext = ConnectUpdate;
            ConnectUpdate.ConectSt = ftdiConectStatus_e.ftdiCSt_NotConnect;

            // Настройка драйвера
            fTDI = new FTDI_Hardware();
          //  fTDI.OnReadMessage += ReciveMsg;
            fTDI.Connect();
            
            // Настройка таймера
            tim = new DispatcherTimer();
            tim.Interval = TimeSpan.FromMilliseconds(100); ;
            tim.Tick += Tim_Elapsed;
            tim.Start();

            // Настройка графика
            /*
            signalPlot = MyPlot.Plot.AddSignal(data);
            MyPlot.Plot.XLabel("Time, ms");
            MyPlot.Plot.YLabel("Value, mV");
            MyPlot.Refresh();
            */
            // create a timer to modify the data
#if PLOT
            _updateDataTimer = new DispatcherTimer();
            _updateDataTimer.Interval = TimeSpan.FromMilliseconds(1);
            _updateDataTimer.Tick += UpdateData;
            _updateDataTimer.Start();
#endif 
            // create a timer to update the GUI
            _renderTimer = new DispatcherTimer();
            _renderTimer.Interval = TimeSpan.FromMilliseconds(100);
            _renderTimer.Tick += Render;
            _renderTimer.Start();

            updateReciveBuf = new Thread(updateReciveBuf_thread);
            updateReciveBuf.Start();
        }

        private void Tim_Elapsed(object sender, EventArgs e)
        {
            // AutoConnect
            if (fTDI.GetFTDIStatus() != FTD2XX_NET.FTDI.FT_STATUS.FT_OK)
            {
                ConnectUpdate.ConectSt = ftdiConectStatus_e.ftdiCSt_NotConnect;
                fTDI.Connect();
            }
            else
            {
                ConnectUpdate.ConectSt = ftdiConectStatus_e.ftdiCSt_Connect;
            }
        }


        // Приняли пакета
        private void ReciveMsg(FTDI_Data_t data)
        {
            lock (locker)
            {
                Rxqueue.Enqueue(data);
                rxWait.Set();
            }
        }

        void UpdateData(object sender, EventArgs e)
        {
            if (nextDataIndex >= data.Length)
            {
                throw new OverflowException("data array isn't long enough to accomodate new data");
                // in this situation the solution would be:
                //   1. clear the plot
                //   2. create a new larger array
                //   3. copy the old data into the start of the larger array
                //   4. plot the new (larger) array
                //   5. continue to update the new array
            }

            double randomValue = Math.Round(rand.NextDouble() - .5, 3);
            double latestValue = data[nextDataIndex - 1] + randomValue;

            data[nextDataIndex] = latestValue;
            //signalPlot.MaxRenderIndex = nextDataIndex;
            nextDataIndex += 1;


            /*
             // "scroll" the whole chart to the left
            data about 400
            Array.Copy(liveData, 1, liveData, 0, liveData.Length - 1);

            // place the newest data point at the end
            double nextValue = ecg.GetVoltage(sw.Elapsed.TotalSeconds);
            liveData[liveData.Length - 1] = nextValue;
             */
            /*
            byte cntElement = (byte)rand.Next();
            byte[] buf = new byte[cntElement];
            rand.NextBytes(buf);
            Byte i = 0;
            while (i != cntElement)
            {
                data[i] = i;
                signalPlot.MaxRenderIndex = i;
                i++;
             }
            */
        }

        void Render(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                /*
               MyPlot.Plot.AxisAuto();
              MyPlot.Refresh();
                */
            }));

        }

        private void updateReciveBuf_thread()
        {
            while (updateReciveBufIsAlive)
            {
                rxWait.WaitOne(100);
                lock (locker)
                {
                    if (Rxqueue.Count > 0)
                    {
                        FTDI_Data_t FTDIdata = Rxqueue.Dequeue();

                        data[nextDataIndex] = FTDIdata.data[0];
                        //signalPlot.MaxRenderIndex = nextDataIndex;
                        nextDataIndex += 1;
                    }
                }
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                //CalcArray(fTDI.DataAll, fTDI.DataAllIndex);
            });
        }

        void CalcArray(byte[] buf, UInt32 index)
        {
            byte[] local_buf = new byte[10000000];
            Array.Copy(buf, 0, local_buf, 0, index);

            while (p_index != index)
            {
                if (++Italue > 8)
                    Italue = 0;
              
                if (buf[p_index] != Italue)
                {
                    ;
                }
                p_index++;

            }
            
        }

    }



    #region Классы для привзяки данных
    public class ConnectUpdate : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ftdiConectStatus_e _ConectSt { get; set; }

        public ftdiConectStatus_e ConectSt
        {
            get
            {
                return _ConectSt;
            }
            set
            {
                if (value == ftdiConectStatus_e.ftdiCSt_NotConnect)
                {
                    _ConectSt = value;
                    Data = "Соединение НЕ УСТАНОВЛЕННО";
                    Color = Brushes.Red;
                }
                else if (value == ftdiConectStatus_e.ftdiCSt_Connect)
                {
                    _ConectSt = value;
                    Data = "Соединение УСТАНОВЛЕННО";
                    Color = Brushes.Green;
                }

            }

        }
        public Brush _Color { get; set; }
        public Brush Color
        {
            get
            {
                return _Color;
            }
            set
            {
                _Color = value;
                NotifyPropertyChanged("Color");

            }
        }

        public string _Data { get; set; }
        public string Data
        {
            get
            {
                return _Data;
            }
            set
            {
                if (_Data != value)
                {
                    _Data = value;
                    NotifyPropertyChanged("Data");
                }
            }
        }

        private void NotifyPropertyChanged(string param)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(param));
            }

        }



    }
    #endregion
}
