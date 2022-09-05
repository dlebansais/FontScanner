namespace FontScanner;

using FontLoader;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

[DebuggerDisplay("{Width} x {Height}")]
public class PageImage
{
    #region Init
    public PageImage(Bitmap bmp)
    {
        Width = bmp.Width;
        Height = bmp.Height;

        Rectangle Rect = new Rectangle(0, 0, Width, Height);
        BitmapData Data = bmp.LockBits(Rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        Stride = Math.Abs(Data.Stride);

        int ByteCount = Data.Stride * Rect.Height;
        ArgbValues = new byte[ByteCount];
        GrayValues = new byte[ByteCount];

        // Copy the RGB values into the array.
        Marshal.Copy(Data.Scan0, ArgbValues, 0, ByteCount);

        bmp.UnlockBits(Data);

        ConvertToGrayscale(ArgbValues, GrayValues, Stride);
    }

    private void ConvertToGrayscale(byte[] argbValues, byte[] grayValues, int stride)
    {
        double MinR = 255;
        int MinRR = 0;
        int MinRG = 0;
        int MinRB = 0;
        double MinG = 255;
        int MinGR = 0;
        int MinGG = 0;
        int MinGB = 0;
        double MinB = 255;
        int MinBR = 0;
        int MinBG = 0;
        int MinBB = 0;

        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                int Offset = (y * stride) + x * 4;
                int R = argbValues[Offset + 2];
                int G = argbValues[Offset + 1];
                int B = argbValues[Offset + 0];

                if (R > G && R > B)
                {
                    double F = (double)(G + B) / (double)R;
                    if (MinR > F)
                    {
                        MinRR = R;
                        MinRG = G;
                        MinRB = B;
                        MinR = F;
                    }
                }

                if (G > R && G > B)
                {
                    double F = (double)(R + B) / (double)G;
                    if (MinG > F)
                    {
                        MinGR = R;
                        MinGG = G;
                        MinGB = B;
                        MinG = F;
                    }
                }

                if (B > R && B > G)
                {
                    double RF = (double)(R + G) / (double)B;
                    if (MinB > RF)
                    {
                        MinBR = R;
                        MinBG = G;
                        MinBB = B;
                        MinB = RF;
                    }
                }
            }

        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                int Offset = (y * stride) + x * 4;
                int R = argbValues[Offset + 2];
                int G = argbValues[Offset + 1];
                int B = argbValues[Offset + 0];

                grayValues[Offset + 3] = 0xFF;

                double RatioR;
                double RatioG;
                double RatioB;
                byte Pixel;

                if (R == G && R == B)
                {
                    grayValues[Offset + 2] = (byte)R;
                    grayValues[Offset + 1] = (byte)G;
                    grayValues[Offset + 0] = (byte)B;
                    continue;
                }

                if (R >= G && R >= B)
                {
                    RatioR = MinRR < 255 ? (R - MinRR) / (255.0 - MinRR) : 1;
                    RatioG = (G - MinRG) / (255.0 - MinRG);
                    RatioB = (B - MinRB) / (255.0 - MinRB);
                }
                else if (G >= R && G >= B)
                {
                    RatioR = (R - MinGR) / (255.0 - MinGR);
                    RatioG = MinGG < 255 ? (G - MinGG) / (255.0 - MinGG) : 1;
                    RatioB = (B - MinGB) / (255.0 - MinGB);
                }
                else
                {
                    Debug.Assert(B >= R && B >= G);

                    RatioR = (R - MinBR) / (255.0 - MinBR);
                    RatioG = (G - MinBG) / (255.0 - MinBG);
                    RatioB = MinBB < 255 ? (B - MinBB) / (255.0 - MinBB) : 1;
                }

                double Ratio = (RatioR + RatioG + RatioB) / 3;
                Pixel = (byte)Math.Ceiling(255 * Ratio);
                grayValues[Offset + 0] = Pixel;
                grayValues[Offset + 1] = Pixel;
                grayValues[Offset + 2] = Pixel;
            }
    }
    #endregion

    #region Properties
    public int Width { get; }
    public int Height { get; }
    #endregion

    #region Client Interface
    public Bitmap ToBitmap()
    {
        Bitmap Result = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);

        Rectangle Rect = new Rectangle(0, 0, Width, Height);
        BitmapData Data = Result.LockBits(Rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        Marshal.Copy(ArgbValues, 0, Data.Scan0, ArgbValues.Length);
        Result.UnlockBits(Data);

        return Result;
    }

    public Bitmap ToBitmapSidesClipped(Rectangle srcRect, out int leftMargin, out int rightMargin)
    {
        int SrcHeight = srcRect.Height;

        leftMargin = srcRect.Width;
        rightMargin = srcRect.Width;

        for (int y = 0; y < SrcHeight; y++)
        {
            int SrcOffset = ((srcRect.Top + y) * Stride) + (srcRect.Left * 4);
            int LineLeft = -1;
            int LineRight = -1;

            for (int x = 0; x < srcRect.Width; x++)
            {
                byte R = ArgbValues[SrcOffset + (x * 4) + 2];
                byte G = ArgbValues[SrcOffset + (x * 4) + 1];
                byte B = ArgbValues[SrcOffset + (x * 4) + 0];

                if (R != 0xFF || G != 0xFF || B != 0xFF)
                {
                    if (LineLeft < 0)
                        LineLeft = x;

                    if (LineRight < x)
                        LineRight = x;
                }
            }

            if (LineLeft >= 0)
            {
                if (leftMargin > LineLeft)
                    leftMargin = LineLeft;
                if (rightMargin > srcRect.Width - LineRight)
                    rightMargin = srcRect.Width - LineRight;
            }
        }

        if (rightMargin > 0)
            rightMargin--;

        if (leftMargin + rightMargin >= srcRect.Width)
        {
            leftMargin = 0;
            rightMargin = 0;
        }

        int Width = srcRect.Width - leftMargin - rightMargin;

        Bitmap Bitmap = new Bitmap(Width, SrcHeight);
        Rectangle DstRect = new Rectangle(0, 0, Width, SrcHeight);
        BitmapData Data = Bitmap.LockBits(DstRect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        byte[] DestArgbValues = new byte[SrcHeight * Data.Stride];

        for (int y = 0; y < SrcHeight; y++)
        {
            int SrcOffset = ((srcRect.Top + y) * Stride) + ((srcRect.Left + leftMargin) * 4);
            int DestOffset = y * Data.Stride;

            for (int x = 0; x < Width; x++)
                for (int n = 0; n < 4; n++)
                    DestArgbValues[DestOffset + (x * 4) + n] = ArgbValues[SrcOffset + (x * 4) + n];
        }

        Marshal.Copy(DestArgbValues, 0, Data.Scan0, DestArgbValues.Length);
        Bitmap.UnlockBits(Data);

        return Bitmap;
    }

    public bool IsWhitePixel(int x, int y)
    {
        int Offset = (y * Stride) + (x * 4);
        byte B = ArgbValues[Offset + 0];
        byte G = ArgbValues[Offset + 1];
        byte R = ArgbValues[Offset + 2];

        return B == ArgbWhite[0] && G == ArgbWhite[1] && R == ArgbWhite[2];
    }

    public bool IsWhiteLine(Rectangle clipping, int y)
    {
        for (int x = 0; x < clipping.Width; x++)
            if (!IsWhitePixel(clipping.Left + x, clipping.Top + y))
                return false;

        return true;
    }

    public bool IsWhiteLine(Rectangle clipping, Rectangle excluded, int row)
    {
        for (int column = 0; column < clipping.Width; column++)
        {
            int x = clipping.Left + column;
            int y = clipping.Top + row;

            if (!excluded.Contains(x, y))
                if (!IsWhitePixel(x, y))
                    return false;
        }

        return true;
    }

    public bool IsWhiteColumn(Rectangle clipping, int x)
    {
        for (int y = 0; y < clipping.Height; y++)
            if (!IsWhitePixel(clipping.Left + x, clipping.Top + y))
                return false;

        return true;
    }

    public bool IsWhiteColumn(Rectangle clipping, Rectangle excluded, int column)
    {
        for (int row = 0; row < clipping.Height; row++)
        {
            int x = clipping.Left + column;
            int y = clipping.Top + row;

            if (!excluded.Contains(x, y))
                if (!IsWhitePixel(x, y))
                    return false;
        }

        return true;
    }

    public PixelArray GetPixelArray(int left, int top, int width, int height, int baseline, bool forbidGrayscale)
    {
        if (IsDominantBlue(left, top, width, height) || forbidGrayscale)
            return new PixelArray(left, width, top, height, ArgbValues, Stride, baseline, clearEdges: false);
        else
            return new PixelArray(left, width, top, height, GrayValues, Stride, baseline, clearEdges: false);
    }

    public PixelArray GetGrayscalePixelArray(int left, int top, int width, int height, int baseline)
    {
        return new PixelArray(left, width, top, height, GrayValues, Stride, baseline, clearEdges: false);
    }

    public void ColorRect(Rectangle rect)
    {
        for (int i = 0; i < rect.Width; i++)
            for (int j = 0; j < rect.Height; j++)
                ColorPixel(rect.Left + i, rect.Top + j);
    }
    #endregion

    #region Implementation
    private bool IsDominantBlue(int left, int top, int width, int height)
    {
        int BlueCount = 0;
        int ColoredCount = 0;

        for (int i = left; i < left + width; i++)
            for (int j = top; j < top + height; j++)
            {
                int Offset = (j * Stride) + (i * 4);
                byte R = ArgbValues[Offset + 2];
                byte G = ArgbValues[Offset + 1];
                byte B = ArgbValues[Offset + 0];

                if (R != 0xFF || G != 0xFF || B != 0xFF)
                {
                    ColoredCount++;

                    if (B > R && B > G)
                        BlueCount++;
                }
            }

        return BlueCount >= (ColoredCount * 2) / 3;
    }

    private void ColorPixel(int x, int y)
    {
        int Offset = (y * Stride) + (x * 4);
        byte B = 0x00;
        byte G = 0x00;
        byte R = 0xFF;

        ArgbValues[Offset + 0] = B;
        ArgbValues[Offset + 1] = G;
        ArgbValues[Offset + 2] = R;
    }

    private byte[] ArgbValues;
    private byte[] GrayValues;
    private int Stride;
    private static readonly byte[] ArgbWhite = new byte[4] { 0xFF, 0xFF, 0xFF, 0xFF };
    #endregion
}
