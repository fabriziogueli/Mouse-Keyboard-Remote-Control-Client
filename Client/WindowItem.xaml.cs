using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Client
{
    /// <summary>
    /// Interaction logic for WindowItem.xaml
    /// </summary>
    public partial class WindowItem : Window
    {

        public MainWindow mw;
        public WindowItem()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (CheckData())
            {
                Console.WriteLine(tusername.Text + "  " + tpassword.Password);
                mw.addListnewItem(tnickname.Text, tip.Text, Int32.Parse(tport.Text), tusername.Text, tpassword.Password, false);
                this.Close();
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Inserisci tutti i campi correttamente", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mw.secondWindow = null;

        }


        private void Button_Click_update(object sender, RoutedEventArgs e)
        {
            if (CheckData())
            {
                Console.WriteLine(tusername.Text + "  " + tpassword.Password);
                mw.updateServer(tnickname.Text, tip.Text, Int32.Parse(tport.Text), tusername.Text, tpassword.Password, false);
                this.Close();
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Inserisci tutti i campi correttamente", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private bool CheckData()
        {
            IPAddress ipadd;
            if (System.Net.IPAddress.TryParse(tip.Text, out ipadd) && tport.Text.All(char.IsDigit) && Int32.Parse(tport.Text) <= 65535
               && !String.IsNullOrEmpty(tnickname.Text) && !String.IsNullOrEmpty(tusername.Text) && !String.IsNullOrEmpty(tpassword.Password) && !String.IsNullOrEmpty(tport.Text))
            {
                return true;
            }
            else
            {
                return false;
            }

        }
    }
}
