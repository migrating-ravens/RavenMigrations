using System;
using System.Collections.Generic;
using System.Linq;

namespace RavenMigrations
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MigrationAttribute : Attribute
    {
        public MigrationAttribute(long version)
            : this(version, null)
        {
            
        }

        public MigrationAttribute(long version, params string[] profiles)
        {
            Version = version;
            Profiles = profiles ?? Enumerable.Empty<string>();
        }

        public long Version { get; set; }
        public IEnumerable<string> Profiles { get; set; }
    }
}
