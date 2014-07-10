using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Raven.Client;

namespace RavenMigrations
{
    public class HotfixRunner
    {
        private HotfixRunner()
        {
        }

        public static void Run(IDocumentStore documentStore, HotfixOptions options)
        {
            if (options == null)
                options = new HotfixOptions();

            if (!options.Assemblies.Any())
                options.Assemblies.Add(Assembly.GetCallingAssembly());

            var hotfixWithAttribute = FindHotfixWithOptions(options);

            // send in the document Store
            var hotfix = hotfixWithAttribute.Hotfix();
            hotfix.Setup(documentStore);

            // todo: possible issue here with sharding
            var hotfixId =
                hotfix.GetHotfixIdFromName(documentStore.Conventions.IdentityPartsSeparator[0]);

            using (var session = documentStore.OpenSession())
            {
                var hotfixDoc = session.Load<HotfixDocument>(hotfixId);

                // we already ran it
                if (hotfixDoc != null)
                    return;

                hotfix.Apply();
                session.Store(new HotfixDocument { Id = hotfixId, Name = hotfixWithAttribute.Attribute.Name });

                session.SaveChanges();
            }
        }

        private static HotfixWithAttribute FindHotfixWithOptions(HotfixOptions options)
        {
            var hotfixes = new List<HotfixWithAttribute>();
            foreach (var assembly in options.Assemblies)
            {
                var hotfixesFromAssembly =
                    from t in assembly.GetLoadableTypes()
                    where typeof (Hotfix).IsAssignableFrom(t)
                    select new HotfixWithAttribute
                    {
                        Hotfix = () => options.HotfixResolver.Resolve(t),
                        Attribute = t.GetHotfixAttribute()
                    };

                hotfixes.AddRange(hotfixesFromAssembly);
            }

            var hotfixesToRun = from m in hotfixes
                where IsInCurrentMigrationProfile(m, options)
                      && IsVersionOrNameMatch(m, options)
                orderby m.Attribute.Version
                select m;

            int numberOfHotfixes = hotfixesToRun.Count();

            if (numberOfHotfixes != 1)
            {
                throw new AmbiguousMatchException(string.Format(CultureInfo.InvariantCulture,
                    "Unable to find hotfix matching version \"{0}\" or name \"{1}\"", options.Version.HasValue ? options.Version.ToString() : "NOT SET", options.HotfixName));
            }

            return hotfixesToRun.Single();
        }

        private static bool IsInCurrentMigrationProfile(HotfixWithAttribute hotfixWithAttribute, HotfixOptions options)
        {
            return string.IsNullOrWhiteSpace(hotfixWithAttribute.Attribute.Profile) ||
                   options.Profiles.Any(
                       x =>
                           StringComparer.InvariantCultureIgnoreCase.Compare(hotfixWithAttribute.Attribute.Profile, x) ==
                           0);
        }

        private static bool IsVersionOrNameMatch(HotfixWithAttribute hotfixWithAttribute, HotfixOptions options)
        {
            // The version takes precedence over the name
            if (options.Version.HasValue)
            {
                return hotfixWithAttribute.Attribute.Version == options.Version;
            }

            return
                StringComparer.InvariantCultureIgnoreCase.Compare(hotfixWithAttribute.Attribute.Name, options.HotfixName) ==
                0;
        }
    }
}