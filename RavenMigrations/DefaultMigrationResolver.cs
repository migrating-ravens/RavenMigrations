using System;

namespace Raven.Migrations
{
    public class DefaultMigrationResolver : IMigrationResolver
    {
        public Migration Resolve(Type migrationType)
        {
            return (Migration)Activator.CreateInstance(migrationType);
        }
    }
}