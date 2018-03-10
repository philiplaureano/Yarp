using System;
using System.Threading;

namespace Yarp
{
    public class Context : IContext
    {
        public Context(object message, Action<object> sendMessage, CancellationToken token)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            SendMessage = sendMessage ?? throw new ArgumentNullException(nameof(sendMessage));
            Token = token;
        }

        public object Message { get; }
        public Action<object> SendMessage { get; }
        public CancellationToken Token { get; }
    }
}