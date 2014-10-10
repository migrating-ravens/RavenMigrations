using System.Linq;
using FluentAssertions;
using Raven.Abstractions.Data;
using Raven.Client.Indexes;
using Raven.Tests.Helpers;
using RavenMigrations.Extensions;
using RavenMigrations.Migrations;
using Xunit;

namespace RavenMigrations.Tests
{
    public class RunnerTests : RavenTestBase
    {
        private readonly IMigrationCollector _collector;
        public RunnerTests()
        {
            _collector = new AttributeBasedMigrationCollector(new DefaultMigrationResolver(),
                () => new[] {typeof (First_Migration), typeof (Second_Migration), typeof (Development_Migration)});
        }

        [Fact]
        public void Document_id_prefix_is_ravenmigrations()
        {
            RavenMigrationHelpers.RavenMigrationsIdPrefix.Should().Be("ravenmigrations");
        }

        [Fact]
        public void Can_change_migration_document_seperator_to_dash()
        {
            MigrationWithProperties.FromTypeWithAttribute(typeof (First_Migration), null)
                .GetMigrationId(seperator: '-')
                .Should().Be("ravenmigrations-0-1-0-0-first-migration");
        }

        [Fact]
        public void Document_id_prefix_is_raven_migrations()
        {
            RavenMigrationHelpers.RavenMigrationsIdPrefix.Should().Be("ravenmigrations");
        }

        [Fact]
        public void Can_get_migration_id_from_migration()
        {
            MigrationWithProperties.FromTypeWithAttribute(typeof (First_Migration), null)
                .GetMigrationId()
                .Should().Be("ravenmigrations/0/1/0/0/first/migration");
        }

        [Fact]
        public void Can_get_migration_properties_from_migration_type()
        {
            var properties = MigrationWithProperties.FromTypeWithAttribute(typeof(First_Migration), null)
                .Properties;
            properties.Should().NotBeNull();
            properties.Version.Major.Should().Be(0);
            properties.Version.Minor.Should().Be(1);
        }

        [Fact]
        public void Default_migration_direction_is_up()
        {
            var options = new MigrationOptions();
            options.Direction.Should().Be(Directions.Up);
        }

        [Fact]
        public void Default_resolver_should_be_DefaultMigrationResolver()
        {
            var options = new MigrationOptions();
            options.MigrationResolver.Should().NotBeNull();
            options.MigrationResolver.Should().BeOfType<DefaultMigrationResolver>();
        }

        [Fact]
        public void Default_migration_resolver_can_instantiate_a_migration()
        {
            var migration = new DefaultMigrationResolver().Resolve(typeof(First_Migration));
            migration.Should().NotBeNull();
        }

        [Fact]
        public void Can_run_an_up_migration_against_a_document_store()
        {
            using (var store = NewDocumentStore())
            {
                new TestDocumentIndex().Execute(store);

                Runner.Run(store, migrationCollector: _collector);
                WaitForIndexing(store);

                using (var session = store.OpenSession())
                {
                    session.Query<TestDocument, TestDocumentIndex>()
                        .Count()
                        .Should().Be(1);
                }
            }
        }

        [Fact]
        public void Calling_run_twice_runs_migrations_only_once()
        {
            using (var store = NewDocumentStore())
            {
                new TestDocumentIndex().Execute(store);

                Runner.Run(store, migrationCollector: _collector);
                // oooops, twice!
                Runner.Run(store, migrationCollector: _collector);
                WaitForIndexing(store);

                using (var session = store.OpenSession())
                {
                    session.Query<TestDocument, TestDocumentIndex>()
                        .Count()
                        .Should().Be(1);
                }
            }
        }

        [Fact]
        public void Can_call_up_then_down_on_migrations()
        {
            using (var store = NewDocumentStore())
            {
                new TestDocumentIndex().Execute(store);

                Runner.Run(store, migrationCollector: _collector);

                WaitForIndexing(store);

                // flip it and reverse it :P
                Runner.Run(store, new MigrationOptions
                {
                    Direction = Directions.Down
                }, _collector);

                WaitForIndexing(store);
                using (var session = store.OpenSession())
                {
                    session.Query<TestDocument, TestDocumentIndex>()
                        .Count()
                        .Should().Be(0);
                }
            }
        }

        [Fact]
        public void Can_call_migrations_up_to_a_certain_version()
        {
            using (var store = NewDocumentStore())
            {
                new TestDocumentIndex().Execute(store);

                Runner.Run(store, new MigrationOptions() {ToVersion = new MigrationVersion(0, 1)}, _collector);
                WaitForIndexing(store);

                using (var session = store.OpenSession())
                {
                    session.Query<TestDocument, TestDocumentIndex>()
                        .Count()
                        .Should().Be(1);

                    var shouldNotExist = session.Load<object>("second-document");
                    shouldNotExist.Should().BeNull();
                }
            }
        }

        [Fact]
        public void Can_call_migrations_down_to_a_certain_version()
        {
            using (var store = NewDocumentStore())
            {
                new TestDocumentIndex().Execute(store);

                Runner.Run(store, migrationCollector: _collector);
                WaitForIndexing(store);

                // migrate down to 
                Runner.Run(store, new MigrationOptions
                {
                    Direction = Directions.Down,
                    ToVersion = new MigrationVersion(0, 2)
                }, _collector);

                using (var session = store.OpenSession())
                {
                    session.Query<TestDocument, TestDocumentIndex>()
                        .Count()
                        .Should().Be(1);

                    var secondMigrationDocument =
                        session.Load<MigrationDocument>(MigrationWithProperties.FromTypeWithAttribute(typeof(Second_Migration), null)
                            .GetMigrationId());
                    secondMigrationDocument.Should().BeNull();

                    var firstMigrationDocument =
                        session.Load<MigrationDocument>(MigrationWithProperties.FromTypeWithAttribute(typeof(First_Migration), null)
                        .GetMigrationId());
                    firstMigrationDocument.Should().NotBeNull();
                }
            }
        }

        [Fact]
        public void Can_call_migrations_with_profile()
        {
            using (var store = NewDocumentStore())
            {
                new TestDocumentIndex().Execute(store);

                Runner.Run(store, new MigrationOptions { Profiles = new[] { "development" } },
                    _collector);
                WaitForIndexing(store);

                using (var session = store.OpenSession())
                {
                    var development = session.Load<object>("development-1");
                    development.Should().NotBeNull();
                }
            }
        }

        [Fact]
        public void Can_call_migrations_ignore_migrations_with_profile()
        {
            using (var store = NewDocumentStore())
            {
                new TestDocumentIndex().Execute(store);

                Runner.Run(store, migrationCollector: _collector);
                WaitForIndexing(store);

                using (var session = store.OpenSession())
                {
                    var development = session.Load<object>("development-1");
                    development.Should().BeNull();
                }
            }
        }

        [Fact]
        public void When_exception_occurs_no_more_steps_are_executed_and_exception_is_stored()
        {
            var collector = new AttributeBasedMigrationCollector(new DefaultMigrationResolver(),
                () => new[] {typeof (Failing_Migration), typeof (Fifth_Migration)});

            using (var store = NewDocumentStore())
            {
                Runner.Run(store, migrationCollector: collector);
                using (var session = store.OpenSession())
                {
                    var failingMigrationDoc = session.Load<MigrationDocument>(
                        MigrationWithProperties.FromTypeWithAttribute(typeof (Failing_Migration), null).GetMigrationId());
                    failingMigrationDoc
                        .Should().NotBeNull();
                    failingMigrationDoc.HasError.Should().BeTrue();
                    failingMigrationDoc.Error.Should().NotBeNull();
                    failingMigrationDoc.Error.IsFixed.Should().BeFalse();
                    failingMigrationDoc.Error.Direction.Should().Be(Directions.Up);
                    failingMigrationDoc.Error.Exception.Should().NotBeNull();
                    failingMigrationDoc.Error.Message.Should().NotBeBlank();

                    session.Load<MigrationDocument>(
                        MigrationWithProperties.FromTypeWithAttribute(typeof (Fifth_Migration), null).GetMigrationId())
                        .Should().BeNull();
                }
            }
        }

        [Fact]
        public void When_there_is_a_failing_migration_do_not_run_more_migrations()
        {
            var collector = new AttributeBasedMigrationCollector(new DefaultMigrationResolver(),
                () => new[] {typeof (Failing_Migration), typeof (Fifth_Migration)});

            using (var store = NewDocumentStore())
            {
                Runner.Run(store, migrationCollector: collector);
                Runner.Run(store, migrationCollector: collector);
                using (var session = store.OpenSession())
                {
                    session.Load<MigrationDocument>(
                        MigrationWithProperties.FromTypeWithAttribute(typeof (Fifth_Migration), null).GetMigrationId())
                        .Should().BeNull();
                }
            }
        }
        
        [Fact]
        public void After_fixing_a_migration_next_migrations_run()
        {
            var collector = new AttributeBasedMigrationCollector(new DefaultMigrationResolver(),
                () => new[] {typeof (Failing_Migration), typeof (Fifth_Migration)});

            using (var store = NewDocumentStore())
            {
                Runner.Run(store, migrationCollector: collector);
                using (var session = store.OpenSession())
                {
                    session.Load<MigrationDocument>(
                        MigrationWithProperties.FromTypeWithAttribute(typeof(Failing_Migration), null).GetMigrationId())
                        .Error.IsFixed = true;
                    session.SaveChanges();
                }

                Runner.Run(store, migrationCollector: collector);
                using (var session = store.OpenSession())
                {
                    session.Load<MigrationDocument>(
                        MigrationWithProperties.FromTypeWithAttribute(typeof (Fifth_Migration), null).GetMigrationId())
                        .Should().NotBeNull();
                }
            }
        }
    }

    public class TestDocument
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class TestDocumentIndex : AbstractIndexCreationTask<TestDocument>
    {
        public TestDocumentIndex()
        {
            Map = tests => from t in tests
                           select new { t.Id, t.Name };
        }
    }

    [Migration(1)]
    public class First_Migration : Migration
    {
        public override void Up()
        {
            using (var session = DocumentStore.OpenSession())
            {
                session.Store(new TestDocument { Name = "Khalid Abuhakmeh" });
                session.SaveChanges();
            }
        }

        public override void Down()
        {
            DocumentStore.WaitForIndexing();
            DocumentStore.DatabaseCommands.DeleteByIndex(new TestDocumentIndex().IndexName, new IndexQuery());
        }
    }

    [Migration(2)]
    public class Second_Migration : Migration
    {
        public override void Up()
        {
            using (var session = DocumentStore.OpenSession())
            {
                session.Store(new { Id = "second-document", Name = "woot!" });
                session.SaveChanges();
            }
        }

        public override void Down()
        {
            using (var session = DocumentStore.OpenSession())
            {
                var doc = session.Load<object>("second-document");
                session.Delete(doc);
                session.SaveChanges();
            }
        }
    }

    [Migration(3, "development")]
    public class Development_Migration : Migration
    {
        public override void Up()
        {
            using (var session = DocumentStore.OpenSession())
            {
                session.Store(new { Id = "development-1" });
                session.SaveChanges();
            }
        }
    }

    [Migration(4)]
    public class Failing_Migration : Migration
    {
        public override void Up()
        {
            using (var session = DocumentStore.OpenSession())
            {
                // fake a migration that will blow up
                var instance = session.Load<dynamic>("non-existing-id");
                instance.Name = instance.Name + " changed";
                session.SaveChanges();
            }
        }
    }

    [Migration(5)]
    public class Fifth_Migration : Migration
    {
        public override void Up()
        {
            using (var session = DocumentStore.OpenSession())
            {
                session.Store(new {Id = "forth-migration-document"});
                session.SaveChanges();
            }
        }
    }
}
