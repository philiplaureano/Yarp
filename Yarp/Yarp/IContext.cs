using System;
using System.Threading;

namespace Yarp
{
    public interface IContext
    {
        object Message { get; }
        Action<object> SendMessage { get; }
        CancellationToken Token { get; }
    }
}