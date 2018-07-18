using System;

namespace Raven.Migrations
{
    public class MigrationRecord
    {
        public MigrationRecord()
        {
            RunOn = DateTimeOffset.UtcNow;
        }

        public string Id { get; set; }
        public DateTimeOffset RunOn { get; set; }
    }
}