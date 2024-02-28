using Integrator.Shared;

namespace Integrator.Console
{
    public class ConsoleLogger : ILogger
    {
        public void WriteLine(string s)
        {
            System.Console.WriteLine(s);
        }

        public void WriteLineIf(bool condition, string s)
        {
            if (condition)
            {
                System.Console.WriteLine(s);
            }
        }
    }
}
