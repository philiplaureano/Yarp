using System;
using Xunit;

namespace Tests
{
    public class FollowerTests : IDisposable
    {
        public FollowerTests()
        {
        }

        [Fact]
        public void MustSwitchToCandidateWhenElectionTimeoutOccurs()
        {
            throw new NotImplementedException("TODO: Implement MustSwitchToCandidateWhenElectionTimeoutOccurs");
        }

        [Fact]
        public void MustVoteForCandidateIfCandidateTermIsHigherThanCurrentTerm()
        {
            throw new NotImplementedException(
                "TODO: Implement MustVoteForCandidateIfCandidateTermIsHigherThanCurrentTerm");
        }

        [Fact]
        public void MustRejectVoteIfVoteRequestIsInvalid()
        {
            throw new NotImplementedException("TODO: Implement MustRejectVoteIfVoteRequestIsInvalid");
        }

        public void Dispose()
        {
        }
    }
}