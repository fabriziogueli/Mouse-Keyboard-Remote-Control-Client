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
using System.Windows.Forms;


namespace Client
{
    public class ClientHook
    {
        private delegate int LowLevelMouseHookProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate int LowLevelKeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam);

        public Server RightServer { get; set; }
        public Server LeftServer { get; set; }

        private Server currentServer;

        private const int WM_MOUSEMOVE = 0x200;
        private const int WM_LBUTTONDOWN = 0x201;
        private const int WM_LBUTTONUP = 0x202;
        private const int WM_LBUTTONDBLCLK = 0x203;
        private const int WM_RBUTTONDOWN = 0x204;
        private const int WM_RBUTTONUP = 0x205;
        private const int WM_RBUTTONDBLCLK = 0x206;
        private const int WM_MBUTTONDOWN = 0x207;
        private const int WM_MBUTTONUP = 0x208;
        private const int WM_MBUTTONDBLCLK = 0x209;
        private const int WM_MOUSEWHEEL = 0x20A;
        private const int WM_XBUTTONDOWN = 0x20B;
        private const int WM_XBUTTONUP = 0x20C;
        private const int WM_XBUTTONDBLCLK = 0x20D;
        private const int WM_MOUSEHWHEEL = 0x20E;

        static int hHook = 0;
        static int hkeyHook = 0;

        //Declare the mouse hook constant.
        //For other hook types, you can obtain these values from Winuser.h in the Microsoft SDK.
        private const int WH_MOUSE_LL = 14;
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x100;
        private const int WM_KEYUP = 0x101;

        //Declare MouseHookProcedure as a HookProc type.
        private static LowLevelMouseHookProc _mproc;
        private static LowLevelKeyboardHookProc _kproc;



        //Declare the wrapper managed POINT class.
        [StructLayout(LayoutKind.Sequential)]
        public class POINT
        {
            public int x;
            public int y;
        }

        //Declare the wrapper managed MouseHookStruct class.
        [StructLayout(LayoutKind.Sequential)]
        public class MouseHookStruct
        {
            public POINT pt;
            public int mouseData;
            public int hwnd;
            public int wHitTestCode;
            public int dwExtraInfo;
        }

        struct MouseStruct
        {
            public MouseHookStruct mhs;
            public int me;
        }

        struct KeyboardStruct
        {
            public IntPtr wparam;
            public KBDLLHOOKSTRUCT kb;
            public Int32 padding;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, UIntPtr dwExtraInfo);


        [DllImport("user32.dll")]
        static extern int SetWindowsHookEx(int idHook, LowLevelKeyboardHookProc lpfn,
        IntPtr hInstance, int threadId);

        //This is the Import for the SetWindowsHookEx function.
        //Use this function to install a thread-specific hook.
        [DllImport("user32.dll", CharSet = CharSet.Auto,
         CallingConvention = CallingConvention.StdCall)]
        static extern int SetWindowsHookEx(int idHook, LowLevelMouseHookProc lpfn,
        IntPtr hInstance, int threadId);

        //This is the Import for the UnhookWindowsHookEx function.
        //Call this function to uninstall the hook.
        [DllImport("user32.dll", CharSet = CharSet.Auto,
         CallingConvention = CallingConvention.StdCall)]
        static extern bool UnhookWindowsHookEx(int idHook);

        //This is the Import for the CallNextHookEx function.
        //Use this function to pass the hook information to the next hook procedure in chain.
        [DllImport("user32.dll", CharSet = CharSet.Auto,
         CallingConvention = CallingConvention.StdCall)]
        static extern int CallNextHookEx(int idHook, int nCode,
        IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        static extern int GetCurrentThreadId();


        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);

        //[StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            internal int vkCode;
            internal int scanCode;
            internal int flags;
            internal int time;
            internal int dwExtraInfo;
        }

        private bool isCapturing = false;
        public MainWindow Win { set; get; }


        public ClientHook()
        {

        }

        public void serverConnect(Server s, Boolean isRightServer)
        {
            if (isRightServer)
            {
                RightServer = s;
                RightServer.Win = Win;
                RightServer.Side = 1;
                Win.setConnectionHandler(RightServer);
                RightServer.Connect();
            }
            else
            {
                LeftServer = s;
                LeftServer.Win = Win;
                LeftServer.Side = 0;
                Win.setConnectionHandler(LeftServer);
                LeftServer.Connect();
            }


        }


        public int KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            //Marshall the data from the callback.

            if (nCode < 0)
            {
                return CallNextHookEx(hkeyHook, nCode, wParam, lParam);
            }
            else
            {
                if (isCapturing && currentServer != null && currentServer.Status == 1)
                {

                    try
                    {
                        //Create a string variable that shows the current mouse coordinates.
                        KBDLLHOOKSTRUCT kb;
                        kb = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));                     

                        KeyboardStruct ks = new KeyboardStruct();
                        ks.kb = kb;
                        ks.wparam = wParam;
                      
                        UdpClient uc = currentServer.getUdpClient();
                        Int32 ik = 1;
                        byte[] keyboard = BitConverter.GetBytes(ik);                      

                        byte[] bytestream = getBytesKey(ks);
                        byte[] sending = new byte[bytestream.Length + sizeof(Int32)];
                        keyboard.CopyTo(sending, 0);
                        bytestream.CopyTo(sending, sizeof(Int32));
                       
                        uc.Send(sending, sending.Length);


                        //You must get the active form because it is a static function.
                        if (wParam == (IntPtr)WM_KEYDOWN)//key is down 0
                        {


                            Console.WriteLine("down sono:" + ks.kb.vkCode);
                        }
                        else if (wParam == (IntPtr)WM_KEYUP) //key is up 1
                        {


                            Console.WriteLine("up sono:" + ks.kb.vkCode);

                        }
                        else { Console.WriteLine("default" + ks.wparam); }
                        return 1;
                    }
                    catch (Exception e)
                    {
                        isCapturing = false;                         
                        currentServer.Status = 0;
                       
                        Win.connectionProblem(currentServer);

                    }
                }
            }
            return CallNextHookEx(hkeyHook, nCode, wParam, lParam); 

        }

        public int MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {

            //Marshall the data from the callback.
            MouseHookStruct MyMouseHookStruct = (MouseHookStruct)Marshal.PtrToStructure(lParam, typeof(MouseHookStruct));
            int msg = wParam.ToInt32();

            double width = System.Windows.SystemParameters.PrimaryScreenWidth;
            double height = System.Windows.SystemParameters.PrimaryScreenHeight;

            MouseStruct mystruct = new MouseStruct();

            mystruct.me = msg;
            mystruct.mhs = MyMouseHookStruct;

            int y = (int)((mystruct.mhs.pt.y / height) * 65535);



            short mouseD = (short)(mystruct.mhs.mouseData >> 16);
            mystruct.mhs.mouseData = mouseD;


            if (nCode < 0)
            {
                return CallNextHookEx(hHook, nCode, wParam, lParam);
            }
            else
            {


                if ((mystruct.mhs.pt.x >= width || mystruct.mhs.pt.x <= 0) && !isCapturing && ((RightServer != null && RightServer.Status == 1) || (LeftServer != null && LeftServer.Status == 1)))
                {

                    if (mystruct.mhs.pt.x >= width && RightServer != null && RightServer.Status == 1 && !isCapturing)
                    {
                        Console.WriteLine("RightServer");
                        isCapturing = true;
                        currentServer = RightServer;
                        mouse_event(0x0001 | 0x8000, (uint)((8 / width) * 65535), (uint)y, 0, UIntPtr.Zero);
                        currentServer.SendLocalClipboard();

                        Win.Capturing();


                        return 1;
                    }
                    else if (mystruct.mhs.pt.x <= 0 && LeftServer != null && LeftServer.Status == 1 && !isCapturing)
                    {
                        Console.WriteLine("LeftServer");
                        isCapturing = true;
                        currentServer = LeftServer;
                        mouse_event(1 | 0x8000, 65520, (uint)y, 0, UIntPtr.Zero);
                        currentServer.SendLocalClipboard();


                        Win.Capturing();

                        return 1;

                    }
                }
                if (mystruct.mhs.pt.x <= 0 && isCapturing && currentServer == RightServer)
                {
                   
                    isCapturing = false;
                    currentServer.GetRemoteClipboard();
                    mouse_event(1 | 0x8000, (uint)(((width - 4) / width) * 65535), (uint)y, 0, UIntPtr.Zero);

                    Win.stopCapturing();


                    return 1;
                }
                else if (mystruct.mhs.pt.x >= width && isCapturing && currentServer == LeftServer)
                {
                  
                    isCapturing = false;
                    currentServer.GetRemoteClipboard();
                    mouse_event(1 | 0x8000, (uint)((10 / width) * 65535), (uint)y, 0, UIntPtr.Zero);


                    Win.stopCapturing();

                    return 1;

                }


                if (isCapturing && currentServer != null && currentServer.Status == 1)
                {
                    try
                    {

                        mystruct.mhs.pt.x = (int)((mystruct.mhs.pt.x / width) * 65535);
                        mystruct.mhs.pt.y = (int)((mystruct.mhs.pt.y / height) * 65535);
                        Mouse.OverrideCursor = System.Windows.Input.Cursors.None;
                        UdpClient uc = currentServer.getUdpClient();
                        String strCaption = "x = " +
                                        MyMouseHookStruct.pt.x.ToString("d") +
                                            "  y = " +
                                MyMouseHookStruct.pt.y.ToString("d");

                        Int32 im = 0;
                        byte[] mouse = BitConverter.GetBytes(im);

                        byte[] bytestream = getBytes(mystruct);
                        Console.WriteLine("Transmitting.....");
                        byte[] sending = new byte[bytestream.Length + sizeof(Int32)];
                        mouse.CopyTo(sending, 0);
                        bytestream.CopyTo(sending, sizeof(Int32));

                        uc.Send(sending, sending.Length);

                        //You must get the active form because it is a static function.
                        Console.WriteLine(strCaption + " event" + msg);

                        if (wParam != (IntPtr)WM_MOUSEMOVE)
                            return 1;

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        isCapturing = false;                       
                        Win.connectionProblem(currentServer);
                    }

                }
                return CallNextHookEx(hHook, nCode, wParam, lParam); 
            }
        }

        byte[] getBytes(MouseStruct mystruct)
        {
            int size = Marshal.SizeOf(mystruct);
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.StructureToPtr(mystruct, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);

            return arr;
        }

        byte[] getBytesKey(KeyboardStruct mystruct)
        {
            int size = Marshal.SizeOf(mystruct);
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.StructureToPtr(mystruct, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);

            return arr;
        }

        public void setMouseHook()
        {
            Console.WriteLine("Move");
            if (hHook == 0)
            {
                // Create an instance of HookProc.
                _mproc = new LowLevelMouseHookProc(MouseHookProc);
                IntPtr hInstance = LoadLibrary("User32");
                Console.WriteLine("SetWindowsHookEx");
                hHook = SetWindowsHookEx(WH_MOUSE_LL,
                            _mproc,
                            hInstance,
                            0);
                //If the SetWindowsHookEx function fails.
                if (hHook == 0)
                {
                    System.Windows.MessageBox.Show("SetWindowsHookEx Failed");
                    return;
                }               
            }
            else
            {
                bool ret = UnhookWindowsHookEx(hHook);
                //If the UnhookWindowsHookEx function fails.
                if (ret == false)
                {
                    System.Windows.MessageBox.Show("UnhookWindowsHookEx Failed");
                    return;
                }
                hHook = 0;
               
            }
        }

        public void setKeyboardHook()
        {
            Console.WriteLine("Inizio Button2");
            if (hkeyHook == 0)
            {
                // Create an instance of HookProc.
                _kproc = new LowLevelKeyboardHookProc(KeyboardHookProc);
                Console.WriteLine("SetWindowsHookEx");
             
                IntPtr hInstance = LoadLibrary("User32");
                hkeyHook = SetWindowsHookEx(WH_KEYBOARD_LL, _kproc, hInstance, 0);              
                //If the SetWindowsHookEx function fails.
                if (hkeyHook == 0)
                {
                    System.Windows.MessageBox.Show("SetWindowsHookEx Failed");
                    return;
                }
          
            }
            else
            {
                bool ret = UnhookWindowsHookEx(hkeyHook);
                //If the UnhookWindowsHookEx function fails.
                if (ret == false)
                {
                    System.Windows.MessageBox.Show("UnhookWindowsHookEx Failed");
                    return;
                }
                hkeyHook = 0;
          
            }
        }


    }
}
