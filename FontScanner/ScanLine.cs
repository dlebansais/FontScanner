namespace FontScanner;

using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

[DebuggerDisplay("{LineNumber}, {Rect.Top}, {Rect.Height}, {Baseline}, {Words.Count} word(s)")]
public class ScanLine
{
    public int LineNumber { get; set; }
    public Rectangle Rect { get; set; }
    public int Baseline { get; set; }
    public List<ScanWord> Words { get; } = new();

    public int EffectiveRight { get { return Words.Count > 0 ? Words[Words.Count - 1].EffectiveRect.Right : 0; } }
}
