using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Yarp
{
    public static class ActorExtensions
    {
        public static void Tell(this IActor actor, object message, IList<object> outbox)
        {
            var source = new CancellationTokenSource();
            var token = source.Token;

            var context = new Context(message, outbox.Add, token);
            var task = actor.TellAsync(context);
            Task.WaitAll(task);
        }

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