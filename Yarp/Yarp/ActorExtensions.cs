using System;
using System.Threading;

namespace Yarp
{
    public static class ActorExtensions
    {
        public static IActor ToActor(this Action<object> handler)
        {
            return new FunctorAdapter(handler);
        }
        public static Action<object> WithMessageHandler(this Func<Action<object>, Action<object>>
            handlerFactory, Action<object> handler)
        {
            return handlerFactory(handler);
        }

        public static Func<Action<object>, Action<object>> CreateSender(this IActor actor,
            CancellationToken token)
        {
            return handler => actor.CreateSender(handler, token);
        }

        public static Action<object> CreateSender(this IActor actor,
            Action<object> outboundMessageHandler, CancellationToken token)
        {
            return async msg =>
            {
                var context = new Context(msg, outboundMessageHandler, token);
                await actor.TellAsync(context);
            };
        }
    }
}