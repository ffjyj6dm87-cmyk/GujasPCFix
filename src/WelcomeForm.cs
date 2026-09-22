using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Diagnostics;

namespace GujasPCFix
{
    internal sealed class WelcomeForm : Form, IGlassHost
    {
        private readonly Timer _timer = new Timer();
        private readonly Stopwatch _clock = new Stopwatch();
        private Bitmap _frost;
        private bool _closing;

        public Image Frost
        {
            get { return _frost; }
        }

        public WelcomeForm()
        {
            Text = "Gujas PC Fix";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(560, 330);
            DoubleBuffered = true;
            BackColor = Color.Black;
            Opacity = 0.82;
            ShowInTaskbar = false;
            TopMost = true;
            Theme.LoadWallpaper();
            BackgroundImage = null;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            _timer.Interval = 16;
            _timer.Tick += delegate { TickFrame(); };
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            RebuildFrost();
            using (GraphicsPath round = Glass.RoundRect(new Rectangle(0, 0, Width, Height), 28))
            {
                Region = new Region(round);
            }
            _clock.Start();
            _timer.Start();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _timer.Stop();
            if (_frost != null) _frost.Dispose();
            base.OnFormClosed(e);
        }

        private void RebuildFrost()
        {
            if (_frost != null) _frost.Dispose();
            _frost = Theme.MakeFrost(ClientSize.Width, ClientSize.Height);
        }

        private void TickFrame()
        {
            if (_clock.ElapsedMilliseconds >= 1400)
            {
                Finish();
                return;
            }
            Invalidate();
        }

        private void Finish()
        {
            if (_closing) return;
            _closing = true;
            _timer.Stop();
            Close();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Theme.Quality(e.Graphics);
            e.Graphics.Clear(Color.FromArgb(7, 12, 23));
            using (LinearGradientBrush glow = new LinearGradientBrush(ClientRectangle,
                Color.FromArgb(12, 42, 78), Color.FromArgb(7, 15, 23), LinearGradientMode.ForwardDiagonal))
                e.Graphics.FillRectangle(glow, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);
            double t = _clock.Elapsed.TotalMilliseconds;
            float welcomeAlpha;
            float load;
            float fadeOut;
            if (t < 400)
            {
                welcomeAlpha = Ease((float)(t / 400.0));
                load = 0;
                fadeOut = 1f;
            }
            else if (t < 1100)
            {
                welcomeAlpha = 1f;
                load = Ease((float)((t - 400.0) / 700.0));
                fadeOut = 1f;
            }
            else
            {
                welcomeAlpha = 1f;
                load = 1f;
                fadeOut = 1f - Ease((float)((t - 1100.0) / 300.0));
            }

            using (SolidBrush glow = new SolidBrush(Color.FromArgb(ClampByte(42 * fadeOut), 36, 143, 255)))
                g.FillEllipse(glow, Width / 2 - 155, 18, 310, 245);

            using (Pen border = new Pen(Color.FromArgb(ClampByte(110 * fadeOut), 65, 145, 255), 1.2f))
            using (GraphicsPath borderPath = Glass.RoundRect(new Rectangle(1, 1, Width - 3, Height - 3), 27))
                g.DrawPath(border, borderPath);

            Rectangle logo = new Rectangle(Width / 2 - 24, 43, 48, 48);
            using (GraphicsPath logoPath = Glass.RoundRect(logo, 13))
            using (LinearGradientBrush logoFill = new LinearGradientBrush(logo, Color.FromArgb(22, 90, 255), Color.FromArgb(77, 208, 225), LinearGradientMode.ForwardDiagonal))
                g.FillPath(logoFill, logoPath);
            using (Font logoFont = new Font("Segoe UI", 20f, FontStyle.Bold))
            using (SolidBrush logoText = new SolidBrush(Color.White))
            using (StringFormat logoCenter = new StringFormat())
            {
                logoCenter.Alignment = StringAlignment.Center;
                logoCenter.LineAlignment = StringAlignment.Center;
                g.DrawString("G", logoFont, logoText, logo, logoCenter);
            }

            int a = ClampByte(welcomeAlpha * fadeOut * 255);
            using (Font hero = new Font("Segoe UI", 24f, FontStyle.Bold, GraphicsUnit.Point))
            using (Font brand = new Font("Segoe UI Semibold", 9.5f, FontStyle.Regular, GraphicsUnit.Point))
            using (Font subtle = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point))
            using (SolidBrush white = new SolidBrush(Color.FromArgb(a, 255, 255, 255)))
            using (SolidBrush mute = new SolidBrush(Color.FromArgb(ClampByte(welcomeAlpha * fadeOut * 190), 210, 210, 214)))
            using (StringFormat center = new StringFormat())
            {
                center.Alignment = StringAlignment.Center;
                center.LineAlignment = StringAlignment.Center;
                g.DrawString("GUJAS PC FIX", hero, white, new RectangleF(0, 105, Width, 48), center);
                g.DrawString("PERFORMANCE CONTROL CENTER", brand, white, new RectangleF(0, 151, Width, 24), center);
                g.DrawString("Welcome to your performance control center", subtle, mute, new RectangleF(0, 184, Width, 20), center);
            }

            Rectangle bar = new Rectangle((Width - 250) / 2, 238, 250, 7);
            using (GraphicsPath track = Glass.RoundRect(bar, 4))
            using (SolidBrush trackBrush = new SolidBrush(Color.FromArgb(ClampByte(90 * fadeOut), 255, 255, 255)))
                g.FillPath(trackBrush, track);
            if (load > 0.01f)
            {
                Rectangle fill = new Rectangle(bar.X, bar.Y, Math.Max(8, (int)(bar.Width * load)), bar.Height);
                using (GraphicsPath fillPath = Glass.RoundRect(fill, 4))
                using (LinearGradientBrush lg = new LinearGradientBrush(fill, Color.FromArgb(ClampByte(245 * fadeOut), 36, 143, 255), Color.FromArgb(ClampByte(220 * fadeOut), 34, 211, 238), LinearGradientMode.Horizontal))
                    g.FillPath(lg, fillPath);
            }
            using (Font version = new Font("Segoe UI", 8.5f))
            using (SolidBrush subtleBrush = new SolidBrush(Color.FromArgb(ClampByte(150 * fadeOut), 139, 151, 174)))
            using (StringFormat center = new StringFormat())
            {
                center.Alignment = StringAlignment.Center;
                g.DrawString("VERSION 2.1  •  REFERENCE EDITION", version, subtleBrush, new RectangleF(0, 272, Width, 20), center);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
                Finish();
            base.OnKeyDown(e);
        }

        private static float Ease(float x)
        {
            if (x < 0) return 0;
            if (x > 1) return 1;
            return x * x * (3f - 2f * x);
        }

        private static int ClampByte(double value)
        {
            if (value < 0) return 0;
            if (value > 255) return 255;
            return (int)value;
        }
    }
}
