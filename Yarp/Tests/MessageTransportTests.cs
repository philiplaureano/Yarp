using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            Assert.True(
                messageOutbox.Count(entry => { return entry is RegisteredActor msg && msg.ActorId == actorId; }) == 1);
        }

        [Fact]
        public void ShouldNotifyRegisteredActorOfActorIdAfterRegistration()
        {
            throw new NotImplementedException("TODO: Implement ShouldNotifyRegisteredActorOfActorId");
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
            var combinedInbox = new ConcurrentBag<object>();
            var sendMessage = _transport.CreateSender(_source.Token)
                .WithMessageHandler(msg =>
                {
                    // Ignore the responses send to the outbox; we only care about the 
                    // actors receiving the messages
                });

            Action<object> receiveMessage = msg => combinedInbox.Add(msg);

            var numberOfExpectedMessages = 1000;
            var fakeActors = new List<IActor>();
            for (var i = 0; i < numberOfExpectedMessages; i++)
            {
                var actorId = Guid.NewGuid();
                var fakeActor = receiveMessage.ToActor();
                sendMessage(new RegisterActor(actorId, fakeActor));

                fakeActors.Add(fakeActor);
            }

            // Broadcast the message
            var senderId = Guid.NewGuid();
            var messageToBroadcast = "Hello, World";
            sendMessage(new BroadcastMessage(senderId, messageToBroadcast));

            Thread.Sleep(500);

            Assert.True(combinedInbox.Count(msg => (msg is string s) && s == messageToBroadcast) ==
                        numberOfExpectedMessages);
        }

        [Fact]
        public void ShouldBeAbleToEnumerateAllKnownActors()
        {
            throw new NotImplementedException("TODO: Implement ShouldBeAbleToEnumerateAllKnownActors");
        }

        [Fact]
        public void ShouldBeAbleToSendMessagesToSpecificActors()
        {
            var combinedInbox = new ConcurrentBag<object>();
            var sendMessage = _transport.CreateSender(_source.Token)
                .WithMessageHandler(msg =>
                {
                    // Ignore the responses send to the outbox; we only care about the 
                    // actors receiving the messages
                });

            Action<object> receiveMessage = msg => combinedInbox.Add(msg);

            // Register the target actor that should receive the message
            var targetId = Guid.NewGuid();
            var targetActor = A.Fake<IActor>();
            sendMessage(new RegisterActor(targetId, targetActor));

            // Register the actors that should not receive the messages at all
            for (var i = 0; i < 1000; i++)
            {
                var actorId = Guid.NewGuid();
                var fakeActor = receiveMessage.ToActor();
                sendMessage(new RegisterActor(actorId, fakeActor));
            }

            var messageToSend = "Hello World";
            sendMessage(new TargetedMessage(targetId, messageToSend));

            Thread.Sleep(500);

            // Verify that the target actor received the message
            A.CallTo(() => targetActor.TellAsync(A<IContext>.Ignored)).MustHaveHappened();

            // ...and none of the other actors received the message
            Assert.True(combinedInbox.Count == 0);
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