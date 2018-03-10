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

            var timer = new Timer(OnTimerCallback, context, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            timer.Change(setMessage.DueTime, setMessage.Period);
            
            return Task.FromResult(0);
        }

        private void OnTimerCallback(object state)
        {
            if (!(state is IContext ctx))
                return;

            var message = ctx.Message;
            if (!(message is SetTimer setTimerMessage))
                return;

            var timerId = setTimerMessage.TimerId;
            ctx.SendMessage(new TimerExpired(timerId));
        }
    }
}