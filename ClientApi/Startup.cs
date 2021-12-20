using System;
using System.Threading;
using Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Clustering.Kubernetes;
using Orleans.Configuration;
using Orleans.Hosting;

namespace ClientApi {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services) {
            var clusterClient = CreateOrleansClient();

            var origins = Configuration["Web:Origins"] ?? throw new InvalidOperationException("missing Web.Origins config value");

            services.AddSingleton(clusterClient);
            services.AddSingleton<IGrainFactory>(clusterClient);

            services.AddAuthorization();

            services.AddControllers().AddNewtonsoftJson(json => json.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc);

            services.AddMvc(o => {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                o.Filters.Add(new AuthorizeFilter(policy));
            });

            services.AddCors(o => o.AddDefaultPolicy(cors =>
                                                         cors.AllowAnyHeader()
                                                             .WithExposedHeaders("content-disposition")
                                                             .AllowAnyMethod()
                                                             .SetIsOriginAllowedToAllowWildcardSubdomains()
                                                             .WithOrigins(origins.Split(','))
                                                             .AllowCredentials()));

            services.AddSignalR().AddNewtonsoftJsonProtocol(conf => {
                conf.PayloadSerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors();

            app.UseAuthentication();

            app.UseAuthorization();


            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });
        }

        private IClientBuilder ConfigureClustering(IClientBuilder builder) {
            return Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST") is null
                       ? builder.UseLocalhostClustering()
                       : builder.UseKubeGatewayListProvider();
        }

        private IClusterClient CreateOrleansClient() {
            var attempts = 5;
            while (attempts > 0)
                try {
                    var clientBuilder =
                        ConfigureClustering(new ClientBuilder())
                            .Configure<ClusterOptions>(options => {
                                options.ClusterId = Configuration["ORLEANS_CLUSTER_ID"];
                                options.ServiceId = Configuration["ORLEANS_SERVICE_ID"];
                            })
                             .ConfigureApplicationParts(p => {
                                 p.AddApplicationPart(typeof(IPingGrain).Assembly).WithReferences();
                             })
                            .ConfigureLogging(logging => logging.AddConsole());
                    var client = clientBuilder.Build();
                    client.Connect().Wait();

                    return client;
                }
                catch (Exception) {
                    Thread.Sleep(1000);
                    attempts--;
                }

            throw new ApplicationException("Unable to connect to orleans");
        }
    }
}