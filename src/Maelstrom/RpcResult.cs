using Maelstrom.Models;
using Maelstrom.Models.MessageBodies;
using System.Diagnostics.CodeAnalysis;

namespace Maelstrom;

public record RpcResult<T> where T : MessageBody
{
    private readonly T? _result;
    private readonly ErrorBody? _error;

    internal RpcResult(Message message)
    {
        if (message.IsError(out var error))
        {
            _error = error;
        }
        else
        {
            var deserializedMessage = message.DeserializeAs<T>();
            _result = deserializedMessage.Body;
        }
    }

    public T Result => _result ?? throw AsException();

    public bool IsSuccess => _result != null;

    public bool IsError([NotNullWhen(true)] out ErrorBody? error)
    {
        error = null;
        if (_error != null)
        {
            error = _error;
            return true;
        }

        return false;
    }

    public void ThrowOnError()
    {
        if (!IsSuccess)
        {
            throw AsException();
        }
    }

    private RpcFailedException AsException() => _error != null
        ? new RpcFailedException(_error)
        : new RpcFailedException("RPC failed with unknown error");
}

public class RpcFailedException : Exception
{
    public ErrorBody? Error { get; private set; }

    public RpcFailedException() : base() { }

    public RpcFailedException(string message) : base(message) { }

    public RpcFailedException(string message, Exception inner) : base(message, inner) { }

    public RpcFailedException(ErrorBody error)
        : base($"RPC received error response: [{error.ErrorCode}] {error.ErrorText}")
    {
        Error = error;
    }
}