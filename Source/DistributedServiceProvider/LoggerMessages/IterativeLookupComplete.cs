using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using DistributedServiceProvider.Base;

namespace LoggerMessages
{
    [ProtoContract]
    public class IterativeLookupComplete
        :BaseMessage
    {
        public const string PIPE_NAME = "Iterative Lookup Complete";

        [ProtoMember(1)]
        public Guid lookupId;

        [ProtoMember(2)]
        public Identifier512[] Closest;

        [ProtoMember(3)]
        public int Steps;

        public IterativeLookupComplete()
        {

        }

        public IterativeLookupComplete(Guid lookupId, IEnumerable<Identifier512> closest, int steps)
        {
            this.lookupId = lookupId;
            this.Closest = closest.ToArray();
            this.Steps = steps;
        }
    }
}
