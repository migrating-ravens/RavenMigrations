using System;
using System.Collections.Generic;
using System.Linq;

namespace Raven.Migrations
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MigrationAttribute : Attribute
    {
        public MigrationAttribute(int version)
            : this(version, null)
        {
            
        }

        public MigrationAttribute(int version, params string[] profiles)
        {
            Version = version;
            Profiles = profiles ?? Enumerable.Empty<string>();
        }

        public int Version { get; set; }
        public IEnumerable<string> Profiles { get; set; }
    }
}
