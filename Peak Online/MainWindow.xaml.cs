using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PeakOnline
{
    public partial class MainWindow : Window
    {
        private const int MaxHistorySize = 10; // Edit here to adjust maximum paths to be stored.
        private const string LastUsedDirectoryKey = "LastUsedDirectory";
        private const string FileHistoryKey = "FileHistory";
        private const string BackgroundColorKey = "BackgroundColor";

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void BtnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
            };

            // Set initial directory from settings if available
            string lastUsedDirectory = ReadSetting(LastUsedDirectoryKey);
            if (!string.IsNullOrEmpty(lastUsedDirectory))
            {
                openFileDialog.InitialDirectory = lastUsedDirectory;
            }

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFile = openFileDialog.FileName;

                // Store the directory of the selected file in settings
                WriteSetting(LastUsedDirectoryKey, System.IO.Path.GetDirectoryName(selectedFile));

                // Add file path to history
                AddFilePathToHistory(selectedFile);

                // Load log data and draw the graph
                LoadLogData(selectedFile);
            }
        }

        private void LoadLogData(string filePath)
        {
            try
            {
                TextFileParser parser = new TextFileParser();

                // This will give you both peakPlayers and dailyPeaks
                var (peakPlayers, dailyPeaks) = parser.ParseLogFile(filePath);

                // Draw the graph based on daily peaks
                DrawGraph(dailyPeaks);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading log data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DrawGraph(List<TextFileParser.DailyPeak> dailyPeaks)
        {
            GraphCanvas.Children.Clear();

            if (dailyPeaks == null || dailyPeaks.Count == 0)
                return;

            double canvasWidth = GraphCanvas.ActualWidth;
            double canvasHeight = GraphCanvas.ActualHeight;
            double xStep = canvasWidth / dailyPeaks.Count;

            int maxPeakPlayers = dailyPeaks.Max(p => p.PeakPlayers);
            double barWidth = xStep * 0.8; // Width of each bar

            for (int i = 0; i < dailyPeaks.Count; i++)
            {
                var dailyPeak = dailyPeaks[i];

                // Calculate bar height based on peak players
                double barHeight = dailyPeak.PeakPlayers / (double)maxPeakPlayers * canvasHeight;

                // Create a rectangle for the bar
                Rectangle bar = new Rectangle
                {
                    Width = barWidth,
                    Height = barHeight,
                    Fill = Brushes.Blue,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };

                // Position the bar
                Canvas.SetLeft(bar, i * xStep + (xStep - barWidth) / 2);
                Canvas.SetBottom(bar, 0);

                // Create a text block for the peak number
                TextBlock peakText = new TextBlock
                {
                    Text = dailyPeak.PeakPlayers.ToString(),
                    Foreground = Brushes.Black,
                    FontWeight = FontWeights.Bold,
                    FontSize = 10, // Base font size for peak players
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                // Position the text block above the bar
                Canvas.SetLeft(peakText, i * xStep + (xStep - barWidth) / 2);
                Canvas.SetBottom(peakText, barHeight + 5); // Add some padding above the bar

                // Format the date string as left-to-right
                string dateString = dailyPeak.Date.ToString("dd/MM");

                // Create a text block for the date
                TextBlock dateText = new TextBlock
                {
                    Text = dateString,
                    Foreground = Brushes.Red,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Left, // Align text to the left
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Dynamically adjust the font size based on the width of the bar
                double maxFontSize = 12; // Define the maximum font size
                double minFontSize = 8; // Define the minimum font size
                double availableWidth = barWidth - 20; // Adjust for padding
                double calculatedFontSize = Math.Min(availableWidth / dateString.Length * 1.5, maxFontSize);
                calculatedFontSize = Math.Max(calculatedFontSize, minFontSize); // Ensure it doesn't go below min

                dateText.FontSize = calculatedFontSize;

                // Position the date text inside the bar, aligned to the left
                Canvas.SetLeft(dateText, i * xStep + (xStep - barWidth) / 2 + 5); // Adjust position for padding
                Canvas.SetBottom(dateText, barHeight / 2 - calculatedFontSize / 2); // Center vertically

                // Add the bar and text to the canvas
                GraphCanvas.Children.Add(bar);
                GraphCanvas.Children.Add(peakText);
                GraphCanvas.Children.Add(dateText);
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // Allow the window to be dragged
                this.DragMove();
            }
        }

        private void CmbBackgroundColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbBackgroundColor.SelectedItem is ComboBoxItem selectedItem)
            {
                string color = selectedItem.Content.ToString();
                Background = (Brush)new BrushConverter().ConvertFromString(color);
                WriteSetting(BackgroundColorKey, color);
            }
        }

        private void BtnResetHistory_Click(object sender, RoutedEventArgs e)
        {
            // Clear the file history
            WriteSetting(FileHistoryKey, string.Empty);
            LoadFilePaths();
        }

        private string ReadSetting(string key)
        {
            using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (isoStore.FileExists(key))
                {
                    using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(key, FileMode.Open, isoStore))
                    using (StreamReader reader = new StreamReader(isoStream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            return string.Empty;
        }

        private void WriteSetting(string key, string value)
        {
            using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
            using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(key, FileMode.Create, isoStore))
            using (StreamWriter writer = new StreamWriter(isoStream))
            {
                writer.Write(value);
            }
        }

        private void AddFilePathToHistory(string filePath)
        {
            // Get the existing history from settings
            List<string> fileHistory = ReadFileHistory();

            // Add the new file path if it's not already in the list
            if (!fileHistory.Contains(filePath))
            {
                fileHistory.Add(filePath);

                // Ensure history size does not exceed limit
                if (fileHistory.Count > MaxHistorySize)
                {
                    fileHistory.RemoveAt(0);
                }

                // Update the settings with new history
                WriteSetting(FileHistoryKey, string.Join(";", fileHistory));
            }

            // Update the ListBox
            LoadFilePaths();
        }

        private List<string> ReadFileHistory()
        {
            string historyString = ReadSetting(FileHistoryKey);
            return string.IsNullOrEmpty(historyString) ? new List<string>() : historyString.Split(';').ToList();
        }

        private void LoadSettings()
        {
            // Set background color from settings
            string backgroundColor = ReadSetting(BackgroundColorKey);
            if (!string.IsNullOrEmpty(backgroundColor))
            {
                Background = (Brush)new BrushConverter().ConvertFromString(backgroundColor);
            }

            // Update the ListBox
            LoadFilePaths();
        }

        private void LoadFilePaths()
        {
            LstFilePaths.Items.Clear();
            List<string> fileHistory = ReadFileHistory();
            foreach (string filePath in fileHistory)
            {
                LstFilePaths.Items.Add(filePath);
            }
        }
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close(); // Closes the application
        }

        private void LstFilePaths_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstFilePaths.SelectedItem is string selectedFile)
            {
                LoadLogData(selectedFile);
            }
        }
    }
}
