using System;
using System.Linq;

namespace RavenMigrations
{
    public static class RavenHotfixHelpers
    {
        public static readonly string RavenHotfixIdPrefix = "ravenhotfixes";

        public static string GetHotfixIdFromName(this Hotfix hotfix, char seperator = '/')
        {
            var type = hotfix.GetType();
            var name = type
                .Name.Replace('_', seperator)
                .TrimEnd(new[] { seperator })
                .ToLowerInvariant();
            var version = type.GetHotfixAttribute().Version;

            return string.Join(seperator.ToString(), new[] {
                RavenHotfixIdPrefix, name, version.ToString()
            }).ToLowerInvariant();
        }

        public static HotfixAttribute GetHotfixAttribute(this Type type)
        {
            var attribute = Attribute.GetCustomAttributes(type)
                .FirstOrDefault(x => x.GetType().IsAssignableFrom(typeof(HotfixAttribute)));
            var hotfixAttribute = (HotfixAttribute)attribute;
            hotfixAttribute.Name = type.Name;
            return hotfixAttribute;
        }
    }
}