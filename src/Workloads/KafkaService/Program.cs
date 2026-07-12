using KafkaService;
using Maelstrom;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddMaelstromNodeWorkload<KafkaLog>();

using IHost host = builder.Build();
await host.RunAsync();
