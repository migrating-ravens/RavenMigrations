using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RavenMigrations.Migrations;

namespace RavenMigrations
{
    public  class AttributeBasedMigrationCollector : IMigrationCollector
    {
        /// <summary>
        /// Helper to build up assembly scanners 
        /// </summary>
        public static Func<IEnumerable<Assembly>, Func<IEnumerable<Type>>> AssemblyScannerBuilder =
            assemblies =>
                () =>
                    assemblies.SelectMany(a => a.GetLoadableTypes().Where(t => typeof (Migration).IsAssignableFrom(t)));

        private readonly IMigrationResolver _resolver;
        private readonly Func<IEnumerable<Type>> _migrationTypeCollector;

        public AttributeBasedMigrationCollector(IMigrationResolver resolver, Func<IEnumerable<Type>> migrationTypeCollector)
        {
            _resolver = resolver;
            _migrationTypeCollector = migrationTypeCollector;
        }

        public IEnumerable<MigrationWithProperties> GetOrderedMigrations(IEnumerable<string> profiles)
        {
            var migrationsToRun = from m in _migrationTypeCollector()
                .Select(t => MigrationWithProperties.FromTypeWithAttribute(t, _resolver))
                where IsInCurrentMigrationProfile(m, profiles)
                select m;

            return migrationsToRun.ToList();
        }
        private static bool IsInCurrentMigrationProfile(MigrationWithProperties migrationWithProperties, IEnumerable<string> profiles)
        {
            return string.IsNullOrWhiteSpace(migrationWithProperties.Properties.Profile) ||
                   profiles.Any(x => StringComparer.InvariantCultureIgnoreCase.Compare(migrationWithProperties.Properties.Profile, x) == 0);
        }
    }
}