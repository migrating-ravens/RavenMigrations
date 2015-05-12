using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RavenMigrations
{
    public static class RavenMigrationHelpers
    {
        public static readonly string RavenMigrationsIdPrefix = "ravenmigrations";

        public static string GetMigrationIdFromName(Migration migration, char seperator = '/')
        {
            var type = migration.GetType();
            var name = type
                .Name.Replace('_', seperator)
                .TrimEnd(new[] { seperator })
                .ToLowerInvariant();
            var version = type.GetMigrationAttribute().Version;

            return string.Join(seperator.ToString(), new[] {
                RavenMigrationsIdPrefix, name, version.ToString()
            }).ToLowerInvariant();
        }

        public static MigrationAttribute GetMigrationAttribute(this Type type)
        {
            var attribute = Attribute.GetCustomAttributes(type)
                .FirstOrDefault(x => x.GetType().IsAssignableFrom(typeof(MigrationAttribute)));
            return (MigrationAttribute)attribute;
        }

        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }
    }
}
