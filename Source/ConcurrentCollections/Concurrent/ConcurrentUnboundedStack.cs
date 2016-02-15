using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ConcurrentCollections.Concurrent
{
    /// <summary>
    /// A threadsafe stack of unlimited size. Pushing onto the stack will allocate, so this may not be suitable for some uses
    /// </summary>
    public class ConcurrentUnboundedStack<T>
    {
        SinglyLinkedNode<T> top;

        public void Push(T value)
        {
            SinglyLinkedNode<T> next = new SinglyLinkedNode<T>() { Value = value };
            do
            {
                next.Next = top;
            }
            while (Interlocked.CompareExchange<SinglyLinkedNode<T>>(ref top, next, next.Next) != next.Next);
        }

        public T Pop()
        {
            if (top == null)
                return default(T);
            return Interlocked.Exchange<SinglyLinkedNode<T>>(ref top, top.Next).Value;
        }

        public bool Pop(out T result)
        {
            if (top == null)
            {
                result = default(T);
                return false;
            }

            result = Interlocked.Exchange<SinglyLinkedNode<T>>(ref top, top.Next).Value;
            return true;
        }

        #region test
#if DEBUG
        public static void Test(int size)
        {
            ConcurrentUnboundedStack<int> mystack = new ConcurrentUnboundedStack<int>();

            Console.WriteLine("Testing stack");

            mystack.Push(1);

            if (mystack.Pop() != 1)
                throw new Exception("Incorrect value popped");

            for (int i = 0; i < size; i++)
            {
                mystack.Push(i);
            }
            for (int i = size - 1; i >= 0; i--)
            {
                if (mystack.Pop() != i)
                    throw new Exception("Incorrect value popped");
            }

            Console.WriteLine("Success");
        }
#endif
        #endregion
    }
}
