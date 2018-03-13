using System;

namespace Yarp.Messages
{
    public class RegisteredActor
    {
        public RegisteredActor(Guid actorId, IActor actor)
        {
            ActorId = actorId;
            Actor = actor;
        }

        public Guid ActorId { get; }
        public IActor Actor { get; }
    }
}