namespace FontScanner;

using System;

public class LetterSkimmer
{
    public LetterSkimmer(ScanLine line)
    {
        Line = line;
        WordIndex = 0;
        LetterIndex = 0;
        ReachedLeft = Line.Words[0].Rect.Left;
        RemaingingWidth = Line.Words[0].LetterOffsetList[0].LetterWidth;
    }

    public ScanLine Line { get; }
    public int WordIndex { get; private set; }
    public int LetterIndex { get; private set; }
    public int ReachedLeft { get; private set; }
    public int RemaingingWidth { get; private set; }

    public bool IsFirstLetter { get { return WordIndex == 0 && LetterIndex == 0; } }
    public bool IsLastLetter { get { return (WordIndex >= Line.Words.Count) || (WordIndex == Line.Words.Count - 1 && LetterIndex >= Line.Words[WordIndex].LetterOffsetList.Count - 1); } }

    private bool IsBeyondEnd { get { return WordIndex >= Line.Words.Count || LetterIndex >= Line.Words[WordIndex].LetterOffsetList.Count; } }

    public ScanWord CurrentWord
    {
        get
        {
            if (IsBeyondEnd)
                throw new InvalidOperationException();

            return Line.Words[WordIndex];
        }
    }

    public LetterOffset CurrentLetterOffset
    {
        get
        {
            if (IsBeyondEnd)
                throw new InvalidOperationException();

            return Line.Words[WordIndex].LetterOffsetList[LetterIndex];
        }
    }

    public bool MoveNext(int lastIncrement)
    {
        ReachedLeft += lastIncrement;

        int LetterLeft = ReachedLeft;
        int LetterRight;

        while (!IsBeyondEnd)
        {
            ScanWord CurrentWord = Line.Words[WordIndex];
            LetterOffset CurrentLetterOffset = CurrentWord.LetterOffsetList[LetterIndex];
            LetterRight = CurrentWord.Rect.Left + CurrentLetterOffset.Offset + CurrentLetterOffset.LetterWidth;

            if (ReachedLeft < LetterRight)
            {
                ReachedLeft = LetterLeft;
                RemaingingWidth = LetterRight - ReachedLeft;
                return true;
            }

            LetterIndex++;

            if (LetterIndex == Line.Words[WordIndex].LetterOffsetList.Count)
            {
                WordIndex++;
                LetterIndex = 0;
            }

            if (WordIndex < Line.Words.Count)
                LetterLeft = Line.Words[WordIndex].Rect.Left + Line.Words[WordIndex].LetterOffsetList[LetterIndex].Offset;
        }

        if (IsBeyondEnd)
            return false;

        return true;
    }
}
