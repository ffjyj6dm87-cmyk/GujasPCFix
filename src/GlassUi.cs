using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GujasPCFix
{
    internal static class Glass
    {
        public static GraphicsPath RoundRect(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            if (d < 2) d = 2;
            if (d > bounds.Width) d = Math.Max(2, bounds.Width);
            if (d > bounds.Height) d = Math.Max(2, bounds.Height);
            Rectangle arc = new Rectangle(bounds.Location, new Size(d, d));
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - d;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - d;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void Liquid(Graphics g, Control control, Rectangle bounds, Color tint, Color sheen, Color edge, int radius)
        {
            Theme.Quality(g);
            Rectangle r = bounds;
            if (r.Width < 4 || r.Height < 4) return;
            r.Width -= 1;
            r.Height -= 1;

            using (GraphicsPath path = RoundRect(r, radius))
            using (GraphicsPath shadow = RoundRect(new Rectangle(r.X, r.Y + 6, r.Width, r.Height), radius))
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
            {
                g.FillPath(shadowBrush, shadow);

                GraphicsState state = g.Save();
                g.SetClip(path, CombineMode.Replace);
                DrawFrostCrop(g, control, bounds);
                using (SolidBrush veil = new SolidBrush(tint))
                    g.FillRectangle(veil, bounds);
                Rectangle top = new Rectangle(r.X, r.Y, r.Width, Math.Max(12, r.Height / 2));
                using (LinearGradientBrush shine = new LinearGradientBrush(top, sheen, Color.FromArgb(0, sheen), LinearGradientMode.Vertical))
                    g.FillRectangle(shine, top);
                Rectangle glare = new Rectangle(r.X + 10, r.Y + 3, Math.Max(20, r.Width / 3), Math.Max(8, r.Height / 7));
                using (GraphicsPath glarePath = new GraphicsPath())
                {
                    glarePath.AddEllipse(glare);
                    using (PathGradientBrush pgb = new PathGradientBrush(glarePath))
                    {
                        pgb.CenterColor = Color.FromArgb(70, 255, 255, 255);
                        pgb.SurroundColors = new Color[] { Color.FromArgb(0, 255, 255, 255) };
                        g.FillPath(pgb, glarePath);
                    }
                }
                g.Restore(state);

                using (Pen inner = new Pen(Color.FromArgb(70, 255, 255, 255), 1f))
                {
                    Rectangle inset = r;
                    inset.Inflate(-1, -1);
                    using (GraphicsPath innerPath = RoundRect(inset, Math.Max(2, radius - 1)))
                        g.DrawPath(inner, innerPath);
                }
                using (Pen border = new Pen(edge, 1.4f))
                    g.DrawPath(border, path);
            }
        }

        private static void DrawFrostCrop(Graphics g, Control control, Rectangle bounds)
        {
            Form form = control.FindForm();
            IGlassHost host = form as IGlassHost;
            if (form == null || host == null || host.Frost == null)
                return;
            Rectangle screen = control.RectangleToScreen(bounds);
            Rectangle src = form.RectangleToClient(screen);
            if (src.Width < 1 || src.Height < 1) return;
            g.DrawImage(host.Frost, bounds, src, GraphicsUnit.Pixel);
        }
    }

    internal class GlassPanel : Panel
    {
        public GlassPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Theme.Quality(e.Graphics);
            Rectangle r = ClientRectangle;
            r.Width -= 1;
            r.Height -= 1;
            using (GraphicsPath path = Glass.RoundRect(r, 14))
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(18, 25, 42)))
            using (Pen border = new Pen(Color.FromArgb(39, 50, 74)))
            {
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            }
            base.OnPaint(e);
        }
    }

    internal class GlassButton : Button
    {
        private bool _hot;
        private bool _down;
        public bool Emphasized;

        public GlassButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent;
            ForeColor = Color.White;
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI Semibold", 11.5f);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hot = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hot = false;
            _down = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            _down = true;
            Invalidate();
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _down = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Theme.Quality(pevent.Graphics);
            Color fill = Emphasized
                ? (_down ? Color.FromArgb(96, 68, 218) : (_hot ? Color.FromArgb(139, 107, 255) : Color.FromArgb(124, 92, 255)))
                : (_down ? Color.FromArgb(26, 35, 54) : (_hot ? Color.FromArgb(39, 50, 74) : Color.FromArgb(27, 36, 56)));
            Color edge = Emphasized ? Color.FromArgb(151, 126, 255) : Color.FromArgb(50, 62, 87);
            Rectangle r = ClientRectangle;
            r.Width -= 1;
            r.Height -= 1;
            using (GraphicsPath path = Glass.RoundRect(r, 10))
            using (SolidBrush brush = new SolidBrush(fill))
            using (Pen pen = new Pen(edge))
            {
                pevent.Graphics.FillPath(brush, path);
                pevent.Graphics.DrawPath(pen, path);
            }
            pevent.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            TextRenderer.DrawText(pevent.Graphics, Text, Font, ClientRectangle, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}
