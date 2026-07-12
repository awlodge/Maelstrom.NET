using Maelstrom;
using TransactionRwRegisterService;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddMaelstromNodeWorkload<TransactionRwRegister>();

using IHost host = builder.Build();
await host.RunAsync();
