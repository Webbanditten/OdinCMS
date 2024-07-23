using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KeplerCMS
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureAppConfiguration((context, config) =>
                    {
                        // Load the configuration
                        var builtConfig = config.Build();
                    
                        // Check if the configuration contains the Sentry DSN
                        var sentryDsn = builtConfig["keplercms:sentry_dsn"];
                        if (!string.IsNullOrEmpty(sentryDsn))
                        {
                            Console.WriteLine("Sentry DSN found, configuring Sentry...");
                            // Configure Sentry if DSN is present
                            webBuilder.UseSentry(o =>
                            {
                                o.Dsn = sentryDsn;
                            });
                        }
                    });
                    
                    webBuilder.UseStartup<Startup>();
                });
    }
}
