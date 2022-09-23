namespace FontScanner;

using FontLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Rectangle = System.Drawing.Rectangle;
using Bitmap = System.Drawing.Bitmap;
using System.Windows.Input;
using System.Threading;
using System.Globalization;

public static partial class PageScanner
{
    private static void UpdateScannedLineList(List<ScanLine> scannedLineList, ScanLine line, ref bool isFirstFigure)
    {
        scannedLineList.Add(line);

        string LineString = string.Empty;
        foreach (Letter Letter in line.LetterList)
            LineString += Letter.DisplayText;

        Debug.WriteLine(LineString);

        isFirstFigure = true;
    }

    private static void UpdateFigureList(Page page, List<ScanLine> scannedLineList, ScanLine line, ref bool isFirstFigure, ref bool isScanComplete)
    {
        bool HasText = false;
        foreach (ScanWord Word in line.Words)
            foreach (Letter Item in Word.Text)
                if (Item != Letter.Unknown)
                {
                    HasText = true;
                    break;
                }

        if (scannedLineList.Count > 0 || HasText)
            isScanComplete = false;

        UpdateFigureList(page, line.Rect, ref isFirstFigure);
    }

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
}
