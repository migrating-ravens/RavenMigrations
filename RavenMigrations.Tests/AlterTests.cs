using System.Threading;
using FluentAssertions;
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
        public void Can_alter_metadata_clr_type()
        {
            using (var store = NewDocumentStore())
            {
                InitialiseWithPerson(store, "Sean Kearon");

                var migration = new MetadataClrTypeMigration();
                migration.Setup(store);

                migration.Up();
                WaitForIndexing(store);

                using (var session = store.OpenSession())
                {
                    var person2 = session.Load<Person2>("People/1");
                    person2.Should().NotBeNull("we should be able to load the person as a Person2.");
                }

                migration.Down();
                WaitForIndexing(store);

                using (var session = store.OpenSession())
                {
                    var person1 = session.Load<Person1>("People/1");
                    person1.Should().NotBeNull("we should be able to load the person as a Person2.");
                }
            }
        }

        [Fact]
        public void Can_migrate_down()
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
        public void Can_migrate_up()
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
                session.Store(new Person1 { Id = "People/1", Name = name });
                session.SaveChanges();
            }
            WaitForIndexing(store);
        }
    }

    public class AlterCollectionMigration : Migration
    {
        public override void Down()
        {
            Alter.Metadata.RavenClrType("Person1s", "RavenMigrations.Tests.Person1, RavenMigrations.Tests");
            WaitForIndexing();
            Alter.Collection("Person1s", JoinFirstNameAndLastNameIntoName);
        }

        public override void Up()
        {
            Alter.Metadata.RavenClrType("Person1s", "RavenMigrations.Tests.Person2, RavenMigrations.Tests");
            WaitForIndexing();
            Alter.Collection("Person1s", SplitNameToFirstNameAndLastName);
        }

        private void JoinFirstNameAndLastNameIntoName(RavenJObject obj)
        {
            var first = obj.Value<string>("FirstName");
            var last = obj.Value<string>("LastName");

            obj["Name"] = first + " " + last;
            obj.Remove("FirstName");
            obj.Remove("LastName");
        }

        private void SplitNameToFirstNameAndLastName(RavenJObject obj)
        {
            var name = obj.Value<string>("Name");
            if (!string.IsNullOrEmpty(name))
            {
                obj["FirstName"] = name.Split(' ')[0];
                obj["LastName"] = name.Split(' ')[1];
            }
            obj.Remove("Name");
        }
    }

    public class MetadataClrTypeMigration : Migration
    {
        public override void Down()
        {
            Alter.Metadata.RavenClrType("Person1s", "RavenMigrations.Tests.Person1, RavenMigrations.Tests");
        }

        public override void Up()
        {
            Alter.Metadata.RavenClrType("Person1s", "RavenMigrations.Tests.Person2, RavenMigrations.Tests");
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