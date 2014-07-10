using System;

namespace RavenMigrations
{
    internal class HotfixWithAttribute
    {
        public Func<Hotfix> Hotfix { get; set; }
        public HotfixAttribute Attribute { get; set; }
    }
}