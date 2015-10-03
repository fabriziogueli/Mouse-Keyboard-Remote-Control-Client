using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class MyClipboard
    {

        [DllImport("Netapi32.dll")]
        private static extern uint NetShareAdd(
            [MarshalAs(UnmanagedType.LPWStr)] string strServer,
            Int32 dwLevel,
            ref SHARE_INFO_502 buf,
            out uint parm_err
        );

        [DllImport("netapi32.dll")]
        static extern uint NetShareDel(
                    [MarshalAs(UnmanagedType.LPWStr)] string strServer,
                    [MarshalAs(UnmanagedType.LPWStr)] string strNetName,
                    Int32 reserved //must be 0
                    );

        [DllImport("netapi32.dll", SetLastError = true)]
        static extern uint NetShareCheck(
            [MarshalAs(UnmanagedType.LPWStr)] string servername,
            [MarshalAs(UnmanagedType.LPWStr)] string device,
            out SHARE_TYPE type
            );

        private enum NetError : uint
        {
            NERR_Success = 0,
            NERR_BASE = 2100,
            NERR_UnknownDevDir = (NERR_BASE + 16),
            NERR_DuplicateShare = (NERR_BASE + 18),
            NERR_BufTooSmall = (NERR_BASE + 23),
            NERR_DeviceNotShared = 0x00000907
        }

        [Flags]
        private enum SHARE_TYPE : uint
        {
            STYPE_DISKTREE = 0,
            STYPE_PRINTQ = 1,
            STYPE_DEVICE = 2,
            STYPE_IPC = 3,
            STYPE_TEMPORARY = 0x40000000,
            STYPE_SPECIAL = 0x80000000,
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SHARE_INFO_502
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string shi502_netname;
            public SHARE_TYPE shi502_type;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string shi502_remark;
            public Int32 shi502_permissions;
            public Int32 shi502_max_uses;
            public Int32 shi502_current_uses;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string shi502_path;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string shi502_passwd;
            public Int32 shi502_reserved;
            public IntPtr shi502_security_descriptor;
        }

        public static void InitializeShare()
        {
            SHARE_TYPE type;
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo d in allDrives)
            {

                string shareName = d.Name.Replace(":\\", "");
                string shareDesc = "";
                string path = d.Name;

                SHARE_INFO_502 info = new SHARE_INFO_502();
                info.shi502_netname = shareName;
                info.shi502_type = SHARE_TYPE.STYPE_DISKTREE | SHARE_TYPE.STYPE_TEMPORARY;
                info.shi502_remark = shareDesc;
                info.shi502_permissions = 0;
                info.shi502_max_uses = -1;
                info.shi502_current_uses = 0;
                info.shi502_path = path;
                info.shi502_passwd = null;
                info.shi502_reserved = 0;
                info.shi502_security_descriptor = IntPtr.Zero;

                uint error = 0;
                uint result;
                if ((result = NetShareAdd(null, 502, ref info, out error)) != 0)
                {
                    Console.WriteLine("result = " + result + " error = " + error);
                }

            }
        }
    }
}