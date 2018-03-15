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
            var stopwatch = new Stopwatch();
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

            stopwatch.Start();
            alarm.Tell(setAlarm);

            Thread.Sleep(1000);

            // Ensure that the alarm is triggered within 5ms of the
            // time limit
            var deadline = timeLimit.TotalMilliseconds + (timeLimit.TotalMilliseconds * .15);
            Assert.True(stopwatch.ElapsedMilliseconds <= deadline);
            Assert.NotEmpty(alarmMessages);

            var alarmMessage = alarmMessages.Where(msg => msg is AlarmTriggered)
                .Cast<AlarmTriggered>()
                .First();

            Assert.Equal(alarmId, alarmMessage.AlarmId);
        }

        [Fact]
        public void ShouldBeAbleToCancelAlarm()
        {
            var alarmMessages = new ConcurrentBag<object>();
            Action<object> alarmHandler = msg => { alarmMessages.Add(msg); };

            var alarm = new AlarmActor();

            var alarmId = Guid.NewGuid();
            var timeLimit = TimeSpan.FromMilliseconds(300);
            var setAlarm = new SetAlarm(alarmId, timeLimit, alarmHandler);
            alarm.Tell(setAlarm);

            // Cancel the alarm before the time limit expires
            alarm.Tell(new CancelAlarm(alarmId));
            Thread.Sleep(400);
            
            Assert.True(alarmMessages.Count(msg => msg is AlarmTriggered) == 0);
        }

        [Fact]
        public void ShouldBeAbleToSnoozeAlarm()
        {
            var alarm = new AlarmActor();

            var stopwatch = new Stopwatch();
            
            var alarmTimes = new ConcurrentBag<TimeSpan>();
            Action<object> alarmHandler = msg =>
            {
                // Track the exact moment that the alarm was triggered
                alarmTimes.Add(stopwatch.Elapsed);
                
                var alarmMessages = new ConcurrentBag<object>();
                alarmMessages.Add(msg);
            };
            
            // Set the alarm and then extend it
            var alarmId = Guid.NewGuid();
            var timeLimit = TimeSpan.FromMilliseconds(300);
            var setAlarm = new SetAlarm(alarmId, timeLimit, alarmHandler);

            stopwatch.Start();
            alarm.Tell(setAlarm);
            alarm.Tell(new SnoozeAlarm(alarmId, TimeSpan.FromMilliseconds(150)));
            Thread.Sleep(500);
            
            Assert.NotEmpty(alarmTimes);
            Assert.True(alarmTimes.First().Milliseconds >= 400);
        }

        public void Dispose()
        {
        }
    }
}