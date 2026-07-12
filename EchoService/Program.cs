using EchoService;
using Maelstrom;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddMaelstromNodeWorkload<EchoServer>();

using IHost host = builder.Build();
await host.RunMaelstromNodeAsync();
