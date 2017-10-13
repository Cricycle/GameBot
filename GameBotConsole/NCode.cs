using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameBotConsole
{
    /// <summary>
    /// Contains the named constants that come as the nCode value to a hook function in windows
    /// The value of nCode varies depending on the context of which hook function it is in
    /// </summary>
    public enum NCode
    {
        HC_ACTION = 0,
        HC_GETNEXT = 1,
        HC_SKIP = 2,
        HC_NOREMOVE = 3,
    }
}
