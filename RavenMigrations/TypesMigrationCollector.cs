using System;
using System.Collections.Generic;

namespace RavenMigrations
{
    public class TypesMigrationCollector : AttributeBasedMigrationCollector
    {
        private readonly IEnumerable<Type> _types;

        public TypesMigrationCollector(IMigrationResolver resolver, IEnumerable<Type> types)
            : base(resolver)
        {
            _types = types;
        }

        protected override IEnumerable<Type> GetMigrationTypes()
        {
            return _types;
        }
    }
}