using System;

namespace Yarp.Messages
{
    public class RequestVoteResult
    {
        public RequestVoteResult(int term, bool voteGranted, Guid voterId, Guid candidateId)
        {
            Term = term;
            VoteGranted = voteGranted;
            VoterId = voterId;
            CandidateId = candidateId;
        }

        public int Term { get; }
        public bool VoteGranted { get; }
        public Guid VoterId { get; }
        public Guid CandidateId { get; }
    }
}