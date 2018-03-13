using System;
using System.Collections.Generic;
using System.Linq;

namespace Yarp.Messages
{
    public class EnumeratedKnownActors
    {
        public EnumeratedKnownActors(IEnumerable<Guid> knownActorIds)
        {
            KnownActorIds = knownActorIds.ToArray();
        }

        public Guid[] KnownActorIds { get; }
    }
}