using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Operations.Indexes;
using Raven.TestDriver;
using Xunit;

namespace Raven.Migrations.Tests
{
    public class MigrationTests : RavenTestDriver
    {
        private readonly ILogger logger = new ConsoleLogger();
        private readonly List<Assembly> assemblies = new List<Assembly> { typeof(MigrationTests).Assembly };

        protected override void PreInitialize(IDocumentStore documentStore)
        {
            documentStore.Conventions.MaxNumberOfRequestsPerSession = 100;
        }

        [Fact]
        public void Can_migrate_up()
        {
            using var store = GetDocumentStore();
            var professorZoom = InitialiseWithPerson(store, "Professor", "Zoom");

            var migration = new AddFullName();
            migration.Setup(store, new MigrationOptions(), logger);

            migration.Up();

            using var session = store.OpenSession();
            var loaded = session.Load<Person>(professorZoom.Id);
            loaded.FullName.Should().Be("Professor Zoom");
        }

        [Fact]
        public void Can_migrate_down()
        {
            using var store = GetDocumentStore();
            var ladyDeathstrike = InitialiseWithPerson(store, "Lady", "Deathstrike");

            var migration = new AddFullName();
            migration.Setup(store, new MigrationOptions(), logger);

            migration.Up();
            WaitForIndexing(store);

            migration.Down();
            WaitForIndexing(store);

            using var session = store.OpenSession();
            var loaded = session.Load<Person>(ladyDeathstrike.Id);
            loaded.FullName.Should().Be(null);
        }

        [Fact(Skip = "no logging in place")]
        public void Logger_WriteInformation_is_called_when_altering_collection()
        {
            var loggerMock = new Mock<ILogger>();

            using (var store = GetDocumentStore())
            {
                var scarletSpider = InitialiseWithPerson(store, "Scarlet", "Spider");

                var migration = new AddFullName();
                migration.Setup(store, new MigrationOptions(), loggerMock.Object);

                migration.Up();
            }

            loggerMock.Verify(logger => logger.LogInformation("Updated {0} documents", 1), "Informational message should indicate how many documents were updated.");
        }

        [Fact]
        public async Task Calling_run_in_parallel_runs_migrations_only_once()
        {
            using var documentStore = GetDocumentStore();
            await new TestDocumentIndex().ExecuteAsync(documentStore);

            var instanceOne = new MigrationRunner(documentStore, new MigrationOptions() { Assemblies = assemblies }, new ConsoleLogger());
            var instanceTwo = new MigrationRunner(documentStore, new MigrationOptions() { Assemblies = assemblies }, new ConsoleLogger());

            var first = Task.Run(() => instanceOne.Run());
            var second = Task.Run(() => instanceTwo.Run());

            await Task.WhenAll(first, second);

            WaitForIndexing(documentStore);
            WaitForUserToContinueTheTest(documentStore);

            using var session = documentStore.OpenSession();
            var testDocCount = session.Query<TestDocument, TestDocumentIndex>().Count();
            testDocCount
                .Should()
                .Be(1);
        }

        [Fact]
        public async Task Migration_should_wait_for_index_to_become_non_stale_if_stale_timeout_set()
        {
            using var documentStore = GetDocumentStore();
            await new TestPersonIndex().ExecuteAsync(documentStore);

            await documentStore.Maintenance.SendAsync(new StopIndexingOperation());
            var professorZoom = InitialiseWithPerson(documentStore, "Professor", "Zoom");

            var migration = new AddFullNameWithStalenessTimeout();
            migration.Setup(documentStore, new MigrationOptions(), logger);

            var migrationTask = Task.Run(() => migration.Up());
            while (migrationTask.Status != TaskStatus.Running)
                await Task.Delay(100);

            await documentStore.Maintenance.SendAsync(new StartIndexingOperation());
            await migrationTask;

            using var session = documentStore.OpenSession();
            var loaded = session.Load<Person>(professorZoom.Id);
            loaded.FullName.Should().Be("Professor Zoom");
        }

        private Person InitialiseWithPerson(IDocumentStore store, string firstName, string lastName)
        {
            using var session = store.OpenSession();
            var person = new Person { Id = "People/1", FirstName = firstName, LastName = lastName };
            session.Store(person);
            session.SaveChanges();
            return person;
        }
    }

    [Migration(1, "alter")]
    public class AddFullName : Migration
    {
        public override void Up()
        {
            PatchCollection("from People update { this.FullName = this.FirstName + ' ' + this.LastName; }");
        }

        public override void Down()
        {
            PatchCollection("from People update { delete this.FullName; }");
        }
    }

    [Migration(2, "alter")]
    public class AddFullNameWithStalenessTimeout : Migration
    {
        public override void Up()
        {
            PatchCollection(
                "from index 'TestPersonIndex' update { this.FullName = this.FirstName + ' ' + this.LastName; }",
                staleTimeout: TimeSpan.FromMinutes(5));
        }

        public override void Down()
        {
            PatchCollection(
                "from index 'TestPersonIndex' update { delete this.FullName; }",
                staleTimeout: TimeSpan.FromMinutes(5));
        }
    }

    public class Person
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
    }

    public class TestPersonIndex : AbstractIndexCreationTask<Person>
    {
        public TestPersonIndex()
        {
            Map = tests => from t in tests
                select new { t.Id, t.FullName };
        }
    }
}