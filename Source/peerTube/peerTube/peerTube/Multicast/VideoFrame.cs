﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace peerTube.Multicast
{
    [ProtoContract]
    public class VideoFrame
    {
        [ProtoMember(1)]
        public byte[] JpegData;
    }
}