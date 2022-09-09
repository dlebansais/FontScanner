namespace FontScanner;

using FontLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Rectangle = System.Drawing.Rectangle;
using Bitmap = System.Drawing.Bitmap;
using System.Windows.Input;
using System.Threading;

public partial class PageScanner
{
    private static void UpdateFigureList(Page page, Rectangle rect, ref bool isFirstFigure)
    {
        if (isFirstFigure)
        {
            page.FigureList.Add(rect);
            isFirstFigure = false;
        }
        else
        {
            Rectangle PreviousRect = new();
            int RectIndex = -1;

            for (int i = 0; i < page.FigureList.Count; i++)
            {
                Rectangle OtherRect = page.FigureList[i];
                if (OtherRect.Bottom <= rect.Top && (RectIndex < 0 || PreviousRect.Bottom < OtherRect.Bottom))
                {
                    PreviousRect = OtherRect;
                    RectIndex = i;
                }
            }

            int Left, Top, Width, Height;
            Rectangle MergedRect;

            if (RectIndex >= 0)
            {
                if (CanMergeRect(page, PreviousRect, rect))
                {
                    Left = PreviousRect.Left < rect.Left ? PreviousRect.Left : rect.Left;
                    Width = (PreviousRect.Right > rect.Right ? PreviousRect.Right : rect.Right) - Left;
                    Top = PreviousRect.Top;
                    Height = rect.Bottom - Top;

                    MergedRect = new(Left, Top, Width, Height);
                    page.FigureList[RectIndex] = MergedRect;
                }
                else
                    page.FigureList.Add(rect);
            }
            else
                page.FigureList.Add(rect);
        }
    }

    private static bool CanMergeRect(Page page, Rectangle rect1, Rectangle rect2)
    {
        using Bitmap Bitmap1 = page.PageImage.ToBitmapSidesClipped(rect1, out int LeftMargin1, out int RightMargin1);
        bool IsLeftImage1 = LeftMargin1 >= RightMargin1 * 5;
        bool IsRightImage1 = RightMargin1 >= LeftMargin1 * 5;
        using Bitmap Bitmap2 = page.PageImage.ToBitmapSidesClipped(rect2, out int LeftMargin2, out int RightMargin2);
        bool IsLeftImage2 = LeftMargin2 >= RightMargin2 * 5;
        bool IsRightImage2 = RightMargin2 >= LeftMargin2 * 5;

        if ((IsLeftImage1 && IsRightImage2) || (IsLeftImage2 && IsRightImage1))
            return false;
        else
            return true;
    }

    private static void MergeFigureList(Page page, ref bool isScanComplete)
    {
        List<Rectangle> FigureList = new(page.FigureList);
        List<ScanLine> LineList = page.LineList;

        FigureList.Sort((Rectangle rect1, Rectangle rect2) => rect1.Top - rect2.Top);

        int i = 0;
        while (i + 1 < FigureList.Count)
        {
            bool CanMerge = true;
            Rectangle Rect1 = FigureList[i];
            Rectangle Rect2 = FigureList[i + 1];

            if (!CanMergeRect(page, Rect1, Rect2))
                CanMerge = false;

            foreach (ScanLine Line in LineList)
            {
                int TextOffset = Line.Rect.Top;

                if (TextOffset >= Rect1.Bottom && TextOffset <= Rect2.Top)
                {
                    CanMerge = false;
                    break;
                }
            }

            if (CanMerge)
            {
                int Left = Rect1.Left < Rect2.Left ? Rect1.Left : Rect2.Left;
                int Width = (Rect1.Right > Rect2.Right ? Rect1.Right : Rect2.Right) - Left;
                int Top = Rect1.Top;
                int Height = Rect2.Bottom - Top;

                FigureList[i] = new Rectangle(Left, Top, Width, Height);
                FigureList.RemoveAt(i + 1);
            }
            else
                i++;
        }

        page.FigureList.Clear();
        page.FigureList.AddRange(FigureList);

        if (!isScanComplete && page.FigureList.Count == 1 && page.LineList.Count == 0)
            isScanComplete = true;
    }

    public static bool ScanProgress(ref int sectionIndex, Font font, Page page)
    {
        ScanLine ProgressLine = page.ProgressLine;
        Rectangle Rect = ProgressLine.Rect;
        int ExpectedLineHeight = font.ProgressTable['/'].Height;

        if (Rect.Height < ExpectedLineHeight)
            return false;

        string ProgressString = string.Empty;

        int Left = 0;
        while (Left < Rect.Width && page.PageImage.IsWhiteColumn(Rect, Left))
            Left++;

        int Right = Rect.Width;
        int? VerticalOffset = null;

        for (;;)
        {
            while (Right > Left && page.PageImage.IsWhiteColumn(Rect, Right - 1))
                Right--;

            if (Right <= Left)
                break;

            PixelArray LineArray = page.PageImage.GetPixelArray(Rect.Left + Left, Rect.Top, Right - Left, Rect.Height, Rect.Height - 1, forbidGrayscale: false);
            //DebugPrintArray(LineArray);

            if (!ScanProgressDigit(font, LineArray, ref VerticalOffset, out char Digit))
                break;

            ProgressString = $"{Digit}{ProgressString}";
            Right -= font.ProgressTable[Digit].Width;
        }

        string[] Splitted = ProgressString.Split('/');
        if (Splitted.Length != 2)
            return false;

        if (!int.TryParse(Splitted[0].Trim(), out int Progress))
            return false;

        if (!int.TryParse(Splitted[1].Trim(), out int Total))
            return false;

        if (Progress <= 0 || Progress > Total)
            return false;

        page.SetProgress(sectionIndex, Progress, Total);

        if (Progress == Total)
            sectionIndex++;

        return true;
    }

    public static bool ScanProgressDigit(Font font, PixelArray lineArray, ref int? verticalOffset, out char digit)
    {
        digit = '\0';

        foreach (KeyValuePair<char, PixelArray> Entry in font.ProgressTable)
        {
            PixelArray Array = Entry.Value;
            //DebugPrintArray(Array);

            bool IsMatch;
            if (Array.Width <= lineArray.Width)
            {
                if (verticalOffset is null)
                {
                    IsMatch = false;
                    int HeightDifference = Math.Abs(Array.Height - lineArray.Height);

                    for (int i = -HeightDifference; i <= HeightDifference; i++)
                        if (PixelArrayHelper.IsRightMatch(Array, lineArray, i))
                        {
                            IsMatch = true;
                            verticalOffset = i;
                            break;
                        }
                }
                else
                    IsMatch = PixelArrayHelper.IsRightMatch(Array, lineArray, (int)verticalOffset);

                if (IsMatch)
                {
                    digit = Entry.Key;
                    return true;
                }
            }
        }

        return false;
    }
}
