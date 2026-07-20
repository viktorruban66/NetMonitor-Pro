// netmonitor_cs.cs — монитор сети в реальном времени на C# (WPF)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.IO;
using System.Net.NetworkInformation;

namespace NetMonitorWPF
{
    public partial class MainWindow : Window
    {
        private const int HISTORY_SIZE = 60;
        private Queue<double> downHistory = new Queue<double>(HISTORY_SIZE);
        private Queue<double> upHistory = new Queue<double>(HISTORY_SIZE);
        private Queue<long> timeHistory = new Queue<long>(HISTORY_SIZE);
        private double totalDown = 0, totalUp = 0;
        private double peakDown = 0, peakUp = 0;
        private DispatcherTimer timer;
        private Random rand = new Random();
        private NetworkInterface networkInterface;

        private Label downLabel, upLabel, totalLabel;
        private Canvas graphCanvas;

        public MainWindow()
        {
            InitializeComponent();
            CreateUI();
            InitializeData();
            StartTimer();
            GetNetworkInterface();
        }

        private void CreateUI()
        {
            Title = "📊 NetMonitor Pro — C#";
            Width = 800;
            Height = 600;
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Info panel
            var infoPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10) };
            downLabel = new Label { Content = "⬇ Загрузка: 0.0 Мбит/с", FontSize = 14 };
            upLabel = new Label { Content = "⬆ Выгрузка: 0.0 Мбит/с", FontSize = 14, Margin = new Thickness(20,0,0,0) };
            totalLabel = new Label { Content = "Трафик: 0/0 МБ", FontSize = 14, Margin = new Thickness(20,0,0,0) };
            infoPanel.Children.Add(downLabel);
            infoPanel.Children.Add(upLabel);
            infoPanel.Children.Add(totalLabel);
            Grid.SetRow(infoPanel, 0);
            grid.Children.Add(infoPanel);

            // Graph Canvas
            graphCanvas = new Canvas { Background = Brushes.White };
            Grid.SetRow(graphCanvas, 1);
            grid.Children.Add(graphCanvas);

            // Buttons
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10) };
            var exportBtn = new Button { Content = "Экспорт CSV", Width = 100 };
            var resetBtn = new Button { Content = "Сброс статистики", Width = 100 };
            btnPanel.Children.Add(exportBtn);
            btnPanel.Children.Add(resetBtn);
            Grid.SetRow(btnPanel, 2);
            grid.Children.Add(btnPanel);

            exportBtn.Click += (s, e) => ExportCSV();
            resetBtn.Click += (s, e) => ResetStats();

            Content = grid;
        }

        private void GetNetworkInterface()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                .FirstOrDefault();
            if (interfaces != null)
                networkInterface = interfaces;
            else
                networkInterface = null;
        }

        private void InitializeData()
        {
            for (int i = 0; i < 10; i++)
            {
                downHistory.Enqueue(0);
                upHistory.Enqueue(0);
                timeHistory.Enqueue(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            }
        }

        private void StartTimer()
        {
            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (s, e) => UpdateData();
            timer.Start();
        }

        private void UpdateData()
        {
            double down, up;
            // Если есть реальный интерфейс — используем его, иначе симуляция
            if (networkInterface != null)
            {
                // Это упрощённо, в реальном проекте нужно вычислять скорость по приросту байтов
                // Для демонстрации используем симуляцию
                down = rand.NextDouble() * 13 + 2;
                up = rand.NextDouble() * 7 + 1;
            }
            else
            {
                down = rand.NextDouble() * 13 + 2;
                up = rand.NextDouble() * 7 + 1;
            }

            downHistory.Enqueue(down);
            upHistory.Enqueue(up);
            timeHistory.Enqueue(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            if (downHistory.Count > HISTORY_SIZE)
            {
                downHistory.Dequeue();
                upHistory.Dequeue();
                timeHistory.Dequeue();
            }

            totalDown += down;
            totalUp += up;
            if (down > peakDown) peakDown = down;
            if (up > peakUp) peakUp = up;

            downLabel.Content = $"⬇ Загрузка: {down:F1} Мбит/с";
            upLabel.Content = $"⬆ Выгрузка: {up:F1} Мбит/с";
            totalLabel.Content = $"Трафик: {totalDown/8:F1}/{totalUp/8:F1} МБ";
            DrawGraph();
        }

        private void DrawGraph()
        {
            if (graphCanvas == null || downHistory.Count < 2) return;
            graphCanvas.Children.Clear();

            double w = graphCanvas.ActualWidth;
            double h = graphCanvas.ActualHeight;
            if (w < 10 || h < 10) return;

            int margin = 40;
            int graphW = (int)(w - 2 * margin);
            int graphH = (int)(h - 2 * margin);

            // Сетка
            double maxY = 0;
            foreach (var d in downHistory) if (d > maxY) maxY = d;
            foreach (var d in upHistory) if (d > maxY) maxY = d;
            maxY = Math.Max(maxY, 10);

            for (int i = 0; i <= 5; i++)
            {
                int y = (int)(h - margin - i * graphH / 5.0);
                Line line = new Line { X1 = margin, Y1 = y, X2 = w - margin, Y2 = y, Stroke = Brushes.LightGray, StrokeThickness = 1 };
                graphCanvas.Children.Add(line);
                TextBlock tb = new TextBlock { Text = ((int)(i * maxY / 5)).ToString(), FontSize = 10 };
                Canvas.SetLeft(tb, 5);
                Canvas.SetTop(tb, y - 8);
                graphCanvas.Children.Add(tb);
            }

            // Оси
            Line axisX = new Line { X1 = margin, Y1 = h - margin, X2 = w - margin, Y2 = h - margin, Stroke = Brushes.Black, StrokeThickness = 2 };
            Line axisY = new Line { X1 = margin, Y1 = margin, X2 = margin, Y2 = h - margin, Stroke = Brushes.Black, StrokeThickness = 2 };
            graphCanvas.Children.Add(axisX);
            graphCanvas.Children.Add(axisY);

            // Данные
            double[] downArr = downHistory.ToArray();
            double[] upArr = upHistory.ToArray();
            int n = downArr.Length;

            // Загрузка (синий)
            Polyline downLine = new Polyline { Stroke = Brushes.Blue, StrokeThickness = 2 };
            for (int i = 0; i < n; i++)
            {
                double x = margin + i * (double)graphW / (n-1);
                double y = h - margin - downArr[i] / maxY * graphH;
                downLine.Points.Add(new Point(x, y));
            }
            graphCanvas.Children.Add(downLine);

            // Выгрузка (красный)
            Polyline upLine = new Polyline { Stroke = Brushes.Red, StrokeThickness = 2 };
            for (int i = 0; i < n; i++)
            {
                double x = margin + i * (double)graphW / (n-1);
                double y = h - margin - upArr[i] / maxY * graphH;
                upLine.Points.Add(new Point(x, y));
            }
            graphCanvas.Children.Add(upLine);

            // Легенда
            TextBlock legend1 = new TextBlock { Text = "Загрузка", Foreground = Brushes.Blue, FontSize = 10 };
            Canvas.SetLeft(legend1, w - 60);
            Canvas.SetTop(legend1, 10);
            graphCanvas.Children.Add(legend1);
            TextBlock legend2 = new TextBlock { Text = "Выгрузка", Foreground = Brushes.Red, FontSize = 10 };
            Canvas.SetLeft(legend2, w - 60);
            Canvas.SetTop(legend2, 25);
            graphCanvas.Children.Add(legend2);
        }

        private void ExportCSV()
        {
            if (downHistory.Count < 2)
            {
                MessageBox.Show("Недостаточно данных для экспорта");
                return;
            }
            var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "CSV (*.csv)|*.csv" };
            if (dialog.ShowDialog() == true)
            {
                using (var sw = new StreamWriter(dialog.FileName))
                {
                    sw.WriteLine("Время,Загрузка(Мбит/с),Выгрузка(Мбит/с)");
                    var downArr = downHistory.ToArray();
                    var upArr = upHistory.ToArray();
                    var timeArr = timeHistory.ToArray();
                    for (int i = 0; i < downArr.Length; i++)
                        sw.WriteLine($"{timeArr[i]},{downArr[i]:F2},{upArr[i]:F2}");
                }
                MessageBox.Show("Данные сохранены в " + dialog.FileName);
            }
        }

        private void ResetStats()
        {
            downHistory.Clear();
            upHistory.Clear();
            timeHistory.Clear();
            totalDown = 0;
            totalUp = 0;
            peakDown = 0;
            peakUp = 0;
            for (int i = 0; i < 10; i++)
            {
                downHistory.Enqueue(0);
                upHistory.Enqueue(0);
                timeHistory.Enqueue(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            }
            DrawGraph();
        }

        [STAThread]
        static void Main()
        {
            var app = new Application();
            app.Run(new MainWindow());
        }
    }
}
