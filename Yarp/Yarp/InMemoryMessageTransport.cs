using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Yarp.Messages;

namespace Yarp
{
    public class InMemoryMessageTransport : IActor
    {
        private ConcurrentDictionary<Guid, IActor> _actors = new ConcurrentDictionary<Guid, IActor>();

        public async Task TellAsync(IContext context)
        {
            if (context.Token.IsCancellationRequested)
                return;

            var message = context.Message;
            if (message is RegisterActor msg && !_actors.ContainsKey(msg.ActorId) && msg.Actor != null)
            {
                _actors[msg.ActorId] = msg.Actor;
                var registeredActorMessage = new RegisteredActor(msg.ActorId, msg.Actor);
                context?.SendMessage(registeredActorMessage);
                await msg.Actor?.TellAsync(new Context(registeredActorMessage, context.SendMessage, context.Token));
            }

            if (message is BroadcastMessage broadcastMessage)
            {
                var messagePayload = broadcastMessage.Message;
                foreach (var actor in _actors.Values)
                {
                    await actor.TellAsync(new Context(messagePayload, context.SendMessage, context.Token));
                }
            }

            if (message is TargetedMessage targetedMessage &&
                _actors.ContainsKey(targetedMessage.TargetActorId))
            {
                await _actors[targetedMessage.TargetActorId]
                    .TellAsync(new Context(targetedMessage, context.SendMessage, context.Token));
            }
        }

        public IReadOnlyCollection<KeyValuePair<Guid, IActor>> RegisteredActors => _actors;
    }
}