using System;
using System.Collections.Generic;
using System.Linq;

namespace Yarp.Messages
{
    public class ElectionOutcome
    {
        public ElectionOutcome(Guid winningActorId, int term, 
            IEnumerable<Guid> knownActors, 
            IEnumerable<RequestVoteResult> votes)
        {
            WinningActorId = winningActorId;
            Term = term;
            KnownActors = knownActors.ToArray();
            Votes = votes.ToArray();
        }

        public Guid WinningActorId { get; }
        public int Term { get; }
        public Guid[] KnownActors { get; }
        public RequestVoteResult[] Votes { get; }
    }
}