using System;
using Microsoft.Extensions.Logging;

namespace Raven.Migrations.Tests
{
    class ConsoleLogger : ILogger<MigrationRunner>
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Console.WriteLine("{0}: {1}", logLevel.ToString(), formatter(state, exception));
        }
    }
}
