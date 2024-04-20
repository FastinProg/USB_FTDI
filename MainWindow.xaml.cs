using FTDI;
using SDReaderBinaryConvector;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using USB_FTDI.View;

namespace USB_FTDI
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;

        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Global.MainControl = MainGrid;
            //Global.MainstackPanel = MainStackPanel;
        }
        private void ButtonConectToDevice_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonChoiseForm_Click(object sender, RoutedEventArgs e)
        {
            UIElement ul = null;
            Button bt = (Button)sender;
            switch (bt.Name)
            {
                case "ButtonConvert":
                    ul = new ConvertBinary();
                    break;

                case "ButtonConectToDevice":
                    ul = new ConectToDevice();
                    break;
                case "ButtonTermianl":
                    ul = new Termainal();
                    break;
                case "ButtonParsing":
                    ul = new DataShow();
                    break;

                default:
                    return;
            }
            Global.AddForm(ul);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Global.BackToForm();
        }
    }
}
