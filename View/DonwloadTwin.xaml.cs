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

namespace USB_FTDI.View
{
    /// <summary>
    /// Логика взаимодействия для DonwloadTwin.xaml
    /// </summary>
    public partial class DonwloadTwin : Window
    {
        public DonwloadTwin()
        {
            InitializeComponent();
        }

        private void ButtonNewTwin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton)
            {
                clickedButton.Tag = "Selected";
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Узнаем путь к файлу
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {

            }
        }
    }
}
