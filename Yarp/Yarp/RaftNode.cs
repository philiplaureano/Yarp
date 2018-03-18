using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Yarp.Messages;

namespace Yarp
{
    public class RaftNode : IActor
    {
        private readonly ConcurrentBag<Action<IContext>> _handlers = new ConcurrentBag<Action<IContext>>();

        private readonly int _term;
        private readonly Guid _nodeId;
        private int _minMilliseconds;
        private int _maxMilliseconds;
        private DateTime _dateLastAppended = DateTime.MinValue;

        public RaftNode() : this(Guid.NewGuid())
        {
        }

        public RaftNode(Guid nodeId, int term = 0)
        {
            _nodeId = nodeId;
            _term = term;

            Initialize();
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

        private void Initialize()
        {
            AddMessageHandler<GetCurrentTerm>(HandleGetCurrentTermRequest);
            AddMessageHandler<GetId>(HandleGetIdRequest);
            AddMessageHandler<SetElectionTimeoutRange>(HandleSetElectionTimeoutRangeRequest);
            AddMessageHandler<GetCurrentElectionTimeOutRange>(HandleGetCurrentElectionTimeOutRangeRequest);
            AddMessageHandler<GetLastUpdatedTimestamp>(HandleGetLastUpdatedTimestamp);
            AddMessageHandler<AppendEntries>(HandleAppendEntries);
        }

        private void AddMessageHandler<TRequest>(Action<IContext, Request<TRequest>> messageHandler)
        {
            var contextHandler = CreateContextHandler(messageHandler);
            _handlers.Add(contextHandler);
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

        private void HandleAppendEntries(IContext context, Request<AppendEntries> request)
        {
            if (request != null && request.RequestMessage != null)
                _dateLastAppended = DateTime.UtcNow;
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