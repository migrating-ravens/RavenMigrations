using System;

namespace Raven.Migrations
{
    internal class MigrationWithAttribute
    {
        public Func<Migration> Migration { get; set; }
        public MigrationAttribute Attribute { get; set; }
    }
}