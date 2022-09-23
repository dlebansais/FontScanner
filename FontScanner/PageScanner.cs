namespace FontScanner;

using FontLoader;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Font = FontLoader.Font;
using Rectangle = System.Drawing.Rectangle;

public static partial class PageScanner
{
    public static bool Scan(Font font, Page page)
    {
        List<ScanLine> LineList = page.LineList;
        List<ScanLine> ScannedLineList = new();
        bool IsFirstFigure = false;
        bool IsScanComplete = true;

        foreach (ScanLine Line in LineList)
            if (Scan(font, page, Line))
                UpdateScannedLineList(ScannedLineList, Line, ref IsFirstFigure);
            else
                UpdateFigureList(page, ScannedLineList, Line, ref IsFirstFigure, ref IsScanComplete);

        return IsScanComplete;
    }

    private static bool Scan(Font font, Page page, ScanLine line)
    {
        GetLineImage(page, line, out PixelArray? SmallImage, out bool IsSkipped);
        if (IsSkipped)
            return false;

        LetterSkimmer Skimmer = new(line);
        bool IsLineScanned = true;

        do
        {
            LastScan.LastLetterWidth = 0;

            if (!Scan(font, page, SmallImage, Skimmer))
            {
                IsLineScanned = false;
                break;
            }
        }
        while (Skimmer.MoveNext(LastScan.LastLetterWidth - LastScan.LastInside));

        if (IsLineScanned)
            MergeWordsAndWhitespace(font, line);

        return IsLineScanned;
    }

    private static bool Scan(Font font, Page page, PixelArray? smallImage, LetterSkimmer skimmer)
    {
        if (!ScanLetter(font, page, skimmer))
        {
            Debug.WriteLine($"Aborting section #{page.SectionIndex} page #{page.Progress} line #{skimmer.Line.LineNumber}");

            if (smallImage is null)
            {
                Rectangle Rect = skimmer.Line.Rect;
                smallImage = page.PageImage.GetPixelArray(Rect.Left, Rect.Top, Rect.Width, Rect.Height, 0, forbidGrayscale: true);
                smallImage = smallImage.Clipped();
            }

            if (smallImage.Width <= MaxSmallImageWidth || smallImage.Height <= MaxSmallImageHeight)
                SkippedImages.Add(smallImage);

            return false;
        }

        return true;
    }

    private static bool ScanLetter(Font font, Page page, LetterSkimmer skimmer)
    {
        ScanSpace ScanSpace = ScanSpaceHelper.Get(font, skimmer, LastScan);
        ScanSpaceMatrix ScanSpaceMatrix = new(ScanSpace);

        ScanWord CurrentWord = skimmer.CurrentWord;
        LetterOffset CurrentLetterOffset = skimmer.CurrentLetterOffset;
        Rectangle WordRect = CurrentWord.Rect;
        Rectangle MatchRect = new();

        Rectangle SingleLetterRect = new(skimmer.ReachedLeft, WordRect.Top, skimmer.RemaingingWidth, WordRect.Height);
        PixelArray SingleLetterArray = page.GetPixelArray(skimmer.CurrentWord, SingleLetterRect, forbidGrayscale: false)
                                           .Clipped();

        if (DisplayDebug)
            DebugPrintArray(SingleLetterArray);

        int LineRectRight = GetLineWorstRight(CurrentWord, CurrentLetterOffset);
        Rectangle RemainingRect = new(skimmer.ReachedLeft, WordRect.Top, LineRectRight - skimmer.ReachedLeft, WordRect.Height);
        PixelArray RemainingArray = page.GetPixelArray(skimmer.CurrentWord, RemainingRect, forbidGrayscale: false);

        if (DisplayDebug)
            DebugPrintArray(RemainingArray);

        bool IsMatch = false;
        while (!IsMatch && ScanSpaceMatrix.MainMoveNext())
        {
            if (ScanSpaceMatrix.IsSingle)
            {
                if (DisplayDebug)
                    DebugPrintArray(SingleLetterArray);

                IsMatch = ScanSingleCharacter(font, page, ScanSpaceMatrix, SingleLetterArray, RemainingArray, skimmer, LastScan.VerticalOffset);
                if (IsMatch)
                {
                    MatchRect = SingleLetterRect;
                }
            }
            else
            {
                if (DisplayDebug)
                    DebugPrintArray(RemainingArray);

                IsMatch = ScanPartial(font, page, ScanSpaceMatrix, RemainingArray, skimmer,  LastScan.VerticalOffset);
                if (IsMatch)
                {
                    MatchRect = new(skimmer.ReachedLeft, WordRect.Top, LineRectRight - LastScan.LastLetterWidth, WordRect.Height);
                }
            }
        }

        if (IsMatch)
        {
            Letter MainLetter = ScanSpaceMatrix.MainLetter;
            CurrentWord.AddLetter(MainLetter, MatchRect);
            LastScan.LetterType = MainLetter.LetterType;
            LastScan.PreviousLetter = MainLetter;
            return true;
        }

        return false;
    }

    private static bool ScanSingleCharacter(Font font, Page page, ScanSpaceMatrix scanSpaceMatrix, PixelArray letterArrayClipped, PixelArray remainingArray, LetterSkimmer skimmer, int verticalOffset)
    {
        Letter MainLetter = scanSpaceMatrix.MainLetter;
        LetterType LetterType = MainLetter.LetterType;

        PixelArray CellArray = font.CharacterTable[MainLetter];
        Debug.Assert(CellArray != PixelArray.Empty);
        PixelArray MergedArray;

        if (LastScan.PreviousMergeArray != PixelArray.Empty && LastScan.PreviousMergeArray.Width + 3 <= CellArray.Width)
        {
            Debug.Assert(LastScan.LastInside > 0);
            MergedArray = PixelArrayHelper.Replace(LastScan.PreviousMergeArray, CellArray)
                                          .Clipped();
        }
        else
            MergedArray = CellArray;

        if (LetterType.IsItalic && skimmer.IsLastLetter)
        {
            if (MergedArray.Width > letterArrayClipped.Width)
            {
                int Cutoff = MergedArray.Width - letterArrayClipped.Width;
                MergedArray = PixelArrayHelper.CutRight(MergedArray, Cutoff);
                MergedArray = MergedArray.Clipped();
            }
        }

        PixelArray ComparedArray;

        if (MainLetter.Text == "…" && remainingArray.Width > MergedArray.Width)
        {
            ComparedArray = remainingArray.GetLeftSide(MergedArray.Width)
                                          .Clipped();
        }
        else
        {
            ComparedArray = letterArrayClipped;
        }

        if (MainLetter.Text == "s" && MainLetter.IsItalic && !MainLetter.IsBold && !LetterType.IsBlue && LetterType.FontSize == 109)
        {
            if (DisplayDebug)
                DebugPrintArray(MergedArray);
            if (DisplayDebug)
                DebugPrintArray(ComparedArray);
            if (DisplayDebug)
                DebugPrintArray(letterArrayClipped);
        }

        if (PixelArrayHelper.IsMatch(MergedArray, ComparedArray, verticalOffset))
        {
            LastScan.LastLetterWidth = MergedArray.Width;
            LastScan.PreviousMergeArray = PixelArray.Empty;
            LastScan.LastInside = 0;
            return true;
        }

        return false;
    }

    private static bool ScanPartial(Font font, Page page, ScanSpaceMatrix scanSpaceMatrix, PixelArray remainingArray, LetterSkimmer skimmer, int verticalOffset)
    {
        Letter MainLetter = scanSpaceMatrix.MainLetter;
        PixelArray CellArray = font.CharacterTable[MainLetter];
        Debug.Assert(CellArray != PixelArray.Empty);

        PixelArray MergedArray;
        if (LastScan.PreviousMergeArray != PixelArray.Empty && LastScan.PreviousMergeArray.Width + 3 <= CellArray.Width)
        {
            Debug.Assert(LastScan.LastInside > 0);
            MergedArray = PixelArrayHelper.Replace(LastScan.PreviousMergeArray, CellArray)
                                          .Clipped();
        }
        else
            MergedArray = CellArray;

        LetterType MainLetterType = MainLetter.LetterType;

        if (MainLetter.Text == "r" && 
            MainLetterType.IsItalic &&
            !MainLetterType.IsBold &&
            !MainLetterType.IsBlue &&
            MainLetterType.FontSize == 109)
        {
        }

        if (MainLetter.Text == "î" &&
            !MainLetterType.IsItalic &&
            !MainLetterType.IsBold &&
            !MainLetterType.IsBlue &&
            MainLetterType.FontSize == 109)
        {
        }

        if (MainLetter.Text == "t" &&
            !MainLetterType.IsItalic &&
            !MainLetterType.IsBold &&
            !MainLetterType.IsBlue &&
            MainLetterType.FontSize == 109)
        {
        }

        if (DisplayDebug)
            DebugPrintArray(MergedArray);
        if (DisplayDebug)
            DebugPrintArray(remainingArray);

        if (remainingArray.Width <= MergedArray.Width)
        {
            if (MainLetterType.IsItalic && skimmer.IsLastLetter)
            {
                if (MergedArray.Width > remainingArray.Width)
                {
                    int Cutoff = MergedArray.Width - remainingArray.Width;
                    MergedArray = PixelArrayHelper.CutRight(MergedArray, Cutoff);
                    MergedArray = MergedArray.Clipped();
                }
            }

            if (PixelArrayHelper.IsMatch(MergedArray, remainingArray, verticalOffset))
            {
                LastScan.LastLetterWidth = MergedArray.Width;
                return true;
            }
        }

        int MaxInside = (int)(MainLetter.LetterType.FontSize * 0.41);
        if (MaxInside >= MergedArray.Width - LastScan.LastInside - 3)
            MaxInside = MergedArray.Width - LastScan.LastInside - 3;

        int CompatibilityWidth;

        for (; MaxInside > 0; MaxInside--)
        {
            CompatibilityWidth = MergedArray.Width - MaxInside;
            if (CompatibilityWidth > remainingArray.Width)
                break;

            if (PixelArrayHelper.IsCompatible(MergedArray, remainingArray, CompatibilityWidth))
                break;
        }

        if (MaxInside > 0)
        {
            bool IsMatch = false;
            while (!IsMatch && scanSpaceMatrix.SecondaryMoveNext())
            {
                //Debug.WriteLine($"Trying {MainLetter.DisplayText} {scanSpaceMatrix.SecondaryLetter.DisplayText}");

                IsMatch = ScanNextCharacter(font, page, scanSpaceMatrix, remainingArray, skimmer, verticalOffset, MainLetter, MergedArray, MaxInside);
            }

            if (IsMatch)
            {
                LastScan.LastLetterWidth = CellArray.Width;
                return true;
            }
        }

        return false;
    }

    private static bool ScanNextCharacter(Font font, Page page, ScanSpaceMatrix scanSpaceMatrix, PixelArray remainingArray, LetterSkimmer skimmer, int verticalOffset, Letter mainLetter, PixelArray mergedArray, int maxInside)
    {
        Letter NextLetter = scanSpaceMatrix.SecondaryLetter;
        PixelArray NextCellArray = font.CharacterTable[NextLetter];
        Debug.Assert(NextCellArray != PixelArray.Empty);

        int CellMergeWidth = NextCellArray.Width;
        int NextCharOffsetY = 0;
        double PerfectMatchRatio = 0;
        int RightOverlapWidth = 0;

        LetterType MainLetterType = mainLetter.LetterType;
        LetterType NextLetterType = NextLetter.LetterType;

        if (mainLetter.Text == "a" && NextLetter.Text == "î" &&
            !MainLetterType.IsItalic && !NextLetterType.IsItalic &&
            !MainLetterType.IsBold &&   !NextLetterType.IsBold &&
            !MainLetterType.IsBlue &&   !NextLetterType.IsBlue &&
            MainLetterType.FontSize == 109 && NextLetterType.FontSize == 109)
        {
        }

        if (mainLetter.Text == "î" && NextLetter.Text == "t" &&
            !MainLetterType.IsItalic && !NextLetterType.IsItalic &&
            !MainLetterType.IsBold && !NextLetterType.IsBold &&
            !MainLetterType.IsBlue && !NextLetterType.IsBlue &&
            MainLetterType.FontSize == 109 && NextLetterType.FontSize == 109)
        {
        }

        if (mainLetter.Text == "t" && NextLetter.Text == "r" &&
            !MainLetterType.IsItalic && !NextLetterType.IsItalic &&
            !MainLetterType.IsBold && !NextLetterType.IsBold &&
            !MainLetterType.IsBlue && !NextLetterType.IsBlue &&
            MainLetterType.FontSize == 109 && NextLetterType.FontSize == 109)
        {
        }

        for (int Inside = 0; Inside < maxInside; Inside++)
        {
            int MaxMergeWidth = mergedArray.Width - Inside + CellMergeWidth;
            PixelArray TwoLettersArray = PixelArrayHelper.Merge(mergedArray, NextCharOffsetY, NextCellArray, Inside, MaxMergeWidth);

            if (DisplayDebug)
                DebugPrintArray(TwoLettersArray);
            if (DisplayDebug)
                DebugPrintArray(remainingArray);

            if (PixelArrayHelper.IsLeftDiagonalMatch(TwoLettersArray, PerfectMatchRatio, RightOverlapWidth, remainingArray, verticalOffset))
            {
                if (Inside > 0)
                {
                    ScanWord CurrentWord = skimmer.CurrentWord;
                    Rectangle WordRect = CurrentWord.Rect;
                    Rectangle InsideRect = new(skimmer.ReachedLeft + mergedArray.Width - Inside, WordRect.Top, Inside, WordRect.Height);
                    PixelArray InsideArray = page.GetPixelArray(skimmer.CurrentWord, InsideRect, forbidGrayscale: true);

                    if (DisplayDebug)
                        DebugPrintArray(InsideArray);

                    LastScan.PreviousMergeArray = InsideArray;
                    LastScan.LastInside = Inside;
                }
                else
                {
                    LastScan.PreviousMergeArray = PixelArray.Empty;
                    LastScan.LastInside = 0;
                }
                return true;
            }
        }

        return false;
    }

    private static ScanInfo LastScan = new();

    private static void GetLineImage(Page page, ScanLine line, out PixelArray? smallImage, out bool isSkipped)
    {
        if (SkippedImages.Count > 0)
        {
            Rectangle Rect = line.Rect;

            smallImage = page.PageImage.GetPixelArray(Rect.Left, Rect.Top, Rect.Width, Rect.Height, 0, forbidGrayscale: true);
            smallImage = smallImage.Clipped();

            if (smallImage.Width <= MaxSmallImageWidth || smallImage.Height <= MaxSmallImageHeight)
            {
                foreach (PixelArray Item in SkippedImages)
                    if (PixelArrayHelper.IsMatch(Item, smallImage, 0))
                    {
                        isSkipped = true;
                        return;
                    }
            }
        }

        smallImage = null;
        isSkipped = false;
    }

    private static void MergeWordsAndWhitespace(Font font, ScanLine line)
    {
        Letter PreviousLetter = Letter.EmptyNormal;
        Rectangle PreviousLetterRect = new();
        bool DebugDistance = false;

        foreach (ScanWord Word in line.Words)
        {
            for (int i = 0; i < Word.Text.Count; i++)
            {
                Letter Letter = Word.Text[i];
                Rectangle LetterRect = Word.TextRect[i];

                if (PreviousLetter != Letter.EmptyNormal)
                {
                    Debug.Assert(!PreviousLetterRect.IsEmpty);

                    int Distance = LetterDistance(font, PreviousLetterRect.Left, PreviousLetter, LetterRect.Left, Letter);
                    int FontWhitespaceDistance = (int)Math.Round(PreviousLetter.LetterType.FontSize * 0.119);

                    if (DebugDistance)
                        Debug.Write($"{Distance}  ");

                    if (Distance >= FontWhitespaceDistance)
                        line.LetterList.Add(Letter.Whitespace);
                }

                line.LetterList.Add(Letter);

                PreviousLetter = Letter;
                PreviousLetterRect = LetterRect;
            }
        }

        if (DebugDistance)
            Debug.WriteLine("");
    }

    private static int LetterDistance(Font font, int previousLetterLeft, Letter previousLetter, int nextLetterLeft, Letter nextLetter)
    {
        PixelArray PreviousArray = font.CharacterTable[previousLetter];
        PixelArray NextArray = font.CharacterTable[nextLetter];
        int LetterDistance = PixelArrayHelper.Distance(PreviousArray, NextArray);
        int OffsetDistance = nextLetterLeft - previousLetterLeft - PreviousArray.Width;

        return LetterDistance + OffsetDistance;
    }

    private static void MergeWordsAndWhitespaceOld(ScanLine line)
    {
        int i = 0;
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

                MergedWord.Merge(PreviousWord);
                MergedWord.Merge(NextWord);

                line.Words[i] = MergedWord;
                line.Words.RemoveAt(i + 1);
            }
            else
                i++;
        }
    }

    private static int GetLineWorstRight(ScanWord word, LetterOffset letterOffset)
    {
        ScanLine Line = word.Line;
        List<ScanWord> WordList = Line.Words;
        int WordIndex = WordList.IndexOf(word);
        Debug.Assert(WordIndex >= 0);
        int LetterOffsetIndex = word.LetterOffsetList.IndexOf(letterOffset);
        Debug.Assert(LetterOffsetIndex >= 0);

        const int MaxLetterIncrement = 5;
        for (int i = 0; i < MaxLetterIncrement; i++)
            if (LetterOffsetIndex + 1 < WordList[WordIndex].LetterOffsetList.Count)
                LetterOffsetIndex++;
            else if (WordIndex + 1 < WordList.Count)
            {
                WordIndex++;
                LetterOffsetIndex = 0;
            }
            else
                break;

        Debug.Assert(WordIndex < WordList.Count);
        Debug.Assert(LetterOffsetIndex < WordList[WordIndex].LetterOffsetList.Count);

        ScanWord WorstWord = WordList[WordIndex];
        LetterOffset WorstLetterOffset = WorstWord.LetterOffsetList[LetterOffsetIndex];
        int WorstRight = WorstWord.Rect.Left + WorstLetterOffset.Offset + WorstLetterOffset.LetterWidth;
        return WorstRight;
    }

    public static void DebugPrintArray(PixelArray array)
    {
        string DebugString = array.GetDebugString();
        Debug.WriteLine(DebugString);
    }

    public static bool DisplayDebug;
    public static int MaxSmallImageWidth = 50;
    public static int MaxSmallImageHeight = 50;
    private static List<PixelArray> SkippedImages = new();

    public static bool IsInterruptable { get; set; }

    public static void PrintDebugOrder()
    {
        /*
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
        */
    }
}
