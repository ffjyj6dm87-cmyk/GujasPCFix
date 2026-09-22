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
            ClientSize = new Size(520, 300);
            DoubleBuffered = true;
            BackColor = Color.Black;
            Opacity = 1.0;
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
            using (GraphicsPath round = Glass.RoundRect(new Rectangle(0, 0, Width, Height), 22))
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
            e.Graphics.Clear(Color.FromArgb(8, 13, 24));
            using (LinearGradientBrush glow = new LinearGradientBrush(ClientRectangle,
                Color.FromArgb(35, 29, 74), Color.FromArgb(8, 13, 24), LinearGradientMode.ForwardDiagonal))
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

            using (SolidBrush glow = new SolidBrush(Color.FromArgb(ClampByte(38 * fadeOut), 124, 92, 255)))
                g.FillEllipse(glow, Width / 2 - 130, 28, 260, 210);

            int a = ClampByte(welcomeAlpha * fadeOut * 255);
            using (Font hero = new Font("Segoe UI", 25f, FontStyle.Bold, GraphicsUnit.Point))
            using (Font brand = new Font("Segoe UI Semibold", 11f, FontStyle.Regular, GraphicsUnit.Point))
            using (Font subtle = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point))
            using (SolidBrush white = new SolidBrush(Color.FromArgb(a, 255, 255, 255)))
            using (SolidBrush mute = new SolidBrush(Color.FromArgb(ClampByte(welcomeAlpha * fadeOut * 190), 210, 210, 214)))
            using (StringFormat center = new StringFormat())
            {
                center.Alignment = StringAlignment.Center;
                center.LineAlignment = StringAlignment.Center;
                g.DrawString("GUJAS PC FIX", hero, white, new RectangleF(0, 70, Width, 48), center);
                g.DrawString("PERFORMANCE CONTROL CENTER", brand, white, new RectangleF(0, 119, Width, 24), center);
                g.DrawString("Preparing system scan and optimization tools", subtle, mute, new RectangleF(0, 153, Width, 20), center);
            }

            Rectangle bar = new Rectangle((Width - 220) / 2, 214, 220, 6);
            using (GraphicsPath track = Glass.RoundRect(bar, 4))
            using (SolidBrush trackBrush = new SolidBrush(Color.FromArgb(ClampByte(90 * fadeOut), 255, 255, 255)))
                g.FillPath(trackBrush, track);
            if (load > 0.01f)
            {
                Rectangle fill = new Rectangle(bar.X, bar.Y, Math.Max(8, (int)(bar.Width * load)), bar.Height);
                using (GraphicsPath fillPath = Glass.RoundRect(fill, 4))
                using (LinearGradientBrush lg = new LinearGradientBrush(fill, Color.FromArgb(ClampByte(245 * fadeOut), 124, 92, 255), Color.FromArgb(ClampByte(220 * fadeOut), 34, 211, 238), LinearGradientMode.Horizontal))
                    g.FillPath(lg, fillPath);
            }
            using (Font version = new Font("Segoe UI", 8.5f))
            using (SolidBrush subtleBrush = new SolidBrush(Color.FromArgb(ClampByte(150 * fadeOut), 139, 151, 174)))
            using (StringFormat center = new StringFormat())
            {
                center.Alignment = StringAlignment.Center;
                g.DrawString("VERSION 2.0", version, subtleBrush, new RectangleF(0, 244, Width, 20), center);
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
