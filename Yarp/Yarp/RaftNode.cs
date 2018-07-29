﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yarp.Messages;

namespace Yarp
{
    public class RaftNode : IActor
    {
        private readonly Action<object> _eventLogger;
        private ConcurrentBag<Action<IContext>> _handlers = new ConcurrentBag<Action<IContext>>();

        private ConcurrentDictionary<Guid, RequestVoteResult> _pendingVotes =
            new ConcurrentDictionary<Guid, RequestVoteResult>();

        private readonly Guid _nodeId;
        private int _term;
        private int _minMilliseconds;
        private int _maxMilliseconds;
        private TimeSpan _currentHeartbeatTimeout;
        private TimeSpan _currentElectionTimeout;
        private DateTime _dateLastAppended = DateTime.MinValue;

        private readonly Action<object> _sendNetworkMessage;
        private readonly Func<IEnumerable<Guid>> _getClusterActorIds;
        private readonly object _synclock = new object();
        private readonly object _logLock = new object();
        private readonly List<(int, object)> _logEntries = new List<(int, object)>();

        private Timer _timer;

        private Guid _votedFor = Guid.Empty;

        private int _lastLogIndex;
        private int _lastLogTerm;
        private double _quorumPercentage = .51;
        private DateTime _electionStartTime = DateTime.MinValue;

        public RaftNode(Action<object> sendNetworkMessage) : this(Guid.NewGuid(), sendNetworkMessage, delegate { },
            () => new Guid[0])
        {
        }

        public RaftNode(Guid nodeId, Action<object> sendNetworkMessage, Action<object> eventLogger,
            Func<IEnumerable<Guid>> getClusterActorIds,
            int term = 0, int heartbeatTimeoutInMilliseconds = 300, int electionTimeoutInMilliseconds = 1000)
        {
            _nodeId = nodeId;
            _sendNetworkMessage = sendNetworkMessage;
            _eventLogger = eventLogger;
            _getClusterActorIds = getClusterActorIds;
            _term = term;
            _currentElectionTimeout = TimeSpan.FromMilliseconds(electionTimeoutInMilliseconds);
            _currentHeartbeatTimeout = TimeSpan.FromMilliseconds(heartbeatTimeoutInMilliseconds);

            Become(Follower);
            LogEvent(new RoleStateChanged(_nodeId, _term, DateTime.UtcNow, RoleState.Follower,
                RoleState.NotYetInitialized));
        }

        public Task TellAsync(IContext context)
        {
            var handlers = _handlers.ToArray();
            foreach (var handler in handlers)
            {
                handler(context);
            }

            return Task.CompletedTask;
        }

        private void Become(Action<ConcurrentBag<Action<IContext>>> loadHandlers)
        {
            lock (_synclock)
            {
                var newHandlers = new ConcurrentBag<Action<IContext>>();
                loadHandlers(newHandlers);
                _handlers = newHandlers;
            }
        }

        private void Initialized(ConcurrentBag<Action<IContext>> handlers)
        {
            AddMessageHandler<Initialize>(HandleInitialize, handlers);
        }

        private void AddCommonBehavior(ConcurrentBag<Action<IContext>> handlers)
        {
            void AddHandler<T>(Action<IContext, Request<T>> handleRequest) =>
                AddMessageHandler(handleRequest, handlers);

            // Unwrap targeted messages by default
            AddMessageHandler<TargetedMessage>(HandleTargetedMessage, handlers);
            AddHandler<GetCurrentTerm>(HandleGetCurrentTermRequest);
            AddHandler<GetId>(HandleGetIdRequest);
            AddHandler<SetElectionTimeoutRange>(HandleSetElectionTimeoutRangeRequest);
            AddHandler<GetCurrentElectionTimeOutRange>(HandleGetCurrentElectionTimeOutRangeRequest);
            AddHandler<GetLastUpdatedTimestamp>(HandleGetLastUpdatedTimestamp);
            AddHandler<AppendEntries>(HandleAppendEntries);
        }

        private void Follower(ConcurrentBag<Action<IContext>> handlers)
        {
            void AddHandler<T>(Action<IContext, Request<T>> handleRequest)
            {
                AddMessageHandler(handleRequest, handlers);
            }

            // Reuse the initializer state handlers
            Initialized(handlers);

            AddCommonBehavior(handlers);
            AddHandler<RequestVote>(HandleRequestVote);

            AddMessageHandler<TimerTick>(HandleFollowerTimerTick, handlers);
        }

        private void Candidate(ConcurrentBag<Action<IContext>> handlers)
        {
            // Reset the election timer
            _electionStartTime = DateTime.UtcNow;

            // Increment the term 
            var newTerm = _term + 1;
            StartElection(newTerm);

            AddCommonBehavior(handlers);
            AddMessageHandler<Response<RequestVote>>(HandleCandidateVoteResults, handlers);
            AddMessageHandler<TimerTick>(HandleCandidateTimerTick, handlers);
        }

        private void HandleCandidateTimerTick(IContext context, TimerTick tick)
        {
            var currentTime = DateTime.UtcNow;
            var timeElapsed = currentTime - _electionStartTime;
            if (timeElapsed > _currentElectionTimeout)
            {
                Become(Candidate);
                return;
            }
        }

        private void HandleCandidateVoteResults(IContext context, Response<RequestVote> response)
        {
            // Ignore results that don't match the current term
            if (!(response.ResponseMessage is RequestVoteResult result))
                return;

            if (result.Term != _term)
                return;

            // Ignore duplicate votes
            var voterId = result.VoterId;
            if (!_pendingVotes.ContainsKey(voterId))
                _pendingVotes[voterId] = result;

            if (_pendingVotes.ContainsKey(voterId) && _pendingVotes[voterId] == null)
                _pendingVotes[voterId] = result;

            // The election is completed either when a majority of the votes come in, or
            // an election timeout occurs
            var quorumCount = _pendingVotes.Keys.Count() * _quorumPercentage;
            if (!(_pendingVotes.Values.Count(item => item != null) >= quorumCount))
                return;

            var votes = _pendingVotes?.Values.Where(v => v != null).ToArray();

            var validVotes = votes.Where(v => v.Term == _term).ToArray();

            var winnerId = Guid.Empty;
            var candidateVotes = validVotes.GroupBy(v => v.CandidateId);
            foreach (var votingGroup in candidateVotes)
            {
                var candidateId = votingGroup.Key;
                var numberOfVotes = votingGroup.Count(v => v.VoteGranted);
                if (numberOfVotes >= quorumCount)
                {
                    winnerId = candidateId;
                    break;
                }
            }

            var outcome = new ElectionOutcome(winnerId, _term, _pendingVotes.Keys, votes);
            LogEvent(outcome);
        }

        private void AddMessageHandler<TRequest>(Action<IContext, Request<TRequest>> messageHandler,
            ConcurrentBag<Action<IContext>> handlers)
        {
            var contextHandler = CreateContextHandler(messageHandler);
            handlers.Add(contextHandler);
        }

        private void AddMessageHandler<TMessage>(Action<IContext, TMessage> messageHandler,
            ConcurrentBag<Action<IContext>> handlers)
        {
            Action<IContext> handler = context =>
            {
                var message = context.Message;
                if (message is TMessage msg)
                    messageHandler(context, msg);
            };

            handlers.Add(handler);
        }

        private Action<IContext> CreateContextHandler<TRequest>(Action<IContext, Request<TRequest>> handler)
        {
            return context =>
            {
                var message = context.Message;
                if (message is Request<TRequest> request)
                    handler(context, request);
            };
        }

        private void HandleInitialize(IContext context, Initialize initMessage)
        {
            if (_timer != null)
                return;
            var frequency = TimeSpan.FromMilliseconds(10);
            _timer = new Timer(OnTimerCallback,
                new TimerState(_nodeId, new object(),
                    context.SendMessage, DateTime.UtcNow, TimeSpan.Zero, frequency, context.Token),
                Timeout.InfiniteTimeSpan,
                Timeout.InfiniteTimeSpan);
            _timer.Change(TimeSpan.Zero, frequency);
        }

        private void OnTimerCallback(object state)
        {
            this.Tell(new TimerTick(_nodeId, DateTime.UtcNow));
        }

        private void HandleTargetedMessage(IContext context, TargetedMessage targetedMessage)
        {
            // Ignore messages not targeted at this current actor
            if (targetedMessage?.TargetActorId != _nodeId)
                return;

            // Unwrap the message and process it
            this.Tell(targetedMessage.Message);
        }

        private void HandleFollowerTimerTick(IContext context, TimerTick tick)
        {
            // Check if the heartbeat timeout has expired
            var timeElapsedSinceLastHeartBeat = DateTime.UtcNow - _dateLastAppended;
            if (timeElapsedSinceLastHeartBeat > _currentHeartbeatTimeout)
            {
                // Become a candidate node
                Become(Candidate);

                LogEvent(new RoleStateChanged(_nodeId, _term, DateTime.UtcNow, RoleState.Candidate,
                    RoleState.Follower));
            }
        }

        private void LogEvent(object eventMessage)
        {
            Task.Run(() => _eventLogger?.Invoke(eventMessage));
        }

        private void StartElection(int newTerm)
        {
            _term = newTerm;

            // Have the candidate vote for itself
            _votedFor = _nodeId;

            // Reset the election timer
            var random = new Random();
            var nextTimeoutInMilliseconds = random.Next(_minMilliseconds, _maxMilliseconds);
            _currentHeartbeatTimeout = TimeSpan.FromMilliseconds(nextTimeoutInMilliseconds);
            _dateLastAppended = DateTime.UtcNow;

            // Send the vote request to other actors
            var otherActors = _getClusterActorIds().Where(id => id != _nodeId).ToArray();

            // Reset the list of pending votes
            _pendingVotes.Clear();
            foreach (var actorId in otherActors)
            {
                if (_pendingVotes.ContainsKey(actorId))
                    continue;

                _pendingVotes[actorId] = null;
                _sendNetworkMessage(new Request<RequestVote>(actorId,
                    new RequestVote(newTerm, _nodeId, _lastLogIndex, _lastLogTerm)));
            }
        }

        private void HandleAppendEntries(IContext context, Request<AppendEntries> request)
        {
            if (request?.RequestMessage == null)
                return;

            _dateLastAppended = DateTime.UtcNow;

            var requesterId = request.RequesterId;
            var appendEntries = request.RequestMessage;

            /* When sending an AppendEntries RPC,
             * the leader includes the term number and index of the entry
             * that immediately precedes the new entry.
             * If the follower cannot find a match for this entry in its own log,
             * it rejects the request to append the new entry.
             */
            var termNumber = appendEntries.PreviousLogTerm;
            var targetIndexToInsertItemAfter = appendEntries.PreviousLogIndex;
            var currentNumberOfEntries = _logEntries.Count;

            // Match term number and index
            var hasIncorrectEntry = currentNumberOfEntries > 0 && _logEntries[targetIndexToInsertItemAfter].Item1 != termNumber;
            if (targetIndexToInsertItemAfter > currentNumberOfEntries || 
                hasIncorrectEntry)
            {
                context?.SendMessage(new Response<AppendEntries>(requesterId, _nodeId,
                    new AppendEntriesResult(_term, false)));
            }

            // Convert to a follower if the other term is higher
            if (_term < appendEntries.Term)
            {
                Become(Follower);
                _term = appendEntries.Term;
            }

            _lastLogTerm = appendEntries.Term;

            // Update the log entries
            var entries = appendEntries.Entries ?? new object[0];
            foreach (var entry in entries)
            {
                _logEntries.Add((appendEntries.Term, entry));
            }

            // Set the last log index
            var newIndexOffset = entries.Length;
            _lastLogIndex += newIndexOffset;

            context?.SendMessage(new Response<AppendEntries>(requesterId, _nodeId,
                new AppendEntriesResult(_term, true)));
        }

        private void HandleRequestVote(IContext context, Request<RequestVote> request)
        {
            // Reply false if term < currentTerm (§5.1)            
            if (request != null && request.RequestMessage != null)
            {
                var currentTerm = _term;
                var voteSuccessful = request?.RequestMessage.Term > currentTerm;
                if (voteSuccessful)
                    _dateLastAppended = DateTime.UtcNow;
                var candidateId = request.RequestMessage.CandidateId;
                context?.SendMessage(new Response<RequestVote>(request.RequesterId, _nodeId,
                    new RequestVoteResult(currentTerm, voteSuccessful, _nodeId, candidateId)));
            }
        }

        private void HandleGetLastUpdatedTimestamp(IContext context, Request<GetLastUpdatedTimestamp> request)
        {
            var requester = request.RequesterId;
            var response = new Response<GetLastUpdatedTimestamp>(requester, _nodeId, _dateLastAppended);
            context?.SendMessage(response);
        }

        private void HandleGetCurrentElectionTimeOutRangeRequest(IContext context,
            Request<GetCurrentElectionTimeOutRange> getTimeoutRequest1)
        {
            var response = new CurrentElectionTimeOutRange(_nodeId, _minMilliseconds,
                _maxMilliseconds);
            context?.SendMessage(
                new Response<GetCurrentElectionTimeOutRange>(getTimeoutRequest1.RequesterId,
                    _nodeId, response));
        }

        private void HandleSetElectionTimeoutRangeRequest(IContext context,
            Request<SetElectionTimeoutRange> setTimeoutRequest1)
        {
            var setTimeoutMessage = setTimeoutRequest1.RequestMessage;
            _minMilliseconds = setTimeoutMessage.MinMilliseconds;
            _maxMilliseconds = setTimeoutMessage.MaxMilliseconds;
            var response = new CurrentElectionTimeOutRange(_nodeId, _minMilliseconds,
                _maxMilliseconds);
            context?.SendMessage(
                new Response<SetElectionTimeoutRange>(setTimeoutRequest1.RequesterId,
                    _nodeId, response));
        }

        private void HandleGetIdRequest(IContext context, Request<GetId> getIdRequest1)
        {
            var senderId = getIdRequest1.RequesterId;
            context?.SendMessage(new Response<GetId>(senderId, _nodeId, _nodeId));
        }

        private void HandleGetCurrentTermRequest(IContext context, Request<GetCurrentTerm> request)
        {
            var senderId = request.RequesterId;
            context?.SendMessage(new Response<GetCurrentTerm>(senderId, _nodeId, _term));
        }
    }
}