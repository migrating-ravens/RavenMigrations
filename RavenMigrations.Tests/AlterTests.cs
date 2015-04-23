using System;
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Indexes;
using Raven.Json.Linq;
using Raven.Tests.Helpers;
using RavenMigrations.Verbs;
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

        [Fact]
        public void Can_migrate_only_subset()
        {
            using (var store = NewDocumentStore())
            {
                var lastModifieds = InitialiseWithAnimals(store);

                Thread.Sleep(50);

                var migration = new AlterCollectionSubsetMigration();
                migration.Setup(store);

                migration.Up();
                WaitForIndexing(store);

                using (var session = store.OpenSession())
                {
                    var animal1 = session.Load<Animal>("Animals/1");
                    animal1.Name.Should().Be("Lion");
                    var animal2 = session.Load<Animal>("Animals/2");
                    animal2.Name.Should().Be("Tiger");

                    var metadata1 = session.Advanced.GetMetadataFor(animal1);
                    metadata1[Constants.LastModified].Value<DateTime>().Should().NotBe(lastModifieds[0]);
                    var metadata2 = session.Advanced.GetMetadataFor(animal2);
                    metadata2[Constants.LastModified].Value<DateTime>().Should().Be(lastModifieds[1]);
                }
            }
        }

        [Fact]
        public void Can_migrate_using_temp_index()
        {
            using (var store = NewDocumentStore())
            {
                var lastModifieds = InitialiseWithAnimals(store);

                Thread.Sleep(50);

                var migration = new AlterCollectionUsingTempIndexMigration();
                migration.Setup(store);

                migration.Up();
                WaitForIndexing(store);

                using (var session = store.OpenSession())
                {
                    var animal1 = session.Load<Animal>("Animals/1");
                    animal1.Name.Should().Be("Lion");
                    var animal2 = session.Load<Animal>("Animals/2");
                    animal2.Name.Should().Be("Tiger");

                    var metadata1 = session.Advanced.GetMetadataFor(animal1);
                    metadata1[Constants.LastModified].Value<DateTime>().Should().NotBe(lastModifieds[0]);
                    var metadata2 = session.Advanced.GetMetadataFor(animal2);
                    metadata2[Constants.LastModified].Value<DateTime>().Should().Be(lastModifieds[1]);
                }

                store.DatabaseCommands.GetIndex(Alter.TemporaryMigrationIndex.Name).Should().BeNull();
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

        private List<DateTime> InitialiseWithAnimals(IDocumentStore store)
        {
            var lastModifieds = new List<DateTime>();
            new RavenDocumentsByEntityName().Execute(store); //https://groups.google.com/forum/#!topic/ravendb/QqZPrRUwEkE
            using (var session = store.OpenSession())
            {
                var animal1 = new Animal { Id = "Animals/1", Name = "Lyon" };
                var animal2 = new Animal { Id = "Animals/2", Name = "Tiger" };
                
                session.Store(animal1);
                session.Store(animal2);

                session.SaveChanges();

                var metadata1 = session.Advanced.GetMetadataFor(animal1);
                var metadata2 = session.Advanced.GetMetadataFor(animal2);

                lastModifieds.Add(metadata1[Constants.LastModified].Value<DateTime>());
                lastModifieds.Add(metadata2[Constants.LastModified].Value<DateTime>());
            }
            WaitForIndexing(store);

            return lastModifieds;
        }
    }

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

    public class AlterCollectionSubsetMigration : Migration
    {
        public override void Up()
        {
            Alter.CollectionSubset("Animals", MigrateDocument);
        }

        private bool MigrateDocument(RavenJObject doc, RavenJObject metadata)
        {
            if (doc["Name"].Value<string>() == "Lyon")
            {
                doc["Name"] = new RavenJValue("Lion");
                return true;
            }

            return false;
        }
    }

    public class AlterCollectionUsingTempIndexMigration : Migration
    {
        public override void Up()
        {
            var map = @"
                from document in docs.Animals
                select new {
                    Name = document.Name
                }
                ";
            var query = "Name:Lyon";
            Alter.DocumentsViaTempIndex(map, query, MigrateDocument);
        }

        private bool MigrateDocument(RavenJObject doc, RavenJObject metadata)
        {
            if (doc["Name"].Value<string>() == "Lyon")
            {
                doc["Name"] = new RavenJValue("Lion");
                return true;
            }

            return false;
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

    public class Animal
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}