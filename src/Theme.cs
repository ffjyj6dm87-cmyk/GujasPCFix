using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

namespace GujasPCFix
{
    internal interface IGlassHost
    {
        Image Frost { get; }
    }

    internal static class Theme
    {
        public static Image Wallpaper;

        public static void LoadWallpaper()
        {
            if (Wallpaper != null) return;
            Wallpaper = FromResource("GujasPCFix.bg.jpg");
            if (Wallpaper == null) Wallpaper = FromResource("GujasPCFix.bg.png");
            if (Wallpaper == null)
            {
                string jpg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "bg.jpg");
                string png = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "bg.png");
                if (File.Exists(jpg)) Wallpaper = Image.FromFile(jpg);
                else if (File.Exists(png)) Wallpaper = Image.FromFile(png);
            }
        }

        public static Bitmap MakeFrost(int width, int height)
        {
            if (width < 8) width = 8;
            if (height < 8) height = 8;
            Bitmap scene = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(scene))
            {
                Quality(g);
                g.Clear(Color.Black);
                if (Wallpaper != null)
                    g.DrawImage(Wallpaper, new Rectangle(0, 0, width, height));
            }

            int tw = Math.Max(8, width / 14);
            int th = Math.Max(8, height / 14);
            Bitmap tiny = new Bitmap(tw, th, PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(tiny))
            {
                Quality(g);
                g.DrawImage(scene, 0, 0, tw, th);
            }
            scene.Dispose();

            Bitmap frost = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(frost))
            {
                Quality(g);
                g.DrawImage(tiny, 0, 0, width, height);
            }
            tiny.Dispose();
            return frost;
        }

        public static void Quality(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        }

        private static Image FromResource(string name)
        {
            try
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
                if (stream == null) return null;
                using (stream)
                {
                    return Image.FromStream(stream);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
