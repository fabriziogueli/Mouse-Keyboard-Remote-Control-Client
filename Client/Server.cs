using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Security.Cryptography;
using System.Net.Sockets;
using System.Net;

namespace Client
{
    public class Server
    {

        public MainWindow Win { set; get; }

        public string Ip { get; set; }
        public int Port { get; set; }
        public short Side { get; set; } //0 = left - 1 = right

        public string Username { get; set; }

        public string Nickname { get; set; }
        public string Password { get; set; }

        public int _status { get; set; } // 0= Disconnected, 1=Connected, 2 = Connecting

        public string WinStatus { get; set; }
        private TcpClient tcpclnt;
        private UdpClient uclient;

        private ECDiffieHellmanCng exch;
        private byte[] publicKey;


        public int Status
        {
            get { return _status; }
            set
            {
                int old = _status;
                _status = value;
                OnPropertyChanged(old, _status, "connected");
            }
        }


        [DllImport("mpr.dll")]
        private static extern int WNetAddConnection2(NetResource netResource,
            string password, string username, int flags);

        [DllImport("mpr.dll")]
        private static extern int WNetCancelConnection2(string name, int flags,
            bool force);

        [StructLayout(LayoutKind.Sequential)]
        public class NetResource
        {
            public ResourceScope Scope;
            public ResourceType ResourceType;
            public ResourceDisplaytype DisplayType;
            public int Usage;
            public string LocalName;
            public string RemoteName;
            public string Comment;
            public string Provider;
        }

        public enum ResourceScope : int
        {
            Connected = 1,
            GlobalNetwork,
            Remembered,
            Recent,
            Context
        };

        public enum ResourceType : int
        {
            Any = 0,
            Disk = 1,
            Print = 2,
            Reserved = 8,
        }

        public enum ResourceDisplaytype : int
        {
            Generic = 0x0,
            Domain = 0x01,
            Server = 0x02,
            Share = 0x03,
            File = 0x04,
            Group = 0x05,
            Network = 0x06,
            Root = 0x07,
            Shareadmin = 0x08,
            Directory = 0x09,
            Tree = 0x0a,
            Ndscontainer = 0x0b
        }


        public Server(string ip, int port, string nickname, string username, string password)
        {
            Ip = ip;
            Port = port;

            Password = password;
            Username = username;
            Nickname = nickname;

            _status = 0;
            WinStatus = "Disconnected";

            exch = new ECDiffieHellmanCng(256);
            exch.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
            exch.HashAlgorithm = CngAlgorithm.Sha256;
            publicKey = exch.PublicKey.ToByteArray();

        }

        private byte[] GetLocalClipboard()
        {
            try
            {
                IDataObject data = Clipboard.GetDataObject();
                ArrayList dataObjects = new ArrayList();
                if (data != null)
                {

                    string[] formats = data.GetFormats();
                    BinaryFormatter bf = new BinaryFormatter();
                    for (int i = 0; i < formats.Length; i++)
                    {
                        object clipboardItem;
                        try
                        {
                            clipboardItem = data.GetData(formats[i]);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                        if (clipboardItem != null && clipboardItem.GetType().IsSerializable)
                        {
                            Console.WriteLine("sending {0}", formats[i]);
                            dataObjects.Add(formats[i]);
                            dataObjects.Add(clipboardItem);
                        }
                        else
                            Console.WriteLine("ignoring {0}", formats[i]);
                    }
                    using (var ms = new MemoryStream())
                    {
                        bf.Serialize(ms, dataObjects);
                        Console.WriteLine("count: " + dataObjects.Count);
                        return ms.ToArray();
                    }
                }
            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message);
            }
            return null;
        }

        public void SendClipboardToStream(byte[] data)
        {
            if (data != null)
            {
                Stream stm;
                stm = tcpclnt.GetStream();
                int ik = 3;
                byte[] ba = BitConverter.GetBytes(ik);
                stm.Write(ba, 0, ba.Length);


                byte[] dummy = new byte[1];
                byte[] len = BitConverter.GetBytes(data.Length);
                stm.Write(len, 0, len.Length);
                stm.Write(data, 0, data.Length);

            }

        }


        public Object GetClipboardFromStream()
        {
            Stream stm;
            stm = tcpclnt.GetStream();
            int ik = 2;
            byte[] ba = BitConverter.GetBytes(ik);
            stm.Write(ba, 0, ba.Length);

            byte[] len = new byte[sizeof(int)];
            stm.Read(len, 0, sizeof(int));

            int length = BitConverter.ToInt32(len, 0);
            int read = 0;
            if (length <= 0)
                return null;
            byte[] data = new byte[length];
            while (read < length)
            {

                read += stm.Read(data, read, length - read);

            }
            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                memStream.Write(data, 0, data.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                var obj = binForm.Deserialize(memStream);
                return obj;
            }
        }

        public void SendLocalClipboard()
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += DoWorkSendLocalClipboard;
            bw.RunWorkerAsync(GetLocalClipboard());
        }

        private void DoWorkSendLocalClipboard(object sender, DoWorkEventArgs eventArgs)
        {
            try
            {
                SendClipboardToStream((byte[])eventArgs.Argument);
            }
            catch (Exception ioe)
            {
               
                    Win.connectionProblem(this);
            }
        }

        public void GetRemoteClipboard()
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += DoWorkGetClipboard;
            bw.RunWorkerCompleted += ClipboardReceived;
            bw.RunWorkerAsync();
        }

        private void DoWorkGetClipboard(object sender, DoWorkEventArgs eventArgs)
        {
            try
            {
                eventArgs.Result = GetClipboardFromStream();
            }
            catch (Exception ioe)
            {

                Win.connectionProblem(this);
            }
        }

        private void ClipboardReceived(object sender, RunWorkerCompletedEventArgs eventArgs)
        {
            if (!eventArgs.Cancelled && eventArgs.Error == null)
            {
                ArrayList data = (ArrayList)eventArgs.Result;
                if (data != null)
                {
                    DataObject dataObj = new DataObject();
                    Console.WriteLine("Count: " + data.Count);
                    for (int i = 0; i < data.Count; i++)
                    {
                        string format = (string)data[i++];
                        Console.WriteLine(format);
                        dataObj.SetData(format, data[i]);
                    }

                    if (dataObj.ContainsFileDropList())
                    {
                        StringCollection files = dataObj.GetFileDropList();
                        dataObj = new DataObject();
                        StringCollection adjusted = new StringCollection();
                        foreach (string f in files)
                        {
                            if (!f.StartsWith("\\"))
                            {
                                string toadd = "\\\\" + Ip + "\\" + f.Replace(":", "");
                                Console.WriteLine(toadd);
                                adjusted.Add(toadd);
                            }
                            else
                            {
                                adjusted.Add(f);
                            }
                        }
                        dataObj.SetFileDropList(adjusted);
                    }
                    Clipboard.SetDataObject(dataObj);

                }
            }
        }

        public void Connect()
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += DoWorkConnect;
            bw.RunWorkerCompleted += ConnectionCompleted;
            bw.RunWorkerAsync();
        }


        private void DoWorkConnect(object sender, DoWorkEventArgs eventArgs)
        {
            try
            {

                IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(Ip), Port);
                uclient = new UdpClient();
                uclient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                uclient.DontFragment = true;
                uclient.Connect(ipep);

                tcpclnt = new TcpClient();
                Console.WriteLine("Connecting.....");
                Status = 2;

                tcpclnt.NoDelay = true;

                tcpclnt.Connect(Ip, Port);
                Console.WriteLine("Connected");

                tcpclnt.GetStream().Write(publicKey, 0, publicKey.Length);
                byte[] serverPublicKey = new byte[72];
                tcpclnt.GetStream().Read(serverPublicKey, 0, 72);
                byte[] derivedKey =
                    exch.DeriveKeyMaterial(CngKey.Import(serverPublicKey, CngKeyBlobFormat.EccPublicBlob));

                StreamWriter stream = new StreamWriter(tcpclnt.GetStream());

                stream.WriteLine(Username);
                stream.Flush();

                Aes aes = new AesCryptoServiceProvider();
                aes.Key = derivedKey;
                byte[] bytes = new byte[aes.BlockSize / 8];
                bytes.Initialize();
                System.Buffer.BlockCopy(Username.ToCharArray(), 0, bytes, 0,
                    bytes.Length > Username.Length * sizeof(char) ? Username.Length * sizeof(char) : bytes.Length);
                aes.IV = bytes;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                MemoryStream ms = new MemoryStream(64);
                CryptoStream csEncrypt = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
                byte[] passArr = Encoding.UTF8.GetBytes(Password);

                csEncrypt.Write(passArr, 0, passArr.Length);
                csEncrypt.Close();


                byte[] tosend = ms.ToArray();


                string encpass = Convert.ToBase64String(tosend, 0, tosend.Length);

                stream.WriteLine(encpass);
                stream.Flush();


                byte[] auth = new byte[sizeof(bool)];
                tcpclnt.GetStream().Read(auth, 0, sizeof(bool));

                bool result = BitConverter.ToBoolean(auth, 0);

                eventArgs.Result = result;


            }
            catch (Exception ioe)
            {
                eventArgs.Result = false;
            }

            if ((bool)eventArgs.Result)
            {
                Action act = new Action(() =>
                {
                    var netResource = new NetResource()
                    {
                        Scope = ResourceScope.GlobalNetwork,
                        ResourceType = ResourceType.Disk,
                        DisplayType = ResourceDisplaytype.Share,
                        RemoteName = "\\\\" + Ip + "\\C"
                    };

                    var result = WNetAddConnection2(
                        netResource,
                        Password,
                        null,
                        0x00000004 | 0x00000008 | 0x1000);

                    if (result != 0)
                    {
                        Console.WriteLine("Result not zero: " + result);
                    }
                });
                Thread t = new Thread(() => act());
                t.Start();

            }
        }

        private void ConnectionCompleted(object sender, RunWorkerCompletedEventArgs eventArgs)
        {
            if (!eventArgs.Cancelled && eventArgs.Error == null)
            {
                bool Connected = (bool)eventArgs.Result;
                if (!Connected)
                {
                    Status = 0;
                    Win.AuthFailed();
                    Console.WriteLine("Non connesso");
                }
                else
                {
                    Status = 1;
                }

            }
            else
            {
                Console.WriteLine(eventArgs.Error.Message);
                Status = 0;


            }


        }

        public Stream getStream()
        {
            return tcpclnt.GetStream();
        }

        public UdpClient getUdpClient()
        {
            return uclient;
        }

        public void Disconnect(Boolean isClosingWin)
        {
            if(!isClosingWin)
            Status = 0;

            try
            {
                if (tcpclnt != null)
                {
                    tcpclnt.GetStream().Dispose();
                    tcpclnt.Close();
                    tcpclnt = null;

                }

                if (uclient != null)
                {
                    uclient.Client.Close();
                    uclient.Close();

                }


                uclient = null;

            }
            catch (Exception e)
            {

                tcpclnt = null;
                uclient = null;
            }

        }

        public event PropertyChangedExtendedEventHandler<int> PropertyChanged;
        protected virtual void OnPropertyChanged(int oldValue, int newValue, [CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedExtendedEventArgs<int>(propertyName, oldValue, newValue));
        }
    }
}
