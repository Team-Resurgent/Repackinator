using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using ManagedBass.FftSignalProvider;
using ManagedBass;
using System;
using System.Collections.Generic;
using System.Reflection;
using Repackinator.Core.Helpers;
using System.Numerics;
using System.Collections;
using System.Security.Cryptography.X509Certificates;
using Avalonia.Input;
using System.Globalization;
using Avalonia.Media.TextFormatting;

namespace Repackinator.Controls
{
    public class CracktroControl : Control
    {
        private struct Star
        {
            public Point Pos { get; set; }
            public float Scale { get; set; }
        }


        private DispatcherTimer mTimer { get; set; }

        private int mPlayBackHandle { get; set; }
        private SignalProvider? mSignalProvider { get; set; }


        #region Drawing

        private string message = "Repackinator brought to you by Team Resurgent and in collaboration with Team Cerbios, what application wouldn't be complete without some retro style credits... Repackinator is Open Source on our GitHub and is open to the Community to contribute to and help evolve the application into something amazing... Music is a remix of the C64 classic Comic Bakery by Stuart Wilson... Coding by EqUiNoX... Application Design by EqUiNoX, HoRnEyDvL, Hazeno... Testing by HoRnEyDvL, Hazeno, Rocky5, navi, Zatchbot... Shout outs in no particular order go to Grizzly Adams, Kekule, Blackbolt, Ryzee119, Xbox7887, Incursion64, Redline99, Empreal96, Fredr1kh, Braxton, Natetronn... Keep your eye out for more future applications from Team Resurgent... Til then, enjoy, and we will see you on our discord................................................................................. ";
        private float charOffset = 0;
        private float scrollPos = 0;
        private float cubeSin = 0;
        private float starsSin = 0;
        private float scrollerSin = 0;
        private Star[] stars = new Star[50];
        private Matrix4x4 viewMatrix;
        private Matrix4x4 projMatrix;
        private Matrix4x4 worldMatrix;
        private List<Vector3> objectPoints = [];
        private List<int> objectIndices = [];

        private static Vector3 Vec3Mat4x4Multiply(Vector3 v, Matrix4x4 m)
        {
            float x = v.X * m.M11 + v.Y * m.M21 + v.Z * m.M31 + m.M41;
            float y = v.X * m.M12 + v.Y * m.M22 + v.Z * m.M32 + m.M42;
            float z = v.X * m.M13 + v.Y * m.M23 + v.Z * m.M33 + m.M43;
            float w = v.X * m.M14 + v.Y * m.M24 + v.Z * m.M34 + m.M44;
            return new Vector3(x, y, z) / w;
        }

        Vector3 Vec3Project(Vector3 v, Matrix4x4 world, Matrix4x4 view, Matrix4x4 proj)
        {
            Vector3 projectedPos = Vec3Mat4x4Multiply(v, worldMatrix * viewMatrix * projMatrix);
            return new Vector3(0 + (1 + projectedPos.X) * 600 / 2, 0 + (1 - projectedPos.Y) * 400 / 2, 0 + projectedPos.Z * 1);
        }

        public static double ConvertToRadians(double angle)
        {
            return Math.PI / 180 * angle;
        }

        private static void DrawLines(DrawingContext context, IReadOnlyList<Point> points)
        {
            var linePen = new Pen(Brushes.Red, 1);
            for (var i = 0; i < points.Count; i += 2)
            {
                var startLocation = points[i];
                var endLocation = points[i + 1];
                context.DrawLine(linePen, startLocation, endLocation);
            }
        }

        private void DrawFFT(DrawingContext context)
        {
            if (mSignalProvider == null)
            {
                return;
            }
            var channelData = mSignalProvider.DataSampleWindowed;
            if (channelData == null)
            {
                return;
            }
            var points = new List<Point>
            {
                new Point(0, 390)
            };
            var channelValues = channelData[0].AdjustToScale(0, 200, true, out _);
            var channelDataLength = channelValues.Data.Length;
            for (var dataIndex = 0; dataIndex < channelDataLength; dataIndex++)
            {
                var value = (int)channelValues.Data[dataIndex];
                if (dataIndex % 8 != 0)
                {
                    continue;
                }
                points.Add(new Point((dataIndex + 1) * (600.0f / (channelDataLength + 1)), 390 - value));
                points.Add(new Point((dataIndex + 1) * (600.0f / (channelDataLength + 1)), 390 - value));
            }
            points.Add(new Point(600, 390));
            DrawLines(context, points);
        }

        private void DrawStars(DrawingContext context)
        {
            var sinPeak = (float)Math.Sin(ConvertToRadians(starsSin)) * 50;
            var cosPeak = (float)Math.Cos(ConvertToRadians(starsSin)) * 50;

            var random = new Random();
            var pointPen = new Pen(Brushes.Yellow, 1);
            for (var i = 0; i < stars.Length; i++)
            {
                var scaledPos = stars[i].Pos * stars[i].Scale;
                var startLocation = scaledPos + new Point(300 + sinPeak, 200 + cosPeak);
                var endLocation = scaledPos + new Point(301 + sinPeak, 201 + cosPeak);

                context.DrawLine(pointPen, startLocation, endLocation);

                stars[i].Scale += 0.01f;

                if (stars[i].Scale >= 2.0f)
                {
                    stars[i].Pos = new Point(random.Next(-300, 300), random.Next(-200, 200));
                    stars[i].Scale = 0.01f;
                }
            }
        }

        private void DrawObject(DrawingContext context)
        {
            var pointBrush = Brushes.Magenta;
            var pointPen = new Pen(pointBrush, 1);
            var linePen = new Pen(Brushes.Blue, 1);
            for (var i = 0; i < objectIndices.Count; i += 2)
            {
                var projected1 = Vec3Project(objectPoints[objectIndices[i]], worldMatrix, viewMatrix, projMatrix);
                var screenLocation1 = new Point(projected1.X, projected1.Y);
                var projected2 = Vec3Project(objectPoints[objectIndices[i + 1]], worldMatrix, viewMatrix, projMatrix);
                var screenLocation2 = new Point(projected2.X, projected2.Y);
                context.DrawEllipse(pointBrush, pointPen, screenLocation1, 3, 3);
                context.DrawLine(linePen, screenLocation1, screenLocation2);

            }
            worldMatrix = Matrix4x4.CreateRotationX((float)ConvertToRadians(cubeSin)) *
                          Matrix4x4.CreateRotationY((float)ConvertToRadians(cubeSin)) *
                          Matrix4x4.CreateRotationZ((float)ConvertToRadians(cubeSin));
        }

        private void DrawScroller(DrawingContext context)
        {
            var textSize = 40;
            var typeface = new Typeface("Arial");
            var culture = new CultureInfo("en-US");

            float x = 0;
            float y = 0;
            int i = (int)charOffset;
            while (x < (600 + textSize))
            {
                var amplitude = 100.0f;
                var peak = (float)Math.Sin(ConvertToRadians(scrollerSin + y));

                var textXOffset = (20 - textSize) / 2;
                var textPos = new Point(x - scrollPos + textXOffset, amplitude * 2 + peak * amplitude);
                var formattedText = new FormattedText(message[i].ToString(), culture, FlowDirection.LeftToRight, typeface, textSize, Brushes.White);

                context.DrawText(formattedText, textPos);
                i++;
                if (i >= message.Length)
                {
                    i = 0;
                }
                x += textSize;
                y += 2f;
            }

            scrollPos += 4;
            if (scrollPos > textSize)
            {
                scrollPos -= textSize;
                charOffset++;
                if (charOffset == message.Length)
                {
                    charOffset = 0;
                }
            }
        }

        #endregion


        private void PlayAudio()
        {
            Bass.Init();
            var musicData = ResourceLoader.GetEmbeddedResourceBytes("Stuart Wilson - Not Another Comic Bakery Remix.mp3", typeof(AboutWindow).GetTypeInfo().Assembly);
            mPlayBackHandle = Bass.CreateStream(musicData, 0, musicData.Length, BassFlags.Default | BassFlags.Loop);
            Bass.ChannelPlay(mPlayBackHandle);

            mSignalProvider = new SignalProvider(DataFlags.FFT1024, true, true) { WindowType = WindowType.Hanning, };
            mSignalProvider.SetChannel(mPlayBackHandle);
        }

        private void StopAudio()
        {
            Bass.StreamFree(mPlayBackHandle);
            Bass.Free();
        }

        private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            StopAudio();
        }

        private void OnTick(object? sender, EventArgs e)
        {
            starsSin += 2.0f;
            if (starsSin >= 360)
            {
                starsSin -= 360;
            }

            cubeSin += 1.0f;
            if (cubeSin >= 360)
            {
                cubeSin -= 360;
            }

            scrollerSin += 2.0f;
            if (scrollerSin >= 360)
            {
                scrollerSin -= 360;
            }

            InvalidateVisual();
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            DrawStars(context);
            DrawObject(context);
            DrawFFT(context);
            DrawScroller(context);
        }

        public CracktroControl()
        {
            var random = new Random();
            for (var i = 0; i < stars.Length; i++)
            {
                stars[i] = new Star
                {
                    Pos = new Point(random.Next(-300, 300), random.Next(-200, 200)),
                    Scale = (float)(random.NextDouble() * 2)
                };
            }

            viewMatrix = Matrix4x4.CreateLookAt(new Vector3(-8, 0, 0), Vector3.Zero, Vector3.UnitY);
            projMatrix = Matrix4x4.CreatePerspectiveFieldOfView((float)ConvertToRadians(60), 600.0f / 400.0f, 1, 10);
            worldMatrix = Matrix4x4.CreateRotationX((float)ConvertToRadians(0));

            objectPoints.Add(new Vector3(-2, -2, -2));
            objectPoints.Add(new Vector3(2, -2, -2));
            objectPoints.Add(new Vector3(2, 2, -2));
            objectPoints.Add(new Vector3(-2, 2, -2));
            objectPoints.Add(new Vector3(-2, -2, 2));
            objectPoints.Add(new Vector3(2, -2, 2));
            objectPoints.Add(new Vector3(2, 2, 2));
            objectPoints.Add(new Vector3(-2, 2, 2));

            objectIndices.Add(0);
            objectIndices.Add(1);
            objectIndices.Add(1);
            objectIndices.Add(2);
            objectIndices.Add(2);
            objectIndices.Add(3);
            objectIndices.Add(3);
            objectIndices.Add(0);

            objectIndices.Add(4);
            objectIndices.Add(5);
            objectIndices.Add(5);
            objectIndices.Add(6);
            objectIndices.Add(6);
            objectIndices.Add(7);
            objectIndices.Add(7);
            objectIndices.Add(4);

            objectIndices.Add(0);
            objectIndices.Add(4);
            objectIndices.Add(1);
            objectIndices.Add(5);
            objectIndices.Add(2);
            objectIndices.Add(6);
            objectIndices.Add(3);
            objectIndices.Add(7);

            DetachedFromVisualTree += OnDetachedFromVisualTree;

            mTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(64) 
            };
            mTimer.Tick += OnTick;
            mTimer.Start();

            PlayAudio();
        }

    }
}
