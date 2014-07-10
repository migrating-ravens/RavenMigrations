using System;

namespace RavenMigrations
{
    public class HotfixDocument
    {
        public HotfixDocument()
        {
            RunOn = DateTimeOffset.UtcNow;
        }

        public string Id { get; set; }
        public DateTimeOffset RunOn { get; set; }
        public string Name { get; set; }
    }
}