using System;
using System.Threading;

namespace Yarp
{
    internal struct TimerState
    {
        public TimerState(Guid timerId, object message, Action<object> sendMessage, 
            DateTime startTimeUtc, TimeSpan dueTime, TimeSpan period, CancellationToken cancellationToken)
        {
            TimerId = timerId;
            Message = message;
            SendMessage = sendMessage;
            StartTimeUtc = startTimeUtc;
            DueTime = dueTime;
            Period = period;
            CancellationToken = cancellationToken;
        }

        public Guid TimerId { get; }
        public object Message { get; }
        public Action<object> SendMessage { get; }
        public DateTime StartTimeUtc { get; }
        public TimeSpan DueTime { get; }
        public TimeSpan Period { get; }
        public CancellationToken CancellationToken { get; }
    }
}