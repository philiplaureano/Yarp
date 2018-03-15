using System;

namespace Yarp.Messages
{
    public class SetAlarm
    {
        public SetAlarm(Guid alarmId, TimeSpan expirationTime, Action<object> alarmHandler)
        {
            AlarmId = alarmId;
            ExpirationTime = expirationTime;
            AlarmHandler = alarmHandler;
        }

        public Guid AlarmId { get; }
        public TimeSpan ExpirationTime { get; }
        public Action<object> AlarmHandler { get; }
    }
}