using System;

namespace Yarp.Messages
{
    public class Response<T>
    {        
        public Response(Guid requesterId, Guid responderId, object responseMessage)
        {
            RequesterId = requesterId;
            ResponderId = responderId;
            ResponseMessage = responseMessage;
        }

        public Guid RequesterId { get; }
        public Guid ResponderId { get; }
        public object ResponseMessage { get; }

        public static Response<T> Empty { get; } = new Response<T>(Guid.Empty,Guid.Empty, new object());
    }
}