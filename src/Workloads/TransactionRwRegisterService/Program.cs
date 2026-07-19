using Maelstrom;
using TransactionRwRegisterService;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddMaelstromNodeWorkload<TransactionRwRegister>();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

using IHost host = builder.Build();
await host.RunMaelstromNodeAsync();
