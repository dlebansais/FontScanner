namespace TestFontScanner;

using FontScanner;
using NUnit.Framework;
using System.Drawing;

[TestFixture]
public class TestPageImage
{
    [Test]
    public void Test1()
    {
        Bitmap TestBitmap = new(256, 256, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        PageImage Image = new PageImage(TestBitmap);
    }
}