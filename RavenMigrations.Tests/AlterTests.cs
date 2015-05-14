using System.Threading;
using FluentAssertions;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Indexes;
using Raven.Json.Linq;
using Raven.Tests.Helpers;
using Xunit;

namespace RavenMigrations.Tests
{
    public class AlterTests : RavenTestBase
    {
        [Fact]
        public void Can_migrate_down_from_new_clr_type()
        {
            using (var store = NewDocumentStore())
            {
                InitialiseWithPerson(store, "Sean Kearon");

                var migration = new AlterCollectionMigration();
                migration.Setup(store);

                migration.Up();
                WaitForIndexing(store);

                migration.Down();
                WaitForIndexing(store);

                using (var session = store.OpenSession())
                {
                    var customer = session.Load<Person1>("People/1");
                    customer.Name.Should().Be("Sean Kearon");
                }
            }
        }

        [Fact]
        public void Can_migrate_up_to_new_clr_type()
        {
            using (var store = NewDocumentStore())
            {
                InitialiseWithPerson(store, "Sean Kearon");

                var migration = new AlterCollectionMigration();
                migration.Setup(store);

                migration.Up();
                WaitForIndexing(store);

                Thread.SpinWait(100000000);

                using (var session = store.OpenSession())
                {
                    var customer = session.Load<Person2>("People/1");
                    customer.FirstName.Should().Be("Sean");
                    customer.LastName.Should().Be("Kearon");
                }
            }
        }

        private void InitialiseWithPerson(IDocumentStore store, string name)
        {
            new RavenDocumentsByEntityName().Execute(store); //https://groups.google.com/forum/#!topic/ravendb/QqZPrRUwEkE
            using (var session = store.OpenSession())
            {
                session.Store(new Person1 {Id = "People/1", Name = name});
                session.SaveChanges();
            }
            WaitForIndexing(store);
        }
    }

    [Migration(1, "alter")]
    public class AlterCollectionMigration : Migration
    {
        public override void Down()
        {
            Alter.Collection("Person1s", MigratePerson2ToPerson1);
        }

        public override void Up()
        {
            Alter.Collection("Person1s", MigratePerson1ToPerson2);
        }

        private void MigratePerson2ToPerson1(RavenJObject doc, RavenJObject metadata)
        {
            var first = doc.Value<string>("FirstName");
            var last = doc.Value<string>("LastName");

            doc["Name"] = first + " " + last;
            doc.Remove("FirstName");
            doc.Remove("LastName");

            metadata[Constants.RavenClrType] = "RavenMigrations.Tests.Person1, RavenMigrations.Tests";
        }

        private void MigratePerson1ToPerson2(RavenJObject doc, RavenJObject metadata)
        {
            var name = doc.Value<string>("Name");
            if (!string.IsNullOrEmpty(name))
            {
                doc["FirstName"] = name.Split(' ')[0];
                doc["LastName"] = name.Split(' ')[1];
            }
            doc.Remove("Name");

            metadata[Constants.RavenClrType] = "RavenMigrations.Tests.Person2, RavenMigrations.Tests";
        }
    }

    public class Person1
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class Person2
    {
        public string FirstName { get; set; }
        public string Id { get; set; }
        public string LastName { get; set; }
    }
}