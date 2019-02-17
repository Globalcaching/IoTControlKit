using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using IoTControlKit.Helpers;
using IoTControlKit.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.AspNetCore;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace IoTControlKit
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
            Program.Configuration = Configuration;
            Program.HostingEnvironment = env;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            //this adds a hosted mqtt server to the services
            services.AddHostedMqttServer(builder =>
            {
                builder.WithDefaultEndpoint()
                    .WithDefaultEndpointPort(1883)
                    .WithApplicationMessageInterceptor((ctx) =>
                    {
                        Services.LoggerService.Instance.LogTrace($"MTTQ Broker=> From={ctx.ClientId} -> Message: Topic={ctx.ApplicationMessage?.Topic}, Payload={(ctx.ApplicationMessage?.Payload == null ? "" : Encoding.UTF8.GetString(ctx.ApplicationMessage.Payload))}, QoS={ctx.ApplicationMessage?.QualityOfServiceLevel}, Retain={ctx.ApplicationMessage?.Retain}");
                    })
                    .WithConnectionValidator((ctx) =>
                    {
                        ctx.ReturnCode = MqttConnectReturnCode.ConnectionAccepted;
                    });
            });

            //this adds tcp server support based on Microsoft.AspNetCore.Connections.Abstractions
            services.AddMqttConnectionHandler();

            //this adds websocket support
            services.AddMqttWebSocketServerAdapter();

            services.AddHttpContextAccessor();
            services.AddMvc()
                .AddJsonOptions(opt =>
                {
                    var resolver = opt.SerializerSettings.ContractResolver;
                    if (resolver != null)
                    {
                        var res = resolver as DefaultContractResolver;
                        res.NamingStrategy = null;  // <<!-- this removes the camelcasing
                    }
                    opt.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    opt.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                }).AddMvcOptions(options =>
                {
                    options.ModelBinderProviders.Insert(0, new DateTimeModelBinderProvider());
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressConsumesConstraintForFormFileParameters = true;
                options.SuppressInferBindingSourcesForParameters = true;
                options.SuppressModelStateInvalidFilter = true;
            });

            var settings = new JsonSerializerSettings();
            settings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
            var serializer = JsonSerializer.Create(settings);
            services.Add(new ServiceDescriptor(typeof(JsonSerializer),
                         provider => serializer,
                         ServiceLifetime.Transient));

            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;

            });


            services.AddSession();

            services.Configure<FormOptions>(options =>
            {
                options.ValueLengthLimit = int.MaxValue;
                options.MultipartBodyLengthLimit = (long)2 * 1024 * 1024 * 1024;
                options.ValueCountLimit = int.MaxValue;
            });

            services.AddHttpClient();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseExceptionHandler(
                 options =>
                 {
                     options.Run(
                     async context =>
                     {
                         context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                         context.Response.ContentType = "text/html";
                         var ex = context.Features.Get<IExceptionHandlerFeature>();
                         if (ex != null)
                         {
                             var err = $"<h1>Error: {ex.Error.Message}</h1>{ex.Error.StackTrace }";
                             await context.Response.WriteAsync(err).ConfigureAwait(false);
                         }
                     });
                 });
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(builder =>
                    builder.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod());

            Program.Logger = Helpers.FileHelper.CreateNLogLogger(Path.Combine(Helpers.EnvironmentHelper.RootDataFolder, "Logs", "IoTControlKit.log"));
            Program.Logger.Info("IoTControlKit startup");

            app.UseStaticFiles();
            app.UseSession();
            app.UseStaticHttpContext();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action}/{id?}",
                    defaults: new { controller = "Home", action = "Index" }
                );
            });

            app.UseWebSockets();
            app.UseSignalR(routes =>
            {
                routes.MapHub<Hubs.IoTControlKitHub>("/hubs/IoTControlKitHub", options => {
                    options.ApplicationMaxBufferSize = int.MaxValue;
                });
            });

            Task.Run(() =>
            {
                ApplicationService.Instance.Run();
            });
        }
    }
}
