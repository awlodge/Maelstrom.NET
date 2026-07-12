using Maelstrom;
using UniqueIdService;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddMaelstromNodeWorkload<UniqueIdGenerator>();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

using IHost host = builder.Build();
await host.RunMaelstromNodeAsync();
