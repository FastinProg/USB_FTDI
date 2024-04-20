using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using RTools;
using System.Runtime.CompilerServices;

namespace USB_FTDI.FTDI_Logic
{
	/// <summary>
	/// Тип данных, готовыъ пакетов
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	unsafe public struct FTDI_Data_t
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
		public byte[] data;
		public UInt32 Lenght;
	};

	/// <summary>
	/// Очередь
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	unsafe public struct FTDI_Queue_t
	{
		public Object locker;
		// Настройка драйвера и пртокола
		public  const int FTDI_TxQueueMaxSize = 5000;
        public const byte LenghtPack = 8;				
        public const Byte StartPack = 0x78;
		public const Byte EndPack = 0x23;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128 * FTDI_TxQueueMaxSize)]
        public FTDI_Data_t[] dataPACK;        // Масив готовых пакетов

		public int HeadPack;         // Количество готовых для чтения пакетов
		public int TailPack;         // Количество прочитанных сообщений 	
        


		// Запись указанного сообщения в очередь
		public int FTDI_TxQueue_WriteMsg(Array buf, Byte size)
		{
			lock (locker)
			{
                int next = this.HeadPack + 1;
                if (next >= FTDI_TxQueueMaxSize)
                    next = 0;

                if (next == this.TailPack)     // Буфер полный
                    return 0;

                Array.Copy(buf, this.dataPACK[this.HeadPack].data, (int)size);
                this.dataPACK[this.HeadPack].Lenght = size;
                this.HeadPack = (byte)next;
                return 1;
            }
		}

		// Чтение из очереди исходящего сообщения
		//	return 0 - ошибка
		//	return 1 -коректео
		public int FTDI_TxQueue_ReadMsg(ref FTDI_Data_t buf)
		{
			lock (locker)
			{
                if (this.HeadPack == this.TailPack)       // Буфер пустой
                    return 0;

                int next = this.TailPack + 1;
                if (next >= FTDI_TxQueueMaxSize)
                    next = 0;

                buf = this.dataPACK[this.TailPack];
                this.TailPack = (byte)next;
                return 1;
            }
			
		}

		// return 1 - пустой
		// return 0 - не пустой
		public int FTDI_TxQueue_IsEmpty()
		{
			lock (locker)
			{
                if (HeadPack == TailPack)       // Буфер пустой
                    return 1;
                else
                    return 0;
            }
		}

		// return 1 - переполнен
		// return 0 - не переполнен
		public int FTDI_TxQueue_IsFull()
		{
			lock (locker)
			{
                int next = HeadPack + 1;
                if (next >= FTDI_TxQueueMaxSize)
                    next = 1;

                if (next == TailPack)     // Буфер полный
                    return 1;
                else
                    return 0;
            }
            
		}

		public byte GetStartByte()
		{
			return StartPack;
		}

		public byte GetStopByte()
		{
			return EndPack;
		}

		public byte GetLenghtPack()
		{
			return LenghtPack;
		}

		public int GetLenghtQueue()
		{
			return FTDI_TxQueueMaxSize;
		}
	};
}
