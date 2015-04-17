using System;
using System.Collections.Generic;
using System.Linq;

namespace RavenMigrations
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MigrationAttribute : Attribute
    {
        public MigrationAttribute(long version)
        {
            Version = version;
        }

        public MigrationAttribute(long version, string profiles)
            : this(version)
        {
            Profiles = profiles;
        }

        public long Version { get; set; }
        public string Profiles { get; set; }

        public IList<string> GetIndividualProfiles()
        {
            return Profiles == null
                ? new List<string>()
                : Profiles
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToList();
        }
    }
}
