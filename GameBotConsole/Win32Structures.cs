using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameBotConsole
{
    public static class Win32Structures
    {
        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
    }
}
