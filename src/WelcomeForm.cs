using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
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

        [DllImport("winmm.dll")]
        private static extern uint timeBeginPeriod(uint period);

        [DllImport("winmm.dll")]
        private static extern uint timeEndPeriod(uint period);

        public Image Frost
        {
            get { return _frost; }
        }

        public WelcomeForm()
        {
            Text = "Gujas PC Fix";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(460, 250);
            DoubleBuffered = true;
            BackColor = Color.Black;
            Opacity = 0.82;
            ShowInTaskbar = false;
            TopMost = true;
            Theme.LoadWallpaper();
            BackgroundImage = null;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            _timer.Interval = 8;
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
            timeBeginPeriod(1);
            _clock.Start();
            _timer.Start();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _timer.Stop();
            timeEndPeriod(1);
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
            if (_clock.ElapsedMilliseconds >= 2400)
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
            if (Theme.Wallpaper != null)
                e.Graphics.DrawImage(Theme.Wallpaper, ClientRectangle);
            else
                e.Graphics.Clear(Color.FromArgb(20, 20, 22));
            using (SolidBrush veil = new SolidBrush(Color.FromArgb(90, 8, 8, 10)))
                e.Graphics.FillRectangle(veil, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);
            double t = _clock.Elapsed.TotalMilliseconds;
            float welcomeAlpha;
            float load;
            float fadeOut;
            if (t < 700)
            {
                welcomeAlpha = Ease((float)(t / 700.0));
                load = 0;
                fadeOut = 1f;
            }
            else if (t < 1900)
            {
                welcomeAlpha = 1f;
                load = Ease((float)((t - 700.0) / 1200.0));
                fadeOut = 1f;
            }
            else
            {
                welcomeAlpha = 1f;
                load = 1f;
                fadeOut = 1f - Ease((float)((t - 1900.0) / 500.0));
            }

            using (SolidBrush dim = new SolidBrush(Color.FromArgb((int)(50 * fadeOut), 0, 0, 0)))
                g.FillRectangle(dim, ClientRectangle);

            int a = ClampByte(welcomeAlpha * fadeOut * 255);
            using (Font hero = new Font("Segoe UI Light", 28f, FontStyle.Regular, GraphicsUnit.Point))
            using (Font brand = new Font("Segoe UI Semibold", 12f, FontStyle.Regular, GraphicsUnit.Point))
            using (Font subtle = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point))
            using (SolidBrush white = new SolidBrush(Color.FromArgb(a, 255, 255, 255)))
            using (SolidBrush mute = new SolidBrush(Color.FromArgb(ClampByte(welcomeAlpha * fadeOut * 190), 210, 210, 214)))
            using (StringFormat center = new StringFormat())
            {
                center.Alignment = StringAlignment.Center;
                center.LineAlignment = StringAlignment.Center;
                g.DrawString("Welcome", hero, white, new RectangleF(0, 48, Width, 48), center);
                g.DrawString("Gujas PC Fix", brand, white, new RectangleF(0, 98, Width, 24), center);
                g.DrawString("Preparing your system console", subtle, mute, new RectangleF(0, 128, Width, 20), center);
            }

            Rectangle bar = new Rectangle((Width - 180) / 2, 178, 180, 6);
            using (GraphicsPath track = Glass.RoundRect(bar, 4))
            using (SolidBrush trackBrush = new SolidBrush(Color.FromArgb(ClampByte(90 * fadeOut), 255, 255, 255)))
                g.FillPath(trackBrush, track);
            if (load > 0.01f)
            {
                Rectangle fill = new Rectangle(bar.X, bar.Y, Math.Max(8, (int)(bar.Width * load)), bar.Height);
                using (GraphicsPath fillPath = Glass.RoundRect(fill, 4))
                using (LinearGradientBrush lg = new LinearGradientBrush(fill, Color.FromArgb(ClampByte(240 * fadeOut), 255, 255, 255), Color.FromArgb(ClampByte(160 * fadeOut), 200, 200, 205), LinearGradientMode.Horizontal))
                    g.FillPath(lg, fillPath);
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
