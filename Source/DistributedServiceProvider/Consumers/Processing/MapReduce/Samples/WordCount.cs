using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.Base;
using Consumers.DataStorage;
using DistributedServiceProvider;
using System.Security.Cryptography;
using DistributedServiceProvider.Base.Extensions;

namespace Consumers.Processing.MapReduce.Samples
{
    public class WordCount
        :ReliableMapReduce<Identifier512, string, string, bool, int>
    {
        private List<Identifier512> chunkKeys = new List<Identifier512>();
        private IDataStore store;

        public WordCount(Guid taskId, IDataStore store)
            :base(taskId)
        {
            this.store = store;
        }

        public IDictionary<string, int> Run(string document, int chunkSize)
        {
            List<string> words = new List<string>(document.Split(' '));

            while (words.Count > 0)
            {
                string doc = "";
                for (int i = 0; i < chunkSize && words.Count > 0; i++)
                {
                    doc += (doc.Length == 0 ? "" : " ") + words[0];
                    words.RemoveAt(0);
                }

                Identifier512 id = Identifier512.NewIdentifier();
                chunkKeys.Add(id);
                store.Put(id, Encoding.Unicode.GetBytes(doc));
            }

            return base.RunTask();
        }

        protected override IEnumerable<KeyValuePair<string, bool>> Map(Identifier512 key, string data)
        {
            data = data.ToLower();
            data = data.Replace(".", " ");
            data = data.Replace(",", " ");
            data = data.Replace("\"", " ");
            data = data.Replace("-", " ");

            foreach (var word in data.Split(new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                yield return new KeyValuePair<string, bool>(word, true);
        }

        protected override int Reduce(string key, IEnumerable<bool> dataPoints)
        {
            return dataPoints.Count();
        }

        /// <summary>
        /// Transforms the input key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        protected override Identifier512 TransformInputKey(Identifier512 key)
        {
            return key;
        }

        protected override Identifier512 TransformOutputKey(string key)
        {
            byte[] b = Encoding.Unicode.GetBytes(key);

            MD5 hasher = MD5.Create();
            byte[] md5Bytes = MD5.Create().ComputeHash(b);
            md5Bytes = md5Bytes.Append(MD5.Create().ComputeHash(BitConverter.GetBytes(b.Length))).ToArray();
            Array.Reverse(b);
            md5Bytes = md5Bytes.Append(MD5.Create().ComputeHash(b)).ToArray();
            md5Bytes = md5Bytes.Append(MD5.Create().ComputeHash(b, 0, b.Length / 2)).ToArray();

            if (md5Bytes.Length * 8 != 512)
                throw new Exception("Length of array should be 512 bits");

            return new Identifier512(md5Bytes);
        }

        protected override string FetchData(Identifier512 key)
        {
            byte[] b = store.Get(key, 1000);
            return Encoding.Unicode.GetString(b);
        }

        protected override IEnumerable<Identifier512> GenerateKeys()
        {
            return chunkKeys;
        }
    }
}
