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

        public WindowItem secondWindow { get; set; }

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
            Dispatcher.Invoke(new Action(() =>
            {
                if (twin == null)
                    twin = new transparentWin();

                twin.Topmost = true;
                twin.Activate();
                twin.Show();
                this.Hide();
            }));
        }

        public void stopCapturing()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                Mouse.OverrideCursor = Cursors.Arrow;
                if (twin != null)
                {
                    this.Activate();
                    this.Show();
                    twin.Hide();
                }
            }));
        }

        private void Button_Click_Connect(object sender, RoutedEventArgs e)
        {

            if ((Server)pcRemote.SelectedItem != null && (((Server)(pcRemote.SelectedItem)).Status == 2))
            {
           //     ((Server)(pcRemote.SelectedItem)).Disconnect();
                return;
            }

            if ((Server)pcRemote.SelectedItem != null && (((Server)(pcRemote.SelectedItem)).Status == 0))
            {
                if (cw == null)
                    cw = new connectWindow();
                else
                    cw.Activate();

                cw.mw = this;
                if (ch.LeftServer != null && (ch.LeftServer.Status == 1 || ch.LeftServer.Status == 2))
                {
                    cw.leftbuttonmonitor.IsEnabled = false;
                }

                if (ch.RightServer != null && (ch.RightServer.Status == 1 || ch.RightServer.Status == 2))
                {
                    cw.rightbuttonmonitor.IsEnabled = false;
                }

                cw.Show();
            }
            else if ((Server)pcRemote.SelectedItem != null && (((Server)(pcRemote.SelectedItem)).Status == 1))
            {
                ((Server)(pcRemote.SelectedItem)).Disconnect(false);
                pcRemote.UnselectAll();
            }
        }


        public void ConnectionHandler(object sender, PropertyChangedExtendedEventArgs<int> args)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (args.PropertyName == "connected")
                {
                    switch (((Server)sender).Status)
                    {
                        case 0: ((Server)sender).WinStatus = "Disconnected"; break;
                        case 1: if (((Server)sender).Side == 0)
                                ((Server)sender).WinStatus = "Connected as LeftServer";

                            else if (((Server)sender).Side == 1)
                                ((Server)sender).WinStatus = "Connected as RightServer";
                            break;
                        case 2: ((Server)sender).WinStatus = "Connecting..."; break;
                    }

                    if (cw != null && cw.IsVisible)
                    {
                        if (ch.LeftServer != null)
                        {
                            if (ch.LeftServer.Status == 1 || ch.LeftServer.Status == 2)
                                cw.leftbuttonmonitor.IsEnabled = false;
                            else if (ch.LeftServer.Status == 0)
                                cw.leftbuttonmonitor.IsEnabled = true;
                        }

                        if (ch.RightServer != null)
                        {
                            if ((ch.RightServer.Status == 1 || ch.RightServer.Status == 2))
                                cw.rightbuttonmonitor.IsEnabled = false;
                            else if (ch.RightServer.Status == 0)
                                cw.rightbuttonmonitor.IsEnabled = true;
                        }

                    }

                    if (((Server)(sender)).Status == 0)
                    {
                        stopCapturing();
                        if (((Server)(sender)).Side == 0)
                            ch.LeftServer = null;
                        else if (((Server)(sender)).Side == 1)
                            ch.RightServer = null;
                    }

                    if (pcRemote.SelectedItem != null && (((Server)(pcRemote.SelectedItem)).Status == 1))
                    {
                        buttonconnect.IsEnabled = true;
                        textconnect.Text = "Disconnect";
                        textconnect.Foreground = new SolidColorBrush(Colors.White);
                    }
                    else if (pcRemote.SelectedItem != null && (((Server)(pcRemote.SelectedItem)).Status == 2))
                    {
                        textconnect.Text = "Connecting";
                        textconnect.Foreground = new SolidColorBrush(Colors.Black);
                        buttonconnect.IsEnabled = false;

                    }
                    else if (pcRemote.SelectedItem != null && (((Server)(pcRemote.SelectedItem)).Status == 0))
                    {
                        buttonconnect.IsEnabled = true;
                        textconnect.Text = "Connect";
                        textconnect.Foreground = new SolidColorBrush(Colors.White);
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

        public void addListnewItem(String nickName, String ipAddress, Int32 port, string user, string pass, Boolean Connection)
        {
            Server s = new Server(ipAddress, port, nickName, user, pass);
            items.Add(s);
            pcRemote.ItemsSource = items;
        }



        public void connectionProblem(Server s)
        {
            if (s.Status != 0)
            {
                s.Disconnect(false);
                stopCapturing();
                Dispatcher.Invoke(new Action(() =>
               {
                   if (s != null && s.Side == 0)
                   {
                       System.Windows.Forms.MessageBox.Show("C'è un problema di connessione con il LeftServer", "Errore", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                   }
                   else if (s != null && s.Side == 1)
                   {
                       System.Windows.Forms.MessageBox.Show("C'è un problema di connessione con il RightServer", "Errore", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);

                   }

               }));
            }
        }

        private void ListViewItem_OnClick(object sender, MouseButtonEventArgs e)
        {


            var item = sender as ListViewItem;
            if (item != null && item.IsSelected)
            {

                var server = ((ListViewItem)sender).Content as Server;
                currentclickedserver = server;


                if (server.Status == 1)
                {
                    textconnect.Text = "Disconnect";
                    textconnect.Foreground = new SolidColorBrush(Colors.White);
                    buttonconnect.IsEnabled = true;
                }
                else if (server.Status == 2)
                {
                    textconnect.Text = "Connecting";
                    textconnect.Foreground = new SolidColorBrush(Colors.Black);
                    buttonconnect.IsEnabled = false;
                }

                else if (server.Status == 0)
                {
                    textconnect.Text = "Connect";
                    textconnect.Foreground = new SolidColorBrush(Colors.White);
                    buttonconnect.IsEnabled = true;
                }

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
                if (((Server)pcRemote.SelectedItem).Status == 0)
                {
                    items.Remove((Server)pcRemote.SelectedItem);
                }
                else
                {
                    MessageBoxResult mbr = MessageBox.Show("Questo Server è attualmente connesso, sei sicuro di volerlo cancellare?", "Notifica", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (mbr == MessageBoxResult.Yes)
                    {
                        ((Server)pcRemote.SelectedItem).Disconnect(false);
                        items.Remove((Server)pcRemote.SelectedItem);
                    }
                }
                if (items.Count == 0)
                    textconnect.Text = "Connect";

            }

        }

        private void Button_Click_modify(object sender, RoutedEventArgs e)
        {
            Server s = (Server)pcRemote.SelectedItem;

            if (s == null)
                return;

            if (s.Status == 1 || s.Status == 2)
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

        public void updateServer(String nickName, String ipAddress, Int32 port, string user, string pass, Boolean Connection)
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

            Thread t = new Thread(MyClipboard.DeleteShare);
            t.Start();
            t.Join();

            if (ch != null && ch.RightServer != null && ch.RightServer.Status == 1)
                ch.RightServer.Disconnect(true);

            if (ch != null && ch.LeftServer != null && ch.LeftServer.Status == 1)
                ch.LeftServer.Disconnect(true);


            Application.Current.Shutdown();
        }

    }



}




