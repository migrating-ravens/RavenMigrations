using Xunit;
using Xunit.Extensions;

namespace RavenMigrations.Tests
{
    public class VersionFromNameTests
    {
        [Theory]
        [InlineData("Something.Migrations.v2.M4_Name", "2.4.0.0")]
        [InlineData("Something.Migrations.v2.M4_5_6_Name", "2.4.5.6")]
        [InlineData("Something.Migrations.v2.M4_5_6_Adding_3_Things", "2.4.5.6")]
        [InlineData("Something.Migrations.v1.M_2_Name", "1.2.0.0")]
        [InlineData("Something.Migrations.v1.M3.v4_Name", "1.3.4.0")]
        [InlineData("Something.Migrations.v1.M2.v4_Name", "1.2.4.0")]
        [InlineData("Something.Migrations.v1_2_3_Name", "1.2.3.0")]
        [InlineData("Something.Migrations.v1_2_Name", "1.2.0.0")]
        [InlineData("Something.Migrations.v1_Name", "0.1.0.0")]
        [InlineData("Something.Migrations.v4223232_Name", "0.4223232.0.0")]
        [InlineData("Something.Migrations.M_4223232_Name", "0.4223232.0.0")]
        [InlineData("RavenMigrations.Tests.Migrations.v1.M_1_3_Third", "1.1.3.0")]
        public void Can_parse_version_from_name(string name, string stringVersion)
        {
            Assert.Equal(FromString(stringVersion).ToString(), VersionFromFullNameParser.ParseFullName(name).ToString());
        }

        [Theory]
        [InlineData("Something.Migrations.v2.M4_5_6_7_Name")]
        public void Throws_invalid_version_exception(string name)
        {
            Assert.Throws<VersionFromFullNameParser.InvalidVersionException>(() => VersionFromFullNameParser.ParseFullName(name));
        }

        private MigrationVersion FromString(string stringVersion)
        {
            var split = stringVersion.Split('.');
            var numbers = new long[4];
            numbers[0] = long.Parse(split[0]);
            numbers[1] = long.Parse(split[1]);
            numbers[2] = long.Parse(split[2]);
            numbers[3] = long.Parse(split[3]);
            return new MigrationVersion(numbers[0], numbers[1], numbers[2], numbers[3]);
        }
    }
}