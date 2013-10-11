using System;

namespace RavenMigrations
{
    public interface IMigrationResolver
    {
        Migration Resolve(Type migrationType);
    }
}