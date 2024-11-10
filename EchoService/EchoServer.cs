﻿using EchoService.Models.MessageBodies;
using Maelstrom;
using Maelstrom.Interfaces;
using Maelstrom.Models;

namespace EchoService;

internal class EchoServer(ILogger<EchoServer> logger, IMaelstromNode node) : Workload(logger, node)
{
    private readonly ILogger<EchoServer> logger = logger;

    [MaelstromHandler(Echo.EchoType)]
    public async Task HandleEcho(Message message)
    {
        var echo = message.DeserializeAs<Echo>();
        logger.LogInformation("Echoing message: {EchoMessage}", echo.EchoMessage);
        await node.ReplyAsync(message, new EchoOk(echo.EchoMessage));
    }
}
