using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using DistributedServiceProvider.Base;

namespace LoggerMessages
{
    [ProtoContract]
    public class BucketState
        :BaseMessage
    {
        public const string PIPE_NAME = "Bucket State";

        [ProtoMember(1)]
        public Identifier512[] Identifiers;

        [ProtoMember(2)]
        public ushort Index;

        [ProtoMember(3)]
        public Identifier512 LocalId;

        public override string ToString()
        {
            return MessageStringBuilder.BuildMessageString(PIPE_NAME,
            new[]
            {
                new KeyValuePair<string, object>("index", Index),
                new KeyValuePair<string, object>("Idenfifier_Count", Identifiers == null ? 0 : Identifiers.Length),
            });
        }
    }
}
