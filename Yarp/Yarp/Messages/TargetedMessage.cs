using System;

namespace Yarp.Messages
{
    public class TargetedMessage
    {
        public TargetedMessage(Guid targetActorId, object message)
        {
            TargetActorId = targetActorId;
            Message = message;
        }

        public Guid TargetActorId { get; }
        public object Message { get; }
    }
}