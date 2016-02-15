using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using System.Threading;
using System.Collections.Concurrent;
using AForge.Video.DirectShow;
using AForge.Video;
using System.Drawing;
using System.IO;
using peerTube.Multicast;
using DistributedServiceProvider;
using DistributedServiceProvider.Base;
using ProtoBuf;

namespace peerTube.Screens
{
    public class BroadcastCapture
        :IScreen
    {
        #region fields
        VideoCaptureDevice webcam;

        Game1 game;
        private double timestamp;

        private object frameSubmitLock = new object();
        public TimeSpan MinimumFramePeriod
        {
            get;
            set;
        }
        private double previousFrameTimestamp = double.MinValue;
        private long videoFrameNumber = long.MinValue;
        private long dataSent = 0;

        BroadcastPeer broadcaster;

        Texture2D texture;

        TimeSpan lastUpdatedDataPerSecondTime;
        int dataRateAssessmentCount = 10;
        long lastUpdatedDataPerSecond = long.MinValue;
        long dataLastSecond;
        long dataPerSecond;

        public long TargetDataRate
        {
            get;
            set;
        }

        System.Drawing.Imaging.ImageCodecInfo imageCodec;
        System.Drawing.Imaging.EncoderParameters encoderParameters;

        private long encoderQualityCap = 100;
        private long encoderQuality;
        public long EncoderQuality
        {
            get
            {
                return encoderQuality;
            }
            private set
            {
                encoderQuality = value;
                encoderParameters.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, EncoderQuality);
            }
        }
        #endregion

        #region construction and initialisation
        public BroadcastCapture(Game1 game, long targetDataRate)
        {
            this.game = game;
            this.TargetDataRate = targetDataRate;
            MinimumFramePeriod = TimeSpan.FromSeconds(1 / 15f);

            InitialiseBroadcast("Broadcast Zero: Hello, World!", game.RoutingTable);
            InitialiseEncoder();
            InitialiseCamera();
        }

        private void InitialiseEncoder()
        {
            imageCodec = GetEncoder(System.Drawing.Imaging.ImageFormat.Jpeg);
            encoderParameters = new System.Drawing.Imaging.EncoderParameters(1);
            EncoderQuality = 25;
        }

        private void InitialiseBroadcast(string name, DistributedRoutingTable routingTable)
        {
            broadcaster = new BroadcastPeer(routingTable.LocalIdentifier, true);
            routingTable.RegisterConsumer(broadcaster);
        }

        private void InitialiseCamera()
        {
            FilterInfoCollection webcamList = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            FilterInfo info = null;
            foreach (FilterInfo i in webcamList)
            {
                info = i;
                break;
            }

            webcam = new VideoCaptureDevice(info.MonikerString);
            webcam.NewFrame += new NewFrameEventHandler((a, b) =>
            {
                SubmitCameraData(b.Frame, timestamp);
            });

            webcam.Start();
        }

        private static System.Drawing.Imaging.ImageCodecInfo GetEncoder(System.Drawing.Imaging.ImageFormat format)
        {
            System.Drawing.Imaging.ImageCodecInfo[] codecs = System.Drawing.Imaging.ImageCodecInfo.GetImageDecoders();

            foreach (System.Drawing.Imaging.ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
        #endregion

        #region submit data
        private void SubmitCameraData(Bitmap data, double timestamp)
        {
            if (!Monitor.TryEnter(frameSubmitLock))
                return;
            else
            {
                try
                {
                    if (timestamp - previousFrameTimestamp > MinimumFramePeriod.TotalMilliseconds)
                    {
                        previousFrameTimestamp = timestamp;
                        Interlocked.Increment(ref videoFrameNumber);

                        MemoryStream jpegData = new MemoryStream();
                        data.Save(jpegData, imageCodec, encoderParameters);
                        jpegData.Position = 0;

                        using (MemoryStream packet = new MemoryStream())
                        {
                            VideoFrame frame = new VideoFrame();
                            frame.JpegData = jpegData.ToArray();

                            Serializer.SerializeWithLengthPrefix<VideoFrame>(packet, frame, PrefixStyle.Base128);

                            Interlocked.Add(ref dataSent, packet.Length * Math.Max(1, broadcaster.ChildrenCount));

                            if (packet.Length > ushort.MaxValue)
                                Interlocked.Decrement(ref encoderQualityCap);
                            else
                                ThreadPool.QueueUserWorkItem(_ => broadcaster.SendVideoData(packet.ToArray()));
                        }

                        var f = Texture2D.FromStream(game.GraphicsDevice, jpegData);

                        var old = Interlocked.Exchange(ref texture, f);
                    }
                }
                finally
                {
                    Monitor.Exit(frameSubmitLock);
                }
            }
        }
        #endregion

        #region update
        public void Update(GameTime time)
        {
            if (videoFrameNumber - lastUpdatedDataPerSecond > dataRateAssessmentCount)
            {
                lastUpdatedDataPerSecond = videoFrameNumber;
                TimeSpan timespan = time.TotalGameTime - lastUpdatedDataPerSecondTime;
                lastUpdatedDataPerSecondTime = time.TotalGameTime;

                dataPerSecond = (long)((dataSent - dataLastSecond) / timespan.TotalSeconds);
                dataLastSecond = dataSent;

                AdjustQuality(dataPerSecond, TargetDataRate);
            }

            Interlocked.Exchange(ref timestamp, time.TotalGameTime.TotalMilliseconds);
        }

        private void AdjustQuality(long dataPerSecond, long TargetDataRate)
        {
            long difference = TargetDataRate - dataPerSecond;
            EncoderQuality += (Math.Abs(difference) > 100 ? 2 : 1) * Math.Sign(difference);

            EncoderQuality = (long)MathHelper.Clamp(EncoderQuality, 0, Interlocked.Read(ref encoderQualityCap) + 1);
        }
        #endregion

        public void Draw(GameTime time)
        {
            float y = -20;

            game.SpriteBatch.Begin();

            if (texture != null)
                game.SpriteBatch.Draw(texture, new Microsoft.Xna.Framework.Rectangle(0, 0, game.GraphicsDevice.Viewport.Width, game.GraphicsDevice.Viewport.Height), Microsoft.Xna.Framework.Color.White);

            //game.SpriteBatch.DrawStringJitter(game.Content.Load<SpriteFont>("Font"), videoFrameNumber + " v/frame", new Vector2(0, y = y + 20), Microsoft.Xna.Framework.Color.White, Microsoft.Xna.Framework.Color.Black);
            //game.SpriteBatch.DrawStringJitter(game.Content.Load<SpriteFont>("Font"), (timestamp / 1000f) + "s", new Vector2(0, y = y + 20), Microsoft.Xna.Framework.Color.White, Microsoft.Xna.Framework.Color.Black);
            //game.SpriteBatch.DrawStringJitter(game.Content.Load<SpriteFont>("Font"), dataSent.ToDataString(), new Vector2(0, y = y + 20), Microsoft.Xna.Framework.Color.White, Microsoft.Xna.Framework.Color.Black);
            //game.SpriteBatch.DrawStringJitter(game.Content.Load<SpriteFont>("Font"), dataPerSecond.ToDataString() + "/s", new Vector2(0, y = y + 20), Microsoft.Xna.Framework.Color.White, Microsoft.Xna.Framework.Color.Black);
            //game.SpriteBatch.DrawStringJitter(game.Content.Load<SpriteFont>("Font"), "Target = " + TargetDataRate.ToDataString() + "/s", new Vector2(0, y = y + 20), Microsoft.Xna.Framework.Color.White, Microsoft.Xna.Framework.Color.Black);
            //game.SpriteBatch.DrawStringJitter(game.Content.Load<SpriteFont>("Font"), "Encoder Quality = " + (EncoderQuality / 100f) + "(" + (encoderQualityCap / 100f) + ")", new Vector2(0, y = y + 20), Microsoft.Xna.Framework.Color.White, Microsoft.Xna.Framework.Color.Black);
            game.SpriteBatch.DrawStringJitter(game.Content.Load<SpriteFont>("Font"), "Children = " + broadcaster.ChildrenCount, new Vector2(0, y = y + 20), Microsoft.Xna.Framework.Color.White, Microsoft.Xna.Framework.Color.Black);
            //game.SpriteBatch.DrawStringJitter(game.Content.Load<SpriteFont>("Font"), "Finger table = " + broadcaster.RoutingTable.ContactCount, new Vector2(0, y = y + 20), Microsoft.Xna.Framework.Color.White, Microsoft.Xna.Framework.Color.Black);

            game.SpriteBatch.End();
        }

        public void Stop()
        {
            webcam.Stop();
            webcam.WaitForStop();
        }
    }
}
