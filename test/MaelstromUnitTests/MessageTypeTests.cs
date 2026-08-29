using Maelstrom.Models;
using Maelstrom.Models.MessageBodies;

namespace MaelstromUnitTests;

public class MessageTypeTests
{
    [Fact]
    public void CanGetMessageType()
    {
        Assert.Equal("init", MessageTypeAttribute.GetMessageType<Init>());
    }

    [Fact]
    public void GetMessageType_ThrowsOnNonMessageBody()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => MessageTypeAttribute.GetMessageType(this.GetType()));
        Assert.Equal("Cannot get message type of MaelstromUnitTests.MessageTypeTests as it does not derive from MessageBody", ex.Message);
    }

    [Fact]
    public void GetMessageType_ThrowsWhenNoMessageTypeAttribute()
    {
        var ex = Assert.Throws<InvalidOperationException>(MessageTypeAttribute.GetMessageType<DummyMessageBody>);
        Assert.Equal("Type MaelstromUnitTests.MessageTypeTests+DummyMessageBody does not have the MessageType attribute", ex.Message);
    }

    private class DummyMessageBody : MessageBody
    {

    }
}
