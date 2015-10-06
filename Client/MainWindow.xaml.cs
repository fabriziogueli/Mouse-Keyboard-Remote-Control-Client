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
using System.Runtime.InteropServices;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {

        private ObservableCollection<Server> items = new ObservableCollection<Server>();
        public Server currentclickedserver { get; set; }

       
        public ClientHook ch { get; set; }

        public WindowItem secondWindow {get; set;}

        public connectWindow cw { get; set; }

        public transparentWin twin { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            ch = new ClientHook();
            ch.Win = this;
            Thread t = new Thread(MyClipboard.InitializeShare);
            t.Start();
            ch.setMouseHook();
            ch.setKeyboardHook();
        }



        /*Read fine server*/
        /*byte[] b1 = new byte[100];
        int k1 = stm.Read(b1, 0, 100);
        for (int i = 0; i < k1; i++)
            Console.Write(Convert.ToChar(b1[i]));
        tcpclnt.Close();*/
        /*        }
                catch (Exception e)
                {
                    Console.WriteLine("Error..... " + e.StackTrace);
                    Console.WriteLine("Error..... " + e.Message);
                }*/



        private void Button_Click_Add(object sender, RoutedEventArgs e)
        {
            if (secondWindow == null)
                secondWindow = new WindowItem();
            else
                secondWindow.Activate();

            secondWindow.mw = this;
            secondWindow.add.Visibility = Visibility.Visible; ;
            secondWindow.update.Visibility = Visibility.Hidden; ;
            secondWindow.Show();


        }

        public void Capturing()
        {
            if (twin == null)
                twin = new transparentWin();

                twin.Topmost = true;
                twin.Activate();              
                twin.Show();
                this.Hide();
            
            
        }

        public void stopCapturing()
        {
             if(twin != null)
             {        
                 this.Activate();
                 this.Show();                
                 twin.Hide();
             }
        }

        private void Button_Click_Connect(object sender, RoutedEventArgs e)
        {

            if ((Server)pcRemote.SelectedItem != null && (((Server)(pcRemote.SelectedItem)).Status.Equals("Connecting...")))
            {
                ((Server)(pcRemote.SelectedItem)).Disconnect();
                return;
            }

            if ((Server)pcRemote.SelectedItem != null && !(((Server)(pcRemote.SelectedItem)).Connected))
            {
                if (cw == null)
                    cw = new connectWindow();
                else
                    cw.Activate();

                cw.mw = this;
                if (ch.LeftServer != null && ch.LeftServer.Connected)
                {
                    cw.leftbuttonmonitor.IsEnabled = false;
                }

                if (ch.RightServer != null && ch.RightServer.Connected)
                {
                    cw.rightbuttonmonitor.IsEnabled = false;
                }

                cw.Show();
            }
            else if ((Server)pcRemote.SelectedItem != null && ((Server)(pcRemote.SelectedItem)).Connected)
            {
                ((Server)(pcRemote.SelectedItem)).Disconnect();
                pcRemote.UnselectAll();
            }
        }


        public void ConnectionHandler(object sender, PropertyChangedExtendedEventArgs<bool> args)
        {
            Dispatcher.Invoke(new Action(() =>
            {
            if (args.PropertyName == "connected")
            {
                if (!((Server)(sender)).Connected)
                {                    
                        stopCapturing();                    
                }

                if(pcRemote.SelectedItem != null && (((Server)(pcRemote.SelectedItem)).Connected))
                {
                    textconnect.Text = "Disconnect";
                }
                else if(pcRemote.SelectedItem != null && !(((Server)(pcRemote.SelectedItem)).Connected))
                {
                    if (((Server)(pcRemote.SelectedItem)).Side == 0)
                        ch.LeftServer = null;
                    else if (((Server)(pcRemote.SelectedItem)).Side == 1)
                        ch.RightServer = null;

                    textconnect.Text = "Connect";
                }
            }
            ICollectionView view = CollectionViewSource.GetDefaultView(items);
            view.Refresh();
                }));    

        }

        public void RefreshListview()
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(items);
            view.Refresh();
        }

        public void AuthFailed()
        {
            System.Windows.Forms.MessageBox.Show("Connessione fallita, riprova!", "Errore", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
        }

        public void addListnewItem(String nickName, String ipAddress, Int16 port, string user, string pass, Boolean Connection)
        {
            Server s = new Server(ipAddress, port, nickName, user, pass);
            s.Status = "Disconnected";
            items.Add(s);
            pcRemote.ItemsSource = items;
        }

        public void connectionProblem(Server s)
        {
            Mouse.OverrideCursor = Cursors.Arrow;
            if(s!= null && s.Side == 0)
            {
                System.Windows.Forms.MessageBox.Show("C'è un problema di connessione con il LeftServer", "Errore", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
            }
            else  if(s!= null && s.Side == 1)
            {
                System.Windows.Forms.MessageBox.Show("C'è un problema di connessione con il RightServer", "Errore", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);

            }
            s.Disconnect();
        }

        private void ListViewItem_OnClick(object sender, MouseButtonEventArgs e)
        {

            var item = sender as ListViewItem;
            if (item != null && item.IsSelected)
            {

                var server = ((ListViewItem)sender).Content as Server;
                currentclickedserver = server;
                if (!buttonconnect.IsEnabled)
                    buttonconnect.IsEnabled = true;

                if (server.Connected || server.Status.Equals("Connecting..."))
                {
                    textconnect.Text = "Disconnect";
                }
                else
                    textconnect.Text = "Connect";
            }
        }

        public void setConnectionHandler(Server s)
        {
            s.PropertyChanged += ConnectionHandler;
        }

        private void Button_Click_delete(object sender, RoutedEventArgs e)
        {
            if ((Server)pcRemote.SelectedItem != null)
            {
                if (!((Server)pcRemote.SelectedItem).Connected)
                {
                    items.Remove((Server)pcRemote.SelectedItem);
                }
                else
                {
                    MessageBoxResult mbr = MessageBox.Show("Questo Server è attualmente connesso, sei sicuro di volerlo cancellare?", "Notifica", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (mbr == MessageBoxResult.Yes)
                    {
                        ((Server)pcRemote.SelectedItem).Disconnect();
                        items.Remove((Server)pcRemote.SelectedItem);
                    }
                }
                if(items.Count == 0)
                    textconnect.Text = "Connect";

            }

        }

        private void Button_Click_modify(object sender, RoutedEventArgs e)
        {
            Server s = (Server)pcRemote.SelectedItem;

            if (s == null)
                return;

            if(s.Connected)
            {
                System.Windows.Forms.MessageBox.Show("Il server che vuoi modificare è attualmente connesso", "Errore", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }

            if (secondWindow == null)
                secondWindow = new WindowItem();
            else
                secondWindow.Activate();

            secondWindow.mw = this;
            secondWindow.tnickname.Text = s.Nickname;
            secondWindow.tip.Text = s.Ip;
            secondWindow.tport.Text = s.Port.ToString();
            secondWindow.tusername.Text = s.Username;
            secondWindow.tpassword.Password = s.Password;
            secondWindow.add.Visibility = Visibility.Hidden; 
            secondWindow.update.Visibility = Visibility.Visible; 
            secondWindow.Show();        
       
        }

        public void updateServer(String nickName, String ipAddress, Int16 port, string user, string pass, Boolean Connection)
        {
            Server s = (Server)pcRemote.SelectedItem;
            s.Nickname = nickName;
            s.Ip = ipAddress;
            s.Username = user;
            s.Password = pass;
            s.Port = port;
            pcRemote.Items.Refresh();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }

    }



}




