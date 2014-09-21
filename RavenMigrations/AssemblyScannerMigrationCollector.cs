using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RavenMigrations.Migrations;

namespace RavenMigrations
{
    public class AssemblyScannerMigrationCollector : AttributeBasedMigrationCollector
    {
        private readonly IEnumerable<Assembly> _assemblies;

        public AssemblyScannerMigrationCollector(IMigrationResolver resolver, IEnumerable<Assembly> assemblies)
            : base(resolver)
        {
            _assemblies = assemblies;
        }

        protected override IEnumerable<Type> GetMigrationTypes()
        {
            return _assemblies.SelectMany(a => RavenMigrationHelpers.GetLoadableTypes(a).Where(t => typeof(Migration).IsAssignableFrom(t)));
        }

    }
}