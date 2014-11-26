using System.Linq;
using RavenMigrations.Tests.Migrations.v1;
using RavenMigrations.Tests.Migrations.v2;
using Xunit;

namespace RavenMigrations.Tests
{
    public class NameBasedMigrationCollectorTests
    {
        [Fact]
        public void Migrations_get_ordered_correctly()
        {
            var collector = new NameBasedMigrationCollector(new DefaultMigrationResolver(),
                () =>
                    new[] { typeof(M1_3_Third), typeof(M_1_2_Second), typeof(M1_Last), typeof(M1_FirstTest), typeof(M_2_FourthTest), typeof(M11_2_FifthTest) }
                );

            var orderedMigrations = collector.GetOrderedMigrations(new string[]{}).ToList();
            Assert.Equal(typeof(M1_FirstTest), orderedMigrations[0].MigrationType);
            Assert.Equal(typeof(M_1_2_Second), orderedMigrations[1].MigrationType);
            Assert.Equal(typeof(M1_3_Third), orderedMigrations[2].MigrationType);
            Assert.Equal(typeof(M_2_FourthTest), orderedMigrations[3].MigrationType);
            Assert.Equal(typeof(M11_2_FifthTest), orderedMigrations[4].MigrationType);
            Assert.Equal(typeof(M1_Last), orderedMigrations[5].MigrationType);

            Assert.Equal(orderedMigrations[0].Properties.Version, new MigrationVersion(1, 1));
            Assert.Equal(orderedMigrations[1].Properties.Version, new MigrationVersion(1, 1, 2));
            Assert.Equal(orderedMigrations[2].Properties.Version, new MigrationVersion(1, 1, 3));
            Assert.Equal(orderedMigrations[3].Properties.Version, new MigrationVersion(1, 2));
            Assert.Equal(orderedMigrations[4].Properties.Version, new MigrationVersion(1, 11, 2));
            Assert.Equal(orderedMigrations[5].Properties.Version, new MigrationVersion(2, 1));
        }
    }
}