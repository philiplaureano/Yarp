using System;

namespace Yarp.Messages
{
    public class AlarmTriggered
    {
        public AlarmTriggered(Guid alarmId, DateTime dateTriggeredUtc)
        {
            AlarmId = alarmId;
            DateTriggeredUtc = dateTriggeredUtc;
        }

        public Guid AlarmId { get; }
        public DateTime DateTriggeredUtc { get; }
    }
}