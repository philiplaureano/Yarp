using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using FakeItEasy;
using Xunit;
using Yarp;
using Yarp.Messages;

namespace Tests
{
    public class MessageTransportTests : IDisposable
    {
        private InMemoryMessageTransport _transport;
        private CancellationTokenSource _source;

        public MessageTransportTests()
        {
            _transport = new InMemoryMessageTransport();
            _source = new CancellationTokenSource();
        }

        public void Dispose()
        {
            _transport = null;
            _source?.Cancel();
            _source = null;
        }

        [Fact]
        public void ShouldBeAbleToRegisterActor()
        {
            var messageOutbox = new ConcurrentBag<object>();

            // Keep track of the responses that come out of the
            // message transport
            var sendMessage = _transport.CreateSender(_source.Token)
                .WithMessageHandler(msg => { messageOutbox.Add(msg); });

            var fakeActor = A.Fake<IActor>();
            var actorId = Guid.NewGuid();
            sendMessage(new RegisterActor(actorId, fakeActor));

            Thread.Sleep(500);

            var registeredActors = _transport.RegisteredActors;
            Assert.True(registeredActors.Count(entry => entry.Key == actorId) > 0);
            Assert.True(messageOutbox.Count(entry =>
            {
                return entry is RegisteredActor msg && msg.ActorId == actorId;
            }) == 1);
        }

        [Fact]
        public void ShouldIgnoreDuplicateActorRegistrations()
        {
            var messageOutbox = new ConcurrentBag<object>();

            // Keep track of the responses that come out of the
            // message transport
            var sendMessage = _transport.CreateSender(_source.Token)
                .WithMessageHandler(msg => { messageOutbox.Add(msg); });

            var fakeActor = A.Fake<IActor>();
            var actorId = Guid.NewGuid();
            for (var i = 0; i < 100; i++)
            {
                sendMessage(new RegisterActor(actorId, fakeActor));
            }

            Thread.Sleep(500);

            var registeredActors = _transport.RegisteredActors;
            Assert.True(registeredActors.Count(entry => entry.Key == actorId) == 1);
        }

        [Fact]
        public void ShouldBeAbleToBroadcastMessagesToAllKnownActors()
        {
            throw new NotImplementedException("TODO: Implement ShouldBeAbleToBroadcastMessagesToAllKnownActors");
        }

        [Fact]
        public void ShouldBeAbleToEnumerateAllKnownActors()
        {
            throw new NotImplementedException("TODO: Implement ShouldBeAbleToEnumerateAllKnownActors");
        }

        [Fact]
        public void ShouldBeAbleToSendMessagesToSpecificActors()
        {
            throw new NotImplementedException("TODO: Implement ShouldBeAbleToSendMessagesToSpecificActors");
        }

        [Fact]
        public void ShouldBeAbleToIdentifyAllActors()
        {
            throw new NotImplementedException("TODO: Implement ShouldBeAbleToIdentifyAllActors");
        }

        [Fact]
        public void ShouldRegisterAllActorsThatCanIdentifyThemselves()
        {
            throw new NotImplementedException("TODO: Implement ShouldRegisterAllActorsThatCanIdentifyThemselves");
        }
    }
}