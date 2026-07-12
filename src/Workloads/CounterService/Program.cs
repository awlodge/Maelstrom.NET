using CounterService;
using Maelstrom;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddMaelstromNodeWorkload<Counter>();

using IHost host = builder.Build();
await host.RunAsync();
