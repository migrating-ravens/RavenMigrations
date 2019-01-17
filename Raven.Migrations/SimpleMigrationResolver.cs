using System;

namespace Raven.Migrations
{
    /// <summary>
    /// Creates <see cref="Migration"/>s using simple object creation via <see cref="Activator.CreateInstance(Type)"/>.
    /// </summary>
    public class SimpleMigrationResolver : IMigrationResolver
    {
        public Migration Resolve(Type migrationType)
        {
            return (Migration)Activator.CreateInstance(migrationType);
        }
    }
}