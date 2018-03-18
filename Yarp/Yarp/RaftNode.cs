using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Yarp.Messages;

namespace Yarp
{
    public class RaftNode : IActor
    {
        private ConcurrentBag<Action<IContext>> _handlers = new ConcurrentBag<Action<IContext>>();

        private readonly int _term;
        private readonly Guid _nodeId;
        private int _minMilliseconds;
        private int _maxMilliseconds;
        private TimeSpan _currentHeartbeatTimeout;
        private DateTime _dateLastAppended = DateTime.MinValue;

        private readonly Action<object> _sendNetworkMessage;
        private Timer _timer;

        private Guid _votedFor = Guid.Empty;

        private readonly object _synclock = new object();
        private int _lastLogIndex;
        private int _lastLogTerm;

        public RaftNode(Action<object> sendNetworkMessage) : this(Guid.NewGuid(), sendNetworkMessage)
        {
        }

        public RaftNode(Guid nodeId, Action<object> sendNetworkMessage, int term = 0,
            int heartbeatTimeoutInMilliseconds = 300)
        {
            _nodeId = nodeId;
            _sendNetworkMessage = sendNetworkMessage;
            _term = term;
            _currentHeartbeatTimeout = TimeSpan.FromMilliseconds(heartbeatTimeoutInMilliseconds);

            Become(Follower);
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
            void AddHandler<T>(Action<IContext, Request<T>> handleRequest)
            {
                AddMessageHandler(handleRequest, handlers);
            }

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
            AddCommonBehavior(handlers);
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

        private void HandleFollowerTimerTick(IContext context, TimerTick tick)
        {
            // Check if the heartbeat timeout has expired
            var timeElapsedSinceLastHeartBeat = DateTime.UtcNow - _dateLastAppended;
            if (timeElapsedSinceLastHeartBeat > _currentHeartbeatTimeout)
            {
                // Increment the term and become a candidate
                var newTerm = _term + 1;

                // Have the candidate vote for itself
                _votedFor = _nodeId;

                // Reset the election timer
                var random = new Random();
                var nextTimeoutInMilliseconds = random.Next(_minMilliseconds, _maxMilliseconds);
                _currentHeartbeatTimeout = TimeSpan.FromMilliseconds(nextTimeoutInMilliseconds);
                _dateLastAppended = DateTime.UtcNow;

                // Broadcast the vote request
                _sendNetworkMessage(new BroadcastMessage(_nodeId,
                    new RequestVote(newTerm, _nodeId, _lastLogIndex, _lastLogTerm)));

                // Become a candidate node
                Become(Candidate);
            }
        }

        private void HandleAppendEntries(IContext context, Request<AppendEntries> request)
        {
            if (request != null && request.RequestMessage != null)
                _dateLastAppended = DateTime.UtcNow;
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

                context?.SendMessage(new Response<RequestVote>(request.RequesterId, _nodeId,
                    new RequestVoteResult(currentTerm, voteSuccessful)));
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