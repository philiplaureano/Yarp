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
        public void ShouldBeAbleToGetFollowerIdWheneverIdIsRequested()
        {
            throw new NotImplementedException("TODO: Implement ShouldBeAbleToGetFollowerIdWheneverIdIsRequested");
        }

        [Fact]
        public void ShouldBeAbleToReturnCurrentLog()
        {
            throw new NotImplementedException("TODO: Implement ShouldBeAbleToReturnCurrentLog");
        }

        [Fact]
        public void ShouldBeAbleToReturnCurrentCommitIndex()
        {
            throw new NotImplementedException("TODO: Implement ShouldBeAbleToReturnCurrentCommitIndex");
        }        

        [Fact]
        public void ShouldReturnCurrentTermWheneverTermIsRequested()
        {
            throw new NotImplementedException("TODO: Implement ShouldReturnCurrentTermWheneverTermIsRequested");
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