using System;

namespace Yarp.Messages
{
    public class EnumerateAllKnownActors
    {
        public EnumerateAllKnownActors(Guid requesterId)
        {
            RequesterId = requesterId;
        }

        public Guid RequesterId { get; }
    }
}