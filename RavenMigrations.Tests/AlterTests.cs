using System.Collections.Generic;
using FluentAssertions;
using Raven.Abstractions.Commands;
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
                migration.Setup(store, new NullLogger());

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
                migration.Setup(store, new NullLogger());

                migration.Up();
                using (var session = store.OpenSession())
                {
                    var customer = session.Load<Person2>("People/1");
                    customer.FirstName.Should().Be("Sean");
                    customer.LastName.Should().Be("Kearon");
                }
            }
        }

        [Fact]
        public void Can_add_additional_commands_as_part_of_migration()
        {
            using (var store = NewDocumentStore())
            {
                InitialiseWithPerson(store, "Sean Kearon");

                var migration = new AlterCollectionMigration();
                migration.Setup(store, new NullLogger());

                migration.Up();

                using (var session = store.OpenSession())
                {
                    var foobaz = session.Load<FooBaz>(1);
                    foobaz.Bar.Should().BeEquivalentTo("loaded");
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
            Alter.CollectionWithAdditionalCommands("Person1s", MigratePerson1ToPerson2);
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

        private IEnumerable<ICommandData> MigratePerson1ToPerson2(RavenJObject doc, RavenJObject metadata)
        {
            var name = doc.Value<string>("Name");
            if (!string.IsNullOrEmpty(name))
            {
                doc["FirstName"] = name.Split(' ')[0];
                doc["LastName"] = name.Split(' ')[1];
            }
            doc.Remove("Name");

            metadata[Constants.RavenClrType] = "RavenMigrations.Tests.Person2, RavenMigrations.Tests";

            var foobaz = new FooBaz
            {
                Id = 1,
                Bar = "loaded"
            };

            var foobazDoc = RavenJObject.FromObject(foobaz);
            var meta = new RavenJObject();
            meta[Constants.RavenEntityName] = "FooBazs";
            var cmd = new PutCommandData
            {
                Document = foobazDoc,
                Key = "foobazs/" + foobaz.Id,
                Metadata = meta
            };

            return new[] {cmd};
        }
    }

    public class FooBaz
    {
        public int Id { get; set; }
        public string Bar { get; set; }
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