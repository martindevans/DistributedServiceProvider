using System;
using System.Collections.Generic;
using System.Text;

namespace ConcurrentCollections.Concurrent
{
    internal class SinglyLinkedNode<T>
    {
        public T Value;

        public SinglyLinkedNode<T> Next;
    }
}
