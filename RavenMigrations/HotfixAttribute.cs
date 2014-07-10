using System;

namespace RavenMigrations
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class HotfixAttribute : Attribute
    {
        public HotfixAttribute(long version)
        {
            Version = version;
        }

        public HotfixAttribute(long version, string profile)
            : this(version)
        {
            Profile = profile;
        }

        public long Version { get; set; }
        public string Profile { get; set; }
        public string Name { get; internal set; }
    }
}