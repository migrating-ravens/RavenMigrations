using System;
using RavenMigrations.Migrations;

namespace RavenMigrations
{
    public class DefaultMigrationResolver : IMigrationResolver
    {
        public Migration Resolve(Type migrationType)
        {
            return (Migration)Activator.CreateInstance(migrationType);
        }
    }
}