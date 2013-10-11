using System;

namespace RavenMigrations
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MigrationAttribute : Attribute
    {
        public MigrationAttribute(long version)
        {
            Version = version;
        }

        public MigrationAttribute(long version, string profile)
            : this(version)
        {
            Profile = profile;
        }

        public long Version { get; set; }
        public string Profile { get; set; }
    }
}
