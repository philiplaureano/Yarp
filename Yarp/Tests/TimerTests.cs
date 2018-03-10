using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Xunit;
using Yarp;
using Yarp.Messages;

namespace Tests
{
    public class TimerTests
    {
        [Fact]
        public void ShouldSendTimerExpiredMessageWhenTimeHasElapsed()
        {
            var timer = new TimerActor();
            var timerId = Guid.NewGuid();

            var elapsedEvents = new ConcurrentBag<TimerExpired>();
            Action<object> handler = msg =>
            {
                var timerExpired = msg as TimerExpired;
                if (timerExpired == null)
                    return;

                if (timerExpired.TimerId == timerId)
                {
                    elapsedEvents.Add(timerExpired);
                }
            };

            var source = new CancellationTokenSource();
            var context = new Context(new SetTimer(timerId, TimeSpan.FromMilliseconds(100), Timeout.InfiniteTimeSpan), handler, source.Token);
            var task = timer.TellAsync(context);

            Task.WaitAll(new[] {task});

            Thread.Sleep(1000);
            Assert.Equal(elapsedEvents.Count, 1);
        }
    }
}