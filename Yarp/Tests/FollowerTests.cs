using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Xunit;
using Yarp;
using Yarp.Messages;

namespace Tests
{
    public class FollowerTests : IDisposable
    {
        private CancellationTokenSource _source;

        public FollowerTests()
        {
            _source = new CancellationTokenSource();
        }

        public void Dispose()
        {
            _source?.Cancel();
            _source = null;
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
            var currentTerm = 42;
            var node = new RaftNode(currentTerm);

            // Collect the results in the outbox
            var outbox = new ConcurrentBag<object>();
            Action<object> outboxHandler = msg => { outbox.Add(msg); };

            // Queue the request
            var requesterId = Guid.NewGuid();
            var token = _source.Token;
            var sendMessage = node.CreateSender(outboxHandler, token);
            sendMessage(new Request<GetCurrentTerm>(requesterId, new GetCurrentTerm()));

            Thread.Sleep(500);

            // Match the current term
            Assert.True(outbox.Count(msg => msg is Response<GetCurrentTerm>) == 1);
            var response = outbox.Cast<Response<GetCurrentTerm>>().First();
            Assert.Equal(currentTerm, response.ResponseMessage);
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
    }
}