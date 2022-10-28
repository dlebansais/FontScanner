namespace FontScanner;

using FontLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Input;
using Font = FontLoader.Font;
using Rectangle = System.Drawing.Rectangle;

public static partial class PageScanner
{
    public static void InitializeSkippedPages(List<int> skipPageNumberList)
    {
        SkipPageNumberList = skipPageNumberList;
    }

    public static List<int> SkipPageNumberList { get; set; } = new();
    public static List<int> SkippedPageNumberList { get; set; } = new();

    public static bool Scan(Font font, Page page)
    {
        List<ScanLine> LineList = page.LineList;
        List<ScanLine> ScannedLineList = new();
        bool IsFirstFigure = false;
        bool IsScanComplete = true;

        if (LineList.Count == 0)
        {
            IsFirstFigure = true;
            page.FigureList.Clear();

            foreach (Rectangle Rect in page.RectangleList)
                UpdateFigureList(page, Rect, ref IsFirstFigure);
        }
        else
        {
            foreach (ScanLine Line in LineList)
            {
                IsAborted = false;

                if (Scan(font, page, Line))
                    UpdateScannedLineList(ScannedLineList, Line, ref IsFirstFigure);
                else
                    UpdateFigureList(page, ScannedLineList, Line, ref IsFirstFigure, ref IsScanComplete);
            }
        }

        return IsScanComplete;
    }

    private static bool Scan(Font font, Page page, ScanLine line)
    {
        GetLineImage(page, line, out PixelArray? SmallImage, out bool IsSkipped);
        if (IsSkipped)
            return false;

        LetterSkimmer Skimmer = new(line);
        bool IsLineScanned = true;
        Stopwatch Stopwatch = new Stopwatch();
        Stopwatch.Start();

        do
        {
            if (IsAborted)
            {
                IsLineScanned = false;
                break;
            }

            LastScan.LastLetterWidth = 0;

            if (!Scan(font, page, SmallImage, Skimmer, Stopwatch))
            {
                IsLineScanned = false;
                break;
            }
        }
        while (Skimmer.MoveNext(LastScan.LastLetterWidth - LastScan.LastInside));

        if (IsLineScanned && !IsAborted)
            MergeWordsAndWhitespace(font, line);

        return IsLineScanned;
    }

    private static bool Scan(Font font, Page page, PixelArray? smallImage, LetterSkimmer skimmer, Stopwatch stopwatch)
    {
        if (!ScanLetter(font, page, skimmer, stopwatch))
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

    static bool DebugPrimaryIterator = false;
    static bool DebugSecondaryIterator = false;

    private static bool ScanLetter(Font font, Page page, LetterSkimmer skimmer, Stopwatch stopwatch)
    {
        ScanSpace PrimaryScanSpace = ScanSpaceHelper.GetPrimary(font, skimmer);
        ScanSpace SecondaryScanSpace = ScanSpaceHelper.GetSecondary(font);
        ScanSpaceMatrix ScanSpaceMatrix = new(PrimaryScanSpace, SecondaryScanSpace);

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
        int OldItemIndex = -1;
        ScanSpaceItem CurrentItem;
        int MainIndexIncrement = 0;

        while (!IsMatch && ScanSpaceMatrix.MainMoveNext())
        {
            if (IsInterrupted())
            {
                SkippedPageNumberList.Add(page.PageIndex);
                break;
            }

            if (stopwatch.Elapsed > TimeSpan.FromMinutes(120))
            {
                while (SkipPageNumberList.Count > 0 && SkipPageNumberList[0] < page.PageIndex)
                    SkipPageNumberList.RemoveAt(0);

                if (SkipPageNumberList.Count > 0 && SkipPageNumberList[0] == page.PageIndex)
                {
                    SkipPageNumberList.RemoveAt(0);
                    IsAborted = true;
                    break;
                }
            }

            int ItemIndex = ScanSpaceMatrix.MainIterator.ItemIndex;
            CurrentItem = PrimaryScanSpace.ItemList[ItemIndex];

            if (OldItemIndex != ItemIndex)
            {
                OldItemIndex = ItemIndex;
                MainIndexIncrement++;

                if (ItemIndex >= 12)
                {
                }

                if (MainIndexIncrement >= 3)
                {
                    if (DebugPrimaryIterator)
                        Debug.WriteLine($"Item Index: {ItemIndex} {CurrentItem.DebugText}");
                }
            }

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
            LastScan.PreviousLetter = MainLetter;

            if (MainLetter.Text == "F" && MainLetter.LetterType.IsItalic)
            {
            }

            ScanSpaceHelper.OptimizeFromLastScan(font, skimmer, LastScan);
            return true;
        }

        return false;
    }

    private static bool ScanSingleCharacter(Font font, Page page, ScanSpaceMatrix scanSpaceMatrix, PixelArray letterArrayClipped, PixelArray remainingArray, LetterSkimmer skimmer, int lastVerticalOffset)
    {
        Letter MainLetter = scanSpaceMatrix.MainLetter;
        LetterType LetterType = MainLetter.LetterType;

        PixelArray CellArray = font.CharacterTable[MainLetter];
        Debug.Assert(CellArray != PixelArray.Empty);
        PixelArray MergedArray;

        if (LastScan.PreviousMergeArray != PixelArray.Empty && LastScan.PreviousMergeArray.Width + 1 <= CellArray.Width)
        {
            Debug.Assert(LastScan.LastInside > 0);
            int MergeOffset = LastScan.VerticalOffset - LastScan.NextCharOffsetY;
            MergedArray = PixelArrayHelper.Replace(LastScan.PreviousMergeArray, CellArray, MergeOffset)
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

        if (!MainLetter.IsSingleGlyph && remainingArray.Width >= MergedArray.Width)
        {
            ComparedArray = remainingArray.GetLeftSide(MergedArray.Width)
                                          .Clipped();
        }
        else
        {
            ComparedArray = letterArrayClipped;
        }

        bool IsSuperscript = MainLetter.Text == "th";

        if (MainLetter.Text == "." && MainLetter.IsItalic && !MainLetter.IsBold && !LetterType.IsBlue && LetterType.FontSize == 88)
        {
            if (DisplayDebug)
                DebugPrintArray(MergedArray);
            if (DisplayDebug)
                DebugPrintArray(ComparedArray);
            if (DisplayDebug)
                DebugPrintArray(letterArrayClipped);
        }

        bool IsMatch;
        if (skimmer.IsFirstLetter)
        {
            List<int> VerticalOffsetList = new() { 0, -1, +1, -2, +2, -3, +3 };

            VerticalOffsetList.Remove(lastVerticalOffset);
            VerticalOffsetList.Insert(0, lastVerticalOffset);

            int ZeroBaseline = MergedArray.Baseline - MergedArray.Height + 1;
            VerticalOffsetList.Remove(ZeroBaseline);
            VerticalOffsetList.Insert(1, ZeroBaseline);

            IsMatch = false;

            foreach (int VerticalOffset in VerticalOffsetList)
                if (PixelArrayHelper.IsMatch(MergedArray, ComparedArray, VerticalOffset))
                {
                    IsMatch = true;
                    LastScan.VerticalOffset = VerticalOffset;
                    break;
                }
        }
        else if (IsSuperscript)
        {
            IsMatch = false;
            int MinVerticalOffset = -MergedArray.Height;
            int MaxVerticalOffset = MergedArray.Height;

            for (int VerticalOffset = MinVerticalOffset; VerticalOffset < MaxVerticalOffset; VerticalOffset++)
                if (PixelArrayHelper.IsMatch(MergedArray, ComparedArray, VerticalOffset))
                {
                    IsMatch = true;
                    LastScan.VerticalOffset = VerticalOffset + LastScan.NextCharOffsetY;
                    break;
                }
        }
        else
        {
            IsMatch = PixelArrayHelper.IsMatch(MergedArray, ComparedArray, lastVerticalOffset + LastScan.NextCharOffsetY);
        }

        if (IsMatch)
        {
            LastScan.LastLetterWidth = MergedArray.Width;
            LastScan.PreviousMergeArray = PixelArray.Empty;
            LastScan.LastInside = 0;
            LastScan.NextCharOffsetY = 0;
            LastScan.ExpectedNextLetter = Letter.EmptyNormal;
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
            int MergeOffset = LastScan.VerticalOffset - LastScan.NextCharOffsetY;
            MergedArray = PixelArrayHelper.Replace(LastScan.PreviousMergeArray, CellArray, MergeOffset)
                                          .Clipped();
        }
        else
            MergedArray = CellArray;

        LetterType MainLetterType = MainLetter.LetterType;

        if (MainLetter.Text == "l" && 
            MainLetterType.IsItalic &&
            !MainLetterType.IsBold &&
            !MainLetterType.IsBlue &&
            MainLetterType.FontSize == 88)
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

        int MaxInside = (int)Math.Round(MainLetter.LetterType.FontSize * 0.41);

        int MinCompatible;
        if (MainLetter.Text == "\'" || MainLetter.Text == "‘" || MainLetter.Text == "’")
            MinCompatible = (int)Math.Round(((MergedArray.Width * 10) + MainLetter.LetterType.FontSize) * 0.01);
        else
            MinCompatible = (int)Math.Round(((MergedArray.Width * 10) + MainLetter.LetterType.FontSize) * 0.022);

        if (MinCompatible >= MergedArray.Width)
            MinCompatible = MergedArray.Width - 1;
        if (MaxInside >= MergedArray.Width - MinCompatible)
            MaxInside = MergedArray.Width - MinCompatible;

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
            if (remainingArray.Width <= MergedArray.Width)
            {
                if (MainLetterType.IsItalic && skimmer.IsLastWord)
                {
                    if (MergedArray.Width > remainingArray.Width)
                    {
                        int Cutoff = MergedArray.Width - remainingArray.Width;
                        MergedArray = PixelArrayHelper.CutRight(MergedArray, Cutoff);
                        MergedArray = MergedArray.Clipped();
                    }
                }

                PixelArray ClippedRemainingArray = remainingArray.Clipped();

                if (DisplayDebug)
                    DebugPrintArray(MergedArray);
                if (DisplayDebug)
                    DebugPrintArray(ClippedRemainingArray);

                if (PixelArrayHelper.IsMatch(MergedArray, ClippedRemainingArray, verticalOffset))
                {
                    LastScan.LastLetterWidth = MergedArray.Width;
                    return true;
                }
            }

            ScanSpaceItem CurrentItem;
            ScanSpaceItem? OldCurrentItem = null;
            bool IsMatch = false;
            while (!IsMatch && scanSpaceMatrix.SecondaryMoveNext())
            {
                if (IsInterrupted())
                    break;

                //Debug.WriteLine($"Trying {MainLetter.DisplayText} {scanSpaceMatrix.SecondaryLetter.DisplayText}");

                CurrentItem = scanSpaceMatrix.SecondaryItem;

                if (OldCurrentItem != CurrentItem)
                {
                    OldCurrentItem = CurrentItem;

                    if (DebugSecondaryIterator)
                        Debug.WriteLine($"Secondary Item: {CurrentItem.DebugText}");
                }

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

    private static bool ScanNextCharacter(Font font, Page page, ScanSpaceMatrix scanSpaceMatrix, PixelArray remainingArray, LetterSkimmer skimmer, int lastVerticalOffset, Letter mainLetter, PixelArray mergedArray, int maxInside)
    {
        Letter NextLetter = scanSpaceMatrix.SecondaryLetter;
        PixelArray NextCellArray = font.CharacterTable[NextLetter];
        Debug.Assert(NextCellArray != PixelArray.Empty);

        int CellMergeWidth = NextCellArray.Width;
        double PerfectMatchRatio = 0;
        int RightOverlapWidth = 0;

        LetterType MainLetterType = mainLetter.LetterType;
        LetterType NextLetterType = NextLetter.LetterType;

        if (mainLetter.Text == "a" && NextLetter.Text == "d" &&
            MainLetterType.IsItalic && NextLetterType.IsItalic &&
            !MainLetterType.IsBold &&   !NextLetterType.IsBold &&
            !MainLetterType.IsBlue &&   !NextLetterType.IsBlue &&
            MainLetterType.FontSize == 88 && NextLetterType.FontSize == 88)
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

        bool IsSuperscript = NextLetter.Text == "th";
        bool IsMatch = false;
        int Inside;
        int NextCharOffsetY = 0;

        if (IsSuperscript)
        {
            int MinNextCharOffsetY = -NextCellArray.Height;
            int MaxNextCharOffsetY = NextCellArray.Height;

            for (Inside = 0; Inside < maxInside; Inside++)
            {
                for (NextCharOffsetY = MinNextCharOffsetY; NextCharOffsetY < MaxNextCharOffsetY; NextCharOffsetY++)
                {
                    int MaxMergeWidth = mergedArray.Width - Inside + CellMergeWidth;
                    PixelArray TwoLettersArray = PixelArrayHelper.Merge(mergedArray, NextCharOffsetY, NextCellArray, Inside, MaxMergeWidth);

                    if (DisplayDebug)
                        DebugPrintArray(TwoLettersArray);
                    if (DisplayDebug)
                        DebugPrintArray(remainingArray);

                    if (skimmer.IsFirstLetter)
                    {
                        List<int> VerticalOffsetList = new() { 0, -1, +1 };
                        VerticalOffsetList.Remove(lastVerticalOffset);
                        VerticalOffsetList.Insert(0, lastVerticalOffset);

                        foreach (int VerticalOffset in VerticalOffsetList)
                            if (PixelArrayHelper.IsLeftDiagonalMatch(TwoLettersArray, PerfectMatchRatio, RightOverlapWidth, remainingArray, VerticalOffset))
                            {
                                IsMatch = true;
                                LastScan.VerticalOffset = VerticalOffset;
                                break;
                            }
                    }
                    else
                    {
                        IsMatch = PixelArrayHelper.IsLeftDiagonalMatch(TwoLettersArray, PerfectMatchRatio, RightOverlapWidth, remainingArray, lastVerticalOffset);
                    }

                    if (IsMatch)
                    {
                        LastScan.NextCharOffsetY = NextCharOffsetY;
                        break;
                    }
                }

                if (IsMatch)
                    break;
            }
        }
        else
        {
            for (Inside = 0; Inside < maxInside; Inside++)
            {
                int MaxMergeWidth = mergedArray.Width - Inside + CellMergeWidth;
                PixelArray TwoLettersArray = PixelArrayHelper.Merge(mergedArray, NextCharOffsetY, NextCellArray, Inside, MaxMergeWidth);

                if (DisplayDebug)
                    DebugPrintArray(TwoLettersArray);
                if (DisplayDebug)
                    DebugPrintArray(remainingArray);

                if (skimmer.IsFirstLetter)
                {
                    List<int> VerticalOffsetList = new() { 0, -1, +1 };
                    VerticalOffsetList.Remove(lastVerticalOffset);
                    VerticalOffsetList.Insert(0, lastVerticalOffset);

                    foreach (int VerticalOffset in VerticalOffsetList)
                        if (PixelArrayHelper.IsLeftDiagonalMatch(TwoLettersArray, PerfectMatchRatio, RightOverlapWidth, remainingArray, VerticalOffset))
                        {
                            IsMatch = true;
                            LastScan.VerticalOffset = VerticalOffset;
                            break;
                        }
                }
                else
                {
                    IsMatch = PixelArrayHelper.IsLeftDiagonalMatch(TwoLettersArray, PerfectMatchRatio, RightOverlapWidth, remainingArray, lastVerticalOffset);
                }

                if (IsMatch)
                {
                    LastScan.NextCharOffsetY = 0;
                    break;
                }
            }
        }

        if (IsMatch)
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

            LastScan.ExpectedNextLetter = NextLetter;
            return true;
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
        bool DebugDistanceLarge = false;

        foreach (ScanWord Word in line.Words)
        {
            for (int i = 0; i < Word.Text.Count; i++)
            {
                Letter Letter = Word.Text[i];
                Rectangle LetterRect = Word.TextRect[i];

                if (PreviousLetter != Letter.EmptyNormal)
                {
                    Debug.Assert(!PreviousLetterRect.IsEmpty);

                    double Distance = LetterDistance(font, PreviousLetterRect.Left, PreviousLetter, LetterRect.Left, Letter);
                    double FontWhitespaceDistance = (int)Math.Round(PreviousLetter.LetterType.FontSize * 0.119);

                    if (Distance >= FontWhitespaceDistance)
                    {
                        if (DebugDistanceLarge)
                            Debug.Write($"{Math.Round(Distance, 2)}/{Math.Round(FontWhitespaceDistance, 2)}  ");

                        line.LetterList.Add(Letter.Whitespace);
                    }
                    else
                    {
                        if (DebugDistance)
                            Debug.Write($"{Math.Round(Distance, 2)}  ");
                    }
                }

                line.LetterList.Add(Letter);

                PreviousLetter = Letter;
                PreviousLetterRect = LetterRect;
            }
        }

        if (DebugDistance || DebugDistanceLarge)
            Debug.WriteLine("");
    }

    private static double LetterDistance(Font font, int previousLetterLeft, Letter previousLetter, int nextLetterLeft, Letter nextLetter)
    {
        bool IsItalic = previousLetter.LetterType.IsItalic || nextLetter.LetterType.IsItalic;
        PixelArray PreviousArray = font.CharacterTable[previousLetter];
        PixelArray NextArray = font.CharacterTable[nextLetter];
        double LetterDistance = IsItalic ? PixelArrayHelper.Distance(PreviousArray, NextArray) : PixelArrayHelper.MaxMinDistance(PreviousArray, NextArray);
        double OffsetDistance = nextLetterLeft - previousLetterLeft - PreviousArray.Width;

        return LetterDistance + OffsetDistance;
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

    private static bool IsInterrupted()
    {
        if (IsAborted)
            return true;

        if (IsInterruptable)
        {
            KeyStates States = Keyboard.GetKeyStates(System.Windows.Input.Key.NumLock);
            if (States != KeyStates.Toggled)
            {
                Debug.WriteLine("Key abort detected");
                Thread.Sleep(1000);
                IsAborted = true;
                return true;
            }
        }

        return false;
    }

    public static bool DisplayDebug;
    public static int MaxSmallImageWidth = 50;
    public static int MaxSmallImageHeight = 50;
    private static List<PixelArray> SkippedImages = new();

    public static bool IsInterruptable { get; set; }
    public static bool IsAborted { get; set; }

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
