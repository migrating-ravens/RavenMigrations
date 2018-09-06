using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Raven.Client.Documents;
using Raven.TestDriver;
using Xunit;

namespace Raven.Migrations.Tests
{
    public class AlterTests : RavenTestDriver
    {
        private ILogger logger = new ConsoleLogger();
        
        protected override void PreInitialize(IDocumentStore documentStore)
        {
            documentStore.Conventions.MaxNumberOfRequestsPerSession = 100;
        }

        [Fact]
        public void Can_migrate_up()
        {
            using (var store = GetDocumentStore())
            {
                var professorZoom = InitialiseWithPerson(store, "Professor", "Zoom");

                var migration = new AddFullName();
                migration.Setup(store, logger);

                migration.Up();

                using (var session = store.OpenSession())
                {
                    var loaded = session.Load<Person>(professorZoom.Id);
                    loaded.FullName.Should().Be("Professor Zoom");
                }
            }
        }

        [Fact]
        public void Can_migrate_down()
        {
            using (var store = GetDocumentStore())
            {
                var ladyDeathstrike = InitialiseWithPerson(store, "Lady", "Deathstrike");

                var migration = new AddFullName();
                migration.Setup(store, logger);

                migration.Up();
                WaitForIndexing(store);

                migration.Down();
                WaitForIndexing(store);

                using (var session = store.OpenSession())
                {
                    var loaded = session.Load<Person>(ladyDeathstrike.Id);
                    loaded.FullName.Should().Be(null);
                }
            }
        }

        [Fact(Skip = "no logging in place")]
        public void Logger_WriteInformation_is_called_when_altering_collection()
        {
            var loggerMock = new Mock<ILogger>();

            using (var store = GetDocumentStore())
            {
                var scarletSpider = InitialiseWithPerson(store, "Scarlet", "Spider");

                var migration = new AddFullName();
                migration.Setup(store, loggerMock.Object);

                migration.Up();
            }

            loggerMock.Verify(logger => logger.LogInformation("Updated {0} documents", 1), "Informational message should indicate how many documents were updated.");
        }

        [Fact(Skip = "no logging in place")]
        public void Logger_WriteInformation_is_called_per_batch_when_altering_collection()
        {
            var loggerMock = new Mock<ILogger>();

            using (var store = GetDocumentStore())
            {
                InitialiseWithPeople(store, new List<Person>
                {
                    new Person {FirstName = "Animal", LastName = "Man" },
                    new Person {FirstName = "Aqua", LastName = "Baby" },
                    new Person {FirstName = "Atom", LastName = "Girl" },
                    new Person {FirstName = "Alex", LastName = "Mercer" },
                    new Person {FirstName = "Killer", LastName = "Croc" }
                });
                var migration = new AddFullName();
                migration.Setup(store, loggerMock.Object);

                migration.Up();
            }

            loggerMock.Verify(l => l.LogInformation("Updated {0} documents", 2), Times.Exactly(2), "Informational message should indicate how many documents were updated.");
            loggerMock.Verify(l => l.LogInformation("Updated {0} documents", 1), Times.Once, "Informational message should indicate how many documents were updated.");
        }

        private Person InitialiseWithPerson(IDocumentStore store, string firstName, string lastName)
        {
            using (var session = store.OpenSession())
            {
                var person = new Person { Id = "People/1", FirstName = firstName, LastName = lastName };
                session.Store(person);
                session.SaveChanges();
                return person;
            }
        }

        private void InitialiseWithPeople(IDocumentStore store, List<Person> people)
        {
            using (var session = store.OpenSession())
            {
                people.ForEach(p => session.Store(p));
                session.SaveChanges();
            }
            WaitForIndexing(store);
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

    public class FooBaz
    {
        public int Id { get; set; }
        public string Bar { get; set; }
    }

    public class Person
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
    }
}