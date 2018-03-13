using System;

namespace Yarp.Messages
{
    public class RegisterActor
    {
        public RegisterActor(Guid actorId, IActor actor)
        {
            ActorId = actorId;
            Actor = actor;
        }

        public Guid ActorId { get; }
        public IActor Actor { get; }
    }
}