using System;

namespace Yarp.Messages
{
    public class TimerExpired
    {
        public TimerExpired(Guid timerId)
        {
            TimerId = timerId;
        }

        public Guid TimerId { get; }
    }
}