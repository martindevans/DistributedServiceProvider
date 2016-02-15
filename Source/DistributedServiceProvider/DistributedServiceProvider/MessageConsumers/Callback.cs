using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.Base;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using ProtoBuf;
using System.IO;
using DistributedServiceProvider.Contacts;

namespace DistributedServiceProvider.MessageConsumers
{
    /// <summary>
    /// Manages collecting responses to messages
    /// </summary>
    public class Callback
        :MessageConsumer
    {
        public const string GUID_STRING = "96de743d-8ec0-451e-a2a3-bb915af1095e";
        public static readonly Guid CONSUMER_ID = new Guid(GUID_STRING);

        public object TokenCount
        {
            get
            {
                return tokens.Count;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Callback"/> class.
        /// </summary>
        /// <param name="networkId">The network id.</param>
        public Callback()
            :base(CONSUMER_ID)
        {
            Serializer.PrepareSerializer<Response>();
        }

        /// <summary>
        /// Deliver a message encoded into the given byte array
        /// </summary>
        /// <param name="source">The source of the message</param>
        /// <param name="message">The message.</param>
        public override void Deliver(Contact source, byte[] message)
        {
            using (MemoryStream m = new MemoryStream(message))
            {
                Response r = Serializer.DeserializeWithLengthPrefix<Response>(m, PrefixStyle.Base128);

                WaitToken token;
                if (tokens.TryRemove(r.CallbackId, out token))
                {
                    token.Source = source;
                    token.Response = r.ResponseBytes;
                }
                else if (r.CallbackId > nextId)
                {
                    throw new Exception("Token not found");
                }
            }
        }

        /// <summary>
        /// Sends the response back to the remote end
        /// </summary>
        /// <param name="local">The local contact</param>
        /// <param name="target">The target.</param>
        /// <param name="callbackId">The callback id.</param>
        /// <param name="responseBytes">The response bytes.</param>
        public void SendResponse(Contact local, Contact target, long callbackId, byte[] responseBytes)
        {
            using (MemoryStream m = new MemoryStream())
            {
                Serializer.SerializeWithLengthPrefix<Response>(m, new Response(callbackId, responseBytes), PrefixStyle.Base128);
                target.Send(local, ConsumerId, m.ToArray());
            }
        }

        [ProtoContract]
        private class Response
        {
            [ProtoMember(1)]
            public long CallbackId;

            [ProtoMember(2)]
            public byte[] ResponseBytes;

            public Response(long id, byte[] response)
            {
                CallbackId = id;
                ResponseBytes = response;
            }

            public Response() { }
        }

        #region token management
        private ConcurrentDictionary<long, WaitToken> tokens = new ConcurrentDictionary<long, WaitToken>();

        private long nextId = long.MinValue;
        /// <summary>
        /// Allocates a new wait token
        /// </summary>
        /// <returns></returns>
        public WaitToken AllocateToken()
        {
            var token = new WaitToken(Interlocked.Increment(ref nextId));

            return tokens.AddOrUpdate(token.Id, token, (a, b) => { throw new Exception("Key should not already be present"); });
        }

        /// <summary>
        /// Frees the given token
        /// </summary>
        /// <param name="token">The token.</param>
        public void FreeToken(WaitToken token)
        {
            WaitToken t;
            bool r = tokens.TryRemove(token.Id, out t);
        }

        /// <summary>
        /// A token which enables waiting for a response
        /// </summary>
        public class WaitToken
        {
            public readonly long Id;
            private ManualResetEvent waitHandle;

            private ReaderWriterLockSlim responseLock = new ReaderWriterLockSlim();
            private bool responseSet = false;
            private byte[] response = null;

            /// <summary>
            /// Gets or sets the source of the reply to this token
            /// </summary>
            /// <value>The source.</value>
            public Contact Source
            {
                get;
                internal set;
            }

            /// <summary>
            /// Gets or sets the response.
            /// </summary>
            /// <value>The response.</value>
            /// <exception cref="InvalidOperationException">Thrown if the response has not arrived yet (ie, you should have called Wait() first)</exception>
            public byte[] Response
            {
                get
                {
                    try
                    {
                        responseLock.EnterReadLock();

                        if (responseSet)
                            return response;
                        else
                            throw new InvalidOperationException("Cannot get response before it has arrived");
                    }
                    finally
                    {
                        responseLock.ExitReadLock();
                    }
                }
                set
                {
                    try
                    {
                        responseLock.EnterWriteLock();

                        responseSet = true;
                        response = value;
                        waitHandle.Set();
                    }
                    finally
                    {
                        responseLock.ExitWriteLock();
                    }
                }
            }

            internal WaitToken(long id)
            {
                this.Id = id;
                waitHandle = new ManualResetEvent(false);
            }

            /// <summary>
            /// Waits for a response to arrive
            /// </summary>
            /// <param name="millisecondsTimeout">The milliseconds timeout.</param>
            /// <returns>true, if a response arrived, or false if it timed out</returns>
            public bool Wait(int millisecondsTimeout)
            {
                return waitHandle.WaitOne(millisecondsTimeout);
            }

            public override string ToString()
            {
                return "{ " + Id + " " + responseSet + " }";
            }
        }
        #endregion
    }
}
