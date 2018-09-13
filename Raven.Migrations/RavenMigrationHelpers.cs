using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Raven.Migrations
{
    public static class RavenMigrationHelpers
    {
        public static readonly string RavenMigrationsIdPrefix = "MigrationRecord";

        public static string GetMigrationDocumentId(Migration migration, char separator)
        {
            const char Underscore = '_';
            var type = migration.GetType();
            var idSafeTypeName = Regex.Replace(type.Name, Underscore + "{2,}", Underscore.ToString())
                .Trim(Underscore);
            var name = idSafeTypeName
                .Replace(Underscore, separator)
                .ToLowerInvariant();
            var version = type.GetMigrationAttribute().Version;

            return string.Join(separator.ToString(), RavenMigrationsIdPrefix, name, version.ToString()).ToLowerInvariant();
        }

        public static MigrationAttribute GetMigrationAttribute(this Type type)
        {
            var attribute = Attribute.GetCustomAttributes(type)
                .FirstOrDefault(x => x is MigrationAttribute);
            return (MigrationAttribute) attribute;
        }

        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }

        public static readonly Func<Type, bool> TypeIsMigration = t => typeof(Migration).IsAssignableFrom(t)
                                                                       && !t.IsAbstract
                                                                       && t.GetConstructor(Type.EmptyTypes) != null;
    }
}