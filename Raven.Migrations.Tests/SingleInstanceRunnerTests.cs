using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Raven.TestDriver;
using Xunit;

namespace Raven.Migrations.Tests
{
    public class SingleInstanceRunnerTests : RavenTestDriver
    {
        [Fact]
        public async Task Calling_run_in_parallel_runs_migrations_only_once()
        {
            using var documentStore = GetDocumentStore();
            await new TestDocumentIndex().ExecuteAsync(documentStore);
            
            var instanceOne = new SingleInstanceRunner(documentStore, GetMigrationOptions(), new ConsoleLogger());
            var instanceTwo = new SingleInstanceRunner(documentStore, GetMigrationOptions(),  new ConsoleLogger() );

            var first = Task.Run(() => instanceOne.Run());
            var second = Task.Run(() => instanceTwo.Run());

            await Task.WhenAll(first, second);
            
            WaitForIndexing(documentStore);
            WaitForUserToContinueTheTest(documentStore);

            using var session = documentStore.OpenSession();
            session.Query<TestDocument, TestDocumentIndex>()
                .Count()
                .Should().Be(1);
        }
        
        private static MigrationOptions GetMigrationOptions()
        {
            var options = new MigrationOptions();
            options.Assemblies.Add(Assembly.GetExecutingAssembly());
            return options;
        }
    }
}