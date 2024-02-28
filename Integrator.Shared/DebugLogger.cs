using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integrator.Shared
{
    public class DebugLogger : ILogger
    {
        public void WriteLine(string message) 
        {
            Debug.WriteLine(message);
        }

        public void WriteLineIf(bool condition, string s)
        {
            Debug.WriteLineIf(condition, s);
        }
    }
}
