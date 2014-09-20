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

        public static string GetMigrationId(this MigrationWithProperties migration, char seperator = '/')
        {
            var type = migration.MigrationType;
            var name = type
                .Name.Replace('_', seperator)
                .TrimEnd(new[] { seperator })
                .ToLowerInvariant();
            var version = migration.Properties.Version;

            return string.Join(seperator.ToString(), new string[]
            {
                RavenMigrationsIdPrefix,
                // store the versions first to facilitate querying and loading based on version
                version.Major.ToString(), version.Minor.ToString(), version.Build.ToString(), version.Revision.ToString(),
                name
            }).ToLowerInvariant();
        }

        public static MigrationProperties GetMigrationPropertiesFromAttribute(this Type type)
        {
            var attribute = Attribute.GetCustomAttributes(type)
                .OfType<MigrationAttribute>()
                .FirstOrDefault();
            if (attribute == null) return null;
            // don't change version to int
            if (attribute.Version > int.MaxValue) throw new Exception(string.Format("Version number too high (max : {0})", int.MaxValue));

            return new MigrationProperties()
            {
                Profile = attribute.Profile,
                // use 0 as a major to ease moving from original attribute version
                Version = new MigrationVersion(0, (int)attribute.Version)
            };
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
