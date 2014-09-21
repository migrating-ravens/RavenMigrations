using System;
using RavenMigrations.Migrations;

namespace RavenMigrations
{
    public interface IMigrationResolver
    {
        Migration Resolve(Type migrationType);
    }
}