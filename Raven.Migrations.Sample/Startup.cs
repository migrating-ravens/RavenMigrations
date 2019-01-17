using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // Add a silly service. Used in one of our patches for demo purposes.
            services.AddTransient<ISillyService>(_ => new SillyService());

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

            // Step 3: Run the migrations.
            // Finds all Migration classes in this assembly and runs any that need to be applied to the database, applying them in order.
            var migrationRunner = app.ApplicationServices.GetRequiredService<MigrationRunner>();
            migrationRunner.Run();
        }
    }
}
