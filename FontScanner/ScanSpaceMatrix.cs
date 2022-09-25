namespace FontScanner;

using FontLoader;
using System.Collections.Generic;
using System.Diagnostics;

public class ScanSpaceMatrix
{
    public ScanSpaceMatrix(ScanSpace primaryScanSpace, ScanSpace secondaryScanSpace)
    {
        PrimaryScanSpace = primaryScanSpace;
        SecondaryScanSpace = secondaryScanSpace;
        MainIterator = new ScanSpaceIterator(PrimaryScanSpace);
        SecondaryIteratorTable = new Dictionary<Letter, ScanSpaceIterator>();

        Reset();
    }

    public ScanSpace PrimaryScanSpace { get; }
    public ScanSpace SecondaryScanSpace { get; }
    public ScanSpaceIterator MainIterator { get; }

    [DebuggerHidden]
    public ScanSpaceItem MainItem { get { return MainIterator.CurrentItem; } }

    [DebuggerHidden]
    public bool IsSingle { get { return MainIterator.CurrentItem.IsSingle; } }

    [DebuggerHidden]
    public Letter MainLetter { get { return MainIterator.CurrentLetter; } }

    [DebuggerHidden]
    public ScanSpaceItem SecondaryItem { get { Debug.Assert(HasSecondary()); return GetSecondary().CurrentItem; } }

    [DebuggerHidden]
    public Letter SecondaryLetter { get { Debug.Assert(HasSecondary()); return GetSecondary().CurrentLetter; } }

    public void Reset()
    {
        SecondaryIteratorTable.Clear();
        ReachedMainIndex = 0;
        ItemToResetAtWhenCompatible = null;
    }

    public bool MainMoveNext()
    {
        if (MainIterator.IsAtLastCharacterOfCurrentItem && ItemToResetAtWhenCompatible is not null)
        {
            if (MainIterator.IsItemCompatibleWith(ItemToResetAtWhenCompatible))
            {
                ItemToResetAtWhenCompatible = null;
                MainIterator.Reset();
                return MainIterator.MoveNext();
            }
        }

        do
        {
            bool IsMoved;
            if (MainIterator.ItemIndex < 0)
                IsMoved = MainIterator.MoveNext();
            else if (MainIterator.ItemIndex < ReachedMainIndex)
                IsMoved = MainIterator.MoveNext(IteratorMoveOption.PartialOnly);
            else
                IsMoved = MainIterator.MoveNext();

            if (!IsMoved)
                return false;
        }
        while ((MainItem.IsSingle && MainIterator.ItemIndex < ReachedMainIndex) || (!MainItem.IsSingle && HasSecondary() && GetSecondary().IsCompleted));

        if (MainItem.IsSingle && ReachedMainIndex < MainIterator.ItemIndex)
            ReachedMainIndex = MainIterator.ItemIndex;

        if (MainIterator.IsAtLastCharacterOfLastItem)
        {
            bool AllSecondaryIteratorsCompleted = true;
            foreach (KeyValuePair<Letter, ScanSpaceIterator> Entry in SecondaryIteratorTable)
            {
                ScanSpaceIterator SecondaryIterator = Entry.Value;
                if (!SecondaryIterator.IsAtLastCharacterOfLastItem)
                {
                    AllSecondaryIteratorsCompleted = false;
                    break;
                }
            }

            if (!AllSecondaryIteratorsCompleted)
                ItemToResetAtWhenCompatible = MainIterator.ItemList[MainIterator.ItemList.Count - 1];
        }

        return true;
    }

    public bool SecondaryMoveNext()
    {
        Debug.Assert(MainIterator.IsEnumerationValid);
        Debug.Assert(!MainItem.IsSingle);

        ScanSpaceIterator SecondaryIterator = GetSecondary();

        if (SecondaryIterator.IsNotCompatibleWithPrimary(MainIterator, out ScanSpaceItem IncompatibleItem))
        {
            ItemToResetAtWhenCompatible = IncompatibleItem;
            return false;
        }

        if (!SecondaryIterator.MoveNext())
            return false;

        Debug.Assert(SecondaryItem.IsSingle);

        return true;
    }

    private bool HasSecondary()
    {
        return SecondaryIteratorTable.ContainsKey(MainIterator.CurrentLetter);
    }

    private ScanSpaceIterator GetSecondary()
    {
        Letter MainLetter = MainIterator.CurrentLetter;

        if (!SecondaryIteratorTable.ContainsKey(MainLetter))
        {
            ScanSpaceIterator SecondaryIterator = new ScanSpaceIterator(SecondaryScanSpace);
            SecondaryIteratorTable.Add(MainLetter, SecondaryIterator);
        }

        return SecondaryIteratorTable[MainLetter];
    }

    private Dictionary<Letter, ScanSpaceIterator> SecondaryIteratorTable { get; }
    private int ReachedMainIndex;
    private ScanSpaceItem? ItemToResetAtWhenCompatible;
}
