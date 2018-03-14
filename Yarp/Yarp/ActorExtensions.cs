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
        public static Action<object> WithOutboundMessageHandler(this Func<Action<object>, Action<object>>
            handlerFactory, Action<object> handler)
        {
            return handlerFactory(handler);
        }

        public static Func<Action<object>, Action<object>> CreateSenderMethod(this IActor actor,
            CancellationToken token)
        {
            return handler => actor.CreateSenderMethod(handler, token);
        }

        public static Action<object> CreateSenderMethod(this IActor actor,
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