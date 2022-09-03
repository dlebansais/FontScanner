using FontScanner;
using NUnit.Framework;
using System.Drawing;

namespace TestFontScanner
{
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
}