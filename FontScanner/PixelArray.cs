namespace FontScanner;

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

[DebuggerDisplay("{Width} x {Height}, {Baseline}")]
public class PixelArray
{
    public static readonly PixelArray Empty = new();
    public const double MaxSuportedDiffRatio = 0.2;

    private PixelArray()
    {
        Width = 0;
        Height = 0;
        Baseline = 0;
        Array = new byte[0, 0];
        IsWhiteColumn = new bool[0];
        ColoredCountColumn = new int[0];
    }

    public PixelArray(int width, int height, int baseline)
    {
        Width = width;
        Height = height;
        Baseline = baseline;

        Array = new byte[Width, Height];
        IsWhiteColumn = new bool[Width];
        ColoredCountColumn = new int[Width];
    }

    public PixelArray(int left, int width, int top, int height, byte[] argbValues, int stride, int baseline, bool clearEdges)
    {
        Width = width;
        Height = height;
        Baseline = baseline;

        Array = new byte[Width, Height];
        IsWhiteColumn = new bool[Width];
        ColoredCountColumn = new int[Width];

        for (int x = 0; x < width; x++)
        {
            bool IsWhite = true;
            int ColoredCount = 0;

            for (int y = 0; y < height; y++)
            {
                int Offset = ((top + y) * stride) + ((left + x) * 4);

                int B = argbValues[Offset + 0];
                int G = argbValues[Offset + 1];
                int R = argbValues[Offset + 2];

                byte Pixel;

                if (clearEdges && (x == 0 || y == 0))
                    Pixel = 0xFF;
                else
                    Pixel = (byte)((R + G + B) / 3);

                Array[x, y] = Pixel;

                if (Pixel != 0xFF)
                {
                    IsWhite = false;

                    if (Pixel != 0)
                        ColoredCount++;
                }
            }

            IsWhiteColumn[x] = IsWhite;
            ColoredCountColumn[x] = ColoredCount;
        }
    }

    public static PixelArray FromBitmap(Bitmap bitmap)
    {
        return FromBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
    }

    public static PixelArray FromBitmap(Bitmap bitmap, Rectangle rect)
    {
        int Width = bitmap.Width;
        int Height = bitmap.Height;
        int Baseline = Height - 1;

        Rectangle FullRect = new Rectangle(0, 0, Width, Height);
        BitmapData Data = bitmap.LockBits(FullRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        int Stride = Math.Abs(Data.Stride);

        int ByteCount = Data.Stride * FullRect.Height;
        byte[] ArgbValues = new byte[ByteCount];

        Marshal.Copy(Data.Scan0, ArgbValues, 0, ByteCount);

        bitmap.UnlockBits(Data);

        return new PixelArray(rect.Left, rect.Width, rect.Top, rect.Height, ArgbValues, Stride, Baseline, clearEdges: false);
    }

    public int Width { get; }
    public int Height { get; }
    public int Baseline { get; }

    private byte[,] Array;
    private bool[] IsWhiteColumn;
    private int[] ColoredCountColumn;

    public bool IsWhite(int x, int y)
    {
        return Array[x, y] == 0xFF;
    }

    public bool IsColored(int x, int y, out byte color)
    {
        color = Array[x, y];
        return color != 0xFF && color != 0;
    }

    public void GetPixel(int x, int y, out uint rgb)
    {
        rgb = Array[x, y];
    }

    private static void ClearPixel(PixelArray p, int x, int y)
    {
        p.Array[x, y] = 0xFF;
    }

    private static void CopyPixel(PixelArray p1, int x1, int y1, PixelArray p2, int x2, int y2, ref bool isWhite, ref int coloredCount)
    {
        byte Pixel = p1.Array[x1, y1];

        p2.Array[x2, y2] = Pixel;
        UpdatePixelFlags(Pixel, ref isWhite, ref coloredCount);
    }

    private static void MixPixel(PixelArray p, int x, int y, PixelArray p1, int x1, int y1, PixelArray p2, int x2, int y2, ref bool isWhite, ref int coloredCount)
    {
        byte Pixel1 = p1.Array[x1, y1];
        byte Pixel2 = p2.Array[x2, y2];

        byte Pixel;

        if (Pixel1 != 0xFF && Pixel2 != 0xFF)
//            Pixel = (byte)((Pixel1 + Pixel2) / 2);
            Pixel = (byte)((Pixel1 * Pixel2) / 255);
        else
            Pixel = (byte)(Pixel1 + Pixel2 - 0xFF);

        p.Array[x, y] = Pixel;
        UpdatePixelFlags(Pixel, ref isWhite, ref coloredCount);
    }

    private static void UpdatePixelFlags(byte pixel, ref bool isWhite, ref int coloredCount)
    {
        if (pixel != 0xFF)
        {
            isWhite = false;

            if (pixel != 0)
                coloredCount++;
        }
    }

    private static void MergePixel(PixelArray Result, PixelArray p1, PixelArray p2, int x, int y, int TotalWidth, int Baseline, ref bool isWhite, ref int coloredCount)
    {
        if (x >= 0 && x < p1.Width && x >= TotalWidth - p2.Width)
        {
            int OffsetY1 = Baseline - p1.Baseline;
            int OffsetY2 = Baseline - p2.Baseline;

            if (y >= OffsetY1 && y < OffsetY1 + p1.Height && y >= OffsetY2 && y < OffsetY2 + p2.Height)
            {
                MixPixel(Result, x, y, p1, x, y - OffsetY1, p2, x - TotalWidth + p2.Width, y - OffsetY2, ref isWhite, ref coloredCount);
                return;
            }
        }

        if (x >= 0 && x < p1.Width)
        {
            int OffsetY = Baseline - p1.Baseline;

            if (y >= OffsetY && y < OffsetY + p1.Height)
            {
                CopyPixel(p1, x, y - OffsetY, Result, x, y, ref isWhite, ref coloredCount);
                return;
            }
        }

        if (x >= TotalWidth - p2.Width)
        {
            int OffsetY = Baseline - p2.Baseline;

            if (y >= OffsetY && y < OffsetY + p2.Height)
            {
                CopyPixel(p2, x - TotalWidth + p2.Width, y - OffsetY, Result, x, y, ref isWhite, ref coloredCount);
                return;
            }
        }

        ClearPixel(Result, x, y);
    }

    public PixelArray Clipped()
    {
        int LeftEdge;
        int RightEdge;
        int TopEdge;
        int BottomEdge;

        for (LeftEdge = 0; LeftEdge < Width; LeftEdge++)
        {
            bool IsEmptyColumn = true;
            for (int y = 0; y < Height; y++)
            {
                if (!IsWhite(LeftEdge, y))
                {
                    IsEmptyColumn = false;
                    break;
                }
            }

            if (!IsEmptyColumn)
                break;
        }

        for (RightEdge = Width; RightEdge > 0; RightEdge--)
        {
            bool IsEmptyColumn = true;
            for (int y = 0; y < Height; y++)
            {
                if (!IsWhite(RightEdge - 1, y))
                {
                    IsEmptyColumn = false;
                    break;
                }
            }

            if (!IsEmptyColumn)
                break;
        }

        for (TopEdge = 0; TopEdge < Height; TopEdge++)
        {
            bool IsEmptyLine = true;
            for (int x = 0; x < Width; x++)
            {
                if (!IsWhite(x, TopEdge))
                {
                    IsEmptyLine = false;
                    break;
                }
            }

            if (!IsEmptyLine)
                break;
        }


        for (BottomEdge = Height; BottomEdge > 0; BottomEdge--)
        {
            bool IsEmptyLine = true;
            for (int x = 0; x < Width; x++)
            {
                if (!IsWhite(x, BottomEdge - 1))
                {
                    IsEmptyLine = false;
                    break;
                }
            }

            if (!IsEmptyLine)
                break;
        }

        if (LeftEdge < RightEdge && TopEdge < BottomEdge)
        {
            if (LeftEdge > 0 || RightEdge < Width || TopEdge > 0 || BottomEdge < Height)
            {
                PixelArray Result = new(RightEdge - LeftEdge, BottomEdge - TopEdge, Baseline - TopEdge);

                for (int x = 0; x < Result.Width; x++)
                {
                    bool IsWhite = true;
                    int ColoredCount = 0;

                    for (int y = 0; y < Result.Height; y++)
                        CopyPixel(this, LeftEdge + x, TopEdge + y, Result, x, y, ref IsWhite, ref ColoredCount);

                    Result.IsWhiteColumn[x] = IsWhite;
                    Result.ColoredCountColumn[x] = ColoredCount;
                }

                return Result;
            }
            else
                return this;
        }
        else
            return Empty;
    }

    public bool IsClipped
    {
        get
        {
            return IsClippedColumn(0) && IsClippedColumn(Width - 1) && IsClippedRow(0) && IsClippedRow(Height - 1);
        }
    }

    public bool IsClippedColumn(int column)
    {
        bool IsWhiteColumn = true;
        for (int y = 0; y < Height; y++)
            if (!IsWhite(column, y))
            {
                IsWhiteColumn = false;
                break;
            }

        return !IsWhiteColumn;
    }

    public bool IsClippedRow(int row)
    {
        bool IsWhiteRow = true;
        for (int x = 0; x < Width; x++)
            if (!IsWhite(x, row))
            {
                IsWhiteRow = false;
                break;
            }

        return !IsWhiteRow;
    }

    public void DebugPrint()
    {
        for (int y = 0; y < Height; y++)
        {
            string Line = string.Empty;

            for (int x = 0; x < Width; x++)
            {
                uint RGB = Array[x, y];
                uint Pixel = (((RGB >> 0) & 0xFF) + ((RGB >> 8) & 0xFF) + ((RGB >> 16) & 0xFF)) / 3;
                Line += Pixel < 0x40 ? "X" : (y == Baseline ? "." : " ");
            }

            Debug.WriteLine(Line);
        }
    }

    public static bool IsMatch(PixelArray p1, PixelArray p2, int verticalOffset)
    {
        Debug.Assert(p1.IsClipped);
        Debug.Assert(p2.IsClipped);

        if (p1.Width != p2.Width || p1.Height != p2.Height)
            return false;

        int Width = p1.Width;
        int Height = p1.Height;

        int BaselineDifference = p2.Baseline - p1.Baseline + verticalOffset;
        //if (Math.Abs(BaselineDifference) > 3)
        if (Math.Abs(BaselineDifference) > 0)
                return false;

        int DiffTotal = 0;
        int MaxSupportedDiff = (int)((Width * Height) * MaxSuportedDiffRatio);

        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                if (!IsMatchPixed(p1, p2, x, y, ref DiffTotal))
                    return false;

                if (DiffTotal > MaxSupportedDiff)
                    return false;
            }

        return true;
    }

    public static bool IsPixelToPixelMatch(PixelArray p1, PixelArray p2)
    {
        if (p1.Width != p2.Width || p1.Height != p2.Height)
            return false;

        int Width = p1.Width;
        int Height = p1.Height;
        int DiffTotal = 0;

        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                if (!IsMatchPixed(p1, p2, x, y, ref DiffTotal))
                    return false;

                if (DiffTotal > 0)
                    return false;
            }

        return true;
    }

    public static void ProfileMatch(PixelArray p1, PixelArray p2)
    {
        int Width = p1.Width;
        int Height = p1.Height;

        for (int y = 0; y < Height; y++)
        {
            string Line = string.Empty;

            for (int x = 0; x < Width; x++)
            {
                p1.GetPixel(x, y, out uint RGB1);
                p2.GetPixel(x, y, out uint RGB2);

                int n = (int)RGB1 - (int)RGB2;

                char c;
                if (n == 0)
                    c = ' ';
                else if (n > 0)
                    c = n <= 9 ? (char)('A' + n) : 'x';
                else
                    c = n >= -9 ? (char)('a' - n) : 'x';

                Line += c;
            }

            Debug.WriteLine(Line);
        }
    }

    public static bool IsLeftMatch(PixelArray p1, PixelArray p2, int verticalOffset, out int firstDiffX)
    {
        int Baseline1 = p1.Baseline;
        int Baseline2 = p2.Baseline + verticalOffset;

        int Width = Math.Max(p1.Width, p2.Width);
        int Baseline = Math.Max(Baseline1, Baseline2);
        int Height = Baseline + Math.Max(p1.Height - Baseline1, p2.Height - Baseline2);

        int DiffTotal = 0;
        int MaxSupportedDiff = (int)((Width * Height) * MaxSuportedDiffRatio);
        firstDiffX = -1;

        for (int x = 0; x < Width; x++)
        {
            if (x < p1.Width && x < p2.Width)
            {
                for (int y = 0; y < Height; y++)
                {
                    int y1 = y - Baseline + Baseline1;
                    int y2 = y - Baseline + Baseline2;

                    if (y1 >= 0 && y2 >= 0 && y1 < p1.Height && y2 < p2.Height)
                    {
                        if (!IsLeftMatchPixed(p1, p2, x, y1, y2, ref DiffTotal))
                            return false;

                        if (DiffTotal > MaxSupportedDiff)
                            return false;

                        if (DiffTotal > 0 && firstDiffX < 0)
                            firstDiffX = x;
                    }
                    else
                    {
                        if (y1 >= 0 && y1 < p1.Height)
                            if (!p1.IsWhite(x, y1))
                                return false;

                        if (y2 >= 0 && y2 < p2.Height)
                            if (!p2.IsWhite(x, y2))
                                return false;
                    }
                }
            }
            else if (x < p1.Width)
            {
                for (int y = 0; y < p1.Height; y++)
                    if (!p1.IsWhite(x, y))
                    {
                        Debug.Assert(!p1.IsWhiteColumn[x]);
                        return false;
                    }

                Debug.Assert(p1.IsWhiteColumn[x]);
            }
        }

        return true;
    }

    public static bool IsLeftDiagonalMatch(PixelArray p1, double perfectMatchRatio, int rightOverlapWidth, PixelArray p2, int verticalOffset)
    {
        int Baseline1 = p1.Baseline;
        int Baseline2 = p2.Baseline + verticalOffset;

        int Width = Math.Max(p1.Width, p2.Width);
        int Baseline = Math.Max(Baseline1, Baseline2);
        int Height = Baseline + Math.Max(p1.Height - Baseline1, p2.Height - Baseline2);

        int DiffTotal = 0;
        int MaxSupportedDiff = (int)((Width * Height) * MaxSuportedDiffRatio);

        bool[,] PixelSoftTaken = new bool[Width, Height];
        bool[,] PixelHardTaken = new bool[Width, Height];
        int PerfectMatchWidth = (int)(Width * (1.0 - perfectMatchRatio));

        for (int y = 0; y < Height; y++)
        {
            int x;

            for (x = 0; x < PerfectMatchWidth; x++)
            {
                int x1 = Width - x - 1;
                int y1 = y - Baseline + Baseline1;

                if (x1 < p1.Width && y1 >= 0 && y1 < p1.Height && !p1.IsWhite(x1, y1))
                    break;
            }

            for (int x2 = x; x2 < Width; x2++)
                PixelSoftTaken[Width - x2 - 1, y] = true;

            for (int x2 = x; x2 + rightOverlapWidth < Width; x2++)
                PixelHardTaken[Width - x2 - rightOverlapWidth - 1, y] = true;
        }

        int SoftTakenPixelCount = 0;
        int MaxSoftTakenPixelCount = 12;

        for (int x = 0; x < Width; x++)
        {
            if (x < p1.Width && x < p2.Width)
            {
                for (int y = 0; y < Height; y++)
                {
                    int y1 = y - Baseline + Baseline1;
                    int y2 = y - Baseline + Baseline2;
                    bool IsDifferent = false;

                    if (y1 >= 0 && y2 >= 0 && y1 < p1.Height && y2 < p2.Height)
                    {
                        if (PixelSoftTaken[x, y])
                        {
                            if (!IsLeftMatchPixed(p1, p2, x, y1, y2, ref DiffTotal))
                                IsDifferent = true;
                            else if (DiffTotal > MaxSupportedDiff)
                                IsDifferent = true;
                        }
                    }
                    else
                    {
                        if (y1 >= 0 && y1 < p1.Height && !p1.IsWhite(x, y1))
                            IsDifferent = true;

                        if (y2 >= 0 && y2 < p2.Height && PixelSoftTaken[x, y] && !p2.IsWhite(x, y2))
                            IsDifferent = true;
                    }

                    if (IsDifferent)
                    {
                        if (PixelHardTaken[x, y] || SoftTakenPixelCount >= MaxSoftTakenPixelCount)
                            return false;

                        SoftTakenPixelCount++;
                    }
                }
            }
            else if (x < p1.Width)
            {
                for (int y = 0; y < p1.Height; y++)
                    if (!p1.IsWhite(x, y))
                    {
                        Debug.Assert(!p1.IsWhiteColumn[x]);
                        return false;
                    }

                Debug.Assert(p1.IsWhiteColumn[x]);
            }
        }

        return true;
    }

    public static bool IsRightMatch(PixelArray p1, PixelArray p2, int verticalOffset)
    {
        int Baseline1 = p1.Baseline;
        int Baseline2 = p2.Baseline + verticalOffset;

        int Width = Math.Max(p1.Width, p2.Width);
        int Baseline = Math.Max(Baseline1, Baseline2);
        int Height = Baseline + Math.Max(p1.Height - Baseline1, p2.Height - Baseline2);

        int DiffTotal = 0;
        int MaxSupportedDiff = (int)((Width * Height) * MaxSuportedDiffRatio);

        for (int x = 0; x < Width; x++)
        {
            if (x < p1.Width && x < p2.Width)
            {
                for (int y = 0; y < Height; y++)
                {
                    int y1 = y - Baseline + Baseline1;
                    int y2 = y - Baseline + Baseline2;

                    if (y1 >= 0 && y2 >= 0 && y1 < p1.Height && y2 < p2.Height)
                    {
                        if (!IsRightMatchPixed(p1, p2, x, y1, y2, ref DiffTotal))
                            return false;

                        if (DiffTotal > MaxSupportedDiff)
                            return false;
                    }
                    else
                    {
                        if (y1 >= 0 && y1 < p1.Height)
                            if (!p1.IsWhite(p1.Width - x - 1, y1))
                                return false;

                        if (y2 >= 0 && y2 < p2.Height)
                            if (!p2.IsWhite(p2.Width - x - 1, y2))
                                return false;
                    }
                }

                Debug.Assert(p1.ColoredCountColumn[p1.Width - x - 1] == p2.ColoredCountColumn[p2.Width - x - 1]);
            }
            else if (x < p1.Width)
            {
                for (int y = 0; y < p1.Height; y++)
                {
                    if (!p1.IsWhite(p1.Width - x - 1, y))
                    {
                        Debug.Assert(!p1.IsWhiteColumn[p1.Width - x - 1]);
                        return false;
                    }
                }

                Debug.Assert(p1.IsWhiteColumn[p1.Width - x - 1]);
            }
        }

        return true;
    }

    private static bool IsMatchPixed(PixelArray p1, PixelArray p2, int x, int y, ref int diffTotal)
    {
        p1.GetPixel(x, y, out uint RGB1);
        p2.GetPixel(x, y, out uint RGB2);

        return IsMatchPixedValue(RGB1, RGB2, ref diffTotal);
    }

    private static bool IsLeftMatchPixed(PixelArray p1, PixelArray p2, int x, int y1, int y2, ref int diffTotal)
    {
        p1.GetPixel(x, y1, out uint RGB1);
        p2.GetPixel(x, y2, out uint RGB2);

        return IsMatchPixedValue(RGB1, RGB2, ref diffTotal);
    }

    private static bool IsRightMatchPixed(PixelArray p1, PixelArray p2, int x, int y1, int y2, ref int diffTotal)
    {
        p1.GetPixel(p1.Width - x - 1, y1, out uint RGB1);
        p2.GetPixel(p2.Width - x - 1, y2, out uint RGB2);

        return IsMatchPixedValue(RGB1, RGB2, ref diffTotal);
    }

    private static bool IsMatchPixedValue(uint rgb1, uint rgb2, ref int diffTotal)
    {
        int Diff = Math.Abs((int)rgb1 - (int)rgb2);
        diffTotal += Diff;

        return Diff <= 5;
    }

    public static bool IsCompatible(PixelArray p1, PixelArray p2, int testWidth)
    {
        Debug.Assert(p1.Width >= testWidth);
        Debug.Assert(p2.Width >= testWidth);

        for (int x = 0; x < testWidth; x++)
            if (p1.IsWhiteColumn[x] != p2.IsWhiteColumn[x] || p1.ColoredCountColumn[x] != p2.ColoredCountColumn[x])
                return false;

        return true;
    }

    public static PixelArray Merge(PixelArray p1, PixelArray p2, int inside)
    {
        return Merge(p1, p2, inside, p1.Width - inside + p2.Width);
    }

    public static PixelArray Merge(PixelArray p1, PixelArray p2, int inside, int maxWidth)
    {
        int Baseline = Math.Max(p1.Baseline, p2.Baseline);
        int TotalWidth = p1.Width - inside + p2.Width;
        int Width = Math.Min(p1.Width - inside + p2.Width, maxWidth);
        int Height = Baseline + Math.Max(p1.Height - p1.Baseline, p2.Height - p2.Baseline);

        PixelArray Result = new PixelArray(Width, Height, Baseline);

        for (int x = 0; x < Width; x++)
        {
            bool IsWhite = true;
            int ColoredCount = 0;

            for (int y = 0; y < Height; y++)
                MergePixel(Result, p1, p2, x, y, TotalWidth, Baseline, ref IsWhite, ref ColoredCount);

            Result.IsWhiteColumn[x] = IsWhite;
            Result.ColoredCountColumn[x] = ColoredCount;
        }

        return Result;
    }

    public static PixelArray Cut(PixelArray p1, int endCutoff)
    {
        if (endCutoff <= 0 || p1.Width <= endCutoff * 2)
            return p1;

        PixelArray Result = new PixelArray(p1.Width - endCutoff, p1.Height, p1.Baseline);

        for (int x = 0; x < Result.Width; x++)
        {
            bool IsWhite = true;
            int ColoredCount = 0;

            for (int y = 0; y < p1.Height; y++)
                CopyPixel(p1, x, y, Result, x, y, ref IsWhite, ref ColoredCount);

            Result.IsWhiteColumn[x] = IsWhite;
            Result.ColoredCountColumn[x] = ColoredCount;
        }

        return Result;
    }

    public PixelArray GetLeftSide(int leftWidth)
    {
        PixelArray Result = new PixelArray(leftWidth, Height, Baseline);

        for (int x = 0; x < leftWidth; x++)
        {
            bool IsWhite = true;
            int ColoredCount = 0;

            for (int y = 0; y < Height; y++)
                CopyPixel(this, x, y, Result, x, y, ref IsWhite, ref ColoredCount);

            Result.IsWhiteColumn[x] = IsWhite;
            Result.ColoredCountColumn[x] = ColoredCount;
        }

        return Result;
    }

    public PixelArray GetRightSide(int rightWidth)
    {
        PixelArray Result = new PixelArray(rightWidth, Height, Baseline);

        for (int x = 0; x < rightWidth; x++)
        {
            bool IsWhite = true;
            int ColoredCount = 0;

            for (int y = 0; y < Height; y++)
                CopyPixel(this, Width - rightWidth + x, y, Result, x, y, ref IsWhite, ref ColoredCount);

            Result.IsWhiteColumn[x] = IsWhite;
            Result.ColoredCountColumn[x] = ColoredCount;
        }

        return Result;
    }

    public static PixelArray Enlarge(PixelArray p1, PixelArray p2)
    {
        if (p1.Baseline >= p2.Baseline && p1.Height - p1.Baseline >= p2.Height - p2.Baseline)
            return p1;

        int TopHeight = Math.Max(p2.Baseline - p1.Baseline, 0);
        int BottomHeight = Math.Max((p2.Height - p2.Baseline) - (p1.Height - p1.Baseline), 0);
        Debug.Assert(TopHeight > 0 || BottomHeight > 0);

        PixelArray Result = new PixelArray(p1.Width, TopHeight + p1.Height + BottomHeight, p1.Baseline + TopHeight);

        for (int x = 0; x < p1.Width; x++)
        {
            bool IsWhite = true;
            int ColoredCount = 0;

            for (int y = 0; y < TopHeight; y++)
                ClearPixel(Result, x, y);

            for (int y = 0; y < p1.Height; y++)
                CopyPixel(p1, x, y, Result, x, TopHeight + y, ref IsWhite, ref ColoredCount);

            for (int y = 0; y < BottomHeight; y++)
                ClearPixel(Result, x, TopHeight + p1.Height + y);

            Result.IsWhiteColumn[x] = IsWhite;
            Result.ColoredCountColumn[x] = ColoredCount;
        }

        return Result;
    }
}
