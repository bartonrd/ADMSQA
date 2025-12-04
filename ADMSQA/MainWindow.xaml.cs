using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using DisplayPointDuplicateTest;
using Microsoft.Win32;

namespace ADMSQA
{
    /// <summary>
    /// Represents an input directory path for the UI binding.
    /// </summary>
    public class InputDirectoryItem : INotifyPropertyChanged
    {
        private string _path = string.Empty;

        public string Path
        {
            get => _path;
            set
            {
                _path = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Path)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<InputDirectoryItem> _inputDirectories = new();

        public MainWindow()
        {
            InitializeComponent();
            InputDirectoriesItemsControl.ItemsSource = _inputDirectories;
        }

        private void BrowseReportOutputButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = ".txt",
                FileName = "duplicates_within_files_report.txt"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                ReportOutputPathTextBox.Text = saveFileDialog.FileName;
            }
        }

        private void AddDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new OpenFolderDialog
            {
                Title = "Select Directory Containing .pts Files"
            };

            if (folderDialog.ShowDialog() == true)
            {
                _inputDirectories.Add(new InputDirectoryItem { Path = folderDialog.FolderName });
            }
        }

        private void RemoveDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (_inputDirectories.Count > 0)
            {
                _inputDirectories.RemoveAt(_inputDirectories.Count - 1);
            }
        }

        private void BrowseInputDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is InputDirectoryItem item)
            {
                var folderDialog = new OpenFolderDialog
                {
                    Title = "Select Directory Containing .pts Files"
                };

                if (folderDialog.ShowDialog() == true)
                {
                    item.Path = folderDialog.FolderName;
                }
            }
        }

        private void RemoveInputDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is InputDirectoryItem item)
            {
                _inputDirectories.Remove(item);
            }
        }

        private void RunTestButton_Click(object sender, RoutedEventArgs e)
        {
            ResultsTextBox.Text = string.Empty;

            // Validate inputs
            if (string.IsNullOrWhiteSpace(ReportOutputPathTextBox.Text))
            {
                MessageBox.Show("Please specify a report output path.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_inputDirectories.Count == 0)
            {
                MessageBox.Show("Please add at least one input directory.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var inputPaths = _inputDirectories
                .Where(d => !string.IsNullOrWhiteSpace(d.Path))
                .Select(d => d.Path)
                .ToArray();

            if (inputPaths.Length == 0)
            {
                MessageBox.Show("Please specify at least one valid input directory path.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate that directories exist
            var missingDirs = inputPaths.Where(p => !Directory.Exists(p)).ToList();
            if (missingDirs.Count > 0)
            {
                MessageBox.Show($"The following directories do not exist:\n{string.Join("\n", missingDirs)}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                RunTestButton.IsEnabled = false;
                ResultsTextBox.Text = "Running test...\n";

                // Run the test using the DisplayPointDuplicateTest library
                var analyzer = new DisplayPointDuplicateAnalyzer();
                var results = analyzer.AnalyzeAndGenerateReport(inputPaths, ReportOutputPathTextBox.Text);

                // Display results summary
                var sb = new StringBuilder();
                sb.AppendLine("Test completed successfully!");
                sb.AppendLine();
                sb.AppendLine($"Total files analyzed: {results.Count}");
                
                var filesWithDuplicates = results.Where(r => r.HasDuplicates).ToList();
                sb.AppendLine($"Files with duplicates: {filesWithDuplicates.Count}");
                
                var totalDuplicates = filesWithDuplicates.Sum(r => r.Duplicates.Count);
                sb.AppendLine($"Total duplicate combinations found: {totalDuplicates}");
                sb.AppendLine();
                sb.AppendLine($"Report saved to: {ReportOutputPathTextBox.Text}");

                if (filesWithDuplicates.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Files with duplicates:");
                    foreach (var file in filesWithDuplicates.OrderBy(f => f.FileName))
                    {
                        sb.AppendLine($"  - {file.FileName}: {file.Duplicates.Count} duplicate combination(s)");
                    }
                }

                ResultsTextBox.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                ResultsTextBox.Text = $"Error running test:\n{ex.Message}";
                MessageBox.Show($"An error occurred while running the test:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                RunTestButton.IsEnabled = true;
            }
        }
    }
}