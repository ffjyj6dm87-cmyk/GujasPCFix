using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GujasPCFix
{
    internal enum AppPage
    {
        Home = 0,
        Optimize = 1,
        Tuner = 2,
        SystemInfo = 3,
        Restore = 4,
        Settings = 5,
        Activity = 6
    }

    internal static class Icons
    {
        public static void Draw(Graphics g, string name, Rectangle r, Color color)
        {
            Theme.Quality(g);
            using (Pen pen = new Pen(color, 1.7f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                int x = r.X;
                int y = r.Y;
                int w = r.Width;
                int h = r.Height;
                if (name == "spark")
                {
                    g.DrawLine(pen, x + w / 2, y + 2, x + w / 2, y + h - 2);
                    g.DrawLine(pen, x + 2, y + h / 2, x + w - 2, y + h / 2);
                    g.DrawLine(pen, x + 5, y + 5, x + w - 5, y + h - 5);
                    g.DrawLine(pen, x + w - 5, y + 5, x + 5, y + h - 5);
                }
                else if (name == "home")
                {
                    Point[] pts = new Point[]
                    {
                        new Point(x + w / 2, y + 3),
                        new Point(x + w - 3, y + h / 2),
                        new Point(x + w - 3, y + h - 3),
                        new Point(x + 3, y + h - 3),
                        new Point(x + 3, y + h / 2)
                    };
                    g.DrawPolygon(pen, pts);
                }
                else if (name == "boost")
                {
                    Point[] pts = new Point[]
                    {
                        new Point(x + w / 2 + 2, y + 2),
                        new Point(x + 4, y + h / 2 + 1),
                        new Point(x + w / 2 - 1, y + h / 2 + 1),
                        new Point(x + w / 2 - 2, y + h - 2),
                        new Point(x + w - 3, y + h / 2 - 1),
                        new Point(x + w / 2 + 1, y + h / 2 - 1)
                    };
                    g.DrawPolygon(pen, pts);
                }
                else if (name == "chip")
                {
                    g.DrawRectangle(pen, x + 5, y + 5, w - 10, h - 10);
                    g.DrawLine(pen, x + 2, y + 8, x + 5, y + 8);
                    g.DrawLine(pen, x + w - 5, y + 8, x + w - 2, y + 8);
                    g.DrawLine(pen, x + 2, y + h - 8, x + 5, y + h - 8);
                    g.DrawLine(pen, x + w - 5, y + h - 8, x + w - 2, y + h - 8);
                }
                else if (name == "pulse")
                {
                    g.DrawLines(pen, new Point[]
                    {
                        new Point(x + 2, y + h / 2),
                        new Point(x + 6, y + h / 2),
                        new Point(x + 9, y + 4),
                        new Point(x + 13, y + h - 4),
                        new Point(x + 16, y + h / 2),
                        new Point(x + w - 2, y + h / 2)
                    });
                }
                else if (name == "restore")
                {
                    g.DrawArc(pen, x + 3, y + 4, w - 7, h - 7, -55, 285);
                    g.DrawLine(pen, x + 3, y + 4, x + 3, y + 11);
                    g.DrawLine(pen, x + 3, y + 4, x + 10, y + 4);
                }
                else if (name == "settings")
                {
                    g.DrawEllipse(pen, x + 3, y + 3, w - 6, h - 6);
                    g.DrawEllipse(pen, x + 8, y + 8, w - 16, h - 16);
                    g.DrawLine(pen, x + w / 2, y, x + w / 2, y + 4);
                    g.DrawLine(pen, x + w / 2, y + h - 4, x + w / 2, y + h);
                    g.DrawLine(pen, x, y + h / 2, x + 4, y + h / 2);
                    g.DrawLine(pen, x + w - 4, y + h / 2, x + w, y + h / 2);
                }
            }
        }
    }

    internal sealed class IconRail : GlassPanel
    {
        public AppPage Page = AppPage.Home;
        public event EventHandler PageChanged;

        public IconRail()
        {
            Size = new Size(68, 720);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Glass.Liquid(e.Graphics, this, ClientRectangle,
                Color.FromArgb(100, 12, 12, 14),
                Color.FromArgb(80, 255, 255, 255),
                Color.FromArgb(140, 230, 230, 235),
                Height / 2 > 40 ? 34 : 20);
            DrawSlot(e.Graphics, 0, "spark", false);
            DrawSlot(e.Graphics, 1, "home", Page == AppPage.Home);
            DrawSlot(e.Graphics, 2, "boost", Page == AppPage.Optimize);
            DrawSlot(e.Graphics, 3, "chip", Page == AppPage.Tuner);
            DrawSlot(e.Graphics, 4, "pulse", Page == AppPage.Activity);
        }

        private void DrawSlot(Graphics g, int index, string icon, bool selected)
        {
            int y = 22 + index * 64;
            Rectangle circle = new Rectangle(16, y, 36, 36);
            if (selected)
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(235, 255, 255, 255)))
                    g.FillEllipse(b, circle);
            }
            Color color = selected ? Color.FromArgb(20, 20, 22) : Color.FromArgb(230, 230, 232);
            Icons.Draw(g, icon, new Rectangle(circle.X + 7, circle.Y + 7, 22, 22), color);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            int index = (e.Y - 22) / 64;
            if (index >= 1 && index <= 4)
            {
                AppPage next = (AppPage)(index - 1);
                if (next != Page)
                {
                    Page = next;
                    Invalidate();
                    if (PageChanged != null) PageChanged(this, EventArgs.Empty);
                }
            }
            base.OnMouseClick(e);
        }
    }

    internal sealed class MenuPane : Panel
    {
        public AppPage Page = AppPage.Home;
        public event EventHandler PageChanged;
        private AppPage _hoverPage = (AppPage)(-1);

        public MenuPane()
        {
            Size = new Size(238, 760);
            BackColor = Color.FromArgb(13, 18, 31);
            DoubleBuffered = true;
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            if (Width < 4 || Height < 4) return;
            Region old = Region;
            using (GraphicsPath path = Glass.RoundRect(new Rectangle(0, 0, Width, Height), 18))
                Region = new Region(path);
            if (old != null) old.Dispose();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);
            g.Clear(Color.FromArgb(13, 18, 31));
            using (Pen edge = new Pen(Color.FromArgb(39, 50, 74)))
                g.DrawRectangle(edge, 0, 0, Width - 1, Height - 1);
            using (Font logo = new Font("Segoe UI", 18f, FontStyle.Bold))
            using (Font version = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (SolidBrush white = new SolidBrush(Color.White))
            using (SolidBrush violet = new SolidBrush(Color.FromArgb(157, 132, 255)))
            {
                g.DrawString("GUJAS", logo, white, 28, 25);
                g.DrawString("PC FIX", logo, violet, 98, 25);
                g.DrawString("VERSION 2.0", version, violet, 31, 62);
            }
            using (Font section = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (SolidBrush muted = new SolidBrush(Color.FromArgb(103, 115, 139)))
                g.DrawString("CONTROL CENTER", section, muted, 30, 102);

            int y = 128;
            y = DrawRow(g, y, "Dashboard", "System overview", "home", AppPage.Home);
            y = DrawRow(g, y, "PC Tweaks", "60 optimizations", "boost", AppPage.Optimize);
            y = DrawRow(g, y, "Games Tweaker", "5 game profiles", "chip", AppPage.Tuner);
            y = DrawRow(g, y, "System Info", "Hardware details", "pulse", AppPage.SystemInfo);
            y = DrawRow(g, y, "Restore", "Undo changes", "restore", AppPage.Restore);
            DrawRow(g, y, "Settings", "App preferences", "settings", AppPage.Settings);

            using (SolidBrush card = new SolidBrush(Color.FromArgb(20, 28, 46)))
            using (GraphicsPath path = Glass.RoundRect(new Rectangle(22, Height - 105, Width - 44, 76), 14))
                g.FillPath(card, path);
            using (SolidBrush green = new SolidBrush(Color.FromArgb(52, 211, 153))) g.FillEllipse(green, 38, Height - 79, 9, 9);
            using (Font status = new Font("Segoe UI Semibold", 9.5f))
            using (Font small = new Font("Segoe UI", 8f))
            using (SolidBrush white = new SolidBrush(Color.FromArgb(235, 240, 250)))
            using (SolidBrush muted = new SolidBrush(Color.FromArgb(135, 148, 172)))
            {
                g.DrawString("Protection active", status, white, 55, Height - 87);
                g.DrawString("Restore points enabled", small, muted, 38, Height - 61);
            }
        }

        private int DrawRow(Graphics g, int y, string text, string sub, string icon, AppPage page)
        {
            Rectangle pill = new Rectangle(18, y, Width - 36, 58);
            bool on = Page == page;
            bool hover = _hoverPage == page;
            using (GraphicsPath path = Glass.RoundRect(pill, 12))
            {
                using (SolidBrush b = new SolidBrush(on ? Color.FromArgb(38, 32, 73) : (hover ? Color.FromArgb(22, 30, 49) : Color.FromArgb(0, 13, 18, 31))))
                    g.FillPath(b, path);
            }
            if (on) using (SolidBrush bar = new SolidBrush(Color.FromArgb(124, 92, 255))) g.FillRectangle(bar, 18, y + 12, 4, 34);
            Color fg = on ? Color.White : Color.FromArgb(195, 204, 220);
            Icons.Draw(g, icon, new Rectangle(36, y + 18, 20, 20), on ? Color.FromArgb(157,132,255) : Color.FromArgb(123,137,161));
            using (Font font = new Font("Segoe UI Semibold", 10f))
            using (Font subFont = new Font("Segoe UI", 8f))
            using (SolidBrush brush = new SolidBrush(fg))
            using (SolidBrush subBrush = new SolidBrush(Color.FromArgb(105, 119, 143)))
            {
                g.DrawString(text, font, brush, 68, y + 10);
                g.DrawString(sub, subFont, subBrush, 68, y + 32);
            }
            return y + 66;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            int y = 128;
            AppPage[] pages = { AppPage.Home, AppPage.Optimize, AppPage.Tuner, AppPage.SystemInfo, AppPage.Restore, AppPage.Settings };
            for (int i = 0; i < pages.Length; i++, y += 66)
                if (Hit(e, y)) { SetPage(pages[i]); break; }
            base.OnMouseClick(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            AppPage next = (AppPage)(-1);
            int y = 128;
            AppPage[] pages = { AppPage.Home, AppPage.Optimize, AppPage.Tuner, AppPage.SystemInfo, AppPage.Restore, AppPage.Settings };
            for (int i = 0; i < pages.Length; i++, y += 66) if (Hit(e, y)) { next = pages[i]; break; }
            if (next != _hoverPage) { _hoverPage = next; Invalidate(); }
            Cursor = next == (AppPage)(-1) ? Cursors.Default : Cursors.Hand;
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hoverPage = (AppPage)(-1);
            Cursor = Cursors.Default;
            Invalidate();
            base.OnMouseLeave(e);
        }

        private bool Hit(MouseEventArgs e, int y)
        {
            return new Rectangle(18, y, Width - 36, 58).Contains(e.Location);
        }

        private void SetPage(AppPage page)
        {
            if (page == Page) return;
            Page = page;
            Invalidate();
            if (PageChanged != null) PageChanged(this, EventArgs.Empty);
        }
    }
}
