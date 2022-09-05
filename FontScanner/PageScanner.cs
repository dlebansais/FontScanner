namespace FontScanner;

using FontLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Rectangle = System.Drawing.Rectangle;
using Bitmap = System.Drawing.Bitmap;

public partial class PageScanner
{
    private static List<int> DoubleVerticalOffsetList = new() { 0, 1, -1, +2, -2, -3, +3, -4, +4, -5, +5 };
    private static List<int> SingleVerticalOffsetList = new() { 0, 1, -1, -3, -2, -4 };

    public static bool Scan(Font font, Page page)
    {
        bool IsScanComplete = true;
        bool IsFirstFigure = false;
        List<ScanLine> LineList = new();

        foreach (ScanLine Line in page.LineList)
        {
            if (Scan(page, Line, font))
            {
                LineList.Add(Line);

                string LineString = string.Empty;
                foreach (ScanWord Word in Line.Words)
                {
                    if (LineString.Length > 0)
                        LineString += " ";
                    LineString += Word.DisplayText;
                }

                Debug.WriteLine(LineString);

                IsFirstFigure = true;
            }
            else
            {
                bool HasText = false;
                foreach (ScanWord Word in Line.Words)
                    foreach (Letter Item in Word.Text)
                        if (Item != Letter.Unknown)
                        {
                            HasText = true;
                            break;
                        }

                if (LineList.Count > 0 || HasText)
                    IsScanComplete = false;

                UpdateFigureList(page, Line.Rect, ref IsFirstFigure);
            }
        }

        page.LineList.Clear();
        page.LineList.AddRange(LineList);

        MergeFigureList(page, ref IsScanComplete);

        return IsScanComplete;
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
            //LineArray.DebugPrint();

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
            //Array.DebugPrint();

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

    private static bool Scan(Page page, ScanLine line, Font font)
    {
        bool IsScanComplete = true;
        int i;

        LastVerticalOffset = 0;

        for (i = 0; i < line.Words.Count; i++)
        {
            ScanWord Word = line.Words[i];
            bool IsLastWord = i == line.Words.Count - 1;

            IsScanComplete &= Scan(page, Word, font, IsLastWord, out bool IsLineScanned);
            if (IsLineScanned)
                break;

            if (!IsScanComplete && StopOnFail)
            {
                Debug.WriteLine($"Aborting section #{page.SectionIndex} page #{page.Progress} line #{line.LineNumber}");
                break;
            }
        }

        i = 0;
        while (i + 1 < line.Words.Count)
        {
            ScanWord PreviousWord = line.Words[i];
            ScanWord NextWord = line.Words[i + 1];

            int SeparatorWidth = 7;

            for (int j = 0; j < PreviousWord.Text.Count; j++)
            {
                Letter Letter = PreviousWord.Text[PreviousWord.Text.Count - j - 1];

                if (Letter != Letter.Whitespace)
                {
                    if (Letter.LetterType.IsItalic)
                        SeparatorWidth = (int)(Letter.LetterType.FontSize * 0.2);
                    else if (Letter.LetterType.IsBold)
                        SeparatorWidth = (int)(Letter.LetterType.FontSize * 0.37);
                    else
                        SeparatorWidth = (int)(Letter.LetterType.FontSize * 0.35);
                    break;
                }
            }

            if (PreviousWord.EffectiveRect.Right + SeparatorWidth >= NextWord.EffectiveRect.Left)
            {
                int Left = PreviousWord.Rect.Left;
                int Top = PreviousWord.Rect.Top < NextWord.Rect.Top ? PreviousWord.Rect.Top : NextWord.Rect.Top;
                int Right = NextWord.Rect.Right;
                int Bottom = PreviousWord.Rect.Bottom > NextWord.Rect.Bottom ? PreviousWord.Rect.Bottom : NextWord.Rect.Bottom;
                Rectangle MergedRect = new(Left, Top, Right - Left, Bottom - Top);

                ScanWord MergedWord = new(MergedRect, line);
                MergedWord.LetterOffsetList.AddRange(PreviousWord.LetterOffsetList);

                for (int j = 0; j < NextWord.LetterOffsetList.Count; j++)
                {
                    LetterOffset NextOffset = NextWord.LetterOffsetList[j];
                    NextOffset.Offset += NextWord.Rect.Left - PreviousWord.Rect.Left;

                    MergedWord.LetterOffsetList.Add(NextOffset);
                }

                MergedWord.Text.AddRange(PreviousWord.Text);
                MergedWord.Text.AddRange(NextWord.Text);

                line.Words[i] = MergedWord;
                line.Words.RemoveAt(i + 1);
            }
            else
                i++;
        }

        return IsScanComplete;
    }

    private static bool Scan(Page page, ScanWord word, Font font, bool isLastWord, out bool isLineScanned)
    {
        isLineScanned = false;

        bool IsScanComplete = true;
        Letter LastLetter = Letter.EmptyNormal;
        int LastMergedWidth = 0;

        for (int i = 0; i < word.LetterOffsetList.Count; i++)
        {
            LetterOffset LetterOffset = word.LetterOffsetList[i];
            bool IsLastLetter = isLastWord && (i == word.LetterOffsetList.Count - 1);

            bool SingleScanSuccess = SingleCharacterScan(page, word, LetterOffset, font, IsLastLetter);

            if (!SingleScanSuccess && i + 1 < word.LetterOffsetList.Count)
            {
                LetterOffset NextLetterOffset = word.LetterOffsetList[i + 1];
                SingleScanSuccess = SingleCharacterScan(page, word, LetterOffset, NextLetterOffset, font);
                if (SingleScanSuccess)
                    i++;
            }

            if (!SingleScanSuccess && i + 2 < word.LetterOffsetList.Count)
            {
                LetterOffset NextLetterOffset = word.LetterOffsetList[i + 1];
                LetterOffset NextNextLetterOffset = word.LetterOffsetList[i + 2];
                SingleScanSuccess = SingleCharacterScan(page, word, LetterOffset, NextLetterOffset, NextNextLetterOffset, font);
                if (SingleScanSuccess)
                    i += 2;
            }

            if (!SingleScanSuccess)
            {
                if (SingleCharLineEndScan(page, word, LetterOffset, font))
                {
                    isLineScanned = true;
                    break;
                }
                else if (MergedWordScan(page, word, LetterOffset, font))
                {
                    break;
                }
                else
                {
                    if (word.Line.Words.IndexOf(word) == 0 && i == 0)
                    {
                        Dictionary<Letter, PixelArray> CharacterTable = font.CharacterTable;
                        List<Letter> PreferredOrder = GetPreferredOrder(CharacterTable);

                        CheckFirstCharacterVerticalOffset(page, word, CharacterTable, PreferredOrder);
                    }

                    if (PartialScan(page, word, i, font, isLastWord, IsLastLetter, ref LastLetter, ref LastMergedWidth, out int LastRight))
                    {
                        while (i + 1 < word.LetterOffsetList.Count && word.LetterOffsetList[i + 1].Offset + word.LetterOffsetList[i + 1].LetterWidth <= LastRight)
                            i++;

                        if (i + 1 < word.LetterOffsetList.Count && word.LetterOffsetList[i + 1].Offset < LastRight)
                        {
                            LetterOffset NextLetterOffset = word.LetterOffsetList[i + 1];
                            int Increase = LastRight - NextLetterOffset.Offset;
                            NextLetterOffset.Offset += Increase;
                            NextLetterOffset.LetterWidth -= Increase;
                            NextLetterOffset.WhitespaceWidth -= Increase;
                            word.LetterOffsetList[i + 1] = NextLetterOffset;
                        }
                    }
                    else
                    {
                        if (WordEndScan(page, word, LetterOffset, isLastWord, font))
                            break;
                        else if (LineEndScan(page, word, LetterOffset, font))
                        {
                            isLineScanned = true;
                            break;
                        }
                        else
                        {
                            word.Text.Add(Letter.Unknown);
                            IsScanComplete = false;
                        }
                    }
                }
            }

            if (!IsScanComplete && StopOnFail)
                break;

            if (word.Text.Count > 0)
            {
                Letter LastWordLetter = word.Text[word.Text.Count - 1];
                LetterType LastLetterType = LastWordLetter.LetterType;
                if (LastLetterType.IsItalic && LetterOffset.WhitespaceWidth >= 7)
                {
                    word.Text.Add(Letter.Whitespace);
                }
            }
        }

        return IsScanComplete;
    }

    private static bool SingleCharacterScan(Page page, ScanWord word, LetterOffset letterOffset, Font font, bool isLastLetter)
    {
        PixelArray LetterArray = page.GetPixelArray(word, letterOffset, forbidGrayscale: false);
        return SingleCharacterScan(word, LetterArray, font, isLastLetter);
    }

    private static bool SingleCharacterScan(Page page, ScanWord word, LetterOffset letterOffset, LetterOffset nextLetterOffset, Font font)
    {
        PixelArray LetterArray = page.GetPixelArray(word, letterOffset.Offset, letterOffset.LetterWidth + letterOffset.WhitespaceWidth + nextLetterOffset.LetterWidth, forbidGrayscale: false);
        return SingleCharacterScan(word, LetterArray, font, isLastLetter: false);
    }

    private static bool SingleCharacterScan(Page page, ScanWord word, LetterOffset letterOffset, LetterOffset nextLetterOffset, LetterOffset nextNextLetterOffset, Font font)
    {
        PixelArray LetterArray = page.GetPixelArray(word, letterOffset.Offset, letterOffset.LetterWidth + letterOffset.WhitespaceWidth + nextLetterOffset.LetterWidth + nextLetterOffset.WhitespaceWidth + nextNextLetterOffset.LetterWidth, forbidGrayscale: false);
        return SingleCharacterScan(word, LetterArray, font, isLastLetter: false);
    }

    private static bool SingleCharacterScan(ScanWord word, PixelArray letterArray, Font font, bool isLastLetter)
    {
        letterArray = letterArray.Clipped();
        Debug.Assert(letterArray != PixelArray.Empty);

        Letter MatchingLetter;
        List<int> VerticalOffsetList = new(SingleVerticalOffsetList);
        VerticalOffsetList.Remove(LastVerticalOffset);
        VerticalOffsetList.Insert(0, LastVerticalOffset);

        foreach (int VerticalOffset in VerticalOffsetList)
        {
            if (IsSingleCharacterMatch(letterArray, font.CharacterTable, VerticalOffset, isLastLetter, out MatchingLetter))
            {
                word.Text.Add(MatchingLetter);
                return true;
            }
        }

        if (IsSingleCharacterBaselineMatch(letterArray, font.CharacterTable, isLastLetter, out MatchingLetter))
        {
            word.Text.Add(MatchingLetter);
            return true;
        }

        return false;
    }

    public static bool IsPreferred(LetterType previousLetterType, LetterType nextLetterType, CharacterPreference characterPreference)
    {
        bool IsPreferred;

        switch (characterPreference)
        {
            case CharacterPreference.Same:
                IsPreferred = previousLetterType.IsItalic == nextLetterType.IsItalic && previousLetterType.IsBold == nextLetterType.IsBold && previousLetterType.IsBlue == nextLetterType.IsBlue && previousLetterType.FontSize == nextLetterType.FontSize;
                break;
            case CharacterPreference.ToggleItalic:
                IsPreferred = previousLetterType.IsItalic != nextLetterType.IsItalic && previousLetterType.IsBold == nextLetterType.IsBold && previousLetterType.IsBlue == nextLetterType.IsBlue && previousLetterType.FontSize == nextLetterType.FontSize;
                break;
            case CharacterPreference.SameFont:
                IsPreferred = (previousLetterType.IsBold != nextLetterType.IsBold || previousLetterType.IsBlue != nextLetterType.IsBlue) && previousLetterType.FontSize == nextLetterType.FontSize;
                break;
            default:
            case CharacterPreference.Other:
                IsPreferred = previousLetterType.FontSize != nextLetterType.FontSize;
                break;
        }

        return IsPreferred;
    }

    private static bool IsSingleCharacterMatch(PixelArray letterArray, Dictionary<Letter, PixelArray> characterTable, int verticalOffset, bool isLastLetter, out Letter matchingLetter)
    {
        return IsSingleCharacterMatch(CharacterPreference.Same, letterArray, characterTable, verticalOffset, isLastLetter, out matchingLetter) ||
               IsSingleCharacterMatch(CharacterPreference.ToggleItalic, letterArray, characterTable, verticalOffset, isLastLetter, out matchingLetter) ||
               IsSingleCharacterMatch(CharacterPreference.SameFont, letterArray, characterTable, verticalOffset, isLastLetter, out matchingLetter) ||
               IsSingleCharacterMatch(CharacterPreference.Other, letterArray, characterTable, verticalOffset, isLastLetter, out matchingLetter);
    }

    private static bool IsSingleCharacterMatch(CharacterPreference characterPreference, PixelArray letterArray, Dictionary<Letter, PixelArray> characterTable, int verticalOffset, bool isLastLetter, out Letter matchingLetter)
    {
        foreach (KeyValuePair<Letter, PixelArray> Entry in characterTable)
        {
            Letter Key = Entry.Key;
            LetterType LetterType = Key.LetterType;
            PixelArray CellArray = Entry.Value;

            if (!IsPreferred(LastLetterType, LetterType, characterPreference))
                continue;

            Debug.Assert(CellArray != PixelArray.Empty);

            if (LetterType.IsItalic && isLastLetter)
            {
                if (CellArray.Width > letterArray.Width)
                {
                    int Cutoff = CellArray.Width - letterArray.Width;
                    CellArray = PixelArrayHelper.Cut(CellArray, Cutoff);
                    CellArray = CellArray.Clipped();
                }
            }

            if (Key.Text == "“" && !LetterType.IsBlue && Key.IsBold && Key.IsItalic && LetterType.FontSize == 29)
            {
                if (DisplayDebug)
                    CellArray.DebugPrint();
                if (DisplayDebug)
                    letterArray.DebugPrint();
            }

            if (PixelArrayHelper.IsMatch(CellArray, letterArray, verticalOffset))
            {
                if (verticalOffset != 0)
                {
                }

                //PixelArray.ProfileMatch(CellArray, letterArray);
                matchingLetter = Key;

                LastLetterType = LetterType;
                LastVerticalOffset = verticalOffset;
                return true;
            }

            if (Key.Text == "4" && verticalOffset == 0) // Special case when the baseline is confusing
            {
                int Diff = CellArray.Height / 4; // 27 -> 21
                if (PixelArrayHelper.IsMatch(CellArray, letterArray, verticalOffset: Diff))
                {
                    //PixelArray.ProfileMatch(CellArray, letterArray);
                    matchingLetter = Key;
                    return true;
                }
            }
        }

        matchingLetter = Letter.Unknown;
        return false;
    }

    private static bool IsSingleCharacterBaselineMatch(PixelArray letterArray, Dictionary<Letter, PixelArray> characterTable, bool isLastLetter, out Letter matchingLetter)
    {
        return IsSingleCharacterBaselineMatch(CharacterPreference.Same, letterArray, characterTable, isLastLetter, out matchingLetter) ||
               IsSingleCharacterBaselineMatch(CharacterPreference.ToggleItalic, letterArray, characterTable, isLastLetter, out matchingLetter) ||
               IsSingleCharacterBaselineMatch(CharacterPreference.SameFont, letterArray, characterTable, isLastLetter, out matchingLetter) ||
               IsSingleCharacterBaselineMatch(CharacterPreference.Other, letterArray, characterTable, isLastLetter, out matchingLetter);
    }

    private static bool IsSingleCharacterBaselineMatch(CharacterPreference characterPreference, PixelArray letterArray, Dictionary<Letter, PixelArray> characterTable, bool isLastLetter, out Letter matchingLetter)
    {
        foreach (KeyValuePair<Letter, PixelArray> Entry in characterTable)
        {
            Letter Key = Entry.Key;
            LetterType LetterType = Key.LetterType;
            PixelArray CellArray = Entry.Value;

            if (!IsPreferred(LastLetterType, LetterType, characterPreference))
                continue;

            Debug.Assert(CellArray != PixelArray.Empty);

            if (LetterType.IsItalic && isLastLetter)
            {
                if (CellArray.Width > letterArray.Width)
                {
                    int Cutoff = CellArray.Width - letterArray.Width;
                    CellArray = PixelArrayHelper.Cut(CellArray, Cutoff);
                    CellArray = CellArray.Clipped();
                }
            }

            if (Key.Text == "“" && !LetterType.IsBlue && !Key.IsBold && !Key.IsItalic && LetterType.FontSize == 28)
            {
                if (DisplayDebug)
                    CellArray.DebugPrint();
                if (DisplayDebug)
                    letterArray.DebugPrint();
            }

            if (PixelArrayHelper.IsPixelToPixelMatch(CellArray, letterArray))
            {
                if (Key.Text == "…" && !LetterType.IsBlue)
                {
                }

                //PixelArray.ProfileMatch(CellArray, letterArray);
                matchingLetter = Key;

                LastLetterType = LetterType;
                LastVerticalOffset = 0;
                return true;
            }
        }

        matchingLetter = Letter.Unknown;
        return false;
    }

    private static int LastVerticalOffset = 0;
    private static LetterType LastLetterType = LetterType.Normal;

    private static bool PartialScan(Page page, ScanWord word, int offsetIndex, Font font, bool isLastWord, bool isLastLetter, ref Letter lastLetter, ref int lastMergedWidth, out int lastRight)
    {
        Letter PreviousLetter = lastLetter;
        int PreviousMergedWidth = lastMergedWidth;
        lastRight = 0;

        LetterOffset letterOffset = word.LetterOffsetList[offsetIndex];

        List<int> VerticalOffsetList = new(DoubleVerticalOffsetList);
        VerticalOffsetList.Remove(LastVerticalOffset);
        VerticalOffsetList.Insert(0, LastVerticalOffset);

        foreach (int VerticalOffset in VerticalOffsetList)
            if (PartialScan(page, word, letterOffset.Offset, letterOffset.Offset + letterOffset.LetterWidth, VerticalOffset, PreviousLetter, PreviousMergedWidth, font, isLastWord, isLastLetter, 0, Letter.Unknown, ref lastLetter, ref lastMergedWidth, out lastRight))
                return true;

        return false;
    }

    private static bool WordEndScan(Page page, ScanWord word, LetterOffset letterOffset, bool isLastWord, Font font)
    {
        int MaxCutoff = 1;

        if (isLastWord)
            MaxCutoff = 3;

        for (int i = 2; i < MaxCutoff; i++)
            if (WordEndScan(page, word, letterOffset, isLastWord, font, i))
                return true;

        return false;
    }

    private static bool WordEndScan(Page page, ScanWord word, LetterOffset letterOffset, bool isLastWord, Font font, int endCutoff)
    {
        PixelArray LetterArray = page.GetPixelArray(word, letterOffset.Offset, forbidGrayscale: false);
        LetterArray = LetterArray.Clipped();
        Debug.Assert(LetterArray != PixelArray.Empty);

        List<int> VerticalOffsetList = new(DoubleVerticalOffsetList);
        VerticalOffsetList.Remove(LastVerticalOffset);
        VerticalOffsetList.Insert(0, LastVerticalOffset);

        Letter MatchingLetter;

        foreach (int VerticalOffset in VerticalOffsetList)
        {
            if (IsSingleCharacterMatch(LetterArray, font.CharacterTable, VerticalOffset, isLastLetter: isLastWord, out MatchingLetter))
            {
                word.Text.Add(MatchingLetter);
                return true;
            }
        }

        if (IsSingleCharacterBaselineMatch(LetterArray, font.CharacterTable, isLastLetter: isLastWord, out MatchingLetter))
        {
            word.Text.Add(MatchingLetter);
            return true;
        }

        Letter LastLetter = Letter.EmptyNormal;
        int LastMergedWidth = 0;

        Letter PreviousLetter = Letter.EmptyNormal;
        int PreviousMergedWidth = 0;

        /*
        List<Letter> Text = word.Text;
        if (Text.Count > 0 && Text[Text.Count - 1].IsItalic)
            PreviousLetter = Letter.EmptyItalic;
        */

        foreach (int VerticalOffset in VerticalOffsetList)
            if (PartialScan(page, word, letterOffset.Offset, word.EffectiveWidth, VerticalOffset, PreviousLetter, PreviousMergedWidth, font, isLastWord, true, endCutoff, Letter.Unknown, ref LastLetter, ref LastMergedWidth, out _))
                return true;

        return false;
    }

    private static bool SingleCharLineEndScan(Page page, ScanWord word, LetterOffset letterOffset, Font font)
    {
        ScanLine Line = word.Line;
        ScanWord LastWord = Line.Words[Line.Words.Count - 1];
        if (word == LastWord)
            return false;

        int Left = word.Rect.Left + letterOffset.Offset;
        int Top = word.Rect.Top;
        int Width = LastWord.Rect.Left + LastWord.EffectiveWidth - Left;
        int Height = word.Rect.Height;
        Rectangle Rect = new(Left, Top, Width, Height);

        PixelArray LetterArray = page.GetPixelArray(word, Rect, forbidGrayscale: false);
        LetterArray = LetterArray.Clipped();
        Debug.Assert(LetterArray != PixelArray.Empty);

        List<int> VerticalOffsetList = new(DoubleVerticalOffsetList);
        VerticalOffsetList.Remove(LastVerticalOffset);
        VerticalOffsetList.Insert(0, LastVerticalOffset);

        Letter MatchingLetter;

        foreach (int VerticalOffset in VerticalOffsetList)
        {
            if (IsSingleCharacterMatch(LetterArray, font.CharacterTable, VerticalOffset, isLastLetter: true, out MatchingLetter))
            {
                word.Text.Add(MatchingLetter);
                return true;
            }
        }

        if (IsSingleCharacterBaselineMatch(LetterArray, font.CharacterTable, isLastLetter: true, out MatchingLetter))
        {
            word.Text.Add(MatchingLetter);
            return true;
        }

        return false;
    }

    private static bool MergedWordScan(Page page, ScanWord word, LetterOffset letterOffset, Font font)
    {
        ScanLine Line = word.Line;
        int Index = Line.Words.IndexOf(word);

        if (Index + 1 < Line.Words.Count)
        {
            ScanWord NextWord = Line.Words[Index + 1];

            int Left = word.Rect.Left + letterOffset.Offset;
            int Top = word.Rect.Top;
            int Width = NextWord.Rect.Left + NextWord.EffectiveWidth - Left;
            int Height = word.Rect.Height;
            Rectangle Rect = new(Left, Top, Width, Height);

            PixelArray RemainingWordArray = page.GetPixelArray(word, Rect, forbidGrayscale: false);
            RemainingWordArray = RemainingWordArray.Clipped();
            if (DisplayDebug)
                RemainingWordArray.DebugPrint();

            bool IsDetected = false;
            Letter MatchingLetter;

            if (IsSingleCharacterMatch(RemainingWordArray, font.CharacterTable, 0, isLastLetter: false, out MatchingLetter))
                IsDetected = true;
            else if (LastVerticalOffset != 0 &&  IsSingleCharacterMatch(RemainingWordArray, font.CharacterTable, LastVerticalOffset, isLastLetter: false, out MatchingLetter))
                IsDetected = true;
            else if (IsSingleCharacterBaselineMatch(RemainingWordArray, font.CharacterTable, isLastLetter: false, out MatchingLetter))
                IsDetected = true;

            if (IsDetected)
            {
                word.Text.Add(MatchingLetter);
                Line.Words.RemoveAt(Index + 1);
                return true;
            }
        }

        if (Index + 2 < Line.Words.Count)
        {
            ScanWord NextNextWord = Line.Words[Index + 2];

            int Left = word.Rect.Left + letterOffset.Offset;
            int Top = word.Rect.Top;
            int Width = NextNextWord.Rect.Left + NextNextWord.EffectiveWidth - Left;
            int Height = word.Rect.Height;
            Rectangle Rect = new(Left, Top, Width, Height);

            PixelArray RemainingWordArray = page.GetPixelArray(word, Rect, forbidGrayscale: false);
            RemainingWordArray = RemainingWordArray.Clipped();
            if (DisplayDebug)
                RemainingWordArray.DebugPrint();

            bool IsDetected = false;
            Letter MatchingLetter;

            if (IsSingleCharacterMatch(RemainingWordArray, font.CharacterTable, 0, isLastLetter: false, out MatchingLetter))
                IsDetected = true;
            else if (LastVerticalOffset != 0 && IsSingleCharacterMatch(RemainingWordArray, font.CharacterTable, LastVerticalOffset, isLastLetter: false, out MatchingLetter))
                IsDetected = true;
            else if (IsSingleCharacterBaselineMatch(RemainingWordArray, font.CharacterTable, isLastLetter: false, out MatchingLetter))
                IsDetected = true;

            if (IsDetected)
            {
                word.Text.Add(MatchingLetter);
                Line.Words.RemoveAt(Index + 1);
                Line.Words.RemoveAt(Index + 1);
                return true;
            }
        }

        return false;
    }

    private static bool LineEndScan(Page page, ScanWord word, LetterOffset letterOffset, Font font)
    {
        ScanLine Line = word.Line;
        ScanWord LastWord = Line.Words[Line.Words.Count - 1];
        if (word == LastWord)
            return false;

        int Left = word.Rect.Left + letterOffset.Offset;
        int Top = word.Rect.Top;
        int Width = LastWord.Rect.Left + LastWord.EffectiveWidth - Left;
        int Height = word.Rect.Height;
        Rectangle Rect = new(Left, Top, Width, Height);

        PixelArray LetterArray = page.GetPixelArray(word, Rect, forbidGrayscale: false);
        LetterArray = LetterArray.Clipped();
        Debug.Assert(LetterArray != PixelArray.Empty);

        Letter LastLetter = Letter.EmptyNormal;
        int LastMergedWidth = 0;

        Letter PreviousLetter = Letter.EmptyNormal;
        int PreviousMergedWidth = 0;

        /*
        List<Letter> Text = word.Text;
        if (Text.Count > 0 && Text[Text.Count - 1].IsItalic)
            PreviousLetter = Letter.EmptyItalic;
        */

        List<int> VerticalOffsetList = new(DoubleVerticalOffsetList);
        VerticalOffsetList.Remove(LastVerticalOffset);
        VerticalOffsetList.Insert(0, LastVerticalOffset);

        foreach (int VerticalOffset in VerticalOffsetList)
            if (PartialScan(page, word, letterOffset.Offset, Width, VerticalOffset, PreviousLetter, PreviousMergedWidth, font, true, true, 0, Letter.Unknown, ref LastLetter, ref LastMergedWidth, out _))
                return true;

        return false;
    }

    private static bool PartialScan(Page page, ScanWord word, int startOffset, int endOffset, int verticalOffset, Letter previousLetter, int previousMergedWidth, Font font, bool isLastWord, bool isLastLetter, int endCutoff, Letter preferredStartLetter, ref Letter lastLetter, ref int lastMergedWidth, out int lastRight)
    {
        bool ForbidGrayscale = previousLetter.LetterType.IsBlue && preferredStartLetter != Letter.Unknown && !preferredStartLetter.LetterType.IsBlue;

        PixelArray RemainingWordArray = page.GetPixelArray(word, startOffset, ForbidGrayscale);
        if (DisplayDebug)
            RemainingWordArray.DebugPrint();

        lastRight = 0;

        if (!TryPartialScan(RemainingWordArray, previousLetter, previousMergedWidth, verticalOffset, font, isLastLetter, endCutoff, preferredStartLetter, out Letter MatchingLetter, out int MergedWidth, out Letter PreferredNextLetter))
            return false;

        word.Text.Add(MatchingLetter);

        //if (MatchingLetter.Text == "…")
        if (MatchingLetter.Text == "t")
        {
        }

        lastLetter = MatchingLetter;
        lastMergedWidth = MergedWidth;

        PixelArray LetterArray = font.CharacterTable[MatchingLetter];
        int NextStartOffset = startOffset + LetterArray.Width - MergedWidth;

        if (NextStartOffset + 3 < endOffset)
        {
            isLastLetter = isLastLetter || (isLastWord && PreferredNextLetter != Letter.Unknown && NextStartOffset + font.CharacterTable[PreferredNextLetter].Width > endOffset);

            return PartialScan(page, word, NextStartOffset, endOffset, LastVerticalOffset, MatchingLetter, MergedWidth, font, isLastWord, isLastLetter, endCutoff, PreferredNextLetter, ref lastLetter, ref lastMergedWidth, out lastRight);
        }
        else
        {
            lastRight = NextStartOffset;
            return true;
        }
    }

    private static bool TryPartialScan(PixelArray remainingWordArray, Letter previousLetter, int previousMergedWidth, int verticalOffset, Font font, bool isLastLetter, int endCutoff, Letter preferredStartLetter, out Letter matchingLetter, out int mergedWidth, out Letter preferredNextLetter)
    {
        Dictionary<Letter, PixelArray> CharacterTable = font.CharacterTable;
        List<Letter> PreferredOrder = GetPreferredOrder(CharacterTable);

        PixelArray PreviousArray = previousLetter.IsEmpty ? PixelArray.Empty : font.CharacterTable[previousLetter];

        if (preferredStartLetter != Letter.Unknown)
        {
            int LetterIndex = PreferredOrder.IndexOf(preferredStartLetter);
            if (LetterIndex > 0)
            {
                PreferredOrder.RemoveAt(LetterIndex);
                PreferredOrder.Insert(0, preferredStartLetter);
            }
        }

        if (TryPartialScan(remainingWordArray, PreviousArray, previousMergedWidth, verticalOffset, CharacterTable, PreferredOrder, isLastLetter, endCutoff, preferredStartLetter, out matchingLetter, out mergedWidth, out preferredNextLetter))
            return true;

        matchingLetter = Letter.EmptyNormal;
        preferredNextLetter = Letter.Unknown;
        return false;
    }

    private static bool TryPartialScan(PixelArray remainingWordArray, PixelArray previousArray, int previousMergedWidth, int verticalOffset, Dictionary<Letter, PixelArray> characterTable, List<Letter> preferredOrder, bool isLastLetter, int endCutoff, Letter preferredStartLetter, out Letter matchingLetter, out int mergedWidth, out Letter preferredNextLetter)
    {
        if (previousArray != PixelArray.Empty)
        {
            PixelArray PreviousArrayRightSide = previousArray.GetRightSide(previousMergedWidth);
            if (DisplayDebug)
                previousArray.DebugPrint();
            if (DisplayDebug)
                PreviousArrayRightSide.DebugPrint();

            if (TryPartialScanSingleCharacter(remainingWordArray, PreviousArrayRightSide, verticalOffset, characterTable, preferredOrder, endCutoff, out matchingLetter, out mergedWidth))
            {
                preferredNextLetter = Letter.Unknown;
                return true;
            }

            if (TryPartialScanDoubleCharacter(remainingWordArray, PreviousArrayRightSide, verticalOffset, characterTable, preferredOrder, isLastLetter, out matchingLetter, out mergedWidth, out preferredNextLetter))
                return true;
        }
        else
        {
            if (TryPartialScanDoubleCharacter(remainingWordArray, verticalOffset, characterTable, preferredOrder, isLastLetter, out matchingLetter, out mergedWidth, out preferredNextLetter))
                return true;
        }

        matchingLetter = Letter.Unknown;
        mergedWidth = 0;
        return false;
    }

    private static bool TryPartialScanSingleCharacter(PixelArray remainingWordArray, PixelArray previousArray, int verticalOffset, Dictionary<Letter, PixelArray> characterTable, List<Letter> preferredOrder, int endCutoff, out Letter matchingLetter, out int mergedWidth)
    {
        return TryPartialScanSingleCharacter(CharacterPreference.Same, remainingWordArray, previousArray, verticalOffset, characterTable, preferredOrder, endCutoff, out matchingLetter, out mergedWidth) ||
               TryPartialScanSingleCharacter(CharacterPreference.ToggleItalic, remainingWordArray, previousArray, verticalOffset, characterTable, preferredOrder, endCutoff, out matchingLetter, out mergedWidth) ||
               TryPartialScanSingleCharacter(CharacterPreference.SameFont, remainingWordArray, previousArray, verticalOffset, characterTable, preferredOrder, endCutoff, out matchingLetter, out mergedWidth) ||
               TryPartialScanSingleCharacter(CharacterPreference.Other, remainingWordArray, previousArray, verticalOffset, characterTable, preferredOrder, endCutoff, out matchingLetter, out mergedWidth);
    }

    private static bool TryPartialScanSingleCharacter(CharacterPreference characterPreference, PixelArray remainingWordArray, PixelArray previousArray, int verticalOffset, Dictionary<Letter, PixelArray> characterTable, List<Letter> preferredOrder, int endCutoff, out Letter matchingLetter, out int mergedWidth)
    {
        mergedWidth = 0;

        foreach (Letter Key in preferredOrder)
        {
            LetterType LetterType = Key.LetterType;
            if (!IsPreferred(LastLetterType, LetterType, characterPreference))
                continue;

            PixelArray CellArray = characterTable[Key];
            PixelArray MergedArray = PixelArrayHelper.Merge(previousArray, CellArray, previousArray.Width);
            int LocalCutoff = endCutoff;

            if (MergedArray.Width > remainingWordArray.Width)
            {
                int MinCutoff = MergedArray.Width - remainingWordArray.Width;

                if (LocalCutoff < MinCutoff)
                    LocalCutoff = MinCutoff;
            }

            MergedArray = PixelArrayHelper.Cut(MergedArray, LocalCutoff);
            MergedArray = PixelArrayHelper.Enlarge(MergedArray, remainingWordArray);

            if (Key.Text == "g" && !Key.LetterType.IsItalic && !Key.LetterType.IsBold && Key.LetterType.IsBlue && Key.LetterType.FontSize == 19.5)
            {
                if (DisplayDebug)
                    MergedArray.DebugPrint();
                if (DisplayDebug)
                    remainingWordArray.DebugPrint();
            }

            if (PixelArrayHelper.IsLeftMatch(MergedArray, remainingWordArray, verticalOffset, out int FirstDiffX))
            {
                matchingLetter = Key;

                if (FirstDiffX >= (MergedArray.Width * 3) / 4)
                    mergedWidth = MergedArray.Width - FirstDiffX;

                LastLetterType = LetterType;
                LastVerticalOffset = verticalOffset;

                return true;
            }
        }

        matchingLetter = Letter.Unknown;
        return false;
    }

    private static void CheckFirstCharacterVerticalOffset(Page page, ScanWord word, Dictionary<Letter, PixelArray> characterTable, List<Letter> preferredOrder)
    {
        bool IsFirstTry = LastLetterType.FontSize == 0;

        if (IsFirstTry)
            LastLetterType = LetterType.WithSizeAndColor(LastLetterType, 29, LastLetterType.IsBlue);

        foreach (int VerticalOffset in DoubleVerticalOffsetList)
            if (CheckFirstCharacterVerticalOffset(page, word, VerticalOffset, characterTable, preferredOrder))
                break;
    }

    private static bool CheckFirstCharacterVerticalOffset(Page page, ScanWord word, int verticalOffset, Dictionary<Letter, PixelArray> characterTable, List<Letter> preferredOrder)
    {
        return CheckFirstCharacterVerticalOffset(page, word, verticalOffset, CharacterPreference.Same, characterTable, preferredOrder) ||
               CheckFirstCharacterVerticalOffset(page, word, verticalOffset, CharacterPreference.ToggleItalic, characterTable, preferredOrder);
    }

    private static bool CheckFirstCharacterVerticalOffset(Page page, ScanWord word, int verticalOffset, CharacterPreference characterPreference, Dictionary<Letter, PixelArray> characterTable, List<Letter> preferredOrder)
    {
        PixelArray RemainingWordArray = page.GetPixelArray(word, 0, forbidGrayscale: false);

        foreach (Letter Key in preferredOrder)
        {
            LetterType LetterType = Key.LetterType;
            if (!IsPreferred(LastLetterType, LetterType, characterPreference))
                continue;

            PixelArray CellArray = characterTable[Key];

            if (Key.Text == "d" && Key.LetterType.IsItalic && !Key.LetterType.IsBold && Key.LetterType.FontSize == 29)
            {
                if (DisplayDebug)
                    CellArray.DebugPrint();
                if (DisplayDebug)
                    RemainingWordArray.DebugPrint();
            }

            double PerfectMatchRatio = 0.0;
            int RightOverlapWidth = (int)(CellArray.Width * 0.2);

            if (PixelArrayHelper.IsLeftDiagonalMatch(CellArray, PerfectMatchRatio, RightOverlapWidth, RemainingWordArray, verticalOffset))
            {
                LastVerticalOffset = verticalOffset;
                return true;
            }
        }

        return false;
    }

    private static Tuple<CharacterPreference, CharacterPreference, bool>[] TryOrderFirst = new[]
    {
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.Same, CharacterPreference.Same, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.Same, CharacterPreference.Same, true),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.ToggleItalic, CharacterPreference.Same, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.ToggleItalic, CharacterPreference.Same, true),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.ToggleItalic, CharacterPreference.ToggleItalic, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.Same, CharacterPreference.ToggleItalic, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.ToggleItalic, CharacterPreference.ToggleItalic, true),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.Same, CharacterPreference.ToggleItalic, true),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.Same, CharacterPreference.SameFont, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.SameFont, CharacterPreference.Same, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.SameFont, CharacterPreference.Same, true),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.Other, CharacterPreference.Same, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.Other, CharacterPreference.Same, true),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.SameFont, CharacterPreference.SameFont, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.Same, CharacterPreference.Other, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.ToggleItalic, CharacterPreference.SameFont, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.ToggleItalic, CharacterPreference.Other, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.SameFont, CharacterPreference.ToggleItalic, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.SameFont, CharacterPreference.Other, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.Other, CharacterPreference.ToggleItalic, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.Other, CharacterPreference.SameFont, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.Other, CharacterPreference.Other, false),
        };
    public static int[] TryOrderFirstStats = new int[TryOrderFirst.Length];

    private static bool TryPartialScanDoubleCharacter(PixelArray remainingWordArray, int verticalOffset, Dictionary<Letter, PixelArray> characterTable, List<Letter> preferredOrder, bool isLastLetter, out Letter matchingLetter, out int mergedWidth, out Letter preferredNextLetter)
    {
        bool IsFirstTry = LastLetterType.FontSize == 0;

        if (IsFirstTry)
            LastLetterType = LetterType.WithSizeAndColor(LastLetterType, 29, LastLetterType.IsBlue);

        for (int i = 0; i < TryOrderFirst.Length; i++)
        {
            CharacterPreference CharacterPreference = TryOrderFirst[i].Item1;
            CharacterPreference NextCharacterPreference = TryOrderFirst[i].Item2;
            bool AllowOverlap = TryOrderFirst[i].Item3;

            if (TryPartialScanDoubleCharacter(CharacterPreference, NextCharacterPreference, AllowOverlap, remainingWordArray, verticalOffset, characterTable, preferredOrder, isLastLetter, out matchingLetter, out mergedWidth, out preferredNextLetter))
            {
                TryOrderFirstStats[i]++;
                return true;
            }

            if (!IsFirstTry && i > 3)
            {
            }
        }

        matchingLetter = Letter.Unknown;
        mergedWidth = 0;
        preferredNextLetter = Letter.Unknown;
        return false;
    }

    private static bool TryPartialScanDoubleCharacter(CharacterPreference characterPreference, CharacterPreference nextCharacterPreference, bool allowOverlap, PixelArray remainingWordArray, int verticalOffset, Dictionary<Letter, PixelArray> characterTable, List<Letter> preferredOrder, bool isLastLetter, out Letter matchingLetter, out int mergedWidth, out Letter preferredNextLetter)
    {
        foreach (Letter Key in preferredOrder)
        {
            LetterType LetterType = Key.LetterType;
            if (!IsPreferred(LastLetterType, LetterType, characterPreference))
                continue;

            PixelArray CellArray = characterTable[Key];

            if (Key.Text == "M" && Key.IsItalic && !Key.IsBold && !LetterType.IsBlue && LetterType.FontSize == 29)
            {
                if (DisplayDebug)
                    remainingWordArray.DebugPrint();
                if (DisplayDebug)
                    CellArray.DebugPrint();
            }

            if (TryPartialScanNextCharacter(nextCharacterPreference, allowOverlap, remainingWordArray, CellArray, verticalOffset, characterTable, preferredOrder, isLastLetter, Key, out mergedWidth, out preferredNextLetter))
            {
                matchingLetter = Key;
                return true;
            }
        }

        matchingLetter = Letter.Unknown;
        mergedWidth = 0;
        preferredNextLetter = Letter.Unknown;
        return false;
    }

    private static Tuple<CharacterPreference, CharacterPreference, bool>[] TryOrderMore = new[]
    {
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.Same, CharacterPreference.Same, false),

            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.Same, CharacterPreference.Same, true),

            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.Same, CharacterPreference.ToggleItalic, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.Same, CharacterPreference.SameFont, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.ToggleItalic, CharacterPreference.Same, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.ToggleItalic, CharacterPreference.ToggleItalic, false),

            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.Same, CharacterPreference.ToggleItalic, true),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.ToggleItalic, CharacterPreference.Same, true),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.ToggleItalic, CharacterPreference.ToggleItalic, true),

            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.SameFont, CharacterPreference.Same, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.SameFont, CharacterPreference.SameFont, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.Same, CharacterPreference.Other, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.ToggleItalic, CharacterPreference.SameFont, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.ToggleItalic, CharacterPreference.Other, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.SameFont, CharacterPreference.ToggleItalic, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.SameFont, CharacterPreference.Other, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.Other, CharacterPreference.Same, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.Other, CharacterPreference.ToggleItalic, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.Other, CharacterPreference.SameFont, false),
            new Tuple<CharacterPreference, CharacterPreference, bool>(CharacterPreference.Other, CharacterPreference.Other, false),
        };
    public static int[] TryOrderMoreStats = new int[TryOrderMore.Length];

    private static bool TryPartialScanDoubleCharacter(PixelArray remainingWordArray, PixelArray previousArray, int verticalOffset, Dictionary<Letter, PixelArray> characterTable, List<Letter> preferredOrder, bool isLastLetter, out Letter matchingLetter, out int mergedWidth, out Letter preferredNextLetter)
    {
        bool IsFirstTry = LastLetterType.FontSize == 0;

        if (IsFirstTry)
            LastLetterType = LetterType.WithSizeAndColor(LastLetterType, 29, LastLetterType.IsBlue);

        for (int i = 0; i < TryOrderMore.Length; i++)
        {
            CharacterPreference CharacterPreference = TryOrderMore[i].Item1;
            CharacterPreference NextCharacterPreference = TryOrderMore[i].Item2;
            bool AllowOverlap = TryOrderMore[i].Item3;

            if (TryPartialScanDoubleCharacter(CharacterPreference, NextCharacterPreference, AllowOverlap, remainingWordArray, previousArray, verticalOffset, characterTable, preferredOrder, isLastLetter, out matchingLetter, out mergedWidth, out preferredNextLetter))
            {
                TryOrderMoreStats[i]++;
                return true;
            }

            if (!IsFirstTry && i > 1)
            {
            }
        }

        matchingLetter = Letter.Unknown;
        mergedWidth = 0;
        preferredNextLetter = Letter.Unknown;
        return false;
    }

    public static void PrintDebugOrder()
    {
        string TryOrderFirstDebugString = string.Empty;
        foreach (int n in TryOrderFirstStats)
        {
            if (TryOrderFirstDebugString.Length > 0)
                TryOrderFirstDebugString += " ";

            TryOrderFirstDebugString += $"{n}";
        }
        Debug.WriteLine($"First: {TryOrderFirstDebugString}");

        string TryOrderMoreDebugString = string.Empty;
        foreach (int n in TryOrderMoreStats)
        {
            if (TryOrderMoreDebugString.Length > 0)
                TryOrderMoreDebugString += " ";

            TryOrderMoreDebugString += $"{n}";
        }
        Debug.WriteLine($"More: {TryOrderMoreDebugString}");
    }

    private static bool TryPartialScanDoubleCharacter(CharacterPreference characterPreference, CharacterPreference nextCharacterPreference, bool allowOverlap, PixelArray remainingWordArray, PixelArray previousArray, int verticalOffset, Dictionary<Letter, PixelArray> characterTable, List<Letter> preferredOrder, bool isLastLetter, out Letter matchingLetter, out int mergedWidth, out Letter preferredNextLetter)
    {
        foreach (Letter Key in preferredOrder)
        {
            LetterType LetterType = Key.LetterType;
            if (!IsPreferred(LastLetterType, LetterType, characterPreference))
                continue;

            PixelArray CellArray = characterTable[Key];
            PixelArray MergedArray = PixelArrayHelper.Merge(previousArray, CellArray, previousArray.Width);

            if (Key.Text == "g" && !Key.IsItalic && !Key.IsBold && LetterType.IsBlue && LetterType.FontSize == 19.5)
            {
                if (DisplayDebug)
                    MergedArray.DebugPrint();
                if (DisplayDebug)
                    remainingWordArray.DebugPrint();
            }

            if (TryPartialScanNextCharacter(nextCharacterPreference, allowOverlap, remainingWordArray, MergedArray, verticalOffset, characterTable, preferredOrder, isLastLetter, Key, out mergedWidth, out preferredNextLetter))
            {
                matchingLetter = Key;
                return true;
            }
        }

        matchingLetter = Letter.Unknown;
        mergedWidth = 0;
        preferredNextLetter = Letter.Unknown;
        return false;
    }

    private static bool TryPartialScanNextCharacter(CharacterPreference nextCharacterPreference, bool allowOverlap, PixelArray remainingWordArray, PixelArray previousArray, int verticalOffset, Dictionary<Letter, PixelArray> characterTable, List<Letter> preferredOrder, bool isLastLetter, Letter previousLetter, out int mergedWidth, out Letter preferredNextLetter)
    {
        int MaxInside = (int)(previousLetter.LetterType.FontSize * 0.38);
        if (MaxInside >= previousArray.Width - 1)
            MaxInside = previousArray.Width - 1;

        for (; MaxInside > 0; MaxInside--)
        {
            int CompatibilityWidth = previousArray.Width - MaxInside;
            if (CompatibilityWidth > remainingWordArray.Width)
                break;

            if (PixelArrayHelper.IsCompatible(previousArray, remainingWordArray, CompatibilityWidth))
                break;
        }

        if (MaxInside > 0)
            if (TryPartialScanNextCharacter(nextCharacterPreference, allowOverlap, remainingWordArray, previousArray, verticalOffset, characterTable, preferredOrder, isLastLetter, previousLetter, MaxInside, out preferredNextLetter, out mergedWidth))
                return true;

        mergedWidth = 0;
        preferredNextLetter = Letter.Unknown;
        return false;
    }

    private static bool TryPartialScanNextCharacter(CharacterPreference characterPreference, bool allowOverlap, PixelArray remainingWordArray, PixelArray previousArray, int verticalOffset, Dictionary<Letter, PixelArray> characterTable, List<Letter> preferredOrder, bool isLastLetter, Letter previousLetter, int maxInside, out Letter preferredNextLetter, out int mergedWidth)
    {
        LetterType PreviousLetterType = previousLetter.LetterType;

        List<Letter> ScanLetterList = new();
        foreach (Letter Key in preferredOrder)
        {
            LetterType LetterType = Key.LetterType;
            if (IsPreferred(PreviousLetterType, LetterType, characterPreference))
                ScanLetterList.Add(Key);
        }

        //ScanLetterList.Sort((Letter l1, Letter l2) => { return characterTable[l2].Width - characterTable[l1].Width; } );

        foreach (Letter Key in ScanLetterList)
        {
            LetterType LetterType = Key.LetterType;

            //Debug.WriteLine($"Testing '{previousLetter.Text}' '{Key.Text}' Italic={LetterType.IsItalic} Bold={LetterType.IsBold} Blue={LetterType.IsBlue} FotnSize={LetterType.FontSize}");

            PixelArray CellArray = characterTable[Key];

            /*
            int CellMergeWidth = (int)(CellArray.Width * 0.38); // Max value for 'bij'
            if (CellMergeWidth < 3)
                CellMergeWidth = 3;
            */

            if (isLastLetter)
            {
            }

            int CellMergeWidth = CellArray.Width;
            double PerfectMatchRatio = allowOverlap || isLastLetter ? 0.0 : 1.0;
            int RightOverlapWidth = allowOverlap || isLastLetter ? (int)(CellArray.Width * 0.2) : 0;

            if (allowOverlap && previousLetter.Text == "r" && Key.Text == "d" && PreviousLetterType.IsItalic && LetterType.IsItalic && !PreviousLetterType.IsBold && !LetterType.IsBold && !PreviousLetterType.IsBlue && !LetterType.IsBlue  && PreviousLetterType.FontSize == 27 && LetterType.FontSize == 27)
            {
            }

            for (int Inside = 0; Inside < maxInside; Inside++)
            {
                int MaxMergeWidth = previousArray.Width - Inside + CellMergeWidth;
                PixelArray MergedArray = PixelArrayHelper.Merge(previousArray, CellArray, Inside, MaxMergeWidth);

                if (isLastLetter && MergedArray.Width > remainingWordArray.Width)
                {
                    int LocalCutoff = MergedArray.Width - remainingWordArray.Width;
                    if (LocalCutoff > 0)
                    {
                        MergedArray = PixelArrayHelper.Cut(MergedArray, LocalCutoff);
                        MergedArray = PixelArrayHelper.Enlarge(MergedArray, remainingWordArray);
                    }
                }

                if (DisplayDebug)
                    MergedArray.DebugPrint();
                if (DisplayDebug)
                    remainingWordArray.DebugPrint();

                if (PixelArrayHelper.IsLeftDiagonalMatch(MergedArray, PerfectMatchRatio, RightOverlapWidth, remainingWordArray, verticalOffset))
                {
                    if (previousLetter.Text == "!" && Key.Text == "\"")
                    {
                    }

                    preferredNextLetter = Key;
                    mergedWidth = Inside;

                    LastLetterType = PreviousLetterType;
                    LastVerticalOffset = verticalOffset;
                    return true;
                }
            }
        }

        preferredNextLetter = Letter.Unknown;
        mergedWidth = 0;
        return false;
    }

    public static List<Letter> GetPreferredOrder(Dictionary<Letter, PixelArray> table)
    {
        List<Letter> Result = new(table.Keys);
        Result.Sort(SortByLikelyness);

        return Result;
    }

    private static int SortByLikelyness(Letter l1, Letter l2)
    {
        long Flags1 = LetterFlags(l1);
        long Flags2 = LetterFlags(l2);

        if (Flags1 > Flags2)
            return -1;
        else if (Flags1 < Flags2)
            return 1;

        if (l1.Text.Length > l2.Text.Length)
            return -1;
        else if (l1.Text.Length < l2.Text.Length)
            return 1;

        if (l1.LetterType.FontSize > l2.LetterType.FontSize)
            return -1;
        else if (l1.LetterType.FontSize < l2.LetterType.FontSize)
            return 1;

        for (int i = 0; i < l1.Text.Length; i++)
        {
            char c1 = l1.Text[i];
            char c2 = l2.Text[i];

            int Order = (int)c1 - (int)c2;

            if (Order != 0)
                return Order;
        }

        return 0;
    }

    private static long LetterFlags(Letter l)
    {
        const long FlagItalic = 0x20;
        const long FlagNotBold = 0x10;
        const long FlagNotUpper = 0x08;
        const long FlagLetter = 0x04;
        const long FlagThreeDots = 0x02;
        const long FlagNotUpperI = 0x01;

        long Flags = 0;

        if (l.IsItalic)
            Flags |= FlagItalic;
        if (!l.IsBold)
            Flags |= FlagNotBold;

        char c = l.Text[0];

        if (!char.IsUpper(c))
            Flags |= FlagNotUpper;
        if (char.IsLetter(c))
            Flags |= FlagLetter;
        if (c == '…')
            Flags |= FlagThreeDots;
        if (c != 'I')
            Flags |= FlagNotUpperI;

        return Flags;
    }

    public static bool DisplayDebug = false;
    public static bool StopOnFail = true;
}
