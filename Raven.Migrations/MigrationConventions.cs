using System;

namespace Raven.Migrations
{
    public class MigrationConventions
    {
        public MigrationConventions()
        {
            TypeIsMigration = RavenMigrationHelpers.TypeIsMigration;
            MigrationDocumentId = RavenMigrationHelpers.GetMigrationDocumentId;
        }

        public Func<Type, bool> TypeIsMigration { get; set; }
        public Func<Migration, char, string> MigrationDocumentId { get; set; }
    }
}