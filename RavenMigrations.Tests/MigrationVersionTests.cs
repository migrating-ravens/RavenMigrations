using FluentAssertions;
using Xunit;

namespace RavenMigrations.Tests
{
    public class MigrationVersionTests
    {
        [Fact]
        public void VersionsWithSameMajorAndNoOtherNumbersAreEqual()
        {
            new MigrationVersion(1).Should().Be(new MigrationVersion(1));
        }

        [Fact]
        public void Version_1_1_GreaterThanVersion_1()
        {
            new MigrationVersion(1, 1).Should().BeGreaterThan(new MigrationVersion(1));
            new MigrationVersion(1).Should().BeLessThan(new MigrationVersion(1, 1));
        }

        [Fact]
        public void Version_1_0_1_GreaterThanVersion_1()
        {
            new MigrationVersion(1, 0, 1).Should().BeGreaterThan(new MigrationVersion(1));
            new MigrationVersion(1).Should().BeLessThan(new MigrationVersion(1, 0, 1));
        }

        [Fact]
        public void Version_1_0_0_1_GreaterThanVersion_1()
        {
            new MigrationVersion(1, 0, 0, 1).Should().BeGreaterThan(new MigrationVersion(1));
            new MigrationVersion(1).Should().BeLessThan(new MigrationVersion(1, 0, 0, 1));
        }

        [Fact]
        public void Version_1_1_0_1_GreaterThanVersion_1_0_1_0()
        {
            new MigrationVersion(1, 1, 0, 1).Should().BeGreaterThan(new MigrationVersion(1, 0, 1));
            new MigrationVersion(1, 0, 1).Should().BeLessThan(new MigrationVersion(1, 1, 0, 1));
        }
    }
}