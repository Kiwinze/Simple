using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MusicPlayerApp
{
    /// <summary>
    /// Кастомная панель, рисующая звуковую волну.
    /// Активная волна рисуется зелёным, "сыгранная" часть — ярче.
    /// </summary>
    public class WaveformPanel : Panel
    {
        private float _progress = 0f;      // 0..1
        private bool _isPlaying = false;
        private readonly Random _rnd = new Random();

        // Кэш "точек" волны, чтобы рисунок не "дрожал" каждые полсекунды
        private float[] _wavePoints;

        public WaveformPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.Black;
            Resize += (s, e) => RebuildWave();
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set { _isPlaying = value; Invalidate(); }
        }

        public float Progress
        {
            get => _progress;
            set
            {
                _progress = Math.Max(0f, Math.Min(1f, value));
                Invalidate();
            }
        }

        private void RebuildWave()
        {
            int steps = Math.Max(100, Width / 2);
            _wavePoints = new float[steps];
            for (int i = 0; i < steps; i++)
            {
                // Смесь нескольких синусоид + шум — похоже на реальную волну
                float x = (float)i / steps;
                float y = (float)(
                    Math.Sin(x * 25) * 0.4 +
                    Math.Sin(x * 60) * 0.25 +
                    Math.Sin(x * 130) * 0.15 +
                    (_rnd.NextDouble() - 0.5) * 0.3);
                _wavePoints[i] = y;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Фон
            g.Clear(Color.Black);

            if (_wavePoints == null || _wavePoints.Length == 0)
                RebuildWave();

            int midY = Height / 2;
            int amplitude = (int)(Height * 0.35);
            float stepX = (float)Width / _wavePoints.Length;

            // Инициализация пера: неактивная часть - тёмно-зелёная,
            // активная - яркая
            using (var penInactive = new Pen(Color.FromArgb(0, 90, 0), 1.4f))
            using (var penActive = new Pen(Color.FromArgb(60, 220, 90), 1.4f))
            {
                int activeIndex = (int)(_wavePoints.Length * _progress);

                for (int i = 0; i < _wavePoints.Length - 1; i++)
                {
                    float x1 = i * stepX;
                    float y1 = midY + _wavePoints[i] * amplitude;
                    float x2 = (i + 1) * stepX;
                    float y2 = midY + _wavePoints[i + 1] * amplitude;

                    var pen = i < activeIndex ? penActive : penInactive;
                    g.DrawLine(pen, x1, y1, x2, y2);

                    // Зеркальное отражение (для симметрии)
                    g.DrawLine(pen, x1, midY - (_wavePoints[i] * amplitude),
                                    x2, midY - (_wavePoints[i + 1] * amplitude));
                }

                // Центральная линия
                using (var centerPen = new Pen(Color.FromArgb(30, 60, 30), 1f))
                {
                    g.DrawLine(centerPen, 0, midY, Width, midY);
                }
            }
        }
    }
}