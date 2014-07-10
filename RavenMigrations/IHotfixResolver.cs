using System;

namespace RavenMigrations
{
    public interface IHotfixResolver
    {
        Hotfix Resolve(Type hotfixType);
    }
}