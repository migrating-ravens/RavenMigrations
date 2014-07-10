using System;

namespace RavenMigrations
{
    public class DefaultHotfixResolver : IHotfixResolver
    {
        public Hotfix Resolve(Type hotfixType)
        {
            return (Hotfix)Activator.CreateInstance(hotfixType);
        }
    }
}