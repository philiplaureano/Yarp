using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
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
            _raftNode = new RaftNode(delegate { });
        }

        public void Dispose()
        {
            _source?.Cancel();
            _source = null;
            _raftNode = null;
        }

        [Fact]
        public void ShouldResetElectionTimerWhenVoteIsGrantedToCandidate()
        {
            // Route all the network output to the collection object
            var nodeId = Guid.NewGuid();
            var outbox = new ConcurrentBag<object>();
            var eventLog = new ConcurrentBag<object>();
            _raftNode = new RaftNode(nodeId, outbox.Add, eventLog.Add, () => new Guid[0]);

            // The first response should be DateTime.Never
            var requesterId = Guid.NewGuid();
            var response = _raftNode.Request(requesterId, () => new GetLastUpdatedTimestamp());
            Assert.NotNull(response);
            Assert.IsType<DateTime>(response.ResponseMessage);

            var lastUpdated = (DateTime) response.ResponseMessage;
            Assert.True(lastUpdated.Equals(default(DateTime)));
            Assert.True(lastUpdated.Equals(DateTime.MinValue));

            var currentTime = DateTime.UtcNow;
            var candidateId = Guid.NewGuid();

            var requestVote = new Request<RequestVote>(requesterId, new RequestVote(42, candidateId, 0, 0));
            _raftNode.Tell(requestVote);

            var secondResponse = _raftNode.Request(requesterId, () => new GetLastUpdatedTimestamp());
            Assert.NotNull(secondResponse);
            Assert.IsType<DateTime>(secondResponse.ResponseMessage);

            // The last updated timestamp should be relatively similar to the current time
            var timestamp = (DateTime) secondResponse.ResponseMessage;
            var timeDifference = timestamp - currentTime;
            Assert.True(timeDifference.TotalMilliseconds >= 0 && timeDifference <= TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void ShouldSendRequestVoteToOtherActorsInTheClusterIfElectionTimerExpires()
        {
            var minMilliseconds = 150;
            var maxMilliseconds = 300;

            var nodeId = Guid.NewGuid();
            var term = 42;

            var numberOfActorsInCluster = 5;
            var actorIds = Enumerable.Range(0, numberOfActorsInCluster)
                .Select(_ => Guid.NewGuid()).ToArray();

            var outbox = new ConcurrentBag<object>();
            var eventLog = new ConcurrentBag<object>();
            _raftNode = new RaftNode(nodeId, outbox.Add, eventLog.Add, () => actorIds, term);

            // Set the request timeout to be from 150-300ms
            var requesterId = Guid.NewGuid();
            _raftNode.Request(requesterId, () => new SetElectionTimeoutRange(minMilliseconds, maxMilliseconds));

            // Start the node            
            _raftNode.Tell(new Initialize());

            // Let the timer expire
            Thread.Sleep(1000);

            var voteRequests = outbox.Where(msg => msg is Request<RequestVote> rv && rv.RequestMessage is RequestVote)
                .Cast<Request<RequestVote>>().ToArray();

            Assert.NotEmpty(voteRequests);
            Assert.True(voteRequests.Count() == numberOfActorsInCluster);

            for (var i = 0; i < numberOfActorsInCluster; i++)
            {
                var request = voteRequests[i];
                var voteRequest = request.RequestMessage;
                Assert.Equal(nodeId, voteRequest.CandidateId);

                // Note: The new candidate must increment the current vote by one
                Assert.Equal(term + 1, voteRequest.Term);
            }
        }

        [Fact]
        public void ShouldEmitChangeEventsWhenChangingRoles()
        {
            var nodeId = Guid.NewGuid();
            var outbox = new ConcurrentBag<object>();
            var eventLog = new ConcurrentBag<object>();
            _raftNode = new RaftNode(nodeId, outbox.Add, eventLog.Add, () => new Guid[0]);

            // Start the node and let it time out
            _raftNode.Tell(new Initialize());
            Thread.Sleep(500);

            bool ShouldContainChangeEvent(object msg)
            {
                return msg is RoleStateChanged rsc &&
                       rsc.ActorId == nodeId &&
                       rsc.OldState == RoleState.Follower &&
                       rsc.NewState == RoleState.Candidate;
            }

            Assert.NotEmpty(eventLog);
            Assert.True(eventLog.Count(ShouldContainChangeEvent) > 0);
        }

        [Fact]
        public void ShouldBeAbleToGetLastTimestampOfLatestHeartbeat()
        {
            // The first response should be DateTime.Never
            var requesterId = Guid.NewGuid();
            var response = _raftNode.Request(requesterId, () => new GetLastUpdatedTimestamp());
            Assert.NotNull(response);
            Assert.IsType<DateTime>(response.ResponseMessage);

            var lastUpdated = (DateTime) response.ResponseMessage;
            Assert.True(lastUpdated.Equals(default(DateTime)));
            Assert.True(lastUpdated.Equals(DateTime.MinValue));

            var currentTime = DateTime.UtcNow;
            var appendEntries = new Request<AppendEntries>(requesterId,
                new AppendEntries(0, Guid.NewGuid(), 0, 0, new object[0], 0));
            _raftNode.Tell(appendEntries);

            var secondResponse = _raftNode.Request(requesterId, () => new GetLastUpdatedTimestamp());
            Assert.NotNull(secondResponse);
            Assert.IsType<DateTime>(secondResponse.ResponseMessage);

            // The last updated timestamp should be relatively similar to the current time
            var timestamp = (DateTime) secondResponse.ResponseMessage;
            var timeDifference = timestamp - currentTime;
            Assert.True(timeDifference.TotalMilliseconds >= 0 && timeDifference <= TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void ShouldBeAbleToGetFollowerIdWheneverIdIsRequested()
        {
            var nodeId = Guid.NewGuid();
            _raftNode = new RaftNode(nodeId, delegate { }, delegate { }, () => new Guid[0]);
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
            Assert.Equal(minMilliseconds, timeoutRange.MinMilliseconds);
            Assert.Equal(maxMilliseconds, timeoutRange.MaxMilliseconds);
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

            _raftNode = new RaftNode(Guid.NewGuid(), delegate { }, delegate { }, () => new Guid[0], currentTerm);

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

        [Fact]
        public void MustSwitchToCandidateWhenElectionTimeoutOccurs()
        {
            throw new NotImplementedException("TODO: Implement MustSwitchToCandidateWhenElectionTimeoutOccurs");
        }

        [Fact]
        public void MustVoteForCandidateIfCandidateTermIsHigherThanCurrentTerm()
        {
            var currentTerm = 42;

            _raftNode = new RaftNode(Guid.NewGuid(), delegate { }, delegate { }, () => new Guid[0], currentTerm);
            var response = _raftNode.Request(Guid.NewGuid(), () => new RequestVote(43, Guid.NewGuid(), 0, 0));

            Assert.NotNull(response);
            Assert.IsType<RequestVoteResult>(response.ResponseMessage);

            var result = (RequestVoteResult) response.ResponseMessage;
            Assert.Equal(currentTerm, result.Term);
            Assert.True(result.VoteGranted);
        }

        [Fact]
        public void MustRejectVoteIfVoteRequestIsInvalid()
        {
            // Reply false if term < currentTerm (§5.1)
            var currentTerm = 42;
            _raftNode = new RaftNode(Guid.NewGuid(), delegate { }, delegate { }, () => new Guid[0], currentTerm);
            var response = _raftNode.Request(Guid.NewGuid(), () => new RequestVote(0, Guid.NewGuid(), 0, 0));

            Assert.NotNull(response);
            Assert.IsType<RequestVoteResult>(response.ResponseMessage);

            var result = (RequestVoteResult) response.ResponseMessage;
            Assert.Equal(currentTerm, result.Term);
            Assert.False(result.VoteGranted);
        }

        [Fact]
        public void ShouldAcceptAppendEntriesOnFirstRequest()
        {
            var numberOfOtherActors = 3;
            var otherActors = Enumerable.Range(0, numberOfOtherActors).Select(_ => Guid.NewGuid());
            var outbox = new ConcurrentBag<object>();
            var eventLog = new ConcurrentBag<object>();
            var startingTerm = 0;

            var leaderId = Guid.NewGuid();
            var nodeId = Guid.NewGuid();
            _raftNode = new RaftNode(nodeId, outbox.Add, eventLog.Add, () => otherActors, startingTerm);
            _raftNode.Tell(new Initialize());

            var term = 42;
            var response = _raftNode.Request(nodeId,
                () => new AppendEntries(term, leaderId, 0, 0, new object[] {"Hello, World"}, 1));

            // The response must be valid
            Assert.NotEqual(Response<AppendEntries>.Empty, response);
            Assert.IsType<AppendEntriesResult>(response.ResponseMessage);

            // The first entry must be successful since there are no prior entries
            var firstResult = (AppendEntriesResult) response.ResponseMessage;
            Assert.True(firstResult.Success);
            Assert.Equal(term, firstResult.Term);
        }

        [Fact]
        public void ShouldRejectAppendEntriesIfFollowerCannotFindAMatchForAnEntryInItsOwnLog()
        {
            /* When sending an AppendEntries RPC,
             * the leader includes the term number and index of the entry
             * that immediately precedes the new entry.
             *
             * If the follower cannot find a match for this entry in its own log,
             * it rejects the request to append the new entry.
             */
            var nodeId = Guid.NewGuid();

            var numberOfOtherActors = 3;
            var otherActors = Enumerable.Range(0, numberOfOtherActors).Select(_ => Guid.NewGuid());
            var outbox = new ConcurrentBag<object>();
            var eventLog = new ConcurrentBag<object>();
            var startingTerm = 0;

            _raftNode = new RaftNode(nodeId, outbox.Add, eventLog.Add, () => otherActors, startingTerm);
            _raftNode.Tell(new Initialize());
            Thread.Sleep(100);

            var term = 42;
            var leaderId = Guid.NewGuid();
            var appendEntries = new AppendEntries(term, leaderId, 1, 41, new object[0], 0);

            var requesterId = Guid.NewGuid();
            var response = _raftNode.Request(requesterId, () => appendEntries);

            Assert.Equal(requesterId, response.RequesterId);
            Assert.Equal(nodeId, response.ResponderId);

            // Reply false if log doesn’t contain an entry at prevLogIndex whose term matches prevLogTerm (§5.3)
            Assert.IsType<AppendEntriesResult>(response.ResponseMessage);

            var result = (AppendEntriesResult) response.ResponseMessage;

            Assert.False(result.Success);
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
    }
}