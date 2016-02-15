using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.Base;
using ProtoBuf;
using System.IO;
using DistributedServiceProvider.Contacts;
using LoggerMessages;

namespace DistributedServiceProvider.MessageConsumers
{
    public class GetClosestNodes
        :MessageConsumer
    {
        private ContactCollection contacts;
        private Callback callback;

        public const string GUID_STRING = "6bf85253-e9ee-4ea2-ae31-1d509978ea36";
        public static readonly Guid GUID = new Guid(GUID_STRING);

        internal GetClosestNodes(ContactCollection contacts, Callback callback)
            : base(GUID)
        {
            this.contacts = contacts;
            this.callback = callback;

            Serializer.PrepareSerializer<LocalContact>();
            Serializer.PrepareSerializer<RequestMessage>();
            Serializer.PrepareSerializer<ResponseMessage>();
        }

        /// <summary>
        /// Gets the closest nodes the remote contact has to a given contact
        /// </summary>
        /// <param name="local">The local contact</param>
        /// <param name="remote">The remote node to ask</param>
        /// <param name="target">The target to search for</param>
        /// <param name="limit">The limit of items to return</param>
        /// <param name="timeout">The maximum time to wait</param>
        /// <returns>a collection of contacts in order of distance from the target, or null if timed out</returns>
        public IEnumerable<Contact> RemoteGetClosest(Contact local, Contact remote, Identifier512 target, int limit, int timeout)
        {
            var token = callback.AllocateToken();
            RequestMessage request = new RequestMessage(token.Id, target, limit);

            using (MemoryStream mStream = new MemoryStream())
            {
                Serializer.SerializeWithLengthPrefix<RequestMessage>(mStream, request, PrefixStyle.Base128);
                remote.Send(local, ConsumerId, mStream.ToArray());
            }

            if (!token.Wait(timeout))
                return NoContacts();

            callback.FreeToken(token);

            return DecodeResponse(token.Response);
        }

        /// <summary>
        /// Finds the closest contact in the network. Does several round trips to the network
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="terminate">An optional function argument which will be passed every node the lookup operation passes, if this function returns true the lookup will terminate at that point</param>
        /// <returns>an IEnumerable&lt;contact&gt; in order of distance from the target. Can be cast into a GetClosestNodes.ClosestResults</returns>
        public IEnumerable<Contact> GetClosestContacts(Identifier512 target, Func<Contact, bool> terminate = null)
        {
            int iterations = 0;
            MinMaxHeap<Contact> heap = new MinMaxHeap<Contact>(new ContactComparer(target));

#if DEBUG
            //Guid lookupId = Guid.NewGuid();
            //new IterativeLookupRequest(lookupId, RoutingTable.LocalIdentifier, RoutingTable.NetworkId, RoutingTable.Configuration, target, RoutingTable.Configuration.LookupConcurrency).Send();
#endif

            try
            {
                if (target == null)
                    throw new NullReferenceException("Target cannot be null");

                heap.AddMany(contacts.ClosestNodes(target).Take(RoutingTable.Configuration.LookupConcurrency));

                bool end = false;
                if (terminate != null)
                {
                    foreach (var peer in heap)
                    {
                        end = terminate(peer);
                        if (end)
                            break;
                    }
                }

                HashSet<Identifier512> contacted = new HashSet<Identifier512>();

#if DEBUG
                //new IterativeLookupStep(lookupId, heap.Select(a => a.Identifier), contacted).Send();
#endif

                if (!end)
                {
                    HashSet<Contact> uniqueDiscoveries;
                    do
                    {
                        iterations++;

                        uniqueDiscoveries = new HashSet<Contact>(                                                       //hashet means we only get each result once
                            heap                                                                                        //from the set of results we know about
                            .AsParallel()
                            .Where(c => !contacted.Contains(c.Identifier))                                              //which we have not already contacted
                            .SelectMany(c => RemoteGetClosest(RoutingTable.LocalContact, c, target, RoutingTable.Configuration.LookupConcurrency, RoutingTable.Configuration.LookupTimeout))                                                                                          //select the closest ones they know about
                            .Where(n => n != null)
                            .Where(r => !heap.Contains(r)));                                                            //remove the results we already know about

                        //Comment
                        //Make the system aware of these potentially new nodes
                        foreach (var c in uniqueDiscoveries)
                            contacts.Update(c);

                        //make sure we never contact these nodes again
                        contacted.UnionWith(heap.Select(a => a.Identifier));

                        //add the new results
                        heap.AddMany(uniqueDiscoveries);

                        //limit the size of the heap
                        while (heap.Count > RoutingTable.Configuration.LookupConcurrency)
                            heap.RemoveMax();

#if DEBUG
                        //new IterativeLookupStep(lookupId, heap.Select(a => a.Identifier), contacted).Send();
#endif

                        if (terminate != null)
                            if (uniqueDiscoveries.Where(a => terminate(a)).FirstOrDefault() != null)
                                break;
                    }
                    while (uniqueDiscoveries.Count != 0 && heap.Minimum.Identifier != target);
                }

                return new ClosestResults(heap, iterations);
            }
            finally
            {
#if DEBUG
                //new IterativeLookupComplete(lookupId, heap.Select(a => a.Identifier), iterations).Send();
#endif
            }
        }

        private IEnumerable<Contact> NoContacts()
        {
            return new List<Contact>();
        }

        public override void Deliver(Contact source, byte[] message)
        {
            RequestMessage m;
            using (MemoryStream mStream = new MemoryStream(message))
                m = Serializer.DeserializeWithLengthPrefix<RequestMessage>(mStream, PrefixStyle.Base128);

            byte[] responseBytes;
            using (MemoryStream mStream = new MemoryStream())
            {
                ResponseMessage response = new ResponseMessage().AddRange(contacts.ClosestNodes(m.Target).Take(m.Limit));
                Serializer.SerializeWithLengthPrefix<ResponseMessage>(mStream, response, PrefixStyle.Base128);

                responseBytes = mStream.ToArray();
            }

            callback.SendResponse(contacts.LocalContact, source, m.CallbackId, responseBytes);
        }

        public class ClosestResults
            :IEnumerable<Contact>
        {
            private MinMaxHeap<Contact> heap;
            public readonly int Iterations;

            internal ClosestResults(MinMaxHeap<Contact> heap, int iterations)
            {
                this.heap = heap;
                Iterations = iterations;
            }

            public IEnumerator<Contact> GetEnumerator()
            {
                while (heap.Count > 0)
                    yield return heap.RemoveMin();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return (this as IEnumerable<Contact>).GetEnumerator();
            }
        }

        /// <summary>
        /// Decodes a response from a remote GetClosestNodes operation
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns></returns>
        private IEnumerable<Contact> DecodeResponse(byte[] response)
        {
            using (MemoryStream mStream = new MemoryStream(response))
            {
                return Serializer.DeserializeWithLengthPrefix<ResponseMessage>(mStream, PrefixStyle.Base128);
            }
        }

        [ProtoContract]
        private class RequestMessage
        {
            [ProtoMember(1)]
            public long CallbackId;

            [ProtoMember(2)]
            public Identifier512 Target;

            [ProtoMember(3)]
            public int Limit;

            public RequestMessage()
            {
                
            }

            public RequestMessage(long callbackId, Identifier512 target, int limit)
            {
                CallbackId = callbackId;
                Target = target;
                Limit = limit;
            }
        }

        [ProtoContract]
        private class ResponseMessage
            :IEnumerable<Contact>
        {
            [ProtoMember(1)]
            private List<Contact> contacts = new List<Contact>();

            public ResponseMessage()
            {
            }

            public ResponseMessage AddRange(IEnumerable<Contact> contacts)
            {
                this.contacts.AddRange(contacts);

                return this;
            }

            public IEnumerator<Contact> GetEnumerator()
            {
                return contacts.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return (this as IEnumerable<Contact>).GetEnumerator();
            }
        }
    }
}
