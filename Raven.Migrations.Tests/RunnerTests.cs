using System.Linq;
using System.Reflection;
using FluentAssertions;
using Raven.Client.Documents.Indexes;
using Raven.TestDriver;
using Xunit;

namespace Raven.Migrations.Tests
{
    public class RunnerTests : RavenTestDriver
    {
        [Fact]
        public void Document_id_prefix_is_ravenmigrations()
        {
            RavenMigrationHelpers.RavenMigrationsIdPrefix.Should().Be("MigrationRecord");
        }

        [Fact]
        public void Can_change_migration_document_seperator_to_dash()
        {
            var options = new MigrationOptions();
            options.Conventions.MigrationDocumentId(new First_Migration(), '-')
                .Should().Be("migrationrecord-first-migration-1");
        }

        [Fact]
        public void Can_get_migration_id_from_migration()
        {
            var options = new MigrationOptions();
            var id = options.Conventions.MigrationDocumentId(new First_Migration(), '/');
            id.Should().Be("migrationrecord/first/migration/1");
        }

        [Fact]
        public void Can_get_migration_id_from_migration_and_correct_leading_or_multiple_underscores()
        {
            var options = new MigrationOptions();
            var id = options.Conventions.MigrationDocumentId(new _has_problems__with_underscores___(), '/');
            id.Should().Be("migrationrecord/has/problems/with/underscores/5");
        }

        [Fact]
        public void Can_get_migration_attribute_from_migration_type()
        {
            var attribute = typeof(First_Migration).GetMigrationAttribute();
            attribute.Should().NotBeNull();
            attribute.Version.Should().Be(1);
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
            options.MigrationResolver.Should().BeOfType<SimpleMigrationResolver>();
        }

        [Fact]
        public void Default_migration_resolver_can_instantiate_a_migration()
        {
            var migration = new SimpleMigrationResolver().Resolve(typeof(First_Migration));
            migration.Should().NotBeNull();
        }

        [Fact]
        public void Can_run_an_up_migration_against_a_document_store()
        {
            using var store = GetDocumentStore();
            new TestDocumentIndex().Execute(store);

            var runner = new MigrationRunner(store, GetMigrationOptions(), new ConsoleLogger());
            runner.Run();
            WaitForIndexing(store);

            using var session = store.OpenSession();
            session.Query<TestDocument, TestDocumentIndex>()
                .Count()
                .Should().Be(1);
        }

        [Fact]
        public void Calling_run_twice_runs_migrations_only_once()
        {
            using var store = GetDocumentStore();
            new TestDocumentIndex().Execute(store);

            var runner = new MigrationRunner(store, GetMigrationOptions(), new ConsoleLogger());
            runner.Run();

            // oooops, twice!
            runner.Run();

            WaitForIndexing(store);

            using var session = store.OpenSession();
            session.Query<TestDocument, TestDocumentIndex>()
                .Count()
                .Should().Be(1);
        }

        [Fact]
        public void Can_call_up_then_down_on_migrations()
        {
            using var store = GetDocumentStore();
            new TestDocumentIndex().Execute(store);

            var runner = new MigrationRunner(store, GetMigrationOptions(), new ConsoleLogger());
            runner.Run();

            WaitForIndexing(store);

            // flip it and reverse it :P
            var options = GetMigrationOptions();
            options.Direction = Directions.Down;
            var reverseRunner = new MigrationRunner(store, options, new ConsoleLogger());
            reverseRunner.Run();

            WaitForIndexing(store);
            using var session = store.OpenSession();
            session.Query<TestDocument, TestDocumentIndex>()
                .Count()
                .Should().Be(0);
        }

        [Fact]
        public void Can_call_migrations_up_to_a_certain_version()
        {
            using var store = GetDocumentStore();
            new TestDocumentIndex().Execute(store);

            var options = GetMigrationOptions();
            options.ToVersion = 1;
            var runner = new MigrationRunner(store, options, new ConsoleLogger());
            runner.Run();
            WaitForIndexing(store);

            using var session = store.OpenSession();
            session.Query<TestDocument, TestDocumentIndex>()
                .Count()
                .Should().Be(1);

            var shouldNotExist = session.Load<object>("second-document");
            shouldNotExist.Should().BeNull();
        }

        [Fact]
        public void Can_call_migrations_down_to_a_certain_version()
        {
            using var store = GetDocumentStore();
            new TestDocumentIndex().Execute(store);

            var runner = new MigrationRunner(store, GetMigrationOptions(), new ConsoleLogger());
            runner.Run();
            WaitForIndexing(store);

            // migrate down to 
            var options = GetMigrationOptions();
            options.Direction = Directions.Down;
            options.ToVersion = 2;
            var downRunner = new MigrationRunner(store, options, new ConsoleLogger());
            downRunner.Run();

            using var session = store.OpenSession();
            session.Query<TestDocument, TestDocumentIndex>()
                .Count()
                .Should().Be(1);

            var secondId = options.Conventions.MigrationDocumentId(new Second_Migration(), '/');
            var secondMigrationDocument = session.Load<MigrationRecord>(secondId);
            secondMigrationDocument.Should().BeNull();

            var id = options.Conventions.MigrationDocumentId(new First_Migration(), '/');
            var firstMigrationDocument = session.Load<MigrationRecord>(id);
            firstMigrationDocument.Should().NotBeNull();
        }

        [Fact]
        public void Can_call_migrations_with_development_profile()
        {
            using var store = GetDocumentStore();
            new TestDocumentIndex().Execute(store);

            var options = GetMigrationOptions();
            options.Profiles = new[] { "development" };
            var runner = new MigrationRunner(store, options, new ConsoleLogger());
            runner.Run();
            WaitForIndexing(store);

            using var session = store.OpenSession();
            var development = session.Load<object>("development-1");
            development.Should().NotBeNull();
        }

        [Fact]
        public void Can_call_migrations_with_demo_profile()
        {
            using var store = GetDocumentStore();
            new TestDocumentIndex().Execute(store);

            var options = GetMigrationOptions();
            options.Profiles = new[] { "demo" };
            var runner = new MigrationRunner(store, options, new ConsoleLogger());
            runner.Run();
            WaitForIndexing(store);

            using var session = store.OpenSession();
            var development = session.Load<object>("development-1");
            development.Should().NotBeNull();
        }

        [Fact]
        public void Can_call_migrations_ignore_migrations_with_profile()
        {
            using var store = GetDocumentStore();
            new TestDocumentIndex().Execute(store);

            var runner = new MigrationRunner(store, GetMigrationOptions(), new ConsoleLogger());
            runner.Run();
            WaitForIndexing(store);

            using var session = store.OpenSession();
            var development = session.Load<object>("development-1");
            development.Should().BeNull();
        }

        [Fact]
        public void Can_call_migrations_that_are_not_direct_subclasses_of_Migration()
        {
            using var store = GetDocumentStore();
            new TestDocumentIndex().Execute(store);

            var options = GetMigrationOptions();
            options.Profiles = new[] { "uses-BaseMigration" };
            var runner = new MigrationRunner(store, options, new ConsoleLogger());
            runner.Run();
            WaitForIndexing(store);

            using var session = store.OpenSession();
            var development = session.Load<object>("migrated-using-BaseMigration");
            development.Should().NotBeNull();
        }
        
        private MigrationOptions GetMigrationOptions()
        {
            var options = new MigrationOptions();
            options.Assemblies.Add(Assembly.GetExecutingAssembly());
            return options;
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
            using var session = DocumentStore.OpenSession();
            session.Store(new TestDocument { Id = "TestDocuments/1", Name = "Yehuda Gavriel" });
            session.SaveChanges();
        }

        public override void Down()
        {
            using var session = DocumentStore.OpenSession();
            session.Delete("TestDocuments/1");
            session.SaveChanges();
        }
    }

    [Migration(2)]
    public class Second_Migration : Migration
    {
        public override void Up()
        {
            using var session = DocumentStore.OpenSession();
            session.Store(new { Id = "second-document", Name = "woot!" });
            session.SaveChanges();
        }

        public override void Down()
        {
            using var session = DocumentStore.OpenSession();
            var doc = session.Load<object>("second-document");
            session.Delete(doc);
            session.SaveChanges();
        }
    }

    [Migration(3, "development", "demo")]
    public class Development_Migration : Migration
    {
        public override void Up()
        {
            using var session = DocumentStore.OpenSession();
            session.Store(new { Id = "development-1" });
            session.SaveChanges();
        }
    }

    [Migration(4, "uses-BaseMigration")]
    public class Subclass_of_BaseMigration : BaseMigration
    {
        public override void Up()
        {
            using var session = DocumentStore.OpenSession();
            session.Store(new { Id = "migrated-using-BaseMigration" });
            session.SaveChanges();
        }
    }    

    [Migration(5, "exclude-me")]
    public class _has_problems__with_underscores___ : Migration
    {
        public override void Up()
        {
        }
    }    
    
    public abstract class BaseMigration : Migration
    {
        public override void Up()
        {
        }
    }
}
