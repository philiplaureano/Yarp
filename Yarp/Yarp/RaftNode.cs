using System;
using System.Threading.Tasks;
using Yarp.Messages;

namespace Yarp
{
    public class RaftNode : IActor
    {
        private readonly int _term;
        private Guid _nodeId = Guid.NewGuid();
        public RaftNode(int term = 0)
        {
            _term = term;
        }

        public Task TellAsync(IContext context)
        {
            var message = context.Message;
            if (message is Request<GetCurrentTerm> request)
            {
                var senderId = request.RequesterId;
                context?.SendMessage(new Response<GetCurrentTerm>(senderId,_nodeId, _term));
            }

            return Task.CompletedTask;
        }
    }
}