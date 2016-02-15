using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ConcurrentCollections.Concurrent
{
    public class SinglyLinkedList<T> where T : IEquatable<T>
    {
        SinglyLinkedNode<T> first;

        public void AddFirst(T data)
        {
            SinglyLinkedNode<T> node = new SinglyLinkedNode<T>() { Value = data };
            do
            {
                node.Next = first;
            }
            while (Interlocked.CompareExchange<SinglyLinkedNode<T>>(ref first, node, node.Next) != node.Next);
        }

        public T RemoveFirst()
        {
            return Interlocked.Exchange<SinglyLinkedNode<T>>(ref first, first.Next).Value;
        }

        public bool Contains(T data)
        {
            SinglyLinkedNode<T> node = first;
            while (node != null)
            {
                if (node.Value.Equals(data))
                    return true;
                node = node.Next;
            }
            return false;
        }

        private int CopyTo(T[] arr, int start, int end)
        {
            int count = 0;
            SinglyLinkedNode<T> n = first;
            for (int i = start; i < end && n != null; i++)
            {
                arr[i] = n.Value;
                n = n.Next;
                count++;
            }
            return count;
        }
    }
}
