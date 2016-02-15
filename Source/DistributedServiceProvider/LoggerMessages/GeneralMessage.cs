using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace LoggerMessages
{
    [ProtoContract]
    public class GeneralMessage
        :BaseMessage
    {
        public const string PIPE_NAME = "General Debug Message";

        [ProtoMember(1)]
        public String Message = "";

        public GeneralMessage(String message)
        {
            this.Message = message;
        }

        public GeneralMessage()
        {

        }

        public override string ToString()
        {
            return Message;
        }
    }
}
