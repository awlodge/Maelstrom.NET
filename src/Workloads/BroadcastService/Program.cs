using BroadcastService;
using Maelstrom;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddMaelstromNodeWorkload<BroadcastService.BroadcastService>();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services.AddHostedService<BroadcastServicePoller>();

using IHost host = builder.Build();
await host.RunMaelstromNodeAsync();
