using System;

namespace Yarp.Messages
{
    public class TimerTick
    {
        public TimerTick(Guid timerId, DateTime timestampUtc)
        {
            TimerId = timerId;
            TimestampUtc = timestampUtc;
        }

        public Guid TimerId { get; }
        public DateTime TimestampUtc { get; }
    }
}