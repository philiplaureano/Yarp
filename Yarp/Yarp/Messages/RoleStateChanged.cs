using System;

namespace Yarp.Messages
{
    public class RoleStateChanged
    {
        public RoleStateChanged(Guid actorId, int term, DateTime dateRecordedUtc, RoleState newState, RoleState oldState)
        {
            ActorId = actorId;
            NewState = newState;
            OldState = oldState;
            DateRecordedUtc = dateRecordedUtc;
            Term = term;
        }

        public Guid ActorId { get; }
        public RoleState NewState { get; }
        public RoleState OldState { get; }
        public DateTime DateRecordedUtc { get; }
        public int Term { get; }
    }
}