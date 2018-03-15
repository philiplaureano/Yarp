using System;

namespace Yarp.Messages
{
    public class CancelAlarm
    {
        public CancelAlarm(Guid alarmId)
        {
            AlarmId = alarmId;
        }

        public Guid AlarmId { get; }
    }
}