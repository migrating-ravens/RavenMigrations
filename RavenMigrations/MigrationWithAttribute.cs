using System;

namespace RavenMigrations
{
    internal class MigrationWithAttribute
    {
        public Func<Migration> Migration { get; set; }
        public MigrationAttribute Attribute { get; set; }
    }
}