using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Yarp.Messages;

namespace Yarp
{
    public class AlarmActor : IActor
    {
        private ConcurrentDictionary<Guid, Action<object>> _alarmHandlers =
            new ConcurrentDictionary<Guid, Action<object>>();       

        public Task TellAsync(IContext context)
        {
            var message = context.Message;
            if (message is SetAlarm setAlarm)
            {
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

                    var targetHandler = _alarmHandlers[currentAlarmId];
                    targetHandler?.Invoke(new AlarmTriggered(alarmId, DateTime.UtcNow));
                    
                    // Once the alarm is triggered, remove the alarm
                    Action<object> getRemovedValue;
                    _alarmHandlers.TryRemove(currentAlarmId, out getRemovedValue);
                };
                
                var timer = new Timer(TimerCallback,
                    alarmId,
                    Timeout.InfiniteTimeSpan,
                    Timeout.InfiniteTimeSpan);

                timer.Change(expirationTime, Timeout.InfiniteTimeSpan);
            }

            return Task.CompletedTask;
        }

        private void OnTimerCallback(object state)
        {
        }
    }
}