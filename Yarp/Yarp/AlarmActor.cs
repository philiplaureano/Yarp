using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yarp.Messages;

namespace Yarp
{
    public class AlarmActor : IActor
    {
        private ConcurrentDictionary<Guid, Action<object>> _alarmHandlers =
            new ConcurrentDictionary<Guid, Action<object>>();

        private ConcurrentDictionary<Guid, Guid> _cancelledAlarms = new ConcurrentDictionary<Guid, Guid>();
        private ConcurrentDictionary<Guid, TimeSpan> _snoozedAlarms = new ConcurrentDictionary<Guid, TimeSpan>();

        public Task TellAsync(IContext context)
        {
            var message = context.Message;
            if (message is SnoozeAlarm snoozeAlarm)
            {
                // Set the snooze time
                _snoozedAlarms[snoozeAlarm.AlarmId] = snoozeAlarm.SnoozeTime;
            }

            if (message is CancelAlarm cancelAlarm && !_cancelledAlarms.ContainsKey(cancelAlarm.AlarmId))
            {
                _cancelledAlarms[cancelAlarm.AlarmId] = cancelAlarm.AlarmId;
            }

            if (!(message is SetAlarm setAlarm)) 
                return Task.CompletedTask;
            
            var alarmId = setAlarm.AlarmId;
            var handler = setAlarm.AlarmHandler;
            var expirationTime = setAlarm.ExpirationTime;

            _alarmHandlers[alarmId] = handler;

            void TimerCallback(object msg)
            {
                if (!(msg is Guid))
                    return;

                var currentAlarmId = (Guid) msg;
                if (!_alarmHandlers.ContainsKey(currentAlarmId))
                    return;

                // Don't trigger the alarm if it has been cancelled
                if (!_cancelledAlarms.ContainsKey(currentAlarmId))
                {
                    // Delay the alarm if it has been snoozed
                    var targetHandler = _alarmHandlers[currentAlarmId];
                    if (_snoozedAlarms.ContainsKey(currentAlarmId))
                    {
                        var snoozeTime = _snoozedAlarms[currentAlarmId];
                        this.Tell(new SetAlarm(currentAlarmId, snoozeTime, targetHandler));

                        TimeSpan ignoredParamter;
                        _snoozedAlarms.TryRemove(currentAlarmId, out ignoredParamter);
                            
                        return;
                    }

                    targetHandler?.Invoke(new AlarmTriggered(alarmId, DateTime.UtcNow));
                }

                // Once the alarm is triggered, remove the alarm
                Action<object> getRemovedValue;
                _alarmHandlers.TryRemove(currentAlarmId, out getRemovedValue);

                // Remove the entry for the cancelled alarm
                if (_cancelledAlarms.ContainsKey(currentAlarmId))
                {
                    Guid ignoredParamter;
                    _cancelledAlarms.TryRemove(currentAlarmId, out ignoredParamter);   
                }
            }

            var timer = new Timer(TimerCallback,
                alarmId,
                Timeout.InfiniteTimeSpan,
                Timeout.InfiniteTimeSpan);

            timer.Change(expirationTime, Timeout.InfiniteTimeSpan);

            return Task.CompletedTask;
        }
    }
}