using System.IO;
using System;
using SmartVault.DataGeneration.Utils;
using SmartVault.Program.Utils;
using Microsoft.Extensions.Configuration;
using System.Data.SQLite;
using System.Linq;
using Dapper;
using SmartVault.Program.BusinessObjects;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace SmartVault.Program
{
    partial class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                return;
            }

            WriteEveryThirdFileToFile(args[0]);
            GetAllFileSizes();
        }

        private static void WriteEveryThirdFileToFile(string accountId)
        {
            Console.WriteLine($"Initializing WriteEveryThirdFileToFile function for accountId {accountId}...");

            try
            {
                FileHelper.CreateTestFileAndInsertIntoDatabase(accountId);

                var configuration = ConfigurationHelper.GetConfiguration();

                string solutionDirectory = FileHelper.GetSolutionDirectory();
                string databaseFileName = configuration["DatabaseFileName"];
                string databaseFilePath = FileHelper.GetDatabaseFilePath(solutionDirectory, databaseFileName);

                if (databaseFilePath == null)
                {
                    throw new FileNotFoundException("Database file not found in Debug or Release directories.");
                }

                string outputFileName = "OutputFile.txt";
                string outputFilePath = Path.Combine(solutionDirectory, "SmartVault.Program", outputFileName);

                using (var connection = new SQLiteConnection($"Data Source={databaseFilePath};"))
                {
                    connection.Open();

                    var documents = connection.Query<(int Id, string FilePath)>(
                        "SELECT Id, FilePath FROM Document WHERE AccountId = @AccountId ORDER BY Id;",
                        new { AccountId = accountId });

                    int documentCount = 0;

                    using (var outputFile = new StreamWriter(outputFilePath, append: true))
                    {
                        foreach (var document in documents)
                        {
                            documentCount++;

                            if (documentCount % 3 == 0)
                            {
                                if (File.Exists(document.FilePath))
                                {
                                    string fileContent = File.ReadAllText(document.FilePath);

                                    if (fileContent.Contains("Smith Property"))
                                    {
                                        outputFile.WriteLine($"--- Document ID: {document.Id}, FilePath: {document.FilePath} ---");
                                        outputFile.WriteLine(fileContent);
                                        outputFile.WriteLine();
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"File not found: {document.FilePath} (Document ID: {document.Id})");
                                }
                            }
                        }
                    }
                }

                Console.WriteLine($"Contents of every third file containing 'Smith Property' have been written to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing WriteEveryThirdFileToFile function: {ex.Message}");
            }
        }

        private static void GetAllFileSizes()
        {
            Console.WriteLine("Initializing GetAllFileSizes function...");

            try
            {
                var configuration = ConfigurationHelper.GetConfiguration();

                string solutionDirectory = FileHelper.GetSolutionDirectory();
                string databaseFileName = configuration["DatabaseFileName"];
                string databaseFilePath = FileHelper.GetDatabaseFilePath(solutionDirectory, databaseFileName);

                if (databaseFilePath == null)
                {
                    throw new FileNotFoundException("Database file not found in Debug or Release directories.");
                }

                long totalFileSize = 0;
                List<string> filePaths = new();

                try
                {
                    using (var connection = new SQLiteConnection($"Data Source={databaseFilePath};"))
                    {
                        connection.Open();
                        filePaths = connection.Query<(int Id, string FilePath)>("SELECT Id, FilePath FROM Document;")
                                              .Where(doc => !string.IsNullOrEmpty(doc.FilePath))
                                              .Select(doc => doc.FilePath)
                                              .ToList();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accessing database: {ex.Message}");
                    return;
                }

                Parallel.ForEach(filePaths, (filePath) =>
                {
                    try
                    {
                        if (File.Exists(filePath))
                        {
                            var fileInfo = new FileInfo(filePath);
                            Interlocked.Add(ref totalFileSize, fileInfo.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to access file {filePath}: {ex.Message}");
                    }
                });

                Console.WriteLine($"Total file size: {totalFileSize / (1024 * 1024)} MB\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing GetAllFileSizes function: {ex.Message}");
            }
        }
    }
}