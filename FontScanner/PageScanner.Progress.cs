namespace FontScanner;

using FontLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Rectangle = System.Drawing.Rectangle;
using Bitmap = System.Drawing.Bitmap;
using System.Windows.Input;
using System.Threading;

public static partial class PageScanner
{
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

        for (; ; )
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
