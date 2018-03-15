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
                Assert.Equal(nodeId, response.ResponderId);
                Assert.Equal(nodeId, response.ResponseMessage);
            };

            RunTest(createMessageToSend, checkResults);
        }

        [Fact]
        public void ShouldBeAbleToSetElectionTimeoutRange()
        {
            var minMilliseconds = 150;
            var maxMilliseconds = 300;

            var requesterId = Guid.NewGuid();
            var message = new Request<SetElectionTimeoutRange>(requesterId,
                new SetElectionTimeoutRange(minMilliseconds, maxMilliseconds));

            var results = _raftNode.Tell(message).ToArray();
            Assert.NotEmpty(results);

            var responses = results.Where(r => r.GetType() == typeof(Response<SetElectionTimeoutRange>))
                .Cast<Response<SetElectionTimeoutRange>>()
                .ToArray();

            Assert.NotEmpty(responses);
            var response = responses.First();

            Assert.Equal(requesterId, response.RequesterId);

            // Note the type will be bool (false) if it fails
            Assert.IsType<CurrentElectionTimeOutRange>(response.ResponseMessage);

            var timeoutRange = (CurrentElectionTimeOutRange) response.ResponseMessage;
            Assert.Equal(minMilliseconds, timeoutRange.MinMilliseconds);
            Assert.Equal(maxMilliseconds, timeoutRange.MaxMilliseconds);
        }

        [Fact]
        public void ShouldBeAbleToGetElectionTimeOutRange()
        {
            // Set the timeout
            var minMilliseconds = 100;
            var maxMilliseconds = 500;
            var requesterId = Guid.NewGuid();
            _raftNode.Request(requesterId,
                () => new SetElectionTimeoutRange(minMilliseconds, maxMilliseconds));

            var response =
                _raftNode.Request(requesterId, () => new GetCurrentElectionTimeOutRange());
            
            Assert.NotEqual(Response<GetCurrentElectionTimeOutRange>.Empty, response);
            Assert.IsType<CurrentElectionTimeOutRange>(response.ResponseMessage);
            
            var timeoutRange = (CurrentElectionTimeOutRange) response.ResponseMessage;
            Assert.Equal(minMilliseconds,timeoutRange.MinMilliseconds);
            Assert.Equal(maxMilliseconds,timeoutRange.MaxMilliseconds);
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
            var sendMessage = _raftNode.CreateSenderMethod(outboxHandler, token);
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