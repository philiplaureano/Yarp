using System;

namespace Yarp.Messages
{
    public class CurrentElectionTimeOutRange
    {
        public CurrentElectionTimeOutRange(Guid nodeId, int minMilliseconds, int maxMilliseconds)
        {
            NodeId = nodeId;
            MinMilliseconds = minMilliseconds;
            MaxMilliseconds = maxMilliseconds;
        }

        public Guid NodeId { get; }
        public int MinMilliseconds { get; }
        public int MaxMilliseconds { get; }
    }
}