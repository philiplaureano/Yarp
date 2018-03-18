using System;

namespace Yarp.Messages
{
    public class RequestVote
    {
        public RequestVote(int term, Guid candidateId, int lastLogIndex, int lastLogTerm)
        {
            Term = term;
            CandidateId = candidateId;
            LastLogIndex = lastLogIndex;
            LastLogTerm = lastLogTerm;
        }

        public int Term { get; }
        public Guid CandidateId { get; }
        public int LastLogIndex { get; }
        public int LastLogTerm { get; }
    }
}