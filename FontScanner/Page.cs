namespace FontScanner;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;

public class Page
{
    public Page(PageImage pageImage, Assembly fontAssembly, string fontNamespace, int sideMargin, bool checkExcludedLetter)
    {
        PageImage = pageImage;

        try
        {
            FillBigLetterList(fontAssembly, fontNamespace);
        }
        catch (Exception ex)
        {
            using FileStream Stream = new("test.txt", FileMode.Create, FileAccess.Write);
            using StreamWriter Writer = new(Stream);
            Writer.WriteLine(ex.Message);
        }

        GetPageTopAndBottom(PageImage, out int TopY, out int BottomY);
        GetContentTopAndBottom(PageImage, ref TopY, ref BottomY, out int TitleTop, out int TitleBottom, out int ProgressTop, out int ProgressBottom);
        PageTop = TitleBottom;
        int TitleLineHeight = TitleBottom - TitleTop;
        TitleLine = new() { Rect = new(0, TitleTop, PageImage.Width, TitleLineHeight), Baseline = TitleLineHeight };
        int ProgressLineHeight = ProgressBottom - ProgressTop;
        ProgressLine = new() { Rect = new(0, ProgressTop, PageImage.Width, ProgressLineHeight), Baseline = ProgressLineHeight - 1 };
        ZoneOfInterest = GetZoneOfInterest(PageImage, TopY, BottomY, sideMargin);
        ExcludedLetter = GetExcludedLetter(PageImage, checkExcludedLetter);
        RectangleList = GetRectangleList(PageImage, ZoneOfInterest, ExcludedLetter.Location);
        LineList = GetScanLineList(PageImage, RectangleList, ExcludedLetter.Location);
        WordList = ExtractWordsAndFigures(PageImage, ZoneOfInterest, LineList, ExcludedLetter.Location);
        FigureList = GetFigureList(PageImage, RectangleList);
    }

    private void FillBigLetterList(Assembly fontAssembly, string fontNamespace)
    {
        if (BigLetterList.Count > 0)
            return;

        using Stream LetterStream = fontAssembly.GetManifestResourceStream($"{fontNamespace}.LetterResources.Letters.png");
        using Bitmap LetterBitmap = new(LetterStream);

        const int LetterCount = 26;
        const char FirstLetter = 'A';
        int CellLength = LetterBitmap.Width / LetterCount;
        Debug.Assert(CellLength > 1);
        int RowCount = LetterBitmap.Height / CellLength;
        Debug.Assert(RowCount > 0);

        for (int i = 0; i < LetterCount; i++)
        {
            char Text = (char)(FirstLetter + i);

            for (int j = 0; j < RowCount; j++)
            {
                Rectangle Rect = new((i * CellLength) + 1, j * CellLength, CellLength - 1, CellLength);
                PixelArray LetterArray = PixelArray.FromBitmap(LetterBitmap, Rect);
                LetterArray = LetterArray.Clipped();

                BigLetter NewBigLetter = new(Text, LetterArray);
                BigLetterList.Add(NewBigLetter);
            }
        }
    }

    private static List<BigLetter> BigLetterList = new();

    public PageImage PageImage { get; }
    public Rectangle ZoneOfInterest { get; }
    public BigLetter ExcludedLetter { get; private set; }
    public ScanLine TitleLine { get; }
    public ScanLine ProgressLine { get; }
    public List<Rectangle> RectangleList { get; }
    public List<ScanLine> LineList { get; }
    public List<ScanWord> WordList { get; }
    public List<Rectangle> FigureList { get; }
    public int SectionIndex { get; private set; }
    public int Progress { get; private set; }
    public int Total { get; private set; }
    public int PageTop { get; }

    private const int MaxLineHeight = 77;

    private static Rectangle GetZoneOfInterest(PageImage pageImage, int topY, int bottomY, int sideMargin)
    {
        int LeftX = sideMargin;
        int RightX = pageImage.Width - sideMargin;

        int x, y;

        for (x = LeftX; x < RightX; x++)
        {
            for (y = topY; y < bottomY; y++)
                if (!pageImage.IsWhitePixel(x, y))
                    break;

            if (y < bottomY)
            {
                LeftX = x;
                break;
            }
        }

        for (x = RightX; x > LeftX; x--)
        {
            for (y = topY; y < bottomY; y++)
                if (!pageImage.IsWhitePixel(x - 1, y))
                    break;

            if (y < bottomY)
            {
                RightX = x;
                break;
            }
        }

        for (y = topY; y < bottomY; y++)
        {
            for (x = LeftX; x < RightX; x++)
                if (!pageImage.IsWhitePixel(x, y))
                    break;

            if (x < RightX)
            {
                topY = y;
                break;
            }
        }

        for (y = bottomY; y > topY; y--)
        {
            for (x = LeftX; x < RightX; x++)
                if (!pageImage.IsWhitePixel(x, y - 1))
                    break;

            if (x < RightX)
            {
                bottomY = y;
                break;
            }
        }

        Debug.WriteLine($"Zone of interest: {LeftX}, {topY}, {RightX - LeftX}, {bottomY - topY}");

        return new Rectangle(LeftX, topY, RightX - LeftX, bottomY - topY);
    }

    private static void GetPageTopAndBottom(PageImage pageImage, out int top, out int bottom)
    {
        top = pageImage.Height / 2;
        bottom = top;

        while (top > 0 && pageImage.IsWhitePixel(0, top - 1))
            top--;

        while (bottom < pageImage.Height && pageImage.IsWhitePixel(0, bottom))
            bottom++;
    }

    private static void GetContentTopAndBottom(PageImage pageImage, ref int top, ref int bottom, out int titleTop, out int titleBottom, out int progressTop, out int progressBottom)
    {
        Rectangle Rect = new(0, 0, pageImage.Width, pageImage.Height);

        DownToLineTop(pageImage, Rect, bottom, ref top);
        titleTop = top;
        DownToLineBottom(pageImage, Rect, bottom, ref top);
        titleBottom = top;
        DownToLineTop(pageImage, Rect, bottom, ref top);

        UpToLineBottom(pageImage, Rect, top, ref bottom);
        progressBottom = bottom;
        UpToLineTop(pageImage, Rect, top, ref bottom);
        progressTop = bottom;
        UpToLineBottom(pageImage, Rect, top, ref bottom);
    }

    private static void DownToLineTop(PageImage pageImage, Rectangle rect, int max, ref int y)
    {
        while (y < max && pageImage.IsWhiteLine(rect, y))
            y++;
    }

    private static void DownToLineBottom(PageImage pageImage, Rectangle rect, int max, ref int y)
    {
        while (y < max && !pageImage.IsWhiteLine(rect, y))
            y++;
    }

    private static void UpToLineBottom(PageImage pageImage, Rectangle rect, int min, ref int y)
    {
        while (y > min + 1 && pageImage.IsWhiteLine(rect, y - 1))
            y--;
    }

    private static void UpToLineTop(PageImage pageImage, Rectangle rect, int min, ref int y)
    {
        while (y > min + 1 && !pageImage.IsWhiteLine(rect, y - 1))
            y--;
    }

    private static BigLetter GetExcludedLetter(PageImage pageImage, bool checkExcludedLetter)
    {
        if (!checkExcludedLetter)
            return BigLetter.None;
        
        PixelArray FullArray = pageImage.GetPixelArray(0, 0, pageImage.Width, pageImage.Height, 0, forbidGrayscale: false);

        foreach (BigLetter BigLetter in BigLetterList)
        {
            PixelArray LetterArray = BigLetter.LetterArray;
            if (LetterArray.Width == 0 || LetterArray.Height == 0)
                continue;

            if (BigLetter.Text == 'F')
            {
            }

            int LetterWidth = LetterArray.Width;
            int LetterHeight = LetterArray.Height;

            int ColoredX = 0;
            int ColoredY = 0;
            bool HasColor = false;
            byte Color = 0;

            for (ColoredX = 0; ColoredX < LetterWidth && !HasColor; ColoredX++)
                for (ColoredY = 0; ColoredY < LetterHeight && !HasColor; ColoredY++)
                    if (LetterArray.IsColored(ColoredX, ColoredY, out Color))
                        HasColor = true;

            if (!HasColor)
                continue;

            // Compensate for ++ at the end of loops
            ColoredX--;
            ColoredY--;

            for (int x = 0; x < pageImage.Width - LetterWidth; x++)
                for (int y = 0; y < pageImage.Height - LetterHeight; y++)
                {
                    if (FullArray.IsColored(x + ColoredX, y + ColoredY, out byte OtherColor) && Color == OtherColor)
                    {
                        PixelArray Array = pageImage.GetGrayscalePixelArray(x, y, LetterWidth, LetterHeight, LetterHeight - 1);
                        if (PixelArray.IsPixelToPixelMatch(LetterArray, Array))
                        {
                            BigLetter Result = BigLetter;
                            Result.Location = new Rectangle(x, y, LetterWidth, LetterHeight);
                            return Result;
                        }
                    }
                }

        }

        return BigLetter.None;
    }

    private static List<Rectangle> GetRectangleList(PageImage pageImage, Rectangle zoneOfInterest, Rectangle excludedRectangle)
    {
        List<Rectangle> RectangleList = new();

        int Offset = 0;
        int Height = 10;

        while (Offset + Height < zoneOfInterest.Height)
        {
            while (Offset + Height < zoneOfInterest.Height && !pageImage.IsWhiteLine(zoneOfInterest, excludedRectangle, Offset + Height))
                Height++;

            Rectangle Rect = new Rectangle(zoneOfInterest.Left, zoneOfInterest.Top + Offset, zoneOfInterest.Width, Height);
            RectangleList.Add(Rect);

            Offset += Height;
            Height = 0;

            while (Offset + Height < zoneOfInterest.Height && pageImage.IsWhiteLine(zoneOfInterest, excludedRectangle, Offset + Height))
                Height++;

            Offset += Height;
            Height = 10;
        }

        return RectangleList;
    }

    private static List<ScanLine> GetScanLineList(PageImage pageImage, List<Rectangle> rectangleList, Rectangle excludedRectangle)
    {
        List<ScanLine> ScanLineList = new();

        int LineNumber = 0;

        foreach (Rectangle Rect in rectangleList)
            if (Rect.Height <= MaxLineHeight)
            {
                Rectangle CleanRect = EliminateUnderline(pageImage, Rect);
                int Baseline = FindBaseline(pageImage, CleanRect, excludedRectangle);

                ScanLineList.Add(new ScanLine() { LineNumber = LineNumber, Rect = CleanRect, Baseline = Baseline });
                LineNumber++;
            }

        List<ScanLine> ListSortedByHeight = new(ScanLineList);
        ListSortedByHeight.Sort(SortByHeight);
        int TopHeightCount = ScanLineList.Count / 4;
        if (TopHeightCount == 0 && ScanLineList.Count > 0)
            TopHeightCount = 1;

        for (int i = 0; i < TopHeightCount; i++)
            ListSortedByHeight.RemoveAt(0);

        int TotalHeight = 0;
        foreach (ScanLine Item in ListSortedByHeight)
            TotalHeight += Item.Rect.Height;
        int AverageHeight = ListSortedByHeight.Count > 0 ? TotalHeight / ListSortedByHeight.Count : 0;
        int MaxMergedHeight = AverageHeight * 2;

        for (int i = 0; i < ScanLineList.Count; i++)
        {
            ScanLine Line = ScanLineList[i];

            if (i > 0 && i + 1 < ScanLineList.Count)
            {
                ScanLine PreviousLine = ScanLineList[i - 1];
                ScanLine NextLine = ScanLineList[i + 1];

                if (NextLine.Rect.Bottom - Line.Rect.Top <= PreviousLine.Rect.Height && PreviousLine.Rect.Height <= MaxMergedHeight)
                {
                    int MinLeft = NextLine.Rect.Left < Line.Rect.Left ? NextLine.Rect.Left : Line.Rect.Left;
                    int MinRight = NextLine.Rect.Right > Line.Rect.Right ? NextLine.Rect.Right : Line.Rect.Right;
                    Rectangle Rect = new Rectangle(MinLeft, Line.Rect.Top, MinRight - MinLeft, NextLine.Rect.Bottom - Line.Rect.Top);
                    int Baseline = FindBaseline(pageImage, Rect, excludedRectangle);

                    Line = new ScanLine() { LineNumber = NextLine.LineNumber, Rect = Rect, Baseline = Baseline };

                    ScanLineList.RemoveAt(i);
                    ScanLineList[i] = Line;
                    i--;
                }
                else if (Line.Rect.Bottom - PreviousLine.Rect.Top <= NextLine.Rect.Height && NextLine.Rect.Height <= MaxMergedHeight)
                {
                    int MinLeft = PreviousLine.Rect.Left < Line.Rect.Left ? PreviousLine.Rect.Left : Line.Rect.Left;
                    int MinRight = PreviousLine.Rect.Right > Line.Rect.Right ? PreviousLine.Rect.Right : Line.Rect.Right;
                    Rectangle Rect = new Rectangle(MinLeft, PreviousLine.Rect.Top, MinRight - MinLeft, Line.Rect.Bottom - PreviousLine.Rect.Top);
                    int Baseline = FindBaseline(pageImage, Rect, excludedRectangle);

                    Line = new ScanLine() { LineNumber = Line.LineNumber, Rect = Rect, Baseline = Baseline };

                    i--;
                    ScanLineList.RemoveAt(i);
                    ScanLineList[i] = Line;
                }
            }
        }

        return ScanLineList;
    }

    private static int SortByHeight(ScanLine l1, ScanLine l2)
    {
        return l2.Rect.Height - l1.Rect.Height;
    }

    private static Rectangle EliminateUnderline(PageImage pageImage, Rectangle rect)
    {
        for(;;)
        {
            int xLeft, xRight;
            for (xLeft = 0; xLeft < rect.Width; xLeft++)
                if (!pageImage.IsWhitePixel(rect.Left + xLeft, rect.Bottom - 1))
                    break;
            for (xRight = rect.Width; xRight > 0; xRight--)
                if (!pageImage.IsWhitePixel(rect.Left + xRight - 1, rect.Bottom - 1))
                    break;

            if (xRight < xLeft + 43)
                break;

            bool IsUnderline = true;
            for (int x = xLeft; x < xRight; x++)
                if (pageImage.IsWhitePixel(rect.Left + x, rect.Bottom - 1))
                {
                    IsUnderline = false;
                    break;
                }

            if (!IsUnderline)
                break;

            rect = new Rectangle(rect.Left, rect.Top, rect.Width, rect.Height - 1);
            if (rect.Height == 0)
                break;
        }

        return rect;
    }

    private static int FindBaseline(PageImage pageImage, Rectangle rect, Rectangle excludedRectangle)
    {
        int[] BlackCount = new int[rect.Height];

        for (int i = 0; i < rect.Height; i++)
        {
            int y = rect.Top + i;

            for (int j = 0; j < rect.Width; j++)
            {
                int x = rect.Left + j;

                if (!excludedRectangle.Contains(x, y))
                {
                    if (!pageImage.IsWhitePixel(x, y))
                        BlackCount[i]++;
                }
            }
        }

        int MaxBlackCount = 0;
        for (int i = 0; i < rect.Height; i++)
            if (MaxBlackCount < BlackCount[i])
                MaxBlackCount = BlackCount[i];

        int BaselineCount = MaxBlackCount / 4;
        int Baseline = rect.Height;

        while (Baseline > 0)
            if (BlackCount[--Baseline] >= BaselineCount)
                break;

        return Baseline;
    }

    private static List<ScanWord> ExtractWordsAndFigures(PageImage pageImage, Rectangle zoneOfInterest, List<ScanLine> scanLineList, Rectangle excludedRectangle)
    {
        List<ScanWord> ScanWordList = new();

        foreach (ScanLine Line in scanLineList)
        {
            ExtractLineWordsAndFiguresNormal(pageImage, Line, excludedRectangle);

            ScanWordList.AddRange(Line.Words);
        }

        return ScanWordList;
    }

    private static void ExtractLineWordsAndFiguresNormal(PageImage pageImage, ScanLine line, Rectangle excludedRectangle)
    {
        Rectangle Rect = line.Rect;
        Rectangle BaselineRectangle = new Rectangle(Rect.Left, Rect.Top, Rect.Width, line.Baseline);

        int WordOffset = 0;
        while (WordOffset < Rect.Width && pageImage.IsWhiteColumn(Rect, excludedRectangle, WordOffset))
            WordOffset++;

        int LetterOffset = WordOffset;
        List<LetterOffset> LetterOffsetList = new();

        while (LetterOffset < Rect.Width)
        {
            int LetterWidth = 0;
            while (LetterOffset + LetterWidth < Rect.Width && !pageImage.IsWhiteColumn(Rect, excludedRectangle, LetterOffset + LetterWidth))
                LetterWidth++;

            int BaselineWhitespaceWidth = 0;
            while (LetterOffset + LetterWidth + BaselineWhitespaceWidth < Rect.Width && pageImage.IsWhiteColumn(BaselineRectangle, excludedRectangle, LetterOffset + LetterWidth + BaselineWhitespaceWidth))
                BaselineWhitespaceWidth++;

            int WhitespaceWidth = 0;
            while (LetterOffset + LetterWidth + WhitespaceWidth < Rect.Width && pageImage.IsWhiteColumn(Rect, excludedRectangle, LetterOffset + LetterWidth + WhitespaceWidth))
                WhitespaceWidth++;

            LetterOffsetList.Add(new LetterOffset() { Offset = LetterOffset - WordOffset, LetterWidth = LetterWidth, WhitespaceWidth = WhitespaceWidth });

            if (LetterOffset + LetterWidth + WhitespaceWidth >= Rect.Width || BaselineWhitespaceWidth > Rect.Height / 5)
            {
                Rectangle WordRectangle = new Rectangle(Rect.Left + WordOffset, Rect.Top, LetterOffset + LetterWidth + WhitespaceWidth - WordOffset, Rect.Height);
                ScanWord NewScanWord = new ScanWord(WordRectangle, line);
                NewScanWord.LetterOffsetList.AddRange(LetterOffsetList);
                line.Words.Add(NewScanWord);

                LetterOffsetList.Clear();
                WordOffset = LetterOffset + LetterWidth + WhitespaceWidth;
            }

            LetterOffset += LetterWidth + WhitespaceWidth;
        }
    }

    private static List<Rectangle> GetFigureList(PageImage pageImage, List<Rectangle> rectangleList)
    {
        List<Rectangle> FigureList = new();

        foreach (Rectangle Rect in rectangleList)
            if (Rect.Height > MaxLineHeight)
                FigureList.Add(Rect);

        return FigureList;
    }

    public PixelArray GetPixelArray(ScanWord word, LetterOffset letterOffset, bool forbidGrayscale)
    {
        int Left = word.Rect.Left + letterOffset.Offset;
        int Top = word.Rect.Top;
        int Width = letterOffset.LetterWidth;
        int Height = word.Rect.Height;

        return PageImage.GetPixelArray(Left, Top, Width, Height, word.Baseline, forbidGrayscale);
    }

    public PixelArray GetPixelArray(ScanWord word, int startOffset, bool forbidGrayscale)
    {
        int Left = word.Rect.Left + startOffset;
        int Top = word.Rect.Top;
        int Width = word.Rect.Width - startOffset;
        int Height = word.Rect.Height;

        return PageImage.GetPixelArray(Left, Top, Width, Height, word.Baseline, forbidGrayscale);
    }

    public PixelArray GetPixelArray(ScanWord word, int startOffset, int width, bool forbidGrayscale)
    {
        int Left = word.Rect.Left + startOffset;
        int Top = word.Rect.Top;
        int Width = Math.Min(word.Rect.Width - startOffset, width);
        int Height = word.Rect.Height;

        return PageImage.GetPixelArray(Left, Top, Width, Height, word.Baseline, forbidGrayscale);
    }

    public PixelArray GetPixelArray(ScanWord word, Rectangle rect, bool forbidGrayscale)
    {
        return PageImage.GetPixelArray(rect.Left, rect.Top, rect.Width, rect.Height, word.Baseline, forbidGrayscale);
    }

    public void SetProgress(int sectionIndex, int progress, int total)
    {
        Debug.Assert(sectionIndex >= 0 && progress >= 1 && progress <= total);

        SectionIndex = sectionIndex;
        Progress = progress;
        Total = total;
    }
}
