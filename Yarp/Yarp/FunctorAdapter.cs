using System;
using System.Threading.Tasks;

namespace Yarp
{
    internal class FunctorAdapter : IActor
    {
        private Action<object> _handler;

        public FunctorAdapter(Action<object> handler)
        {
            _handler = handler;
        }

        public Task TellAsync(IContext context)
        {
            if (context.Token.IsCancellationRequested)
                return Task.FromCanceled(context.Token);
            
            _handler(context.Message);
            return Task.CompletedTask;
        }
    }
}