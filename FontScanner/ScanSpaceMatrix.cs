namespace FontScanner;

using FontLoader;
using System.Collections.Generic;

public class ScanSpaceMatrix
{
    public ScanSpaceMatrix(ScanSpace scanSpace)
    {
        MainIterator = new ScanSpaceIterator(scanSpace);
        Reset();
    }

    public ScanSpaceIterator MainIterator { get; }
    public bool IsSingle { get { return MainIterator.IsSingle; } }
    public Letter MainLetter { get { return MainIterator.CurrentLetter; } }
    public Letter SecondaryLetter { get { return GetSecondary(MainIterator.CurrentLetter).CurrentLetter; } }

    public void Reset()
    {
        SecondaryIteratorTable.Clear();
        ReachedMainIndex = 0;
    }

    public bool MainMoveNext()
    {
        bool IsSecondaryIteratorCreated = SecondaryIteratorTable.ContainsKey(MainIterator.CurrentLetter);

        bool IsMoved;
        if (IsSecondaryIteratorCreated)
        {
            if (ReachedMainIndex < MainIterator.ItemIndex)
            {
                ReachedMainIndex = MainIterator.ItemIndex;
                MainIterator.Reset();
            }

            IsMoved = MainIterator.MoveNextNoWhitespacePartialOnly();
        }
        else
            IsMoved = MainIterator.MoveNextNoWhitespace();

        return IsMoved;
    }

    public bool SecondaryMoveNext()
    {
        ScanSpaceIterator SecondaryIterator = GetSecondary(MainIterator.CurrentLetter);

        if (SecondaryIterator.ItemIndex > MainIterator.ItemIndex)
            return false;

        bool IsMoved = SecondaryIterator.MoveNextNoWhitespaceSingleOnly();

        return IsMoved;
    }

    private ScanSpaceIterator GetSecondary(Letter letter)
    {
        if (!SecondaryIteratorTable.ContainsKey(letter))
            SecondaryIteratorTable.Add(letter, new ScanSpaceIterator(MainIterator.ScanSpace));

        return SecondaryIteratorTable[letter];
    }

    private Dictionary<Letter, ScanSpaceIterator> SecondaryIteratorTable { get; } = new();
    private int ReachedMainIndex;
}
