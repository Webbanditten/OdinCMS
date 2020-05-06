using System.Globalization;
using System.Net;
using KeplerCMS.Data;
using KeplerCMS.Services;
using KeplerCMS.Services.Implementations;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
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


            // Add authentication
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = "HabboCookie";
                options.LoginPath = new Microsoft.AspNetCore.Http.PathString("/account");
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

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICommandQueueService, CommandQueueService>();
            services.AddScoped<IMenuService, MenuService>();
            services.AddScoped<IPageService, PageService>();
            services.AddScoped<IFuseService, FuseService>();
            services.AddScoped<IUploadService, UploadService>();
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
                new CultureInfo(Configuration["culture"])
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

            // who are you?  
            app.UseAuthentication();

            // are you allowed?  
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "slug",
                    pattern: "{slug}",
                    defaults: new { controller = "Page", action = "Index" },
                    constraints: new { slug = ".+" }
                );

                endpoints.MapControllerRoute(
                    name: "slugWithSub",
                    pattern: "{slug}/{subSlug}",
                    defaults: new { controller = "Page", action = "Index" },
                    constraints: new { slug = ".+" }
                );

                endpoints.MapControllerRoute(
                    name: "images",
                    pattern: "images/{category}/{fileName}",
                    defaults: new { controller = "Images", action = "Index" }
                );

                endpoints.MapControllerRoute(
                name: "MyArea",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(name: "api", pattern: "api/{controller=MeApiController}");
            });

        }
    }
}
