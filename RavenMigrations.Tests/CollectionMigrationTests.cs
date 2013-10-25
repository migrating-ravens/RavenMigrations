using System.Linq;
using FluentAssertions;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Indexes;
using Raven.Json.Linq;
using Raven.Tests.Helpers;
using RavenMigrations.Tests.Old;
using Xunit;

namespace RavenMigrations.Tests
{
    namespace Old
    {
        public class Person
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }
    }

    namespace New
    {
        public class Person
        {
            public string FirstName { get; set; }
            public string Id { get; set; }
            public string LastName { get; set; }
        }
    }

    public class CollectionMigrationTests : RavenTestBase
    {
        [Fact]
        public void Can_migrate_down()
        {
            using (var store = NewDocumentStore())
            {
                InitialiseWithPerson(store, "Sean Kearon");

                var migration = new CollectionDocumentMigration();
                migration.Setup(store);
                migration.Up();
                WaitForIndexing(store);

                migration.Down();
                UpdateCollectionMetadataToAllowLoadingByType<Person>(store, "People");
                WaitForIndexing(store);

                using (var session = store.OpenSession())
                {
                    var customer = session.Load<Person>("People/1");
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
                var migration = new CollectionDocumentMigration();
                migration.Setup(store);
                migration.Up();
                WaitForIndexing(store);

                using (var session = store.OpenSession())
                {
                    var customer = session.Load<New.Person>("People/1");
                    customer.FirstName.Should().Be("Sean");
                    customer.LastName.Should().Be("Kearon");
                }
            }
        }

        private void AddInitialPerson(IDocumentStore store, string name)
        {
            using (var session = store.OpenSession())
            {
                session.Store(new Person {Name = name});
                session.SaveChanges();
            }
        }

        private string GetMetadataClrTypeName<T>()
        {
            return string.Join(",", typeof (T).AssemblyQualifiedName.Split(new[] {','}).Take(2));
        }

        private void InitialiseWithPerson(IDocumentStore store, string name)
        {
            new RavenDocumentsByEntityName().Execute(store); //https://groups.google.com/forum/#!topic/ravendb/QqZPrRUwEkE
            AddInitialPerson(store, name);
            UpdateCollectionMetadataToAllowLoadingByType<New.Person>(store, "People");
            WaitForIndexing(store);
        }

        private void UpdateCollectionMetadataToAllowLoadingByType<T>(IDocumentStore store, string tag)
        {
            var assemblyName = GetMetadataClrTypeName<T>();
            WaitForIndexing(store);
            store.DatabaseCommands.UpdateByIndex(
                "Raven/DocumentsByEntityName",
                new IndexQuery {Query = "Tag:" + tag},
                new[]
                {
                    new PatchRequest
                    {
                        Type = PatchCommandType.Modify,
                        Name = "@metadata",
                        Nested = new[]
                        {
                            new PatchRequest
                            {
                                Type = PatchCommandType.Set,
                                Name = "Raven-Clr-Type",
                                Value = new RavenJValue(assemblyName)
                            }
                        }
                    }
                });
        }
    }

    [Migration(1, "CollectionDocumentMigration")]
    public class CollectionDocumentMigration : Migration
    {
        public override void Down()
        {
            Alter.Collection("People", JoinFirstNameAndLastNameIntoName);
        }

        public override void Up()
        {
            Alter.Collection("People", SplitNameToFirstNameAndLastName);
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
}