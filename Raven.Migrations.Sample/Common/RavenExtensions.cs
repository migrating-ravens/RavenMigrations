using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Migrations.Sample.Model;
using System;
using System.Linq;
using System.Net.Http;

namespace Raven.Migrations.Sample.Common;

public static class RavenExtensions
{
    public static IDocumentStore EnsureExists(this IDocumentStore store)
    {
        try
        {
            using var dbSession = store.OpenSession();
            dbSession.Query<Shipper>().Take(0).ToList();
        }
        catch (Client.Exceptions.Database.DatabaseDoesNotExistException)
        {
            // Create the database.
            store.Maintenance.Server.Send(new Client.ServerWide.Operations.CreateDatabaseOperation(new Client.ServerWide.DatabaseRecord
            {
                DatabaseName = store.Database
            }));

            // Create sample data
            using var httpClient = new HttpClient();
            using var response = httpClient.Send(new HttpRequestMessage(HttpMethod.Post, "http://live-test.ravendb.net/databases/Demo/studio/sample-data"));
            Console.WriteLine("Creating sample data result: {0} - {1}", response.StatusCode, response.ReasonPhrase);
        }

        return store;
    }
}