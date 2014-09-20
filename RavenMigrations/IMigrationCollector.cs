using System.Collections.Generic;

namespace RavenMigrations
{
    public interface IMigrationCollector
    {
        IEnumerable<MigrationWithProperties> GetOrderedMigrations(IEnumerable<string> profile);
    }
}