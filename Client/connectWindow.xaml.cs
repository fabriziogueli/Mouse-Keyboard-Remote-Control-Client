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
using System.Windows.Shapes;

namespace Client
{
    /// <summary>
    /// Interaction logic for connectWindow.xaml
    /// </summary>
    public partial class connectWindow : Window
    {

        public MainWindow mw { get; set; }
        public connectWindow()
        {
            InitializeComponent();
        }

        public void Button_ClickRight(object sender, RoutedEventArgs e)
        {
            mw.ch.serverConnect(mw.currentclickedserver, true);
            this.Close();
        }

        public void Button_ClickLeft(object sender, RoutedEventArgs e)
        {
            mw.ch.serverConnect(mw.currentclickedserver, false);
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mw.pcRemote.UnselectAll();
            mw.cw = null;
        }

    }
}
