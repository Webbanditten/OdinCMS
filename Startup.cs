using System.Globalization;
using System.Net;
using KeplerCMS.BackgroundServices;
using KeplerCMS.Data;
using KeplerCMS.Filters;
using KeplerCMS.Helpers;
using KeplerCMS.Hubs;
using KeplerCMS.Services;
using KeplerCMS.Services.Implementations;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Mjml.AspNetCore;
using Sentry;
using Westwind.Globalization.AspnetCore;

namespace KeplerCMS
{
    public class Startup
    {
        public IHostEnvironment CurrentEnvironment { get; }
        public IConfiguration Configuration { get; }
        
        public Startup(IConfiguration configuration, IHostEnvironment env)
        {
            Configuration = configuration;
            CurrentEnvironment = env;
        }

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

            services.AddFluentEmail(Configuration.GetSection("keplercms:mailAddress").Value)
            .AddMailGunSender(Configuration.GetSection("keplercms:mailgunDomain").Value, Configuration.GetSection("keplercms:mailgunApiKey").Value, FluentEmail.Mailgun.MailGunRegion.EU);

            services.AddRouting(options => options.LowercaseUrls = true);


            services.AddControllersWithViews().AddViewLocalization();

            // this *has to go here* after view localization has been initialized
            // so that Pages can localize - note required even if you're not using
            // the DbResource manager.
            services.AddTransient<IViewLocalizer, DbResViewLocalizer>();

            services.AddScoped<IUserService, UserService>();
            services.AddSingleton<ICommandQueueService, CommandQueueService>();
            services.AddScoped<IMenuService, MenuService>();
            services.AddScoped<IPageService, PageService>();
            services.AddScoped<IFuseService, FuseService>();
            services.AddScoped<IUploadService, UploadService>();
            services.AddScoped<ICreditService, CreditService>();
            services.AddScoped<INewsService, NewsService>();
            services.AddScoped<IPromoService, PromoService>();
            services.AddScoped<IHomeService, HomeService>();
            services.AddScoped<IRoomService, RoomService>();
            services.AddScoped<IRoomChatlogsService, RoomChatlogsService>();
            services.AddScoped<ITraxService, TraxService>();
            services.AddScoped<IFriendService, FriendService>();
            services.AddScoped<ISettingsService, SettingsService>();
            services.AddScoped<IHabbowoodService, HabbowoodService>();
            services.AddScoped<ITagService, TagService>();
            services.AddScoped<IPhotoService, PhotoService>();
            services.AddScoped<ICatalogueService, CatalogueService>();
            services.AddScoped<IMailService, MailService>();
            services.AddScoped<IRewardService, RewardService>();
            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddScoped<IFurniService, FurniService>();

            services.AddMjmlServices(o =>
            {
                if (CurrentEnvironment.IsDevelopment())
                {
                    o.DefaultKeepComments = true;
                    o.DefaultBeautify = true;
                }
                else
                {
                    o.DefaultKeepComments = false;
                    o.DefaultMinify = true;
                }
            });

            if (!CurrentEnvironment.IsDevelopment())
            {
                services.AddMemoryCache();
            }
            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });
            
            services.AddHostedService<HabboActivityBackgroundService>();
            
            services.AddSignalR();
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
                //app.UseExceptionHandler("/Home/Error");
                app.UseDeveloperExceptionPage();
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
                    name: "newsWithSlug",
                    pattern: "news/{slug}",
                    defaults: new { controller = "News", action = "Index" },
                    constraints: new { slug = ".+" }
                );

                endpoints.MapControllerRoute(
                    name: "images",
                    pattern: "images/{category}/{fileName}",
                    defaults: new { controller = "Images", action = "Index" }
                );

                endpoints.MapControllerRoute(
                name: "CustomAreaHandler",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Page}/{action=Index}/{slug?}");

                endpoints.MapControllerRoute(name: "api", pattern: "api/{controller=MeApiController}");


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

                endpoints.MapHub<ChatLogHub>("sockets/housekeeping/chatlogs");
                endpoints.MapHub<InfobusHub>("sockets/housekeeping/infobus");
            });

        }
    }
}
