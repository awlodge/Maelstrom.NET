using Maelstrom;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddMaelstromNodeWorkload<BroadcastService.BroadcastService>();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

using IHost host = builder.Build();
await host.RunMaelstromNodeAsync();
