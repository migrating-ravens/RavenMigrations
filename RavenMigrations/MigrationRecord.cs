using System;

namespace Raven.Migrations
{
    public class MigrationRecord
    {
        public string Id { get; set; }
        public DateTimeOffset RunOn { get; set; } = DateTimeOffset.UtcNow;
    }
}