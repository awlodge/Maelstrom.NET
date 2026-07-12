using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TransactionRwRegisterService.Models;

internal class Operation
{
    public Operation(OperationType operationType, int key, int? val)
    {
        OperationType = operationType;
        Key = key;
        Val = val;
    }

    [SetsRequiredMembers]
    public Operation(string opType, int key, int? val)
    {
        OperationType = opType switch
        {
            "r" => OperationType.Read,
            "w" => OperationType.Write,
            _ => throw new Exception($"Unexpected operation type: {opType}")
        };
        Key = key;
        Val = val;
    }

    public required OperationType OperationType { get; set; }

    public string OperationTypeString => OperationType switch
    {
        OperationType.Read => "r",
        OperationType.Write => "w",
        _ => throw new Exception($"Unexpected operation type: {OperationType}")
    };

    public required int Key { get; set; }

    public int? Val { get; set; }
}

internal enum OperationType
{
    Read,
    Write,
}

internal class OperationListConverter : JsonConverter<List<Operation>>
{
    public override List<Operation> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var operationList = JsonSerializer.Deserialize<List<List<JsonElement>>>(ref reader, options);
        return operationList == null
            ? throw new JsonException("Failed to deserialize to list")
            : operationList
                .Select(op => new Operation(op[0].GetString()!, op[1].GetInt32(), op[2].ValueKind == JsonValueKind.Null ? null : op[2].GetInt32()))
                .ToList();
    }

    public override void Write(Utf8JsonWriter writer, List<Operation> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var op in value)
        {
            writer.WriteStartArray();
            writer.WriteStringValue(op.OperationTypeString);
            writer.WriteNumberValue(op.Key);
            if (op.Val == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteNumberValue((int)op.Val);
            }
            writer.WriteEndArray();
        }

        writer.WriteEndArray();
    }
}
