using System;
using System.Collections.Generic;
using System.Linq;
using RavenMigrations.Migrations;

namespace RavenMigrations
{
    /// <summary>
    /// This runner takes the version of a migration from the namespaces and class name.
    /// It does not yet support profiles.
    /// </summary>
    public class NameBasedMigrationCollector : IMigrationCollector
    {
        private readonly IMigrationResolver _resolver;
        private readonly Func<IEnumerable<Type>> _migrationTypeCollector;

        public NameBasedMigrationCollector(IMigrationResolver resolver, Func<IEnumerable<Type>> migrationTypeCollector)
        {
            _resolver = resolver;
            _migrationTypeCollector = migrationTypeCollector;
        }

        public IEnumerable<MigrationWithProperties> GetOrderedMigrations(IEnumerable<string> profile)
        {
            // TODO: support profiles
            var migrationsToRun = _migrationTypeCollector()
                .Select(t => new MigrationWithProperties
                {
                    MigrationType = t,
                    Migration = (Func<Migration>) (() => _resolver.Resolve(t)),
                    Properties = new MigrationProperties
                    {
                        Profile = string.Empty,
                        Version = VersionFromFullNameParser.ParseFullName(t.FullName)
                    }
                })
                .OrderBy(m => m.Properties.Version);

            return migrationsToRun.ToList();
        }
    }
}