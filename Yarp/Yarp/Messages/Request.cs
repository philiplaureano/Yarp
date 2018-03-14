using System;

namespace Yarp.Messages
{
    public class Request<T>
    {
        public Request(Guid requesterId, T requestMessage)
        {
            RequesterId = requesterId;
            RequestMessage = requestMessage;
        }

        public Guid RequesterId { get; }
        public T RequestMessage { get; }
    }
}