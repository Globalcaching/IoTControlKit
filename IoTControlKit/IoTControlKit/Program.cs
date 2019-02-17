using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MQTTnet.AspNetCore;
using Serilog;
using Serilog.Events;

namespace IoTControlKit
{
    public class Program
    {
        public static IConfiguration Configuration { get; set; }
        public static IHostingEnvironment HostingEnvironment { get; set; }
        public static NLog.Logger Logger;

        private static CancellationTokenSource _cancellationTokenSource = new System.Threading.CancellationTokenSource();

        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Error()
#if RELEASE
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
#else
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Information)
#endif
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                Log.Information("Starting web host");
                CreateWebHostBuilder(args).Build().RunAsync(_cancellationTokenSource.Token).GetAwaiter().GetResult();
                return 0;
            }
            catch(Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Close();
                Log.CloseAndFlush();
                NLog.LogManager.Shutdown();
            }
        }

        public static void Close()
        {
            Program.Logger?.Factory?.Dispose();
        }

        public static void Shutdown()
        {
            _cancellationTokenSource.Cancel();
        }


        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            string customPort = "5000";
            if (args != null)
            {
                customPort = (from a in args where a.StartsWith("p=") select a.Substring(2)).FirstOrDefault() ?? customPort;
            }
            return WebHost.CreateDefaultBuilder(args)
                //.UseUrls($"http://*:{customPort}")
                .UseKestrel(options =>
                {
                    options.Limits.MaxRequestBodySize = null;
                    options.ListenAnyIP(1883, l => l.UseMqtt());
                    options.ListenAnyIP(int.Parse(customPort)); // default http pipeline

                })
                .UseStartup<Startup>()
                .UseSerilog();
        }
    }
}
