using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.Base;
using ProtoBuf;

namespace LoggerMessages
{
    [ProtoContract]
    public class IterativeLookupRequest
        :BaseMessage
    {
        public const string PIPE_NAME = "Iterative lookup request begun";

        [ProtoMember(1)]
        public Guid lookupId;

        [ProtoMember(2)]
        public Identifier512 LocalIdentifier;

        [ProtoMember(3)]
        public Guid NetworkId;

        [ProtoMember(4)]
        public Configuration Configuration;

        [ProtoMember(5)]
        public Identifier512 target;

        [ProtoMember(6)]
        public int limit;

        public IterativeLookupRequest(Guid lookupId, Identifier512 LocalIdentifier, Guid NetworkId, Configuration Configuration, Identifier512 target, int limit)
        {
            this.lookupId = lookupId;
            this.LocalIdentifier = LocalIdentifier;
            this.NetworkId = NetworkId;
            this.Configuration = Configuration;
            this.target = target;
            this.limit = limit;
        }

        public IterativeLookupRequest()
        {

        }

        public override string ToString()
        {
            return MessageStringBuilder.BuildMessageString("IterativeLookupRequest", new KeyValuePair<string, object>[]
                {
                    new KeyValuePair<string, object>("lookupId", lookupId),
                    new KeyValuePair<string, object>("LocalIdentifier", LocalIdentifier),
                    new KeyValuePair<string, object>("NetworkId", NetworkId),
                    new KeyValuePair<string, object>("target", target),
                    new KeyValuePair<string, object>("limit", limit),
                }
            );
        }
    }
}
