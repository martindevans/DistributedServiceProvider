using System;
using System.Collections.Generic;
using System.Linq;
using DistributedServiceProvider.Base;

namespace Consumers.Processing.MapReduce.Samples
{
    public class Sort
        : ReliableMapReduce<int, List<int>, int, int, List<int>>
    {
        private int[] data;
        private int chunks;
        private int chunkSize;

        public Sort(Guid taskId, int[] data, int chunks)
            : base(taskId)
        {
            this.data = data;
            this.chunks = chunks;
            this.chunkSize = data.Length / chunks;
        }

        protected override IEnumerable<KeyValuePair<int, int>> Map(int key, List<int> data)
        {
            float reduceSize = 50000000;

            foreach (var item in data)
                yield return new KeyValuePair<int, int>((int)(((int)(item / reduceSize)) * reduceSize), item);
        }

        protected override List<int> Reduce(int key, IEnumerable<int> dataPoints)
        {
            return dataPoints.OrderBy(a => a).ToList();
        }

        protected override IEnumerable<int> GenerateKeys()
        {
            for (int i = 0; i < 100; i++)
                yield return i;
        }

        protected override List<int> FetchData(int key)
        {
            List<int> l = new List<int>();

            int start = key * chunkSize;
            int end = Math.Min(start + chunkSize, data.Length);

            for (int i = start; i < end; i++)
                l.Add(data[i]);

            return l;
        }

        protected override Identifier512 TransformInputKey(int key)
        {
            return Identifier512.CreateKey(key);
        }

        protected override Identifier512 TransformOutputKey(int key)
        {
            return Identifier512.CreateKey(key);
        }

        public IEnumerable<int> Run()
        {
            var result = base.RunTask();

            foreach (var item in result.OrderBy(a => a.Key).SelectMany(a => a.Value))
                yield return item;
        }
    }
}