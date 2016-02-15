using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider;
using ProtoBuf;
using DistributedServiceProvider.Base;

namespace LoggerMessages
{
    [ProtoContract]
    public class DRTConstructed
        :BaseMessage
    {
        public const string PIPE_NAME = "DRT Constructed";

        [ProtoMember(1)]
        private byte[] identifierBytes;

        public Identifier512 localIdentifier
        {
            get
            {
                return new Identifier512(identifierBytes);
            }
            private set
            {
                identifierBytes = value.GetBytes().ToArray();
            }
        }

        [ProtoMember(2)]
        byte[] guidBytes;

        public Guid networkId
        {
            get
            {
                return new Guid(guidBytes);
            }
            private set
            {
                guidBytes = value.ToByteArray();
            }
        }

        [ProtoMember(3)]
        private Configuration configuration;

        public DRTConstructed(Identifier512 localIdentifier, Guid networkId, Configuration configuration)
        {
            this.localIdentifier = localIdentifier;
            this.networkId = networkId;
            this.configuration = configuration;
        }

        public DRTConstructed()
        {

        }

        public override string ToString()
        {
            return MessageStringBuilder.BuildMessageString("DRT_Constructed", new KeyValuePair<string, object>[]
                {
                    new KeyValuePair<string, object>("LocalIdentifier", localIdentifier),
                    new KeyValuePair<string, object>("NetworkId", networkId),
                }
            );
        }
    }
}
