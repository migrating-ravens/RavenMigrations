using System.Linq;
using FluentAssertions;
using Raven.Client.Indexes;
using Raven.Tests.Helpers;
using Xunit;

namespace RavenMigrations.Tests
{
    public class HotfixRunnerTests : RavenTestBase
    {
        [Fact]
        public void Document_id_prefix_is_ravenhotfixes()
        {
            RavenHotfixHelpers.RavenHotfixIdPrefix.Should().Be("ravenhotfixes");
        }

        [Fact]
        public void Can_change_hotfix_document_seperator_to_dash()
        {
            new Some_Hotfix().GetHotfixIdFromName(seperator: '-')
                .Should().Be("ravenhotfixes-some-hotfix-1");
        }

        [Fact]
        public void Can_get_hotfix_id_from_hotfox()
        {
            var id = new Some_Hotfix().GetHotfixIdFromName();
            id.Should().Be("ravenhotfixes/some/hotfix/1");
        }

        [Fact]
        public void Can_get_hotfix_attribute_from_hotfix_type()
        {
            var attribute = typeof(Some_Hotfix).GetHotfixAttribute();
            attribute.Should().NotBeNull();
            attribute.Version.Should().Be(1);
            attribute.Name.Should().Be("Some_Hotfix");
        }

        [Fact]
        public void Default_resolver_should_be_DefaultHotfixResolver()
        {
            var options = new HotfixOptions();
            options.HotfixResolver.Should().NotBeNull();
            options.HotfixResolver.Should().BeOfType<DefaultHotfixResolver>();
        }

        [Fact]
        public void Default_hotfix_resolver_can_instantiate_a_hotfix()
        {
            var hotfix = new DefaultHotfixResolver().Resolve(typeof(Some_Hotfix));
            hotfix.Should().NotBeNull();
        }

        [Fact]
        public void Can_run_an_hotfix_against_a_document_store()
        {
            using (var store = NewDocumentStore())
            {
                new TestDocumentIndex().Execute(store);

                HotfixRunner.Run(store, new HotfixOptions { Version = 1 });
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
        public void Calling_run_twice_runs_hotfix_only_once()
        {
            using (var store = NewDocumentStore())
            {
                new TestDocumentIndex().Execute(store);

                HotfixRunner.Run(store, new HotfixOptions { Version = 1 });
                // oooops, twice!
                HotfixRunner.Run(store, new HotfixOptions { Version = 1 });
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
        public void Can_apply_hotfix_by_a_hotfix_version()
        {
            using (var store = NewDocumentStore())
            {
                new TestDocumentIndex().Execute(store);

                HotfixRunner.Run(store, new HotfixOptions { Version = 1 });
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
        public void Can_apply_hotfix_by_a_hotfix_name()
        {
            using (var store = NewDocumentStore())
            {
                new TestDocumentIndex().Execute(store);

                HotfixRunner.Run(store, new HotfixOptions { HotfixName = "Some_Hotfix" });
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
        public void Can_apply_hotfix_with_profile_and_version()
        {
            using (var store = NewDocumentStore())
            {
                new TestDocumentIndex().Execute(store);

                HotfixRunner.Run(store, new HotfixOptions { Profiles = new[] { "development" }, Version = 3});
                WaitForIndexing(store);

                using (var session = store.OpenSession())
                {
                    var development = session.Load<object>("development-1");
                    development.Should().NotBeNull();
                }
            }
        }

        [Fact]
        public void Can_apply_hotfix_with_profile_and_same_version()
        {
            using (var store = NewDocumentStore())
            {
                new TestDocumentIndex().Execute(store);

                HotfixRunner.Run(store, new HotfixOptions { Profiles = new[] { "production" }, Version = 3 });
                WaitForIndexing(store);

                using (var session = store.OpenSession())
                {
                    var development = session.Load<object>("production-1");
                    development.Should().NotBeNull();
                }
            }
        }

        [Fact]
        public void Can_apply_hotfix_with_profile_and_name()
        {
            using (var store = NewDocumentStore())
            {
                new TestDocumentIndex().Execute(store);

                HotfixRunner.Run(store, new HotfixOptions { Profiles = new[] { "development" }, HotfixName = "Development_Hotfix" });
                WaitForIndexing(store);

                using (var session = store.OpenSession())
                {
                    var development = session.Load<object>("development-1");
                    development.Should().NotBeNull();
                }
            }
        }

        [Fact]
        public void Can_apply_hotfix_ignore_hotfix_with_profile()
        {
            using (var store = NewDocumentStore())
            {
                new TestDocumentIndex().Execute(store);

                HotfixRunner.Run(store, new HotfixOptions { Version = 1 });
                WaitForIndexing(store);

                using (var session = store.OpenSession())
                {
                    var development = session.Load<object>("development-1");
                    development.Should().BeNull();

                    var production = session.Load<object>("production-1");
                    production.Should().BeNull();
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

        [Hotfix(1)]
        public class Some_Hotfix : Hotfix
        {
            public override void Apply()
            {
                using (var session = DocumentStore.OpenSession())
                {
                    session.Store(new TestDocument { Name = "Khalid Abuhakmeh" });
                    session.SaveChanges();
                }
            }
        }

        [Hotfix(2)]
        public class Another_Hotfix : Hotfix
        {
            public override void Apply()
            {
                using (var session = DocumentStore.OpenSession())
                {
                    session.Store(new { Id = "second-document", Name = "woot!" });
                    session.SaveChanges();
                }
            }
        }

        [Hotfix(3, "development")]
        public class Development_Hotfix : Hotfix
        {
            public override void Apply()
            {
                using (var session = DocumentStore.OpenSession())
                {
                    session.Store(new { Id = "development-1" });
                    session.SaveChanges();
                }
            }
        }

        [Hotfix(3, "production")]
        public class Production_Hotfix : Hotfix
        {
            public override void Apply()
            {
                using (var session = DocumentStore.OpenSession())
                {
                    session.Store(new { Id = "production-1" });
                    session.SaveChanges();
                }
            }
        }

    }
}