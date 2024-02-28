namespace Integrator.Shared
{
    public class LoggerDecorator : ILoggerDecorator, ILogger
    {
        private readonly List<ILogger> loggers;

        public LoggerDecorator(params ILogger[] loggers) 
        {
            this.loggers = loggers.ToList();
        }

        public void Add(ILogger logger)
        {
            loggers.Add(logger);
        }

        public void WriteLine(string s)
        {
            foreach (var log in loggers)
            {
                log.WriteLine(s);
            }
        }

        public void WriteLineIf(bool condition, string s)
        {
            foreach(var log in loggers)
            {
                log.WriteLineIf(condition, s);
            }
        }
    }
}
