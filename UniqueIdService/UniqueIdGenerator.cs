﻿using Maelstrom;
using Maelstrom.Interfaces;
using Maelstrom.Models;
using UniqueIdService.Models.MessageBodies;

namespace UniqueIdService;

internal class UniqueIdGenerator(ILogger<UniqueIdGenerator> logger, IMaelstromNode node) : Workload(logger, node)
{
    private readonly ILogger<UniqueIdGenerator> logger = logger;
    private int _idCounter = 0;

    [MaelstromHandler(Generate.GenerateType)]
    public async Task HandleGenerate(Message message)
    {
        var generatedId = GenerateId();
        logger.LogInformation("Received Generate request, generated id {Id}", generatedId);
        await node.ReplyAsync(message, new GenerateOk(GenerateId()));
    }

    private int GenerateId()
    {
        // Id is generated from a combination of process Id, random seed and message Id.
        return 1000000000 * _idCounter++ + 100000 * new Random().Next(0, 100) + Environment.ProcessId;
    }
}
