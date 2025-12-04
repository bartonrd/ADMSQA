using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DisplayPointDuplicateTest
{
    /// <summary>
    /// Represents a duplicate point combination found within a file.
    /// </summary>
    public class DuplicateEntry
    {
        /// <summary>
        /// The value of column 1.
        /// </summary>
        public string Column1 { get; set; } = string.Empty;

        /// <summary>
        /// The value of column 2.
        /// </summary>
        public string Column2 { get; set; } = string.Empty;

        /// <summary>
        /// The value of column 5.
        /// </summary>
        public string Column5 { get; set; } = string.Empty;

        /// <summary>
        /// The line numbers where this duplicate combination appears.
        /// </summary>
        public List<int> LineNumbers { get; set; } = new List<int>();

        /// <summary>
        /// The number of occurrences of this duplicate.
        /// </summary>
        public int OccurrenceCount => LineNumbers.Count;
    }

    /// <summary>
    /// Represents the duplicate analysis results for a single file.
    /// </summary>
    public class FileDuplicateResult
    {
        /// <summary>
        /// The name of the file analyzed.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// The full path to the file analyzed.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// The list of duplicate entries found in this file.
        /// </summary>
        public List<DuplicateEntry> Duplicates { get; set; } = new List<DuplicateEntry>();

        /// <summary>
        /// Whether the file has any duplicates.
        /// </summary>
        public bool HasDuplicates => Duplicates.Count > 0;
    }

    /// <summary>
    /// Provides functionality to analyze .pts files for duplicate display points.
    /// Duplicates are identified based on the combination of Column1 + Column2 + Column5.
    /// </summary>
    public class DisplayPointDuplicateAnalyzer
    {
        /// <summary>
        /// Analyzes .pts files in the specified directories for duplicate display points
        /// and generates a report at the specified output path.
        /// </summary>
        /// <param name="inputPaths">An array of directory paths containing .pts files to analyze.</param>
        /// <param name="reportOutputPath">The file path where the report will be written.</param>
        /// <returns>A list of FileDuplicateResult objects containing the analysis results.</returns>
        public List<FileDuplicateResult> AnalyzeAndGenerateReport(string[] inputPaths, string reportOutputPath)
        {
            var results = Analyze(inputPaths);
            GenerateReport(results, reportOutputPath);
            return results;
        }

        /// <summary>
        /// Analyzes .pts files in the specified directories for duplicate display points.
        /// </summary>
        /// <param name="inputPaths">An array of directory paths containing .pts files to analyze.</param>
        /// <returns>A list of FileDuplicateResult objects containing the analysis results.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown when an input path does not exist.</exception>
        public List<FileDuplicateResult> Analyze(string[] inputPaths)
        {
            var results = new List<FileDuplicateResult>();

            foreach (var inputPath in inputPaths)
            {
                if (!Directory.Exists(inputPath))
                {
                    throw new DirectoryNotFoundException($"The specified input directory does not exist: {inputPath}");
                }

                var ptsFiles = Directory.GetFiles(inputPath, "*.pts", SearchOption.TopDirectoryOnly);

                foreach (var filePath in ptsFiles)
                {
                    var result = AnalyzeFile(filePath);
                    results.Add(result);
                }
            }

            return results;
        }

        /// <summary>
        /// Analyzes a single .pts file for duplicate display points.
        /// </summary>
        /// <param name="filePath">The path to the .pts file to analyze.</param>
        /// <returns>A FileDuplicateResult containing the analysis results for this file.</returns>
        public FileDuplicateResult AnalyzeFile(string filePath)
        {
            var result = new FileDuplicateResult
            {
                FileName = Path.GetFileName(filePath),
                FilePath = filePath
            };

            var comboTracker = new Dictionary<(string, string, string), List<int>>();

            int lineNum = 0;
            foreach (var line in File.ReadLines(filePath))
            {
                lineNum++;
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                {
                    continue;
                }

                var parts = trimmedLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 5)
                {
                    var col1 = parts[0];
                    var col2 = parts[1];
                    var col5 = parts[4];

                    var key = (col1, col2, col5);

                    if (!comboTracker.ContainsKey(key))
                    {
                        comboTracker[key] = new List<int>();
                    }
                    comboTracker[key].Add(lineNum);
                }
            }

            // Find duplicates (entries with more than one occurrence)
            foreach (var kvp in comboTracker.Where(x => x.Value.Count > 1))
            {
                result.Duplicates.Add(new DuplicateEntry
                {
                    Column1 = kvp.Key.Item1,
                    Column2 = kvp.Key.Item2,
                    Column5 = kvp.Key.Item3,
                    LineNumbers = kvp.Value
                });
            }

            // Sort duplicates by column values (numeric sort for numbers, string sort as fallback)
            result.Duplicates = result.Duplicates
                .OrderBy(d => int.TryParse(d.Column1, out var c1) ? c1 : 0)
                .ThenBy(d => d.Column1)
                .ThenBy(d => int.TryParse(d.Column2, out var c2) ? c2 : 0)
                .ThenBy(d => d.Column2)
                .ToList();

            return result;
        }

        /// <summary>
        /// Generates a text report of the duplicate analysis results.
        /// </summary>
        /// <param name="results">The analysis results to include in the report.</param>
        /// <param name="reportOutputPath">The file path where the report will be written.</param>
        public void GenerateReport(List<FileDuplicateResult> results, string reportOutputPath)
        {
            var filesWithDuplicates = results.Where(r => r.HasDuplicates).ToList();
            var totalFilesAnalyzed = results.Count;

            // Ensure the output directory exists
            var outputDir = Path.GetDirectoryName(reportOutputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            using (var writer = new StreamWriter(reportOutputPath))
            {
                writer.WriteLine(new string('=', 80));
                writer.WriteLine("DUPLICATES WITHIN EACH FILE REPORT");
                writer.WriteLine(new string('=', 80));
                writer.WriteLine($"Total files analyzed: {totalFilesAnalyzed}");
                writer.WriteLine($"Files with internal duplicates: {filesWithDuplicates.Count}");
                writer.WriteLine("Duplicate Key: Column1 + Column2 + Column5");
                writer.WriteLine(new string('=', 80));
                writer.WriteLine();

                if (filesWithDuplicates.Count == 0)
                {
                    writer.WriteLine("No duplicate combinations found within any file.");
                }
                else
                {
                    foreach (var fileResult in filesWithDuplicates.OrderBy(f => f.FileName))
                    {
                        writer.WriteLine(new string('-', 80));
                        writer.WriteLine($"FILE: {fileResult.FileName}");
                        writer.WriteLine($"Number of duplicate combinations: {fileResult.Duplicates.Count}");
                        writer.WriteLine(new string('-', 80));

                        foreach (var dup in fileResult.Duplicates)
                        {
                            writer.WriteLine($"  Column1: {dup.Column1}, Column2: {dup.Column2}, Column5: {dup.Column5}");
                            writer.WriteLine($"    Appears on lines: {string.Join(", ", dup.LineNumbers)}");
                            writer.WriteLine($"    Occurrence count: {dup.OccurrenceCount}");
                        }
                        writer.WriteLine();
                    }
                }
            }
        }
    }
}
