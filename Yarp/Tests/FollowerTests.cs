using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private RaftNode _raftNode;

        public FollowerTests()
        {
            _source = new CancellationTokenSource();
            _raftNode = new RaftNode();
        }

        public void Dispose()
        {
            _source?.Cancel();
            _source = null;
            _raftNode = null;
        }

        [Fact]
        public void ShouldBeAbleToGetFollowerIdWheneverIdIsRequested()
        {
            var nodeId = Guid.NewGuid();
            _raftNode = new RaftNode(nodeId);
            var requesterId = Guid.NewGuid();
            Func<object> createMessageToSend = () => new Request<GetId>(requesterId, new GetId());
            Action<IEnumerable<object>> checkResults = outbox =>
            {
                var response = outbox.Cast<Response<GetId>>().First();
                Assert.Equal(requesterId, response.RequesterId);
                Assert.Equal(nodeId,response.ResponderId);
                Assert.Equal(nodeId, response.ResponseMessage);
            };

            RunTest(createMessageToSend, checkResults);
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

            _raftNode = new RaftNode(Guid.NewGuid(), currentTerm);

            // Queue the request
            var requesterId = Guid.NewGuid();
            Func<object> createMessageToSend = () => new Request<GetCurrentTerm>(requesterId,
                new GetCurrentTerm());

            // Match the current term
            void CheckResults(IEnumerable<object> results)
            {
                Assert.True(results.Count(msg => msg is Response<GetCurrentTerm>) == 1);
                var response = results.Cast<Response<GetCurrentTerm>>().First();
                Assert.Equal(currentTerm, response.ResponseMessage);
            }

            RunTest(createMessageToSend, CheckResults);
        }

        private void RunTest(Func<object> createMessageToSend,
            Action<IEnumerable<object>> checkResults)
        {
            // Collect the results in the outbox
            var outbox = new ConcurrentBag<object>();
            Action<object> outboxHandler = msg => { outbox.Add(msg); };

            var token = _source.Token;
            var sendMessage = _raftNode.CreateSender(outboxHandler, token);
            sendMessage(createMessageToSend());

            Thread.Sleep(500);

            checkResults(outbox);
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