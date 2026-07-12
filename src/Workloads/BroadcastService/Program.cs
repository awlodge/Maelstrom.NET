using Maelstrom;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddMaelstromNodeWorkload<BroadcastService.BroadcastService>();

using IHost host = builder.Build();
await host.RunAsync();
