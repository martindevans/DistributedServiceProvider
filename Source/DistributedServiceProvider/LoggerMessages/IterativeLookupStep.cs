using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.Base;
using ProtoBuf;

namespace LoggerMessages
{
    [ProtoContract]
    public class IterativeLookupStep
        :BaseMessage
    {
        public const string PIPE_NAME = "Iterative lookup request step";

        [ProtoMember(1)]
        public List<Identifier512> Heap;

        [ProtoMember(2)]
        public List<Identifier512> Contacted;

        [ProtoMember(3)]
        private byte[] guidBytes;

        public Guid LookupId
        {
            get
            {
                return new Guid(guidBytes);
            }
            set
            {
                guidBytes = value.ToByteArray();
            }
        }

        public IterativeLookupStep(Guid lookupId, IEnumerable<Identifier512> heap, IEnumerable<Identifier512> contacted)
        {
            Heap = new List<Identifier512>(heap);
            Contacted = new List<Identifier512>(contacted);
            LookupId = lookupId;
        }

        public IterativeLookupStep()
        {
            Heap = new List<Identifier512>();
            Contacted = new List<Identifier512>();
        }

        public override string ToString()
        {
            return MessageStringBuilder.BuildMessageString("IterativeLookupStep", new KeyValuePair<string, object>[]
                {
                    new KeyValuePair<string, object>("LookupId", LookupId),
                    new KeyValuePair<string, object>("Heap", Heap.ToJsonString()),
                    new KeyValuePair<string, object>("Contacted", Contacted.ToJsonString()),
                });
        }
    }
}
