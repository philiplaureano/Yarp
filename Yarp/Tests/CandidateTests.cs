using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Yarp;
using Yarp.Messages;

namespace Tests
{
    public class CandidateTests
    {
        private RaftNode _raftNode;

        [Fact]
        public void ShouldWinElectionIfMajorityOfVotesReceived()
        {
            var numberOfOtherActors = 9;
            var actorIds = Enumerable.Range(0, numberOfOtherActors)
                .Select(_ => Guid.NewGuid()).ToArray();

            Func<IEnumerable<Guid>> getOtherActors = () => actorIds;

            var outbox = new ConcurrentBag<object>();
            var eventLog = new ConcurrentBag<object>();
            var nodeId = Guid.NewGuid();
            _raftNode = new RaftNode(nodeId, outbox.Add, eventLog.Add, getOtherActors);

            var numberOfSuccessfulVotes = 5;
            var numberOfFailedVotes = 4;

            IEnumerable<Response<RequestVote>> CreateVotes(int term, bool result, int numberOfVotes) =>
                Enumerable.Range(0, numberOfVotes).Select(index => new Response<RequestVote>(nodeId,
                    actorIds[index], new RequestVoteResult(term, result, actorIds[index], nodeId)));

            // Create an election where 5 out of 9 votes are in favor of the
            // candidate node
            var newTerm = 1;
            var successfulVotes = CreateVotes(newTerm, true, numberOfSuccessfulVotes);
            var failedVotes = CreateVotes(newTerm, false, numberOfFailedVotes);
            var combinedVotes = successfulVotes
                .Union(failedVotes).ToArray();

            // Start the node and let the election timeout expire
            // in order to trigger a new election
            _raftNode.Tell(new Initialize());
            Thread.Sleep(200);

            foreach (var actorId in getOtherActors())
            {
                // Verify the contents of every vote request sent out
                // by the node
                bool ShouldContainVoteRequest(object msg)
                {
                    if (msg is Request<RequestVote> rrv &&
                        rrv.RequestMessage is RequestVote requestVote)
                    {
                        return rrv.RequesterId == actorId &&
                               requestVote.CandidateId == nodeId &&
                               requestVote.Term == newTerm;
                    }

                    return false;
                }

                Assert.True(outbox.Count(ShouldContainVoteRequest) > 0);
            }

            // Send the vote responses back to the node
            var source = new CancellationTokenSource();
            var token = source.Token;
            var tasks = combinedVotes.Select(vote => _raftNode.TellAsync(new Context(vote, outbox.Add, token)))
                .ToArray();

            // Wait until all vote responses have been sent back to the node
            Task.WaitAll(tasks);

            // The node should post an election outcome message
            Assert.NotEmpty(eventLog);

            var outcome = eventLog.Where(msg => msg != null && msg is ElectionOutcome)
                .Cast<ElectionOutcome>()
                .First();

            Assert.Equal(nodeId, outcome.WinningActorId);
            Assert.Equal(newTerm, outcome.Term);
            Assert.Subset(new HashSet<Guid>(actorIds), new HashSet<Guid>(outcome.KnownActors));

            // Verify the votes
            var quorumCount = actorIds.Length * .51;
            var matchingVotes = 0;
            foreach (var vote in combinedVotes)
            {
                Assert.IsType<RequestVoteResult>(vote.ResponseMessage);

                var currentVote = (RequestVoteResult) vote.ResponseMessage;

                bool HasMatchingVote(RequestVoteResult result)
                {
                    return currentVote.VoteGranted == result.VoteGranted &&
                           currentVote.CandidateId == result.CandidateId &&
                           currentVote.Term == result.Term &&
                           currentVote.VoterId == result.VoterId;
                }

                matchingVotes += outcome.Votes.Count(HasMatchingVote);
            }

            // There should be a majority vote in favor of the candidate
            Assert.True(matchingVotes >= quorumCount);
        }

        [Fact]
        public void ShouldStartNewElectionIfElectionTimeoutOccurs()
        {
            var numberOfOtherActors = 3;
            var actorIds = Enumerable.Range(0, numberOfOtherActors)
                .Select(_ => Guid.NewGuid()).ToArray();

            Func<IEnumerable<Guid>> getOtherActors = () => actorIds;

            var electionTimeoutInMilliseconds = 1000;

            var initialTerm = 0;
            var outbox = new ConcurrentBag<object>();
            var eventLog = new ConcurrentBag<object>();
            var nodeId = Guid.NewGuid();
            _raftNode = new RaftNode(nodeId, outbox.Add, eventLog.Add, getOtherActors, initialTerm,
                electionTimeoutInMilliseconds);

            // Start the node and let it timeout to trigger an election
            _raftNode.Tell(new Initialize());
            Thread.Sleep(200);

            // Verify that the vote requests have been sent
            Assert.True(
                outbox.Count(msg => msg is Request<RequestVote> rv && rv.RequestMessage.Term == initialTerm + 1) ==
                numberOfOtherActors);

            // Avoid sending any votes so that the election has to restart
            Thread.Sleep(electionTimeoutInMilliseconds);

            // If another round of Request<RequestVote> messages goes out, it means
            // that the node has restarted the election
            Assert.True(
                outbox.Count(msg => msg is Request<RequestVote> rv && rv.RequestMessage.Term == initialTerm + 2) ==
                numberOfOtherActors);
        }

        [Fact]
        public void ShouldSendHeartbeatMessagesAfterWinningElection()
        {
            throw new NotImplementedException("TODO: Implement ShouldSendHeartbeatMessagesAfterWinningElection");
        }

        [Fact]
        public void ShouldRevertToFollowerAfterLosingAnElection()
        {
            throw new NotImplementedException("TODO: Implement ShouldRevertToFollowerAfterLosingAnElection");
        }

        [Fact]
        public void ShouldRevertToFollowerUponReceivingHeartbeatFromALeaderWithAHigherTerm()
        {
            throw new NotImplementedException(
                "TODO: Implement ShouldRevertToFollowerUponReceivingHeartbeatFromALeaderWithAHigherTerm");
        }

        [Fact]
        public void ShouldBecomeLeaderAfterWinningAnElection()
        {
            throw new NotImplementedException("TODO: Implement ShouldBecomeLeaderAfterWinningAnElection");
        }

        [Fact]
        public void ShouldBecomeFollowerIfAppendEntriesReceivedFromNewLeader()
        {
            var electionTimeoutInMilliseconds = 1000;

            var initialTerm = 0;
            var outbox = new ConcurrentBag<object>();
            var eventLog = new ConcurrentBag<object>();
            var nodeId = Guid.NewGuid();

            var numberOfOtherActors = 2;

            IEnumerable<Guid> getOtherActors()
            {
                return Enumerable.Range(0, numberOfOtherActors).Select(_ => Guid.NewGuid());
            }

            _raftNode = new RaftNode(nodeId, outbox.Add, eventLog.Add, getOtherActors, initialTerm,
                electionTimeoutInMilliseconds);

            void BlockUntilTrue(Func<bool> condition)
            {
                while (!condition())
                {
                }
            }

            // Start the node and let it timeout to trigger an election
            _raftNode.Tell(new Initialize());
            eventLog.BlockUntilAny(msg => msg is RoleStateChanged rsc && rsc.Term == initialTerm);

            // Look for the change in state to a candidate node
            Assert.True(eventLog.Count(msg => msg is RoleStateChanged rsc &&
                                              rsc.NewState == RoleState.Candidate &&
                                              rsc.Term == initialTerm) == 1);

            // Send a single heartbeat with a higher term so that the node reverts to 
            // a follower state
            var leaderId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();
            _raftNode.Tell(new Request<AppendEntries>(requesterId,
                new AppendEntries(42, leaderId, 0, 0, new object[0], 0)));

            // Wait for a response
            outbox.BlockUntilAny(msg =>
                msg is Response<AppendEntries> response && response.ResponseMessage is AppendEntriesResult ae &&
                ae.Term == 42);

            var result = outbox.CastAs<Response<AppendEntries>>().First();
            Assert.IsType<AppendEntriesResult>(result.ResponseMessage);

            var responseMessage = (AppendEntriesResult) result.ResponseMessage;
            Assert.True(responseMessage.Success);

            // There should also be a state change that shows
            // that the candidate node reverted back to a follower state
            eventLog.ShouldHaveAtLeastOne(msg=>msg is RoleStateChanged rsc && rsc.NewState== RoleState.Follower && rsc.Term > 42);
        }
    }
}