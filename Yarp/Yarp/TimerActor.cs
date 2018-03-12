using System;
using System.Threading;
using System.Threading.Tasks;
using Yarp.Messages;

namespace Yarp
{
    public class TimerActor : IActor
    {
        public Task TellAsync(IContext context)
        {
            var message = context.Message;
            if (!(message is SetTimer setMessage))
                return Task.FromCanceled(context.Token);

            var timer = new Timer(OnTimerCallback,
                new TimerState(setMessage.TimerId, message,
                    context.SendMessage, DateTime.UtcNow, setMessage.DueTime, setMessage.Period, context.Token),
                Timeout.InfiniteTimeSpan,
                Timeout.InfiniteTimeSpan);

            timer.Change(setMessage.DueTime, setMessage.Period);

            return Task.FromResult(0);
        }

        private void OnTimerCallback(object state)
        {
            if (!(state is TimerState timerState))
                return;

            if (timerState.CancellationToken.IsCancellationRequested)
                return;
            
            var message = timerState.Message;
            if (!(message is SetTimer setTimerMessage))
                return;

            var timerId = setTimerMessage.TimerId;

            var currentTime = DateTime.UtcNow;
            var expirationTime = timerState.StartTimeUtc + timerState.DueTime;
            if (timerState.Period == Timeout.InfiniteTimeSpan && currentTime > expirationTime)
            {
                timerState.SendMessage(new TimerExpired(timerId));
                return;
            }

            timerState.SendMessage(new TimerTick(timerId, currentTime));
        }
    }
}