using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yarp.Messages;

namespace Yarp
{
    public static class ActorExtensions
    {
        public static Response<T> Request<T>(this IActor actor, Guid requesterId, Func<T> createRequest)
        {
            var message = new Request<T>(requesterId, createRequest());
            var results = actor.Tell(message).ToArray();

            if (results.Length == 0)
                return Response<T>.Empty;
            
            // Return only the first matching response by default            
            var responses = results.Where(r => r.GetType() == typeof(Response<T>))
                .Cast<Response<T>>()
                .ToArray();

            return responses.First();
        }
        public static IEnumerable<object> Tell(this IActor actor, object message)
        {
            var outbox = new List<object>();
            actor.Tell(message, outbox.Add);
            return outbox;
        }

        public static void Tell(this IActor actor, object message, IList<object> outbox)
        {
            actor.Tell(message, outbox.Add);
        }

        public static void Tell(this IActor actor, object message, Action<object> handleOutboundMessage)
        {
            var source = new CancellationTokenSource();
            var token = source.Token;

            var context = new Context(message, handleOutboundMessage, token);
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