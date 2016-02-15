using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.MessageConsumers;
using DistributedServiceProvider.Contacts;
using ProtoBuf;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using DistributedServiceProvider.Base;
using HandyCollections;

namespace peerTube.Multicast
{
    /// <summary>
    /// A single peer which is watching the multicast for this network. Every node in this network is watching this video
    /// </summary>
    public class BroadcastPeer
        : MultiMessageConsumer
    {
        #region fields
        [LinkedConsumer(GetClosestNodes.GUID_STRING)]
        public GetClosestNodes nodeFinder = null;

        [LinkedConsumer(Callback.GUID_STRING)]
        public Callback callback = null;

        public TimeSpan ChildrenTimeout = TimeSpan.FromSeconds(20);

        public readonly Identifier512 RootNode;
        public readonly bool Root;

        private HashSet<Contact> parents = new HashSet<Contact>();
        public int ParentCount
        {
            get
            {
                lock (parents)
                {
                    return parents.Count;
                }
            }
        }

        private ConcurrentDictionary<Contact, DateTime> children = new ConcurrentDictionary<Contact, DateTime>();
        public int ChildrenCount
        {
            get
            {
                return children.Count;
            }
        }

        public ConcurrentQueue<VideoFrame> VideoFrames = new ConcurrentQueue<VideoFrame>();

        public static readonly Guid GUID = Guid.Parse("26967969-4735-40dd-8846-12bb594f8cc4");
        #endregion

        #region constructors
        public BroadcastPeer(Identifier512 rootNode, bool root)
            : base(GUID)
        {
            Root = root;
            RootNode = rootNode;

            BindProcessors(new Dictionary<byte, Action<Contact, byte[]>>()
            {
                { (byte)PacketFlag.VideoFrame, VideoFrameDataProcessor },
                { (byte)PacketFlag.ParentRequest, ParentRequestProcessor },
                { (byte)PacketFlag.Pong, PongRequestProcessor },
            });
        }
        #endregion

        /// <summary>
        /// Establish another connection to receive multicast data from
        /// </summary>
        public bool Connect(int timeout)
        {
            if (Root)
                throw new InvalidOperationException("Root of broadcast tree; cannot connect to a higher node");

            bool success = false;

            //move through nodes, attempting to connect to every single one, terminate once you're connected to a single one
            var r = nodeFinder.GetClosestContacts(RootNode, c =>
            {
                success |= ConnectToNodeAsParent(c, timeout);
                return success;
            });

            return success;
        }

        private bool ConnectToNodeAsParent(Contact node, int timeout)
        {
            bool isAlreadyChild = children.ContainsKey(node);

            bool isAlreadyParent;
            lock (parents)
                isAlreadyParent = parents.Contains(node);

            if (isAlreadyParent || isAlreadyChild)
                return false;

            Callback.WaitToken token = callback.AllocateToken();
            try
            {
                using (MemoryStream m = new MemoryStream())
                {
                    Serializer.SerializeWithLengthPrefix<ParentageRequest>(m, new ParentageRequest()
                    {
                        CallbackId = token.Id,
                    }, PrefixStyle.Base128);

                    base.Send(node, ConsumerId, (byte)PacketFlag.ParentRequest, m.ToArray());
                }

                if (!token.Wait(timeout))
                    return false;

                if (token.Response[0] == 0)
                    return false;

                //add candidate to set of parents, as this request has been successful
                lock (parents)
                {
                    parents.Add(node);
                }

                return true;
            }
            finally
            {
                callback.FreeToken(token);
            }
        }

        #region video
        public void SendVideoData(byte[] data)
        {
            SendData(data, PacketFlag.VideoFrame);
        }

        private void VideoFrameDataProcessor(Contact c, byte[] data)
        {
            new LoggerMessages.GeneralMessage("Received a frame " + DateTime.Now.Second + " " + DateTime.Now.Millisecond).Send();

            VideoFrame frame = Serializer.DeserializeWithLengthPrefix<VideoFrame>(new MemoryStream(data), PrefixStyle.Base128);

            VideoFrames.Enqueue(frame);

            ThreadPool.QueueUserWorkItem(_ => base.Send(c, ConsumerId, (byte)PacketFlag.Pong, new byte[] { 1 }));

            if (ChildrenCount > 0)
            {
                ThreadPool.QueueUserWorkItem(_ => SendVideoData(data));
                new LoggerMessages.GeneralMessage("Sending a message to a child " + DateTime.Now.Second + " " + DateTime.Now.Millisecond).Send();
            }
        }
        #endregion

        private void SendData(byte[] data, PacketFlag flag)
        {
            HashSet<Contact> childrenCopy = new HashSet<Contact>();
            HashSet<Contact> timedOut = new HashSet<Contact>();

            foreach (var item in children)
                if (item.Value + ChildrenTimeout > DateTime.Now)
                    childrenCopy.Add(item.Key);
                else
                    timedOut.Add(item.Key);

            DateTime d;
            foreach (var child in timedOut)
                children.TryRemove(child, out d);

            foreach (var child in childrenCopy.Where(a => a != null).Where(a => !a.Equals(RoutingTable.LocalContact)))
                Send(child, ConsumerId, (byte)flag, data);
        }

        private void ParentRequestProcessor(Contact c, byte[] data)
        {
            using (MemoryStream m = new MemoryStream(data))
            {
                var request = Serializer.DeserializeWithLengthPrefix<ParentageRequest>(m, PrefixStyle.Base128);

                if ((ChildrenCount < 2 || children.ContainsKey(c)) && (ParentCount > 0 || Root))
                {
                    callback.SendResponse(RoutingTable.LocalContact, c, request.CallbackId, new byte[] { 1 });

                    children[c] = DateTime.Now;
                }
                else
                {
                    callback.SendResponse(RoutingTable.LocalContact, c, request.CallbackId, new byte[] { 0 });
                }
            }
        }

        private void PongRequestProcessor(Contact c, byte[] data)
        {
            DateTime d;
            if (children.TryGetValue(c, out d))
                children.TryUpdate(c, DateTime.Now, d);
        }

        private enum PacketFlag
            :byte
        {
            VideoFrame = 0,
            ParentRequest = 1,
            AudioFrame = 2,
            Pong = 3,
        }

        [ProtoContract]
        private class ParentageRequest
        {
            [ProtoMember(1)]
            public long CallbackId;
        }

    }
}
