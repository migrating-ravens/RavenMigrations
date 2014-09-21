using System;

namespace RavenMigrations
{
    public class MigrationDocument
    {
        public MigrationDocument()
        {
            RunOn = DateTimeOffset.UtcNow;
        }

        public string Id { get; set; }
        public bool HasError { get; set; }
        public MigrationError Error { get; set; }
        public DateTimeOffset RunOn { get; set; }

        public void CaptureException(Exception exception, Directions direction)
        {
            HasError = true;
            Error = new MigrationError()
            {
                Exception = exception,
                Message = exception.Message,
                Direction = direction
            };
        }
    }

    public class MigrationError
    {
        public bool IsFixed { get; set; }
        public string FixedNote { get; set; }
        public Directions Direction { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
        
    }
}