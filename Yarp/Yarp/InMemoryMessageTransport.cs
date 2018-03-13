using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Yarp.Messages;

namespace Yarp
{
    public class InMemoryMessageTransport : IActor
    {
        private ConcurrentDictionary<Guid,IActor> _actors = new ConcurrentDictionary<Guid, IActor>();
        public Task TellAsync(IContext context)
        {
            if (context.Token.IsCancellationRequested)
                return Task.FromCanceled(context.Token);
            
            var message = context.Message;
            if (message is RegisterActor msg && !_actors.ContainsKey(msg.ActorId))
            {
                _actors[msg.ActorId] = msg.Actor;
                context?.SendMessage(new RegisteredActor(msg.ActorId, msg.Actor));
            }
                
            return Task.FromResult(0);
        }

        public IReadOnlyCollection<KeyValuePair<Guid, IActor>> RegisteredActors => _actors;
    }
}