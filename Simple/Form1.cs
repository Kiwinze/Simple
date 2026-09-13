using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace MusicPlayerApp
{
    public partial class Form1 : Form
    {
        private WMPLib.WindowsMediaPlayer wmp;
        private readonly List<string> _filePaths = new List<string>();

        public Form1()
        {
            InitializeComponent();
        }

        // ----------------------------------------------------
        // Загрузка формы
        // ----------------------------------------------------
        private void Form1_Load(object sender, EventArgs e)
        {
            wmp = new WMPLib.WindowsMediaPlayer();
            wmp.settings.volume = trackVolume.Value;
            trackVolume.Value = 50;

            trackProgress.Maximum = 100;
            trackProgress.Value = 0;

            lblStatus.Text = "Пауза";
            lblTime.Text = "00:00";
        }

        // ----------------------------------------------------
        // Выбор файлов
        // ----------------------------------------------------
        private void btnSelectSong_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Выберите аудиофайлы";
                ofd.Multiselect = true;
                ofd.Filter = "MP3 файлы (*.mp3)|*.mp3|" +
                             "WAV файлы (*.wav)|*.wav|" +
                             "Все аудио|*.mp3;*.wav;*.wma;*.m4a;*.aac";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    foreach (var file in ofd.FileNames)
                    {
                        string display = Path.GetFileName(file);
                        if (!lstPlaylist.Items.Contains(display))
                        {
                            lstPlaylist.Items.Add(display);
                            _filePaths.Add(file);
                        }
                    }

                    if (lstPlaylist.Items.Count > 0 && lstPlaylist.SelectedIndex < 0)
                        lstPlaylist.SelectedIndex = 0;
                }
            }
        }

        // ----------------------------------------------------
        // Двойной клик по песне
        // ----------------------------------------------------
        private void lstPlaylist_DoubleClick(object sender, EventArgs e)
        {
            PlaySelected();
        }

        // ----------------------------------------------------
        // Play / Pause
        // ----------------------------------------------------
        private void btnPlay_Click(object sender, EventArgs e)
        {
            if (wmp == null) return;

            if (string.IsNullOrEmpty(wmp.URL))
            {
                PlaySelected();
                return;
            }

            if (wmp.playState == WMPLib.WMPPlayState.wmppsPlaying)
            {
                wmp.controls.pause();
                btnPlay.Text = "▶";
                lblStatus.Text = "Пауза";
                waveformPanel.IsPlaying = false;
            }
            else
            {
                wmp.controls.play();
                btnPlay.Text = "❚❚";
                lblStatus.Text = "Включено";
                waveformPanel.IsPlaying = true;
            }
        }

        // ----------------------------------------------------
        // Предыдущий трек
        // ----------------------------------------------------
        private void btnPrev_Click(object sender, EventArgs e)
        {
            if (lstPlaylist.Items.Count == 0) return;

            int idx = lstPlaylist.SelectedIndex - 1;
            if (idx < 0) idx = lstPlaylist.Items.Count - 1;

            lstPlaylist.SelectedIndex = idx;
            PlaySelected();
        }

        // ----------------------------------------------------
        // Следующий трек
        // ----------------------------------------------------
        private void btnNext_Click(object sender, EventArgs e)
        {
            if (lstPlaylist.Items.Count == 0) return;

            int idx = lstPlaylist.SelectedIndex + 1;
            if (idx >= lstPlaylist.Items.Count) idx = 0;

            lstPlaylist.SelectedIndex = idx;
            PlaySelected();
        }

        // ----------------------------------------------------
        // Воспроизведение выбранного трека
        // ----------------------------------------------------
        private void PlaySelected()
        {
            if (lstPlaylist.SelectedIndex < 0)
            {
                MessageBox.Show("Сначала выберите песню из списка.", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_filePaths.Count == 0 ||
                lstPlaylist.SelectedIndex >= _filePaths.Count)
            {
                MessageBox.Show("Файл не найден. Нажмите 'Select Song' заново.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string path = _filePaths[lstPlaylist.SelectedIndex];

            if (!File.Exists(path))
            {
                MessageBox.Show("Файл не найден: " + path, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            wmp.URL = path;
            wmp.controls.play();

            btnPlay.Text = "❚❚";
            lblStatus.Text = "Включено";
            waveformPanel.IsPlaying = true;

            timerProgress.Start();
        }

        // ----------------------------------------------------
        // Таймер: обновление прогресса и времени
        // ----------------------------------------------------
        private void timerProgress_Tick(object sender, EventArgs e)
        {
            if (wmp == null || wmp.currentMedia == null) return;

            double duration = wmp.currentMedia.duration;
            double position = wmp.controls.currentPosition;

            if (duration > 0)
            {
                float progress = (float)(position / duration);
                trackProgress.Value = (int)(progress * 100);
                waveformPanel.Progress = progress;
            }

            lblTime.Text = FormatTime(position);

            // Автопереход к следующему треку
            if (wmp.playState == WMPLib.WMPPlayState.wmppsStopped &&
                btnPlay.Text == "❚❚")
            {
                btnNext_Click(null, null);
            }
        }

        // ----------------------------------------------------
        // Перемотка
        // ----------------------------------------------------
        private void trackProgress_Scroll(object sender, EventArgs e)
        {
            if (wmp == null || wmp.currentMedia == null) return;

            double duration = wmp.currentMedia.duration;
            if (duration > 0)
            {
                wmp.controls.currentPosition = duration * trackProgress.Value / 100.0;
            }
        }

        // ----------------------------------------------------
        // Громкость
        // ----------------------------------------------------
        private void trackVolume_Scroll(object sender, EventArgs e)
        {
            if (wmp != null)
                wmp.settings.volume = trackVolume.Value;
        }

        // ----------------------------------------------------
        // Форматирование времени мм:сс
        // ----------------------------------------------------
        private string FormatTime(double seconds)
        {
            int totalSec = (int)seconds;
            int min = totalSec / 60;
            int sec = totalSec % 60;
            return $"{min:00}:{sec:00}";
        }

        // ----------------------------------------------------
        // Закрытие
        // ----------------------------------------------------
        private void btnClose_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Закрыть приложение?", "Выход",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (wmp != null)
                    wmp.controls.stop();
                Application.Exit();
            }
        }
    }
}