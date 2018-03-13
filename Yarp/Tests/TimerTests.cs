using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Xunit;
using Yarp;
using Yarp.Messages;

namespace Tests
{
    public class TimerTests : IDisposable
    {
        private TimerActor _timerActor;
        private Guid _timerId;
        private ConcurrentBag<object> _elapsedEvents;
        private CancellationTokenSource _cancellationTokenSource;

        public TimerTests()
        {
            _timerActor = new TimerActor();
            _timerId = Guid.NewGuid();
            _elapsedEvents = new ConcurrentBag<object>();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Dispose()
        {
            _timerActor = null;
            _timerId = Guid.Empty;

            _elapsedEvents?.Clear();
            _elapsedEvents = null;

            _cancellationTokenSource = null;
        }

        [Fact]
        public void ShouldSendNoMessagesIfCancellationIsRequested()
        {
            _cancellationTokenSource.Cancel();

            Func<TimerTick, bool> filter = msg =>
                msg.TimerId == _timerId;

            var delay = TimeSpan.FromMilliseconds(0);
            var interval = TimeSpan.FromMilliseconds(5);

            var sendMessage = _timerActor.CreateSender(_cancellationTokenSource.Token)
                .WithMessageHandler(CreateCollector(filter));

            sendMessage(new SetTimer(_timerId, delay, interval));

            // Sleep to avoid a race condition
            Thread.Sleep(1000);

            // There should be zero events that have fired            
            Assert.Empty(_elapsedEvents);
        }

        [Fact]
        public void ShouldSendTimerTickMessageWhenAPeriodicIntervalIsSpecified()
        {
            Func<TimerTick, bool> filter = msg =>
                msg.TimerId == _timerId;

            var delay = TimeSpan.FromMilliseconds(0);
            var interval = TimeSpan.FromMilliseconds(5);

            var sendMessage = _timerActor.CreateSender(_cancellationTokenSource.Token)
                .WithMessageHandler(CreateCollector(filter));

            sendMessage(new SetTimer(_timerId, delay, interval));

            // Sleep to avoid a race condition
            Thread.Sleep(1000);

            // There should be at least a couple of events that have fired            
            Assert.True(_elapsedEvents.Count(e => e is TimerTick) > 1);
        }

        [Fact]
        public void ShouldSendTimerExpiredMessageWhenTimeHasElapsed()
        {
            // Arrange
            Func<TimerExpired, bool> hasTimeExpired = msg =>
                msg.TimerId == _timerId;

            // Act
            var delay = TimeSpan.FromMilliseconds(100);
            var interval = Timeout.InfiniteTimeSpan;

            var sendMessage = _timerActor.CreateSender(_cancellationTokenSource.Token)
                .WithMessageHandler(CreateCollector(hasTimeExpired));

            sendMessage(new SetTimer(_timerId, delay, interval));

            Thread.Sleep(1000);

            // Assert
            Assert.Equal(_elapsedEvents.Count, 1);
        }

        private Action<object> CreateCollector<TMessage>(Func<TMessage, bool> filter)
            where TMessage : class
        {
            return msg =>
            {
                if (!(msg is TMessage timerMessage))
                    return;

                if (filter(timerMessage))
                    _elapsedEvents.Add(timerMessage);
            };
        }
    }
}