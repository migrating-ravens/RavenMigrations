using System;
using System.Collections.Generic;
using System.Linq;

namespace Raven.Migrations
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MigrationAttribute : Attribute
    {
        public MigrationAttribute(long version)
            : this(version, new string[] { })
        {
            
        }

        public MigrationAttribute(long version, params string[] profiles)
        {
            Version = version;
            Profiles = profiles ?? Enumerable.Empty<string>();
        }

        public long Version { get; set; }
        public IEnumerable<string> Profiles { get; set; } = Enumerable.Empty<string>();
        public string Description { get; set; } = string.Empty;
    }
}
