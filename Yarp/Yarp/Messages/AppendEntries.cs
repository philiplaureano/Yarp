using System;

namespace Yarp.Messages
{
    public class AppendEntries
    {
        public AppendEntries(int term, Guid leaderId, int previousLogIndex, int previousLogTerm, object[] entries,
            int leaderCommitIndex)
        {
            Term = term;
            LeaderId = leaderId;
            PreviousLogIndex = previousLogIndex;
            PreviousLogTerm = previousLogTerm;
            Entries = entries ?? throw new ArgumentNullException(nameof(entries));
            LeaderCommitIndex = leaderCommitIndex;
        }

        public int Term { get; }
        public Guid LeaderId { get; }
        public int PreviousLogIndex { get; }
        public int PreviousLogTerm { get; }
        public object[] Entries { get; }
        public int LeaderCommitIndex { get; }
    }
}