using System.Linq;
using FluentAssertions;
using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;
using Raven.Tests.Helpers;
using Xunit;

namespace RavenMigrations.Tests
{
    public class PatchMigrationTests : RavenTestBase
    {

        public PatchMigrationTests()
        {

        }

        [Fact]
        public void Patch_runs_only_for_the_given_entity_type()
        {
            var collector = new TypesMigrationCollector(new DefaultMigrationResolver(),
                new[] {typeof (CreateDocument), typeof (PatchDocument)});

            using (var store = NewDocumentStore())
            {
                Runner.Run(store, migrationCollector: collector);
                using (var session = store.OpenSession())
                {
                    var sampleDocument = session.Load<SampleDoc>("sample-document");
                    sampleDocument.Name.Should().Be("woot patched");
                    var otherSampleDocument = session.Load<OtherSampleDoc>("other-sample-document");
                    otherSampleDocument.Name.Should().Be("woot");
                }
            }
        }

        [Fact]
        public void Can_run_patch_down()
        {
            var collector = new TypesMigrationCollector(new DefaultMigrationResolver(),
                   new[] { typeof(CreateDocument), typeof(PatchDocument) });

            using (var store = NewDocumentStore())
            {
                Runner.Run(store, migrationCollector: collector);
                Runner.Run(store, new MigrationOptions() {Direction = Directions.Down}, collector);
                using (var session = store.OpenSession())
                {
                    var sampleDocument = session.Load<SampleDoc>("sample-document");
                    sampleDocument.Name.Should().Be("woot");
                    var otherSampleDocument = session.Load<OtherSampleDoc>("other-sample-document");
                    otherSampleDocument.Name.Should().Be("woot");
                }
            }
        }
    }


    internal class SampleDoc
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    internal class OtherSampleDoc
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    [Migration(1)]
    internal class CreateDocument : Migration
    {
        public override void Up()
        {
            using (var session = DocumentStore.OpenSession())
            {
                session.Store(new SampleDoc {Id = "sample-document", Name = "woot"});
                session.Store(new OtherSampleDoc {Id = "other-sample-document", Name = "woot"});
                session.SaveChanges();
            }
        }
    }

    [Migration(2)]
    internal class PatchDocument : EntityPatchMigration<SampleDoc>
    {
        public override string UpPatch
        {
            get { return @"
this.Name = this.Name + ' patched';
"; }
        }

        public override string DownPatch
        {
            get { return @"
this.Name = this.Name.replace(' patched','');
"; }
        }
    }
}