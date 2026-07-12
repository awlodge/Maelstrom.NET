using Maelstrom;
using UniqueIdService;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddMaelstromNodeWorkload<UniqueIdGenerator>();

using IHost host = builder.Build();
await host.RunAsync();
