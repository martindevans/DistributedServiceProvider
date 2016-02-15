using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace LoggerMessages
{
    [ProtoContract,
    ProtoInclude(2, typeof(DRTConstructed)),
    ProtoInclude(3, typeof(IterativeLookupRequest)),
    ProtoInclude(4, typeof(IterativeLookupStep)),
    ProtoInclude(5, typeof(GeneralMessage)),
    ProtoInclude(7, typeof(BucketState)),
    ProtoInclude(8, typeof(IterativeLookupComplete)),]
    public abstract class BaseMessage
    {
        [ProtoMember(1)]
        private long timestamp;

        public DateTime CreationTime
        {
            get
            {
                return new DateTime(timestamp);
            }
            set
            {
                timestamp = value.Ticks;
            }
        }

        public BaseMessage()
        {

        }
    }
}
