using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KeplerCMS.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Westwind.Globalization;
using Westwind.Globalization.AspnetCore;

namespace KeplerCMS
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
            // Standard ASP.NET Localization features are recommended
            // Make sure this is done FIRST!
            services.AddLocalization(options =>
            {
                // I prefer Properties over the default `Resources` folder
                // due to namespace issues if you have a Resources type as
                // most people do for shared resources.
                options.ResourcesPath = "Resources";
            });


            // Replace StringLocalizers with Db Resource Implementation
            services.AddSingleton(typeof(IStringLocalizerFactory),
                                  typeof(DbResStringLocalizerFactory));
            services.AddSingleton(typeof(IHtmlLocalizerFactory),
                                  typeof(DbResHtmlLocalizerFactory));


            // Required: Enable Westwind.Globalization (opt parm is optional)
            // shown here with optional manual configuration code
            services.AddWestwindGlobalization(opt =>
            {
                opt.ConnectionString = Configuration.GetConnectionString("DefaultConnection");
                opt.ConfigureAuthorizeLocalizationAdministration(actionContext =>
                {
                    return false; 
                });

            });







            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
              
            });

            services.AddDbContextPool<DataContext>(
              options => options.UseMySQL(Configuration.GetConnectionString("DefaultConnection")
           ));

            services.AddRouting(options => options.LowercaseUrls = true);


            services.AddControllersWithViews().AddViewLocalization();

            // this *has to go here* after view localization has been initialized
            // so that Pages can localize - note required even if you're not using
            // the DbResource manager.
            services.AddTransient<IViewLocalizer, DbResViewLocalizer>();

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
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Disable this because its going to be run behind a proxy
            //app.UseHttpsRedirection();

            var supportedCultures = new[]
            {
                new CultureInfo("da-DK")
                //new CultureInfo("en-US")
            };


            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(Configuration["culture"]),
                // Formatting numbers, dates, etc.
                SupportedCultures = supportedCultures,
                // UI strings that we have localized.
                SupportedUICultures = supportedCultures
            });



            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();
            app.UseAuthentication();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                   name: "default",
                   pattern: "{controller=Home}/{action=Index}/{id?}");
            });

        }
    }
}
