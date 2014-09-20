using System;
using System.Collections.Generic;
using System.Linq;

namespace RavenMigrations
{
    public abstract class AttributeBasedMigrationCollector : IMigrationCollector
    {
        private IMigrationResolver _resolver;

        public AttributeBasedMigrationCollector(IMigrationResolver resolver)
        {
            _resolver = resolver;
        }

        public IEnumerable<MigrationWithProperties> GetOrderedMigrations(IEnumerable<string> profiles)
        {
            var migrationsToRun = from m in GetMigrationTypes()
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

        protected abstract IEnumerable<Type> GetMigrationTypes();

    }
}