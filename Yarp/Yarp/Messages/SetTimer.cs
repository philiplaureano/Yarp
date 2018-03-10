using System;

namespace Yarp.Messages
{
    public class SetTimer
    {
        public SetTimer(Guid timerId, TimeSpan dueTime, TimeSpan period)
        {
            TimerId = timerId;
            DueTime = dueTime;
            Period = period;
        }

        public Guid TimerId { get; }
        public TimeSpan DueTime { get; }
        public TimeSpan Period { get; }
    }
}