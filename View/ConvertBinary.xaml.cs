using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using USB_FTDI;

namespace SDReaderBinaryConvector
{
    /// <summary>
    /// Логика взаимодействия для ConvertBinary.xaml
    /// </summary>
    public partial class ConvertBinary : UserControl
    {
        FileProperty myFile = new FileProperty();
        const Int16 BYTE_IN_CARDIO_CYCLE = 32;                 // 4 байта на канал
        const Int16 AMOUNT_CHANNEL = 8;                                // Колиество каналов

        public ConvertBinary()
        {
            InitializeComponent();
        }

        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {
            Global.MainControl.Children.Remove(this);
        }

        private void ChoiseFile_Click(object sender, RoutedEventArgs e)
        {
            // Узнаем путь к файлу
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                myFile.InputFileName = openFileDialog.SafeFileName;
                myFile.InputFilePath = openFileDialog.FileName;
                myFile.InputFileSize = Convert.ToString(new FileInfo(openFileDialog.FileName).Length) + "\tбайт";

            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text file (*.txt)|*.txt|C# file (*.cs)|*.cs";

            if (saveFileDialog.ShowDialog() == true) 
            {
              
                myFile.OutputFileName = saveFileDialog.SafeFileName;
                myFile.OutputFilePath = saveFileDialog.FileName;
            }
            System.IO.File.WriteAllText(myFile.OutputFilePath, "Привет");
            
            this.DataContext = myFile;
            // Создаем поток для чтения
            using (FileStream inputFileRead = new FileStream(myFile.InputFilePath, FileMode.Open, FileAccess.Read))
            {
                // Создаем поток для чтение бинарников
                using (BinaryReader binaryReader = new BinaryReader(inputFileRead, Encoding.UTF8))
                {
                    using (FileStream outputFileWrite = new FileStream(myFile.OutputFilePath, FileMode.Open, FileAccess.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter(outputFileWrite))
                        {
                            // Создали шапку
                            string Title = string.Empty;
                            for (int i = 0; i < AMOUNT_CHANNEL; i++)
                                Title += "Канал номер:" + i.ToString() + "\t";
                            streamWriter.WriteLine(Title);

                            Int64 sizeInputFilev = inputFileRead.Length;    // Определяем длину в байтах
                            Int64 cntByte = 0;                             // Счетчик считаных байтов

                            // Парсер
                            // Записываем только кардиоцикл целиком, по 64 канала, иначе не пишем
                            while ((cntByte + BYTE_IN_CARDIO_CYCLE) <= sizeInputFilev)
                            {
                                Byte[] masiv = new byte[BYTE_IN_CARDIO_CYCLE];                   // Локальный массив для считанных байтов
                                binaryReader.Read(masiv, 0, BYTE_IN_CARDIO_CYCLE);               // считали в массив один кардиоцикол по 4 байта на канал
                                
                                // Парсим массив в строку
                                string teststring = string.Empty;
                                for (int i = 0; i < BYTE_IN_CARDIO_CYCLE; i+=4)
                                {
                                    Int32 value = BitConverter.ToInt32(masiv,i);        // Складываем 4 байта из массива в перменную
                                    teststring += value.ToString() + "\t";              // Записываем в строку
                                }
                                streamWriter.WriteLine(teststring);
                                cntByte += BYTE_IN_CARDIO_CYCLE;
                            }
                            MessageBox.Show("успех");
                        }

                    }
                }
            }
        }
    }
    // Класс для привязки 
    public class FileProperty
    {
        public string InputFilePath { get; set; }            // Путь к входному файлу
        public string InputFileName { get; set; }            // Имя входного файла
        public string InputFileSize { get; set; }            // Размер входного файла

        public string OutputFilePath { get; set; }            // Путь к выходному файлу
        public string OutputFileName { get; set; }            // Имя выходного файла
        public string OutputFileSize { get; set; }            // Размер выходного файла   
    }
}
