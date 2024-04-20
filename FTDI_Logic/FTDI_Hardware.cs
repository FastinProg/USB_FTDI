//#define TEST

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using FTD2XX_NET;
using USB_FTDI.FTDI_Logic;

namespace FTDI
{
    public enum ftdiConectStatus_e { ftdiCSt_NotConnect, ftdiCSt_Connect };

    class FTDI_Hardware
    {
        #region Поля класса
        private int FTDI_ARRAY_LENGHT = 10000;
        // Потоки для отправки и приема сообщений
        private Thread txThread;
        private Thread rxThread;
        private Thread ActionThread;                                            // Поток для генерации событий
        private Thread rxParserThread;                                          // Парсер входного буфера на пакеты

        // Флаги работы потока
        private bool rxThreadAlive = true;
        private bool txThreadAlive = true;
        private bool rxParserTreadAlive = true;

        // Локеры для потоков
        private object txlocker = new object();
        private object rxlocker = new object();

        // Задержка
        private EventWaitHandle rxWait = new AutoResetEvent(false);
        public EventWaitHandle eventWait = new AutoResetEvent(false);
        private EventWaitHandle rxBufNewVal = new AutoResetEvent(false);
        private EventWaitHandle txBufNewVal = new AutoResetEvent(false);


        // Очередь на принятых сообщений
        public FTDI_Queue_t RX_FTDI_Queue;
        public FTDI_Queue_t TX_FTDI_Queue;
        public FTDI_Queue_t Action_FTDI_Queue;

        public Queue<Array> rxBufQueue = new Queue<Array>(1000);

        private UInt32 ftdiDeviceCount;                                                                         // Номер устройства
        private FTD2XX_NET.FTDI myFtdiDevice;                                                                   // Экземпляр класса, описывающий устройство
        private static FTD2XX_NET.FTDI.FT_STATUS ftStatus = FTD2XX_NET.FTDI.FT_STATUS.FT_OK;                    // Текущий статус

        public Action CloseFile;

        #endregion


        public enum FTDI_Hardware_Status_e
        {
            ftdiSt_OK,
            ftdiSt_DeviceNumberError,
            ftdiSt_DeviceInfoError,
            ftdiSt_DeviceOpenError,
            ftdiSt_DeviceSpeedSetingError,
            ftdiSt_DeviceSettingError,
            ftdiSt_DeviceFlowControlError,
            ftdiSt_DeviceReadWriteTimeout,
        };

        // Конструкутор
        public FTDI_Hardware()
        {
            // Инициализация очереди
            // Прием
            RX_FTDI_Queue = new FTDI_Queue_t();
            RX_FTDI_Queue.dataPACK = new FTDI_Data_t[RX_FTDI_Queue.GetLenghtQueue()];
            for (int i = 0; i < RX_FTDI_Queue.GetLenghtQueue(); i++)
                RX_FTDI_Queue.dataPACK[i].data = new byte[RX_FTDI_Queue.GetLenghtPack()];
            RX_FTDI_Queue.locker = new object();

            // Отправка
            TX_FTDI_Queue = new FTDI_Queue_t();
            TX_FTDI_Queue.dataPACK = new FTDI_Data_t[TX_FTDI_Queue.GetLenghtQueue()];
            for (int i = 0; i < TX_FTDI_Queue.GetLenghtQueue(); i++)
                TX_FTDI_Queue.dataPACK[i].data = new byte[TX_FTDI_Queue.GetLenghtPack() + 2];
            TX_FTDI_Queue.locker = new object();

            // Queue designs for synchronize obtained data with even registrarion
            // Очередь созданная для синхронизации полученных данных с событием приема

            Action_FTDI_Queue = new FTDI_Queue_t();
            Action_FTDI_Queue.dataPACK = new FTDI_Data_t[RX_FTDI_Queue.GetLenghtQueue()];
            for (int i = 0; i < Action_FTDI_Queue.GetLenghtQueue(); i++)
                Action_FTDI_Queue.dataPACK[i].data = new byte[Action_FTDI_Queue.GetLenghtPack()];
            Action_FTDI_Queue.locker = new object();

            myFtdiDevice = new FTD2XX_NET.FTDI();
            ftdiDeviceCount = 0;
        }

        // Установка соединения
        public FTDI_Hardware_Status_e Connect()
        {

            // Определяем количество подключенных устройств FTDI
            ftStatus = myFtdiDevice.GetNumberOfDevices(ref ftdiDeviceCount);

            // Проверка статуса
            if (ftStatus != FTD2XX_NET.FTDI.FT_STATUS.FT_OK)
                return FTDI_Hardware_Status_e.ftdiSt_DeviceNumberError;


            // Проверка кол-ва устройств
            if (ftdiDeviceCount == 0)
            {
                ftStatus = FTD2XX_NET.FTDI.FT_STATUS.FT_DEVICE_NOT_OPENED;
                return FTDI_Hardware_Status_e.ftdiSt_DeviceNumberError;
            }
            // Создаем массив с информацией об устройстве
            FTD2XX_NET.FTDI.FT_DEVICE_INFO_NODE[] ftdiDeviceList = new FTD2XX_NET.FTDI.FT_DEVICE_INFO_NODE[ftdiDeviceCount];

            // Заполняем список устройств
            ftStatus = myFtdiDevice.GetDeviceList(ftdiDeviceList);

            // Проверка после заполнения информации об устройстве
            if (ftStatus == FTD2XX_NET.FTDI.FT_STATUS.FT_OK)
            {
                // Можно вывести эту инфу
                /*
				for (UInt32 i = 0; i < ftdiDeviceCount; i++)
				{
					Console.WriteLine("Device Index: " + i.ToString());
					Console.WriteLine("Flags: " + String.Format("{0:x}", ftdiDeviceList[i].Flags));
					Console.WriteLine("Type: " + ftdiDeviceList[i].Type.ToString());
					Console.WriteLine("ID: " + String.Format("{0:x}", ftdiDeviceList[i].ID));
					Console.WriteLine("Location ID: " + String.Format("{0:x}", ftdiDeviceList[i].LocId));
					Console.WriteLine("Serial Number: " + ftdiDeviceList[i].SerialNumber.ToString());
					Console.WriteLine("Description: " + ftdiDeviceList[i].Description.ToString());
					Console.WriteLine("");
				}
				*/
            }
            else
                return FTDI_Hardware_Status_e.ftdiSt_DeviceInfoError;

            // Открываем COM Port
            ftStatus = myFtdiDevice.OpenBySerialNumber(ftdiDeviceList[0].SerialNumber);

            // Проверка
            if (ftStatus != FTD2XX_NET.FTDI.FT_STATUS.FT_OK)
            {
                ftStatus = FTD2XX_NET.FTDI.FT_STATUS.FT_DEVICE_NOT_OPENED;
                return FTDI_Hardware_Status_e.ftdiSt_DeviceOpenError;
            }
            ftStatus = myFtdiDevice.SetBaudRate(1000000);
            ftStatus = myFtdiDevice.SetEventNotification(FTD2XX_NET.FTDI.FT_EVENTS.FT_EVENT_RXCHAR, rxWait);
            ftStatus = myFtdiDevice.SetLatency(20);
            if (ftStatus != FTD2XX_NET.FTDI.FT_STATUS.FT_OK)
                return FTDI_Hardware_Status_e.ftdiSt_DeviceSpeedSetingError;

            // Установка параметров
            ftStatus = myFtdiDevice.SetDataCharacteristics(FTD2XX_NET.FTDI.FT_DATA_BITS.FT_BITS_8, FTD2XX_NET.FTDI.FT_STOP_BITS.FT_STOP_BITS_1, FTD2XX_NET.FTDI.FT_PARITY.FT_PARITY_NONE);
            if (ftStatus != FTD2XX_NET.FTDI.FT_STATUS.FT_OK)
                return FTDI_Hardware_Status_e.ftdiSt_DeviceSettingError;

            // Set flow control - set RTS/CTS flow control 
            ftStatus = myFtdiDevice.SetFlowControl(FTD2XX_NET.FTDI.FT_FLOW_CONTROL.FT_FLOW_RTS_CTS, 0x11, 0x13);
            if (ftStatus != FTD2XX_NET.FTDI.FT_STATUS.FT_OK)
                return FTDI_Hardware_Status_e.ftdiSt_DeviceFlowControlError;


            // Set read timeout to 5 seconds, write timeout to infinite
            ftStatus = myFtdiDevice.SetTimeouts(1000, 1000);
            if (ftStatus != FTD2XX_NET.FTDI.FT_STATUS.FT_OK)
                return FTDI_Hardware_Status_e.ftdiSt_DeviceReadWriteTimeout;

            myFtdiDevice.Purge(FTD2XX_NET.FTDI.FT_PURGE.FT_PURGE_RX);

            rxThread = new Thread(recieve_tread);
            txThread = new Thread(txThradMethod);
            rxParserThread = new Thread(rxParser);

            rxParserThread.Priority = ThreadPriority.Highest;
            rxThread.Priority = ThreadPriority.Highest;

            rxParserThread.Start();
            rxThread.Start();
            //ActionThread.Start();
            txThread.Start();

            // Успешная установка соединения
            return FTDI_Hardware_Status_e.ftdiSt_OK;
        }

        private void addStartStopBit(ref Byte[] data, ref Byte size)
        {
            byte[] temp = new byte[size + 2];
            Array.Copy(data, 0, temp, 1, size);
            temp[0] = FTDI_Queue_t.StartPack;
            temp[size + 1] = FTDI_Queue_t.EndPack;
            data = temp;
            size += 2;
        }

        public void addTxMessengeInQueue(Byte[] data, Byte size)
        {
            lock (txlocker)
            {
                addStartStopBit(ref data, ref size);
                TX_FTDI_Queue.FTDI_TxQueue_WriteMsg(data, size);
                txBufNewVal.Set();
            }
        }

        public void txThradMethod()
        {
            FTDI_Data_t data = new FTDI_Data_t();
            data.data = new byte[64];
            data.Lenght = 0;

            while (txThreadAlive)
            {
                txBufNewVal.WaitOne(1000);
                lock (txlocker)
                {
                    if (TX_FTDI_Queue.FTDI_TxQueue_IsEmpty() == 0)
                    {
                        uint numWriteBute = 0;
                        TX_FTDI_Queue.FTDI_TxQueue_ReadMsg(ref data);
                        numWriteBute = data.Lenght;
                        myFtdiDevice.Write(data.data, data.Lenght, ref numWriteBute);
                    }
                }
            }
        }

        public void rxParser()
        {
            bool OK_PARS = false;
            bool ParsingIsActive = false;
            bool DataIsProcessed = false;
            int Index_Start_Byte = -1;
            //byte[] data = new byte[65536];
            byte[] data = new byte[6553600];
            byte[] pre_data = new byte[10000];
            int lenght = 0;
            int left_pointer = 0;
            int right_pointer = 0;
            int currentLenght = 0;

            while (rxParserTreadAlive)
            {
                rxBufNewVal.WaitOne(100);
                byte[] buf = new byte[RX_FTDI_Queue.GetLenghtPack()];       // Массив в которой считываем данные
                lock (rxlocker)
                {
                    if (rxBufQueue.Count >= 1)
                    {
                        // Копируем данные во временный буфер
                        Array new_data = rxBufQueue.Dequeue();
                        // Копируем из временного буфера в постоянный
                        Array.Copy(new_data, 0, data, right_pointer, new_data.Length);
                        // смещаем правый указатель на границу полученного массива
                        right_pointer = right_pointer + new_data.Length - 1;                 // - 1 т.к. индекс

                        DataIsProcessed = true;

                        // Пока не обработали этот массив, не берем следующий
                        while (DataIsProcessed == true)
                        {
                            if (ParsingIsActive == false)
                            {
                                // Ищем индекс между границей указателей
                                // проверить нулевую и отприцательную границу
                                //currentLenght = right_pointer - left_pointer;   // + 1 т.к. длина

                                Index_Start_Byte = Array.IndexOf(data, RX_FTDI_Queue.GetStartByte(), left_pointer, right_pointer - left_pointer + 1);
                                if (Index_Start_Byte != -1)
                                {
                                    ParsingIsActive = true;
                                }
                                // Если не нашли стартовый байт обнулем указатель, ждем новых данных
                                else
                                {
                                    DataIsProcessed = false;
                                    left_pointer = 0;
                                    right_pointer = 0;
                                }
                            }
                            // Если начали парсить пакет, ждем минимальной длины пакета, провереям стоповый байт
                            if (ParsingIsActive == true)
                            {
                                // Проверка длины на возможность найти пакет
                                if ((right_pointer - Index_Start_Byte) >= (RX_FTDI_Queue.GetLenghtPack() + 1))
                                {
                                    ParsingIsActive = false;
                                    int indexEndPack = (int)(RX_FTDI_Queue.GetLenghtPack() + Index_Start_Byte + 1);
                                    if ((Byte)data.GetValue(indexEndPack) == RX_FTDI_Queue.GetStopByte())
                                    {
                                        // Сдвигаем указатель
                                        left_pointer += RX_FTDI_Queue.GetLenghtPack() + 2;  // + 2 
                                        currentLenght = right_pointer - left_pointer + 1;   // + 1 т.к. длина
                                                                                            // Добавляем в очередь без стартовых и стоповых
                                        Array.Copy(data, Index_Start_Byte + 1, buf, 0, RX_FTDI_Queue.GetLenghtPack());
                                        RX_FTDI_Queue.FTDI_TxQueue_WriteMsg(buf, (byte)(RX_FTDI_Queue.GetLenghtPack()));
                                        eventWait.Set();
                                        OK_PARS = true;
                                    }
                                    else
                                    {
                                        OK_PARS = false;
                                        ;
                                    }

                                    // если левый указатель не равен правому, то еще есть данные
                                    if (left_pointer <= right_pointer)
                                    {
                                        // если неудачно распарсили, сдвигаем левый указатель на еденицу, а не на размер пакета
                                        if (OK_PARS == false)
                                            left_pointer++;
                                    }
                                    else
                                    {
                                        DataIsProcessed = false;
                                        left_pointer = 0;
                                        right_pointer = 0;
                                    }
                                }
                                // Недостаточно длины, берем следующий массив и ищем там стоп байт
                                else
                                {
                                    // Array.Copy(new_data, 0, pre_data,0 , new_data.Length);
                                    ParsingIsActive = false;
                                    DataIsProcessed = false;
                                    right_pointer++;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void recieve_tread()
        {
            uint numBytesAvailable = 0;             // Доступное кол-во байт для чтения
            uint numBytesRead = 0;                  // Кол-во прочитанных байт

            while (rxThreadAlive)
            {
                rxWait.WaitOne(100);
                ftStatus = myFtdiDevice.GetRxBytesAvailable(ref numBytesAvailable);

                if (numBytesAvailable > 1)
                {
                    byte[] local_buf = new byte[numBytesAvailable];
                    myFtdiDevice.Read(local_buf, numBytesAvailable, ref numBytesRead);

                    lock (rxlocker)
                    {
                        rxBufQueue.Enqueue(local_buf);
                    }
                    rxBufNewVal.Set();
                }
            }
        }

        public FTDI_Data_t GetAvalibleData()
        {
            FTDI_Data_t data = new FTDI_Data_t();
            RX_FTDI_Queue.FTDI_TxQueue_ReadMsg(ref data);
            return data;

        }

        public FTD2XX_NET.FTDI.FT_STATUS GetFTDIStatus()
        {
            return ftStatus;
        }

    }
}

