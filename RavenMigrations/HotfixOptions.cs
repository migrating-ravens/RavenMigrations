using System.Collections.Generic;
using System.Reflection;

namespace RavenMigrations
{
    public class HotfixOptions
    {
        public HotfixOptions()
        {
            Assemblies = new List<Assembly>();
            Profiles = new List<string>();
            HotfixResolver = new DefaultHotfixResolver();
        }

        public IList<Assembly> Assemblies { get; set; }
        public IList<string> Profiles { get; set; }
        public IHotfixResolver HotfixResolver { get; set; }
        public long? Version { get; set; }
        public string HotfixName { get; set; }
    }
}