namespace FontScanner;

using FontLoader;
using System.Collections.Generic;
using System.Diagnostics;

public class ScanSpaceIterator
{
    public ScanSpaceIterator(ScanSpace scanSpace)
    {
        ScanSpace = scanSpace;
        CurrentLetter = Letter.EmptyNormal;
        ItemCount = ScanSpace.ItemList.Count;
        Reset();
    }

    public ScanSpace ScanSpace { get; }
    public Letter CurrentLetter { get; private set; }
    public bool IsSingle { get; private set; }
    public int ItemIndex { get; private set; }
    public int ItemCount { get; }

    public void Reset()
    {
        ItemIndex = 0;
        CharacterIndex = 0;
        FontSizeIndex = 0;
    }

    public bool MoveNext()
    {
        return MoveNextInternal(IteratorMoveOption.Default);
    }

    public bool MoveNextNoWhitespace()
    {
        do
        {
            if (!MoveNextInternal(IteratorMoveOption.Default))
                return false;
        }
        while (CurrentLetter.IsWhitespace);

        return true;
    }

    public bool MoveNextNoWhitespaceSingleOnly()
    {
        do
        {
            if (!MoveNextInternal(IteratorMoveOption.SingleOnly))
                return false;
        }
        while (CurrentLetter.IsWhitespace || !IsSingle);

        return true;
    }

    public bool MoveNextNoWhitespacePartialOnly()
    {
        do
        {
            if (!MoveNextInternal(IteratorMoveOption.PartialOnly))
                return false;
        }
        while (CurrentLetter.IsWhitespace || IsSingle);

        return true;
    }

    private bool MoveNextInternal(IteratorMoveOption option)
    {
        if (ItemIndex >= ItemCount)
            return false;

        List<ScanSpaceItem> ItemList = ScanSpace.ItemList;
        ScanSpaceItem Item = ItemList[ItemIndex];
        Debug.Assert(CharacterIndex < Item.CharacterList.Count + Item.SuperscriptList.Count);
        Debug.Assert(FontSizeIndex < Item.FontSizeList.Count);

        double FontSize = Item.FontSizeList[FontSizeIndex];

        TypeFlags TypeFlags = Item.TypeFlags;
        bool IsBlue = TypeFlags.HasFlag(TypeFlags.Blue);
        bool IsItalic = TypeFlags.HasFlag(TypeFlags.Italic);
        bool IsBold = TypeFlags.HasFlag(TypeFlags.Bold);

        LetterType CurrentLetterType = new(FontSize, IsBlue, IsItalic, IsBold);

        if (CharacterIndex < Item.CharacterList.Count)
            CurrentLetter = new(Item.CharacterList[CharacterIndex], CurrentLetterType);
        else
            CurrentLetter = new(Item.SuperscriptList[CharacterIndex - Item.CharacterList.Count], CurrentLetterType);

        IsSingle = Item.IsSingle;

        Increment(option);
        return true;
    }

    private void Increment(IteratorMoveOption option)
    {
        ScanSpaceItem Item = ScanSpace.ItemList[ItemIndex];

        if ((Item.IsSingle && option == IteratorMoveOption.PartialOnly) || (!Item.IsSingle && option == IteratorMoveOption.SingleOnly))
        {
            ItemIndex++;
            CharacterIndex = 0;
            FontSizeIndex = 0;
        }
        else
        {
            CharacterIndex++;
            if (CharacterIndex >= Item.CharacterList.Count + Item.SuperscriptList.Count)
            {
                CharacterIndex = 0;
                FontSizeIndex++;

                if (FontSizeIndex >= Item.FontSizeList.Count)
                {
                    FontSizeIndex = 0;
                    ItemIndex++;
                }
            }
        }
    }

    private int CharacterIndex;
    private int FontSizeIndex;
}
