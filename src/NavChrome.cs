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
        Activity = 3
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

    internal sealed class MenuPane : GlassPanel
    {
        public AppPage Page = AppPage.Home;
        public bool OptimizeOpen = true;
        public event EventHandler PageChanged;

        public MenuPane()
        {
            Size = new Size(300, 720);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Glass.Liquid(e.Graphics, this, ClientRectangle,
                Color.FromArgb(110, 10, 10, 12),
                Color.FromArgb(75, 255, 255, 255),
                Color.FromArgb(145, 230, 230, 235),
                28);
            Graphics g = e.Graphics;
            Theme.Quality(g);
            using (Font title = new Font("Segoe UI Semibold", 13f))
            using (SolidBrush tb = new SolidBrush(Color.White))
            {
                Icons.Draw(g, "spark", new Rectangle(22, 22, 16, 16), Color.White);
                g.DrawString("Menu", title, tb, 44, 18);
            }

            int y = 62;
            y = DrawRow(g, y, "Home", AppPage.Home, false);
            y = DrawRow(g, y, "Optimize", AppPage.Optimize, true);
            if (OptimizeOpen)
            {
                y = DrawChild(g, y, "60 performance tweaks");
                y = DrawChild(g, y, "Search and categories");
                y = DrawChild(g, y, "Restore Windows defaults");
            }
            y = DrawRow(g, y, "Games Tweaker", AppPage.Tuner, false);
            DrawRow(g, y, "Activity", AppPage.Activity, false);
        }

        private int DrawRow(Graphics g, int y, string text, AppPage page, bool expander)
        {
            Rectangle pill = new Rectangle(16, y, Width - 32, 42);
            bool on = Page == page;
            using (GraphicsPath path = Glass.RoundRect(pill, 21))
            {
                if (on)
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(240, 255, 255, 255)))
                        g.FillPath(b, path);
                }
            }
            Color fg = on ? Color.FromArgb(18, 18, 20) : Color.FromArgb(235, 235, 238);
            using (Font font = new Font("Segoe UI", 10.5f, on ? FontStyle.Bold : FontStyle.Regular))
            using (SolidBrush brush = new SolidBrush(fg))
            {
                g.DrawString(text, font, brush, 36, y + 10);
            }
            if (expander)
            {
                string mark = OptimizeOpen ? "-" : "+";
                using (Font font = new Font("Segoe UI", 12f))
                using (SolidBrush brush = new SolidBrush(fg))
                    g.DrawString(mark, font, brush, Width - 48, y + 8);
            }
            return y + 48;
        }

        private int DrawChild(Graphics g, int y, string text)
        {
            Rectangle pill = new Rectangle(40, y, Width - 56, 34);
            using (GraphicsPath path = Glass.RoundRect(pill, 17))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(Page == AppPage.Optimize ? 40 : 18, 255, 255, 255)))
                g.FillPath(b, path);
            using (Font font = new Font("Segoe UI", 9f))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(210, 210, 214)))
                g.DrawString(text, font, brush, 56, y + 8);
            return y + 38;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            int y = 62;
            if (Hit(e, y)) SetPage(AppPage.Home);
            y += 48;
            Rectangle opt = new Rectangle(16, y, Width - 32, 42);
            if (opt.Contains(e.Location))
            {
                if (e.X > Width - 56)
                    OptimizeOpen = !OptimizeOpen;
                else
                    SetPage(AppPage.Optimize);
                Invalidate();
                return;
            }
            y += 48;
            if (OptimizeOpen) y += 38 * 3;
            if (Hit(e, y)) SetPage(AppPage.Tuner);
            y += 48;
            if (Hit(e, y)) SetPage(AppPage.Activity);
            base.OnMouseClick(e);
        }

        private bool Hit(MouseEventArgs e, int y)
        {
            return new Rectangle(16, y, Width - 32, 42).Contains(e.Location);
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
