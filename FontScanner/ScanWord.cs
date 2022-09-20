namespace FontScanner;

using FontLoader;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;

[DebuggerDisplay("({Rect.Left}, {Rect.Top}) - ({Rect.Width}, {Rect.Height}) - '{DisplayText}'")]
public class ScanWord
{
    public ScanWord(Rectangle rect, ScanLine line)
    {
        Rect = rect;
        Line = line;
    }

    public Rectangle Rect { get; }
    public ScanLine Line { get; }
    public List<LetterOffset> LetterOffsetList { get; } = new();
    private List<Letter> LetterList { get; set; } = new();
    private List<Rectangle> RectList { get; set; } = new();

    public int Baseline { get { return Line.Baseline; } }
    public int EffectiveWidth { get { return LetterOffsetList[LetterOffsetList.Count - 1].Offset + LetterOffsetList[LetterOffsetList.Count - 1].LetterWidth; } }
    public Rectangle EffectiveRect { get { return new Rectangle(Rect.Left, Rect.Top, EffectiveWidth, Rect.Height); } }
    public ReadOnlyCollection<Letter> Text { get { return LetterList.AsReadOnly(); } }
    public ReadOnlyCollection<Rectangle> TextRect { get { return RectList.AsReadOnly(); } }

    public void AddLetter(Letter letter, Rectangle letterRect)
    {
        LetterList.Add(letter);
        RectList.Add(letterRect);
    }

    public void Merge(ScanWord other)
    {
        LetterList.AddRange(other.LetterList);
        RectList.AddRange(other.RectList);
    }

    public string DisplayText
    {
        get
        {
            string Result = string.Empty;

            foreach (Letter Item in LetterList)
                Result += Item.DisplayText;

            return Result;
        }
    }

    public void Clear()
    {
        LetterList.Clear();
        RectList.Clear();
    }
}
