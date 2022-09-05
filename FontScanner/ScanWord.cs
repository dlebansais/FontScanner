namespace FontScanner;

using FontLoader;
using System.Collections.Generic;
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
    public List<Letter> Text { get; set; } = new();

    public int Baseline { get { return Line.Baseline; } }
    public int EffectiveWidth { get { return LetterOffsetList[LetterOffsetList.Count - 1].Offset + LetterOffsetList[LetterOffsetList.Count - 1].LetterWidth; } }
    public Rectangle EffectiveRect { get { return new Rectangle(Rect.Left, Rect.Top, EffectiveWidth, Rect.Height); } }
    
    public string DisplayText
    {
        get
        {
            string Result = string.Empty;

            foreach (Letter Item in Text)
                Result += Item.DisplayText;

            return Result;
        }
    }

    public void Clear()
    {
        Text.Clear();
    }
}
