using System;
using System.Net;
using Grains;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Clustering.Kubernetes;
using Orleans.Configuration;
using Orleans.Hosting;

await Host.CreateDefaultBuilder(Environment.GetCommandLineArgs())
    .ConfigureLogging((ctx, builder) =>
            builder
                .ClearProviders()
                .AddConsole()
                .AddConfiguration(ctx.Configuration.GetSection("Logging")))
    .UseOrleans(builder =>
                    ConfigureHosting(builder)
                        .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(PingGrain).Assembly).WithReferences())
                        .UseInMemoryReminderService()
                        .UseDashboard(opt => { opt.Port = 8091; })
                        .AddMemoryGrainStorage("ArchiveStorage")
                        .AddMemoryGrainStorage("PubSubStore")
                        .AddMemoryGrainStorageAsDefault()
    )
    .ConfigureServices((_, services) => {
        services.Configure<ConsoleLifetimeOptions>(o => { o.SuppressStatusMessages = true; });
    })
    .RunConsoleAsync();

ISiloBuilder ConfigureHosting(ISiloBuilder builder) {
    if (Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST") is not null)
        return
            builder
                .UseKubernetesHosting()
                .UseKubeMembership();

    return builder
        .UseLocalhostClustering()
        .Configure<EndpointOptions>(o => o.AdvertisedIPAddress = IPAddress.Loopback)
        .Configure<ClusterOptions>(o => {
            o.ClusterId = "dev";
            o.ServiceId = "TradingSilo";
        });
}