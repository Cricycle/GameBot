using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;
using System.Text;
using GameBotConsole;
using static GameBotConsole.Win32Structures;

class InterceptKeys
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private static LowLevelKeyboardProc _proc = HookCallback;
    private static IntPtr _hookID = IntPtr.Zero;

    public static void Mainz()
    {
        //Application.Run();
        //UnhookWindowsHookEx(_hookID);
        ProcessStartInfo pro = new ProcessStartInfo("cmd.exe");
        // Creating an Instance of the Process Class
        // which will help to execute our Process
        Process proStart = new Process
        {

            // Setting up the Process Name here which we are
            // going to start from ProcessStartInfo
            StartInfo = pro
        };
        //Calling the Start Method of Process class to
        // Invoke our Process viz 'cmd.exe'
        proStart.Start();
        _hookID = SetHook(_proc, proStart);

        proStart.WaitForExit();
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc, Process pro)
    {
        using (ProcessModule curModule = pro.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            Console.Out.WriteLine(vkCode);
        }


        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}

namespace GameBot
{
    public class Blah
    {
        [StructLayout(LayoutKind.Sequential)]
        [DataContract]
        public class EventMsg
        {
            [DataMember]
            public int message;
            [DataMember]
            public int paramL;
            [DataMember]
            public int paramH;
            [DataMember]
            public int time;
            [DataMember]
            public int hwnd;

            public override string ToString()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(EventMsg));
                    ser.WriteObject(ms, this);
                    byte[] json = ms.ToArray();
                    return Encoding.UTF8.GetString(json, 0, json.Length);
                }
            }

            public static EventMsg ParseFromJson(string json)
            {
                using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(EventMsg));
                    return ser.ReadObject(ms) as EventMsg;
                }
            }
        }
        
        private static IntPtr journalHook;
        private HookProc JournalRecordProcedure;
        private HookProc JournalPlaybackProcedure;
        private static EventMsg currentValue;
        private static int lastTime;
        private static StreamReader sr;

        public static void Main2(string[] args)
        {
            Blah blah = new Blah();
            blah.RunNotWorking();
        }

        public void Run()
        {
            IntPtr hookID = User32.SetHook(KeyboardHookCallback, IDHook.WH_KEYBOARD_LL);
            Application.Run();
            User32.UnhookWindowsHookEx(hookID);
        }

        public void RunNotWorking()
        {
            // Set up hook with function we defined
            var res = Kernel32.GetLastError();
            IntPtr hinstance = Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]);
            JournalRecordProcedure = JournalRecordProc;
            //var temp = SetWindowsHookEx(WH_JOURNALRECORD, JournalRecordProcedure, hinstance, 0);
            var temp = User32.SetHook(JournalRecordProcedure, IDHook.WH_JOURNALRECORD);
            res = Kernel32.GetLastError();
            Console.Out.WriteLine($"Res = {res}");
            Application.Run();
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(5));
            // Remove hook
            User32.UnhookWindowsHookEx(temp);
            /*
            Console.Out.WriteLine("Playback starts");

            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));

            // Hook up our playback function
            using (sr = new StreamReader("tempfile.txt"))
            {
                hinstance = Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]);
                JournalPlaybackProcedure = JournalPlaybackProc;
                journalHook = SetWindowsHookEx(WH_JOURNALPLAYBACK, JournalPlaybackProcedure, hinstance, 0);

                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));
                Console.WriteLine("Sleeping");

                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(10));
            }
            */
        }

        private const int WM_KEYDOWN = 0x0100;

        private static IntPtr KeyboardHookCallback(
            int nCode, IntPtr wParam, IntPtr lParam)
        {

            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Console.WriteLine((Keys)vkCode);
            }
            return User32.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        public static IntPtr JournalRecordProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            //throw new Exception("checking");
            if (nCode < 0) return User32.CallNextHookEx(journalHook, nCode, wParam, lParam);

            EventMsg msg = (EventMsg)Marshal.PtrToStructure(lParam, typeof(EventMsg));

            //do what you like to save the events
            using (StreamWriter sw = new StreamWriter("tempfile.txt", true))
            {
                sw.WriteLine(msg);
            }

            return User32.CallNextHookEx(journalHook, nCode, wParam, lParam);
        }
        public static IntPtr JournalPlaybackProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            //throw new Exception("checking");
            switch ((NCode)nCode)
            {
                case NCode.HC_GETNEXT:
                    //currentValue is a class level variable I use
                    //which gets it's value from the HC_SKIP section.
                    //this way no matter how many times HC_GETNEXT is called
                    //we can give the same value
                    EventMsg msg = currentValue;

                    //lastTime is a class level variable I use
                    //to save the time of the last EventMsg so
                    //we can calculate delta.
                    int delta = msg.time - lastTime;

                    Marshal.StructureToPtr(msg, lParam, true);

                    lastTime = msg.time;
                    
                    return new IntPtr(delta > 0 ? delta : 0);

                case NCode.HC_SKIP:

                    string data = null;
                    try
                    {
                        data = sr.ReadLine();
                    }
                    catch (Exception)
                    {
                        sr.Close();
                    }

                    //if our script is done we can stop.            
                    if (data == null)
                    {
                        currentValue = null;
                        User32.UnhookWindowsHookEx(journalHook);
                        return IntPtr.Zero;
                    }

                    //get your message and put it in currentMessage
                    currentValue = EventMsg.ParseFromJson(data);

                    break;

                default:
                    return User32.CallNextHookEx(journalHook, nCode, wParam, lParam);
            }

            return IntPtr.Zero;
        }
    }

}