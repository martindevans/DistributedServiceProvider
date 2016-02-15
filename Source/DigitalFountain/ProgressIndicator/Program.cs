using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DigitalFountain;
using System.Drawing;

namespace ProgressIndicator
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] b = new byte[800 * 10];
            new Random().NextBytes(b);

            Fountain f = new Fountain(1234, b, 10);

            Bucket bucket = new Bucket(f.BlockSize, f.BlockCount);

            List<bool[]> progresses = new List<bool[]>();

            while (!bucket.IsComplete)
            {
                bucket.AddPacket(f.CreatePacket());

                progresses.Add(bucket.ProgressIndicator().ToArray());
            }

            Bitmap image = new Bitmap(f.BlockCount, progresses.Count);
            int x = 0;
            int y = 0;
            foreach (var item in progresses)
            {
                foreach (var pix in item)
                {
                    image.SetPixel(x, y, pix ? Color.Green : Color.Red);
                    x++;
                }
                x = 0;
                y++;
            }

            image.Save("Progress chart.bmp");
        }
    }
}
