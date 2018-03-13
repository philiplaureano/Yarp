using System;

namespace Yarp.Messages
{
    public class BroadcastMessage
    {
        public BroadcastMessage(Guid senderId, object message)
        {
            SenderId = senderId;
            Message = message;
        }

        public Guid SenderId { get; }
        public object Message { get; }
    }
}