using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.Base.Extensions;
using System.Collections;
using ProtoBuf;
using System.Diagnostics;
using System.Security.Cryptography;

namespace DistributedServiceProvider.Base
{
    /// <summary>
    /// An identifier for a node
    /// </summary>
    [ProtoContract]
    public class Identifier512
        :ICloneable, IComparable<Identifier512>
    {
        /// <summary>
        /// The maximum length of an identifier in bits
        /// </summary>
        public const int BIT_LENGTH = 512;

        [ProtoMember(1)]
        private readonly byte[] bytes;

        private int hashCode;

        #region constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Identifier512"/> class.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <exception cref="ArgumentException">Thrown if there are too many bits</exception>
        public Identifier512(IEnumerable<byte> bytes)
        {
            int lengthDifference = BIT_LENGTH / 8 - bytes.Count();

            if (lengthDifference < 0)
                throw new ArgumentException(String.Format("Cannot initialise with more than {0} bits of data", BIT_LENGTH));

            while (lengthDifference > 0)
            {
                bytes = bytes.Append<byte>(0);
                lengthDifference--;
            }

            this.bytes = bytes.ToArray();
            hashCode = CalculateHashCode();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Identifier512"/> class.
        /// </summary>
        /// <param name="ints">The ints.</param>
        public Identifier512(int[] ints)
            : this(ints.SelectMany(a => BitConverter.GetBytes(a)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Identifier512"/> class.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <param name="e"></param>
        /// <param name="f"></param>
        /// <param name="g"></param>
        /// <param name="h"></param>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="k"></param>
        /// <param name="l"></param>
        /// <param name="m"></param>
        /// <param name="n"></param>
        /// <param name="o"></param>
        /// <param name="p"></param>
        public Identifier512(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j, int k, int l, int m, int n, int o, int p)
            : this(new int[] { a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Identifier512"/> class.
        /// </summary>
        /// <param name="a">A guid representing the first 128 bits</param>
        /// <param name="b">A guid representing the second 128 bits</param>
        /// <param name="c">A guid representing the third 128 bits</param>
        /// <param name="d">A guid representing the fourth 128 bits</param>
        public Identifier512(Guid a, Guid b, Guid c, Guid d)
            :this(a.ToByteArray().Append(b.ToByteArray()).Append(c.ToByteArray()).Append(d.ToByteArray()))
        {
        }

        /// <summary>
        /// Here for protobuf.net
        /// </summary>
        private Identifier512()
        {
        }

        [ProtoAfterDeserialization]
        private void PostDeserealise()
        {
            hashCode = CalculateHashCode();
        }
        #endregion

        #region distance calculation
        public static Identifier512 SelectClosest(Identifier512 a, Identifier512 b, Identifier512 target)
        {
            var a2Target = Distance(a, target);
            var b2Target = Distance(a, target);

            if (a2Target < b2Target)
                return a;
            else
                return b;
        }

        public static Identifier512 Distance(Identifier512 a, Identifier512 b)
        {
            return a ^ b;
        }
        #endregion

        #region prefix calculation
        /// <summary>
        /// Calculates the distance metric between the two identifiers
        /// </summary>
        /// <param name="a">An identifier</param>
        /// <param name="b">An identifier</param>
        /// <returns></returns>
        public static int CommonPrefixLength(Identifier512 a, Identifier512 b)
        {
            int distance = 0;

            //look through to find the byte which is different
            for (int i = 0; i < BIT_LENGTH / 8; i++)
            {
                if (a.bytes[i] == b.bytes[i])
                {
                    distance += 8;
                }
                else
                {
                    distance += LookupCommonPrefixLength(a.bytes[i], b.bytes[i]);
                    break;
                }
            }

            return distance;
        }

        private static int LookupCommonPrefixLength(byte a, byte b)
        {
            return prefixLengthLookup[a ^ b];
        }

        /// <summary>
        /// The length of the prefix of the two bytes which are xored together to form the index
        /// http://stackoverflow.com/questions/3313909/finding-the-length-of-the-common-prefix-in-two-bytes
        /// </summary>
        private readonly static byte[] prefixLengthLookup = new byte[]
        {
            8, 7, 6, 6, 5, 5, 5, 5, 4, 4, 4, 4, 4, 4, 4, 4,
            3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        };
        #endregion

        /// <summary>
        /// Gets a key which is the hash of this one
        /// </summary>
        /// <returns></returns>
        public Identifier512 GetHashedKey()
        {
            Identifier512 unhashedKey1 = this + 1;
            Identifier512 unhashedKey2 = unhashedKey1 + 1;
            Identifier512 unhashedKey3 = unhashedKey2 + 1;
            SHA512 hasher = SHA512.Create();
            byte[] b =
                hasher.ComputeHash(this.GetBytes().ToArray())
                .Append(hasher.ComputeHash(unhashedKey1.GetBytes().ToArray()))
                .Append(hasher.ComputeHash(unhashedKey2.GetBytes().ToArray()))
                .Append(hasher.ComputeHash(unhashedKey3.GetBytes().ToArray())).ToArray();

            if (b.Length * 8 < BIT_LENGTH)
                throw new Exception("Length of array should be " + BIT_LENGTH  + " bits or more");

            return new Identifier512(b.Take(BIT_LENGTH / 8));
        }

        private static readonly Random random = new Random();
        /// <summary>
        /// Create a new, random, identifier
        /// </summary>
        /// <returns></returns>
        public static Identifier512 NewIdentifier()
        {
            lock (random)
            {
                byte[] b = new byte[BIT_LENGTH / 8];
                random.NextBytes(b);
                return new Identifier512(b);
            }
        }

        /// <summary>
        /// Gets the bytes which make up this identifier
        /// </summary>
        /// <returns></returns>
        public IEnumerable<byte> GetBytes()
        {
            return bytes;
        }

        /// <summary>
        /// Gets the ints which make up this identifier
        /// </summary>
        /// <returns></returns>
        public IEnumerable<int> GetInts()
        {
            Debug.Assert(bytes.Length % 4 == 0);

            int[] ints = new int[bytes.Length / 4];
            for (int i = 0; i < bytes.Length / 4; i++)
                ints[i] = BitConverter.ToInt32(bytes, i * 4);

            return ints;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return new Guid(bytes.Skip(0).Take(16).ToArray()) + "/"
                + new Guid(bytes.Skip(16).Take(16).ToArray()) + "/"
                + new Guid(bytes.Skip(32).Take(16).ToArray()) + "/"
                + new Guid(bytes.Skip(48).Take(16).ToArray());
        }

        public override bool Equals(object obj)
        {
            Identifier512 id = obj as Identifier512;
            if (id == null)
                return base.Equals(obj);
            else
                return Equals(id);
        }

        public bool Equals(Identifier512 other)
        {
            if (other == null)
                return false;

            if (bytes.Length != other.bytes.Length)
                return false;

            for (int i = 0; i < bytes.Length; i++)
                if (bytes[i] != other.bytes[i])
                    return false;

            return true;
        }

        private int CalculateHashCode()
        {
            //Algorithm from StackOverflow answer: http://stackoverflow.com/questions/16340/how-do-i-generate-a-hashcode-from-a-byte-array-in-c/425184#425184
            unchecked
            {
                var result = 0;
                foreach (byte b in bytes)
                    result = (result * 31) ^ b;
                return result;
            }
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        #region operators
        public static bool operator ==(Identifier512 a, Identifier512 b)
        {
            // If both are null, or both are same instance, return true.
            if (Object.ReferenceEquals(a, b))
                return true;

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
                return false;

            return a.Equals(b);
        }

        public static bool operator !=(Identifier512 a, Identifier512 b)
        {
            return !(a == b);
        }

        public static Identifier512 operator ^(Identifier512 a, Identifier512 b)
        {
            return new Identifier512(a.GetBytes().Zip(b.GetBytes(), (x, y) => (byte)(x ^ y)));
        }

        public static bool operator <(Identifier512 a, Identifier512 b)
        {
            if (a.bytes.Length != b.bytes.Length)
                throw new NotImplementedException("Handle this case where the identifiers use different bit lengths (if required)");

            for (int i = 0; i < BIT_LENGTH / 8; i++)
            {
                if (a.bytes[i] < b.bytes[i])
                    return true;
                else if (a.bytes[i] > b.bytes[i])
                    return false;
            }
            return false;
        }

        public static bool operator >(Identifier512 a, Identifier512 b)
        {
            if (a.bytes.Length != b.bytes.Length)
                throw new NotImplementedException("Handle this case where the identifiers use different bit lengths (if required)");

            for (int i = 0; i < BIT_LENGTH / 8; i++)
            {
                if (a.bytes[i] > b.bytes[i])
                    return true;
                else if (a.bytes[i] < b.bytes[i])
                    return false;
            }
            return false;
        }

        public static bool operator <=(Identifier512 a, Identifier512 b)
        {
            if (a == b)
                return true;

            if (a.bytes.Length != b.bytes.Length)
                throw new NotImplementedException("Handle this case where the identifiers use different bit lengths (if required)");

            for (int i = 0; i < BIT_LENGTH / 8; i++)
            {
                if (a.bytes[i] < b.bytes[i])
                    return true;
                else if (a.bytes[i] > b.bytes[i])
                    return false;
            }
            return false;
        }

        public static bool operator >=(Identifier512 a, Identifier512 b)
        {
            if (a == b)
                return true;

            if (a.bytes.Length != b.bytes.Length)
                throw new NotImplementedException("Handle this case where the identifiers use different bit lengths (if required)");

            for (int i = 0; i < BIT_LENGTH / 8; i++)
            {
                if (a.bytes[i] > b.bytes[i])
                    return true;
                else if (a.bytes[i] < b.bytes[i])
                    return false;
            }
            return false;
        }

        public static Identifier512 operator+(Identifier512 a, int value)
        {
            byte[] b = new byte[a.bytes.Length];
            a.bytes.CopyTo(b, 0);

            for (int i = b.Length - 1; i >= 0; i--)
            {
                int v = b[i] + value;
                if (v > byte.MaxValue)
                {
                    b[i] = (byte)(value % byte.MaxValue);
                    value -= byte.MaxValue * b[i];
                }
                else
                    b[i] = (byte)v;
            }

            return new Identifier512(b);
        }
        #endregion

        public static Identifier512 CreateKey(long i)
        {
            int a;
            int b;
            unsafe
            {
                int* iPtr = (int*)&i;
                a = iPtr[0];
                b = iPtr[1];
            }

            unchecked
            {
                return new Identifier512(a, b * 2, a * 3, b * 4, a * 5, b * 6, a * 7, b * 8, a * 9, b * 10, a * 11, b * 12, a * 13, b * 14, a * 15, b * 16);
            }
        }

        public static Identifier512 CreateKey(int i)
        {
            unchecked
            {
                return new Identifier512(i, i * 2, i * 3, i * 3, i * 4, i * 5, i * 6, i * 7, i * 8, i * 9, i * 10, i * 11, i * 12, i * 13, i * 14, i * 15);
            }
        }

        public static Identifier512 CreateKey(string s)
        {
            MD5 hasher = MD5.Create();

            byte[] s1 = hasher.ComputeHash(Encoding.ASCII.GetBytes(s));

            byte[] b = s1.Append(s1).Append(s1).Append(s1).ToArray();

            if (b.Length * 8 != 512)
                throw new Exception("Length of array should be 512 bits");

            return new Identifier512(b).GetHashedKey();
        }

        public object Clone()
        {
            byte[] b = new byte[bytes.Length];
            bytes.CopyTo(b, 0);

            return new Identifier512(b);
        }

        public static Identifier512 Parse(string p)
        {
            if (p.Equals("?"))
                return Identifier512.NewIdentifier();

            var s = p.Split('/');

            return new Identifier512(Guid.Parse(s[0]), Guid.Parse(s[1]), Guid.Parse(s[2]), Guid.Parse(s[3]));
        }

        public int CompareTo(Identifier512 other)
        {
            if (this < other)
                return -1;
            else if (this > other)
                return 1;
            else
                return 0;
        }
    }
}
