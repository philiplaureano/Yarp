using System;

namespace Yarp.Messages
{
    public class SnoozeAlarm
    {
        public SnoozeAlarm(Guid alarmId, TimeSpan snoozeTime)
        {
            AlarmId = alarmId;
            SnoozeTime = snoozeTime;
        }

        public Guid AlarmId { get; }
        public TimeSpan SnoozeTime { get; }
    }
}