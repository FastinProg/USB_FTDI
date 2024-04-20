using FTDI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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
using USB_FTDI.FTDI_Logic;
using USB_FTDI;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using RTools;

namespace USB_FTDI.View
{
    /// <summary>
    /// Логика взаимодействия для Termainal.xaml
    /// </summary>
    public partial class Termainal : UserControl
    {
        private DispatcherTimer tim;
        private FTDI_Hardware FTDI;
        private Semaphore writingInTb = new Semaphore(0, 1);
        private Thread FTDI_ReadData;
        private FileStream fileStream;
        private StreamWriter stremWriter;
        private IntPtr p;
        private bool AlocateIsCreate = false;
        // Флаги работы потока
        private bool rxThreadAlive = true;
        // Экземпляры привзяки
        ConnectUpdate ConnectUpdate = new ConnectUpdate();

        public Termainal()
        {
            InitializeComponent();
            this.Loaded += Termainal_Loaded;
            this.Unloaded += Termainal_Unloaded;
        }

        private void Termainal_Unloaded(object sender, RoutedEventArgs e)
        {
            rxThreadAlive = false;
            stremWriter.Close();
            fileStream.Close();
            Marshal.FreeHGlobal(p);
        }

        private void Termainal_Loaded(object sender, RoutedEventArgs e)
        {
            // Привязка
            tblConnectStatus.DataContext = ConnectUpdate;
            ConnectUpdate.ConectSt = ftdiConectStatus_e.ftdiCSt_NotConnect;

            // Настройка драйвера
            FTDI = new FTDI_Hardware();
            //FTDI.OnReadMessage += ReciveMsg;

            // Настройка таймера
            tim = new DispatcherTimer();
            tim.Interval = TimeSpan.FromMilliseconds(100); ;
            tim.Tick += Tim_Elapsed;
            tim.Start();

            // Create direcoty for records log
            string currentDirectoryPath = Environment.CurrentDirectory;
            currentDirectoryPath += @"\LOG\";
            // Create new directory, if it need
            if (!Directory.Exists(currentDirectoryPath))
                Directory.CreateDirectory(currentDirectoryPath);

            string currentTime = DateTime.Now.ToString();
            currentTime = currentTime.Replace(' ', '_');
            currentTime = currentTime.Replace(':', '_');

            currentDirectoryPath += currentTime + ".txt";
            fileStream = new FileStream(currentDirectoryPath, FileMode.CreateNew);
            stremWriter = new StreamWriter(fileStream, Encoding.UTF8);
            p = Marshal.AllocHGlobal(8);
            writingInTb.Release();
            FTDI_ReadData = new Thread(ReciveMsg);
            FTDI_ReadData.Start();
        }

        private void Tim_Elapsed(object sender, EventArgs e)
        {
            //  throw new NotImplementedException();
        }

        public void ReciveMsg()
        {
            // initialize memmory
            FTDI_Data_t data = new FTDI_Data_t();
            CanOpen_t msg = new CanOpen_t();

            while (rxThreadAlive)
            {
                // FTDI.eventWait.WaitOne(50);
                while (FTDI.RX_FTDI_Queue.FTDI_TxQueue_IsEmpty() == 0)
                {
                    FTDI.RX_FTDI_Queue.FTDI_TxQueue_ReadMsg(ref data);


                    // We can casting and type conversions to CanOpe_t
                    if (data.Lenght == 8)
                    {
                        Marshal.Copy(data.data, 0, p, 8);
                        msg = (CanOpen_t)Marshal.PtrToStructure(p, typeof(CanOpen_t));

                        // Непрерывное чтение данных, необходимо сохранить в текстовый файл
                        
                        if (msg.cmd == (byte)SDO_Answer.sdoAns_DataContinuos)
                        {
                            msg.data &= 0xffffff;                       // 24 bit

                            if (msg.index == (ushort)SDO_ParamIndex_e.sdoReadContinunuos)
                            {
                                Int32 value = Convert.ToInt32(msg.data.ToString("G"));
                                // It is negative amount
                                if ((value & 0x800000) != 0)
                                    value |= (0xff << 24);

                                double rez = (double)(value * (0.000000286));
                                //double rez = (2.4 / value) + (2^23);

                                if (msg.subindex == 0)
                                    stremWriter.Write("\n" + rez + "\t");
                                else
                                    stremWriter.Write(rez + "\t"); 

                            }
                        }
                        else if (msg.cmd == (byte)SDO_Answer.sdoAns_SuccessfulRead || msg.cmd == (byte)SDO_Answer.sdoAns_SuccessfulRead)
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                writingInTb.WaitOne();
                                string str = string.Empty;
                                string substr = string.Empty;
                                try { str = tbRecieve.Text; }
                                catch { }

                                for (UInt32 i = 0; i < data.Lenght - 1; i++)
                                    substr += "0x" + data.data[i].ToString("X") + " ";

                                if (str.Length == 0)
                                    str += substr;
                                else if (str[str.Length - 1] != '\n')
                                    str += " " + substr;
                                else
                                    str += substr;

                                tbRecieve.Text = str + "\n";
                                writingInTb.Release();
                            }));

                        }

                    }

                }
            }
        }
      



        private void btConnect_Click(object sender, RoutedEventArgs e)
        {
            AsynkButtonClick();
        }

        async void AsynkButtonClick()
        {
            btConnect.IsEnabled = false;

            Task task1 = new Task(() =>
            {

            });

            await Task.Run(() =>
            {
                FTDI.Connect();
                task1.Wait(1000);
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    btConnect.IsEnabled = true;
                    if (FTDI.GetFTDIStatus() == FTD2XX_NET.FTDI.FT_STATUS.FT_OK)
                        ConnectUpdate.ConectSt = ftdiConectStatus_e.ftdiCSt_Connect;
                }));
            });

        }

        private void btClearReciveTerminal_Click(object sender, RoutedEventArgs e)
        {
            writingInTb.WaitOne();
            tbRecieve.Text = string.Empty;
            writingInTb.Release();
        }

        private void btSendTx_Click(object sender, RoutedEventArgs e)
        {
            /*   byte[] data = new byte[8];
               for (byte i = 0; i < 8; i++)
                   data[i] = 0x1;
               FTDI.addTxMessengeInQueue(data, 8);*/
            string str = string.Empty;
            try
            {
                str = tboxTx.Text;

                byte[] data = new byte[20];
                int index_data = 0;
                char[] charSeparators = new char[] { '&' };
                string[] lines = str.Split(charSeparators, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    data[index_data] = Byte.Parse(line, System.Globalization.NumberStyles.HexNumber);
                    index_data++;
                }
                FTDI.addTxMessengeInQueue(data, (byte)index_data);

                /*
                 * На крайняк по указателям
                 * можно распарсить
                                unsafe
                                {
                                    fixed(char* pointer = str)
                                    {
                                        char* p = pointer;
                                        int cnt = strlenght;
                                        while (cnt != 0)
                                        {
                                            if (*p == '&')
                                            { 
                                             }
                                        }

                                    }

                                }
                */
            }
            catch
            {
                str = string.Empty;
            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // FTDI.CloseFile();
        }
    }
}
