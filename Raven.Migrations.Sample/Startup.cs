using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using Raven.Migrations.Sample.Services;

namespace Raven.Migrations.Sample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();

            // Add a silly service. Used in one of our patches for demo purposes.
            services.AddTransient<ISillyService>(_ => new SillyService());

            // Setup in Raven Migrations in 3 steps:
            //
            // 1. Create a Raven document store and add it as a singleton.
            // 2. Call services.AddRavenDbMigrations() to add a MigrationRunner.
            // 3. In .Configure(...), call migrationRunner.Run(). This will run any Migration objects in your code. See /Migrations directory for some sample migrations.

            // Step 1: Add Raven singleton.
            var docStore = new DocumentStore
            {
                Database = "Demo",
                Urls = new[] { "http://live-test.ravendb.net" }
            };
            docStore.Initialize();
            services.AddSingleton<IDocumentStore>(docStore);

            // Step 2: Add MigrationRunner singleton. 
            // Later we'll use this to execute our migrations inside Startup.Configure(...) method below.
            services.AddRavenDbMigrations();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });

            // Step 3: Run the migrations.
            // Finds all Migration classes in this assembly and runs any that 
            // need to be applied to the database, applying them in order.
            var migrationRunner = app.ApplicationServices.GetRequiredService<MigrationRunner>();
            migrationRunner.Run();
        }
    }
}
