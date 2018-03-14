using System;
using System.Threading.Tasks;
using Yarp.Messages;

namespace Yarp
{
    public class RaftNode : IActor
    {
        private readonly int _term;
        private Guid _nodeId;

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
            return Task.CompletedTask;
        }
    }
}