using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Taixiu
{
    public partial class MainWindow : Window
    {
        long myMoney = 10000000;
        long betTai = 0;
        long betXiu = 0;
        long currentChip = 10000;
        bool isRolling = false;

        // Particle system fields
        readonly List<Particle> particles = new();
        readonly Random rnd = new();
        const int ParticleCount = 70;
        const double ConnectionDistance = 120;

        // Discord users (simulated)
        readonly List<string> discordUsers = new();

        public MainWindow()
        {
            InitializeComponent();
            UpdateUI();
        }

        void UpdateUI()
        {
            txtTotalMoney.Text = myMoney.ToString("N0");
            txtBetTai.Text = betTai.ToString("N0");
            txtBetXiu.Text = betXiu.ToString("N0");
        }

        // --- Game switching ---
        private void SetGameView(string game)
        {
            TaiXiuGrid.Visibility = game == "TaiXiu" ? Visibility.Visible : Visibility.Collapsed;
            BauCuaGrid.Visibility = game == "BauCua" ? Visibility.Visible : Visibility.Collapsed;
            TienLenGrid.Visibility = game == "TienLen" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnGameTaiXiu_Click(object sender, RoutedEventArgs e) => SetGameView("TaiXiu");
        private void BtnGameBauCua_Click(object sender, RoutedEventArgs e) => SetGameView("BauCua");
        private void BtnGameTienLen_Click(object sender, RoutedEventArgs e) => SetGameView("TienLen");

        // --- Left panel toggle ---
        private void BtnToggleLeft_Click(object sender, RoutedEventArgs e)
        {
            LeftPanel.Visibility = LeftPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        // --- Discord login simulation popup ---
        private void BtnDiscordLogin_Click(object sender, RoutedEventArgs e)
        {
            // In a real integration you'd start OAuth2 flow here.
            // For now we show a simple input popup so you can simulate logged users.
            txtDiscordName.Text = string.Empty;
            DiscordLoginPopup.Visibility = Visibility.Visible;
        }

        private void BtnDiscordCancel_Click(object sender, RoutedEventArgs e)
        {
            DiscordLoginPopup.Visibility = Visibility.Collapsed;
        }

        private void BtnDiscordConfirm_Click(object sender, RoutedEventArgs e)
        {
            var name = txtDiscordName.Text?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Enter a display name to simulate Discord login.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AddDiscordUser(name);
            DiscordLoginPopup.Visibility = Visibility.Collapsed;
            // Ensure left panel visible so user sees logged users
            LeftPanel.Visibility = Visibility.Visible;
        }

        void AddDiscordUser(string displayName)
        {
            // Prevent duplicates for demo
            if (!discordUsers.Contains(displayName))
            {
                discordUsers.Add(displayName);
                UpdateDiscordUserList();
            }
        }

        void UpdateDiscordUserList()
        {
            lstDiscordUsers.Items.Clear();
            foreach (var u in discordUsers)
            {
                lstDiscordUsers.Items.Add(new ListBoxItem { Content = u });
            }
        }

        private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            // Ensure ParticleCanvas covers window
            ParticleCanvas.Width = ActualWidth;
            ParticleCanvas.Height = ActualHeight;
            SizeChanged += (_, __) =>
            {
                ParticleCanvas.Width = ActualWidth;
                ParticleCanvas.Height = ActualHeight;
            };

            InitializeParticles();
            CompositionTarget.Rendering += OnRender;
        }

        private void MainWindow_Unloaded(object? sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering -= OnRender;
        }

        void InitializeParticles()
        {
            particles.Clear();
            ParticleCanvas.Children.Clear();

            for (int i = 0; i < ParticleCount; i++)
            {
                var p = new Particle
                {
                    X = rnd.NextDouble() * Math.Max(ParticleCanvas.ActualWidth, ActualWidth),
                    Y = rnd.NextDouble() * Math.Max(ParticleCanvas.ActualHeight, ActualHeight),
                    VX = (rnd.NextDouble() - 0.5) * 0.6,
                    VY = (rnd.NextDouble() - 0.5) * 0.6,
                    Radius = rnd.Next(2, 4)
                };

                var ellipse = new Ellipse
                {
                    Width = p.Radius * 2,
                    Height = p.Radius * 2,
                    Fill = Brushes.White,
                    Opacity = 0.9
                };

                p.UI = ellipse;

                // Ensure particles are above the connecting lines
                Canvas.SetZIndex(ellipse, 2);
                ParticleCanvas.Children.Add(ellipse);
                particles.Add(p);
            }
        }

        void OnRender(object? sender, EventArgs e)
        {
            double width = ParticleCanvas.ActualWidth;
            double height = ParticleCanvas.ActualHeight;
            if (width <= 0 || height <= 0) return;

            // Update particle positions
            foreach (var p in particles)
            {
                p.X += p.VX;
                p.Y += p.VY;

                if (p.X <= 0 || p.X >= width) p.VX = -p.VX;
                if (p.Y <= 0 || p.Y >= height) p.VY = -p.VY;

                Canvas.SetLeft(p.UI, p.X - p.Radius);
                Canvas.SetTop(p.UI, p.Y - p.Radius);
            }

            // Remove previous connection lines
            for (int i = ParticleCanvas.Children.Count - 1; i >= 0; i--)
            {
                if (ParticleCanvas.Children[i] is Line) ParticleCanvas.Children.RemoveAt(i);
            }

            // Draw connections
            for (int i = 0; i < particles.Count; i++)
            {
                var a = particles[i];
                for (int j = i + 1; j < particles.Count; j++)
                {
                    var b = particles[j];
                    double dx = a.X - b.X;
                    double dy = a.Y - b.Y;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist <= ConnectionDistance)
                    {
                        double alpha = 1.0 - (dist / ConnectionDistance);
                        var line = new Line
                        {
                            X1 = a.X,
                            Y1 = a.Y,
                            X2 = b.X,
                            Y2 = b.Y,
                            Stroke = Brushes.White,
                            StrokeThickness = 1,
                            Opacity = 0.12 + 0.7 * alpha // subtle to strong
                        };
                        Canvas.SetZIndex(line, 1); // behind the white dots
                        ParticleCanvas.Children.Add(line);
                    }
                }
            }
        }

        // --- Existing game logic ---
        private void BtnSelectChip_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            currentChip = long.Parse(btn.Tag.ToString());
            txtSelectedChip.Text = "Đang chọn: " + currentChip.ToString("N0");
        }

        private void BtnBetTai_Click(object sender, RoutedEventArgs e)
        {
            if (isRolling) return;
            if (myMoney >= currentChip)
            {
                myMoney -= currentChip;
                betTai += currentChip;
                UpdateUI();
            }
        }

        private void BtnBetXiu_Click(object sender, RoutedEventArgs e)
        {
            if (isRolling) return;
            if (myMoney >= currentChip)
            {
                myMoney -= currentChip;
                betXiu += currentChip;
                UpdateUI();
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            if (isRolling) return;
            myMoney += betTai + betXiu;
            betTai = 0;
            betXiu = 0;
            UpdateUI();
        }

        private async void BtnRoll_Click(object sender, RoutedEventArgs e)
        {
            if (betTai == 0 && betXiu == 0) return;

            isRolling = true;
            btnXoc.IsEnabled = false;
            BowlCover.Visibility = Visibility.Visible;

            await System.Threading.Tasks.Task.Delay(1500); // Chờ 1.5s

            Random rnd = new();
            int d1 = rnd.Next(1, 7);
            int d2 = rnd.Next(1, 7);
            int d3 = rnd.Next(1, 7);
            int total = d1 + d2 + d3;

            string[] icons = { "⚀", "⚁", "⚂", "⚃", "⚄", "⚅" };
            dice1.Text = icons[d1 - 1];
            dice2.Text = icons[d2 - 1];
            dice3.Text = icons[d3 - 1];
            txtResultScore.Text = total.ToString();

            BowlCover.Visibility = Visibility.Collapsed;

            long win = 0;
            if (total >= 11 && total <= 17) // TÀI
            {
                if (betTai > 0) win += betTai * 2;
                MessageBox.Show("VỀ TÀI (" + total + ")");
            }
            else // XỈU
            {
                if (betXiu > 0) win += betXiu * 2;
                MessageBox.Show("VỀ XỈU (" + total + ")");
            }

            if (win > 0) myMoney += win;

            betTai = 0; betXiu = 0;
            isRolling = false;
            btnXoc.IsEnabled = true;
            UpdateUI();
        }

        private void BtnDonate_Click(object sender, RoutedEventArgs e)
        {
            PopupQR.Visibility = Visibility.Visible;
        }

        private void CloseQR_Click(object sender, RoutedEventArgs e)
        {
            PopupQR.Visibility = Visibility.Collapsed;
        }

        // --- Placeholder handlers for new games ---
        private void BtnBauCuaBet_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Bầu Cua demo: betting not yet implemented. Use this handler to implement real logic.", "Bầu Cua", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnTienLenStart_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Tiến Lên demo: game start not implemented. Create a separate window or view for full gameplay.", "Tiến Lên", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Particle helper class
        class Particle
        {
            public double X;
            public double Y;
            public double VX;
            public double VY;
            public double Radius;
            public Ellipse UI = null!;
        }

        // Add this field to your MainWindow class if it does not already exist
        private Canvas ParticleCanvas => (Canvas)FindName("ParticleCanvas");
    }
}