using Maelstrom.Models;
using Maelstrom.Models.MessageBodies;
using System.Text.Json;

namespace MaelstromUnitTests;

public class MessageSerializationTests
{
    [Fact]
    public void CanSerializeMessage()
    {
        var body = new Init("n2", ["n1", "n2"])
        {
            MsgId = 1
        };
        var message = new Message<Init>("n1", "n2", body);
        var serialized = message.Serialize();
        Assert.Equal("{\"body\":{\"node_id\":\"n2\",\"node_ids\":[\"n1\",\"n2\"],\"type\":\"init\",\"msg_id\":1},\"src\":\"n1\",\"dest\":\"n2\"}", serialized);
    }

    [Fact]
    public void CanDeserializeToGenericMessageBody()
    {
        var input = "{\"body\":{\"node_id\":\"n2\",\"node_ids\":[\"n1\",\"n2\"],\"type\":\"init\",\"msg_id\":1},\"src\":\"n1\",\"dest\":\"n2\"}";
        var message = Message.Deserialize(input);
        Assert.NotNull(message);
        Assert.Equal("n1", message.Src);
        Assert.Equal("n2", message.Dest);
        Assert.Equal("init", message.Body.Type);
        Assert.Equal(1, message.Body.MsgId);
    }

    [Fact]
    public void CanDeserializeToSpecificMessageBody()
    {
        var input = "{\"body\":{\"node_id\":\"n2\",\"node_ids\":[\"n1\",\"n2\"],\"type\":\"init\",\"msg_id\":1},\"src\":\"n1\",\"dest\":\"n2\"}";
        var message = Message.Deserialize(input);
        Assert.NotNull(message);
        Assert.Equal("n1", message.Src);
        Assert.Equal("n2", message.Dest);
        Assert.IsType<MessageBodyBase>(message.Body);

        var message2 = message.DeserializeAs<Init>();
        Assert.NotNull(message2);
        Assert.Equal("n1", message2.Src);
        Assert.Equal("n2", message2.Dest);
        Assert.IsType<Init>(message2.Body);

        Assert.Equal("init", message2.Body.Type);
        Assert.Equal(1, message2.Body.MsgId);
        Assert.Equal("n2", message2.Body.NodeId);
        Assert.Equal(["n1", "n2"], message2.Body.NodeIds);
    }

    [Fact]
    public void CannotDeserializeToWrongMessageType()
    {
        var input = "{\"body\":{\"node_id\":\"n2\",\"node_ids\":[\"n1\",\"n2\"],\"type\":\"not_init\",\"msg_id\":1},\"src\":\"n1\",\"dest\":\"n2\"}";
        var message = Message.Deserialize(input);
        Assert.NotNull(message);
        Assert.Equal("n1", message.Src);
        Assert.Equal("n2", message.Dest);
        Assert.IsType<MessageBodyBase>(message.Body);

        var ex = Assert.Throws<JsonException>(message.DeserializeAs<Init>);
        Assert.Equal("Incorrect message type 'not_init' to deserialize to Maelstrom.Models.MessageBodies.Init", ex.Message);
    }
}