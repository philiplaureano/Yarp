using System;
using Xunit;
using Yarp;

namespace Tests
{
    public class CandidateTests
    {
        private RaftNode _raftNode;

        public CandidateTests()
        {
        }

        [Fact]
        public void ShouldWinElectionIfMajorityOfVotesReceived()
        {
            throw new NotImplementedException("TODO: Implement ShouldWinElectionIfMajorityOfVotesReceived");
        }

        [Fact]
        public void ShouldSendHeartbeatMessagesAfterWinningElection()
        {
            throw new NotImplementedException("TODO: Implement ShouldSendHeartbeatMessagesAfterWinningElection");
        }
    }
}