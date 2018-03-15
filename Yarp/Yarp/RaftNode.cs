using System;
using System.Threading.Tasks;
using Yarp.Messages;

namespace Yarp
{
    public class RaftNode : IActor
    {
        private readonly int _term;
        private Guid _nodeId;
        private int _minMilliseconds;
        private int _maxMilliseconds;

        public RaftNode() : this(Guid.NewGuid())
        {
        }

        public RaftNode(Guid nodeId, int term = 0)
        {
            _nodeId = nodeId;
            _term = term;
        }

        public Task TellAsync(IContext context)
        {
            var message = context.Message;
            if (message is Request<GetCurrentTerm> request)
            {
                var senderId = request.RequesterId;
                context?.SendMessage(new Response<GetCurrentTerm>(senderId, _nodeId, _term));
            }

            if (message is Request<GetId> getIdRequest)
            {
                var senderId = getIdRequest.RequesterId;
                context?.SendMessage(new Response<GetId>(senderId, _nodeId, _nodeId));
            }

            if (message is Request<SetElectionTimeoutRange> setTimeoutRequest)
            {
                var setTimeoutMessage = setTimeoutRequest.RequestMessage;
                _minMilliseconds = setTimeoutMessage.MinMilliseconds;
                _maxMilliseconds = setTimeoutMessage.MaxMilliseconds;
                
                var response = new CurrentElectionTimeOutRange(_nodeId,_minMilliseconds,
                    _maxMilliseconds);
                
                context?.SendMessage(
                    new Response<SetElectionTimeoutRange>(setTimeoutRequest.RequesterId,
                    _nodeId,response));
            }

            if (message is Request<GetCurrentElectionTimeOutRange> getTimeoutRequest)
            {
                var response = new CurrentElectionTimeOutRange(_nodeId,_minMilliseconds,
                    _maxMilliseconds);
                
                context?.SendMessage(
                    new Response<GetCurrentElectionTimeOutRange>(getTimeoutRequest.RequesterId,
                        _nodeId,response));
            }
            
            return Task.CompletedTask;
        }
    }
}