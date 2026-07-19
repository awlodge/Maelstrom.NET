
namespace Maelstrom.TestSupport;

public interface IKvStore
{
    Task<bool> ExpectReadAsync<T>(string serviceName, T expectedKey, TimeSpan timeout = default) where T : IComparable<T>;

    Task<bool> ExpecCasAsync<T, U>(string serviceName, T expectedKey, U expectedFrom, U expectedTo, bool expectedCreateIfNotExists, TimeSpan timeout = default)
        where T : IComparable<T>
        where U : IComparable<U>;
}