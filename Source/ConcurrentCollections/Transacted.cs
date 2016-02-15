using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ConcurrentCollections
{
    /// <summary>
    /// A set of methods which run in interlocked transactions
    /// All methods are threadsafe and reentrant
    /// </summary>
    public static class Transacted
    {
        #region general transactions
        /// <summary>
        /// For a given Value and set of parameters, calculates a new value
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <param name="value">the current value</param>
        /// <param name="args">arguments for the calculation</param>
        /// <param name="exchange">If true, the transaction will take place, if false the transaction will terminate</param>
        /// <returns>a new value to attempt to put into the value field</returns>
        public delegate T GetValue<T>(T value, T[] args, out bool exchange);

        /// <summary>
        /// Attempts to transact a value in for another value. Calls the calculate method possibly multiple times.
        /// </summary>
        /// <typeparam name="T">the type of the value and all arguments</typeparam>
        /// <param name="calculate">a method which calculates a new value to transact in</param>
        /// <param name="value">a field to put a new value into</param>
        /// <param name="args">a set of method arguments</param>
        /// <returns>the original value</returns>
        public static T Transaction<T>(GetValue<T> calculate, ref T value, params T[] args) where T : class
        {
            T ret = value;
            T oldValue;
            T newValue;
            bool exchange = false;
            do
            {
                oldValue = value;
                newValue = calculate(value, args, out exchange);
                if (!exchange)
                    break;
            } while ((ret = Interlocked.CompareExchange<T>(ref value, newValue, oldValue)) != oldValue);

            if (exchange)
                Thread.MemoryBarrier();

            return ret;
        }

        /// <summary>
        /// Transactions the specified calculate.
        /// </summary>
        /// <param name="calculate">Calculates a new value</param>
        /// <param name="value">The value.</param>
        /// <param name="args">Some arguments</param>
        /// <returns>the original value</returns>
        public static double Transaction(GetValue<double> calculate, ref double value, params double[] args)
        {
            double ret = value;
            double oldValue;
            double newValue;
            bool exchange = false;
            do
            {
                oldValue = value;
                newValue = calculate(value, args, out exchange);
                if (!exchange)
                    break;
            } while ((ret = Interlocked.CompareExchange(ref value, newValue, oldValue)) != oldValue);

            if (exchange)
                Thread.MemoryBarrier();

            return ret;
        }

        /// <summary>
        /// Transactions the specified calculate.
        /// </summary>
        /// <param name="calculate">Calculates a new value</param>
        /// <param name="value">The value.</param>
        /// <param name="args">Some arguments</param>
        /// <returns>the original value</returns>
        public static float Transaction(GetValue<float> calculate, ref float value, params float[] args)
        {
            float ret = value;
            float oldValue;
            float newValue;
            bool exchange = false;
            do
            {
                oldValue = value;
                newValue = calculate(value, args, out exchange);
                if (!exchange)
                    break;
            } while ((ret = Interlocked.CompareExchange(ref value, newValue, oldValue)) != oldValue);

            if (exchange)
                Thread.MemoryBarrier();

            return ret;
        }

        /// <summary>
        /// Transactions the specified calculate.
        /// </summary>
        /// <param name="calculate">Calculates a new value</param>
        /// <param name="value">The value.</param>
        /// <param name="args">Some arguments</param>
        /// <returns>the original value</returns>
        public static int Transaction(GetValue<int> calculate, ref int value, params int[] args)
        {
            int ret = value;
            int oldValue;
            int newValue;
            bool exchange = false;
            do
            {
                oldValue = value;
                newValue = calculate(value, args, out exchange);
                if (!exchange)
                    break;
            } while ((ret = Interlocked.CompareExchange(ref value, newValue, oldValue)) != oldValue);

            if (exchange)
                Thread.MemoryBarrier();

            return ret;
        }

        /// <summary>
        /// Transactions the specified calculate.
        /// </summary>
        /// <param name="calculate">Calculates a new value</param>
        /// <param name="value">The value.</param>
        /// <param name="args">Some arguments</param>
        /// <returns>the original value</returns>
        public static long Transaction(GetValue<long> calculate, ref long value, params long[] args)
        {
            long ret = value;
            long oldValue;
            long newValue;
            bool exchange = false;
            do
            {
                oldValue = value;
                newValue = calculate(value, args, out exchange);
                if (!exchange)
                    break;
            } while ((ret = Interlocked.CompareExchange(ref value, newValue, oldValue)) != oldValue);

            if (exchange)
                Thread.MemoryBarrier();

            return ret;
        }
        #endregion

        #region predicate transactions
        /// <summary>
        /// Given a pair of values, decides if newValue needs to be transacted into value
        /// </summary>
        /// <typeparam name="T">the type</typeparam>
        /// <param name="value">the current value</param>
        /// <param name="newValue">the new value which is a condidate for transacting into value</param>
        /// <returns>True, if a transaction needs to take place, otherwise false</returns>
        public delegate bool Exchange<T>(T value, T newValue);

        /// <summary>
        /// Transacts a value into a given field
        /// </summary>
        /// <typeparam name="T">the type</typeparam>
        /// <param name="exchange">a method which decides if the transaction needs to go ahead, or terminate</param>
        /// <param name="value">the field to transact a new value into</param>
        /// <param name="newValue">the new value</param>
        /// <returns>the original value</returns>
        public static T Transaction<T>(Exchange<T> exchange, ref T value, T newValue) where T : class
        {
            T ret = value;
            T oldValue;
            bool ex = false;
            do
            {
                oldValue = value;
                if (!exchange(value, newValue))
                    break;
            } while ((ret = Interlocked.CompareExchange<T>(ref value, newValue, oldValue)) != oldValue);

            if (ex)
                Thread.MemoryBarrier();

            return ret;
        }

        /// <summary>
        /// Transacts a value into a given field
        /// </summary>
        /// <param name="exchange">a method which decides if the transaction needs to go ahead, or terminate</param>
        /// <param name="value">the field to transact a new value into</param>
        /// <param name="newValue">the new value</param>
        /// <returns>the original value</returns>
        public static double Transaction(Exchange<double> exchange, ref double value, double newValue)
        {
            double ret = value;
            double oldValue;
            bool ex = false;
            do
            {
                oldValue = value;
                if (!exchange(value, newValue))
                    break;
            } while ((ret = Interlocked.CompareExchange(ref value, newValue, oldValue)) != oldValue);

            if (ex)
                Thread.MemoryBarrier();

            return ret;
        }

        /// <summary>
        /// Transacts a value into a given field
        /// </summary>
        /// <param name="exchange">a method which decides if the transaction needs to go ahead, or terminate</param>
        /// <param name="value">the field to transact a new value into</param>
        /// <param name="newValue">the new value</param>
        /// <returns>the original value</returns>
        public static float Transaction(Exchange<float> exchange, ref float value, float newValue)
        {
            float ret = value;
            float oldValue;
            bool ex = false;
            do
            {
                oldValue = value;
                if (!exchange(value, newValue))
                    break;
            } while ((ret = Interlocked.CompareExchange(ref value, newValue, oldValue)) != oldValue);

            if (ex)
                Thread.MemoryBarrier();

            return ret;
        }

        /// <summary>
        /// Transacts a value into a given field
        /// </summary>
        /// <param name="exchange">a method which decides if the transaction needs to go ahead, or terminate</param>
        /// <param name="value">the field to transact a new value into</param>
        /// <param name="newValue">the new value</param>
        /// <returns>the original value</returns>
        public static int Transaction(Exchange<int> exchange, ref int value, int newValue)
        {
            int ret = value;
            int oldValue;
            bool ex = false;
            do
            {
                oldValue = value;
                if (!exchange(value, newValue))
                    break;
            } while ((ret = Interlocked.CompareExchange(ref value, newValue, oldValue)) != oldValue);

            if (ex)
                Thread.MemoryBarrier();

            return ret;
        }

        /// <summary>
        /// Transacts a value into a given field
        /// </summary>
        /// <param name="exchange">a method which decides if the transaction needs to go ahead, or terminate</param>
        /// <param name="value">the field to transact a new value into</param>
        /// <param name="newValue">the new value</param>
        /// <returns>the original value</returns>
        public static long Transaction(Exchange<long> exchange, ref long value, long newValue)
        {
            long ret = value;
            long oldValue;
            bool ex = false;
            do
            {
                oldValue = value;
                if (!exchange(value, newValue))
                    break;
            } while ((ret = Interlocked.CompareExchange(ref value, newValue, oldValue)) != oldValue);

            if (ex)
                Thread.MemoryBarrier();

            return ret;
        }
        #endregion

        #region test
#if DEBUG
        public static void Test(int testSize)
        {
            {
                Console.WriteLine("Testing transactions using a custom atomic increment method");
                ValueType v = 1;
                Transaction<ValueType>(Increment, ref v);
                if ((int)v != 2)
                    throw new Exception("Incorrect");
                DateTime start = DateTime.Now;
                for (int i = 0; i < testSize; i++)
                {
                    Transaction<ValueType>(Increment, ref v);
                }
                Console.WriteLine("Time per transaction = " + (DateTime.Now - start).TotalMilliseconds / ((float)testSize));
                if ((int)v != testSize + 2)
                    throw new Exception("Incorrect");
                Console.WriteLine("Success!");
            }

            {
                Console.WriteLine("Testing transactions using a custom atomic increment method");
                int v = 1;
                Transaction(Increment, ref v);
                if ((int)v != 2)
                    throw new Exception("Incorrect");
                for (int i = 0; i < testSize; i++)
                {
                    Transaction(Increment, ref v);
                }
                if ((int)v != testSize + 2)
                    throw new Exception("Incorrect");
                Console.WriteLine("Success!");
            }

            {
                Console.WriteLine("Testing predicate transactions");
                int v = 1;
                Transaction(delegate { return true; }, ref v, 2);
                if (v != 2)
                    throw new Exception("Transaction failed!");
                Transaction(delegate { return false; }, ref v, 1);
                if (v != 2)
                    throw new Exception("Transaction did not terminate!");
            }
        }

        private static ValueType Increment(ValueType v, ValueType[] args, out bool exchange)
        {
            exchange = true;
            return (ValueType)(((int)v) + 1);
        }

        private static int Increment(int v, int[] args, out bool exchange)
        {
            exchange = true;
            return v + 1;
        }
#endif
        #endregion
    }
}
