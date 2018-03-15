using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Xunit;
using Yarp;
using Yarp.Messages;

namespace Tests
{
    public class AlarmTests : IDisposable
    {
        [Fact]
        public void ShouldBeAbleToSetAlarmForSpecificInterval()
        {
            var alarm = new AlarmActor();

            // Note: Alarms are only one-time events
            var stopwatch = Stopwatch.StartNew();
            var alarmMessages = new ConcurrentBag<object>();
            Action<object> alarmHandler = msg =>
            {
                // Keep track of when the alarm is triggered
                stopwatch.Stop();
                alarmMessages.Add(msg);
            };

            var alarmId = Guid.NewGuid();
            var timeLimit = TimeSpan.FromMilliseconds(300);
            var setAlarm = new SetAlarm(alarmId, timeLimit, alarmHandler);
            alarm.Tell(setAlarm);

            Thread.Sleep(500);

            // Ensure that the alarm is triggered within 5ms of the
            // time limit
            Assert.True(stopwatch.Elapsed <= timeLimit + TimeSpan.FromMilliseconds(timeLimit.TotalMilliseconds * .05));
            Assert.NotEmpty(alarmMessages);

            var alarmMessage = alarmMessages.Where(msg => msg is AlarmTriggered)
                .Cast<AlarmTriggered>()
                .First();

            Assert.Equal(alarmId, alarmMessage.AlarmId);
        }

        [Fact]
        public void ShouldBeAbleToCancelAlarm()
        {
            throw new NotImplementedException("TODO: Implement ShouldBeAbleToCancelAlarm");
        }

        [Fact]
        public void ShouldBeAbleToSnoozeAlarm()
        {
            throw new NotImplementedException("TODO: Implement ShouldBeAbleToSnoozeAlarm");
        }

        public void Dispose()
        {
        }
    }
}