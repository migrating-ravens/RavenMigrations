using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;

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
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // Add Raven singleton.
            var docStore = new DocumentStore
            {
                Database = "Demo",
                Urls = new[] { "http://live-test.ravendb.net" }
            };
            docStore.Initialize();
            services.AddSingleton<IDocumentStore>(docStore);

            // Now add MigrationRunner singleton.
            services.AddRavenDbMigrations(); // Optional: .AddRavenDbMigrations(options => ...);

            // We can run the migrations anytime we wish. 
            // For this sample, we'll run them here, inside ConfigureServices.
            var migrationRunner = services.BuildServiceProvider().GetRequiredService<MigrationRunner>();
            migrationRunner.Run(); // Finds all Migration classes in this assembly and runs any that need to be applied to the database, applying them in order.
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc();
        }
    }
}
