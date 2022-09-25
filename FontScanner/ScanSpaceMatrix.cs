namespace FontScanner;

using FontLoader;
using System.Collections.Generic;

public class ScanSpaceMatrix
{
    public ScanSpaceMatrix(ScanSpace primaryScanSpace, ScanSpace secondaryScanSpace)
    {
        PrimaryScanSpace = primaryScanSpace;
        SecondaryScanSpace = secondaryScanSpace;
        MainIterator = new ScanSpaceIterator(PrimaryScanSpace);
        Reset();
        SecondaryScanSpace = secondaryScanSpace;
    }

    public ScanSpace PrimaryScanSpace { get; }
    public ScanSpace SecondaryScanSpace { get; }
    public ScanSpaceIterator MainIterator { get; }
    public bool IsSingle { get { return MainIterator.IsSingle; } }
    public Letter MainLetter { get { return MainIterator.CurrentLetter; } }
    public Letter SecondaryLetter { get { return SecondaryIteratorTable.Count > 0 ? GetSecondary(MainIterator.CurrentLetter).CurrentLetter : Letter.EmptyNormal; } }

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

        if (!SecondaryIterator.IsCompatibleWith(MainIterator))
            return false;

        bool IsMoved = SecondaryIterator.MoveNextNoWhitespaceSingleOnly();

        return IsMoved;
    }

    private ScanSpaceIterator GetSecondary(Letter letter)
    {
        if (!SecondaryIteratorTable.ContainsKey(letter))
        {
            ScanSpaceIterator SecondaryIterator = new ScanSpaceIterator(SecondaryScanSpace);
            SecondaryIteratorTable.Add(letter, SecondaryIterator);
        }

        return SecondaryIteratorTable[letter];
    }

    private Dictionary<Letter, ScanSpaceIterator> SecondaryIteratorTable { get; } = new();
    private int ReachedMainIndex;
}
