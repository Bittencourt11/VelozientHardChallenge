using Dapper;
using SmartVault.DataGeneration.Utils;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartVault.Program.Utils
{
    public static class FileHelper
    {
        public static string GetSolutionDirectory()
        {
            var directory = Directory.GetCurrentDirectory();

            while (!Directory.GetFiles(directory, "*.sln").Any())
            {
                directory = Directory.GetParent(directory)?.FullName;
                if (directory == null)
                    throw new DirectoryNotFoundException("Solution directory not found.");
            }

            return directory;
        }

        public static string GetDatabaseFilePath(string solutionDirectory, string databaseFileName)
        {
            string debugPath = Path.Combine(solutionDirectory, "SmartVault.DataGeneration", "bin", "Debug", "net5.0", databaseFileName);
            string releasePath = Path.Combine(solutionDirectory, "SmartVault.DataGeneration", "bin", "Release", "net5.0", databaseFileName);

            return File.Exists(debugPath) ? debugPath : File.Exists(releasePath) ? releasePath : null;
        }

        public static void CreateTestFileAndInsertIntoDatabase(string accountId)
        {
            var configuration = ConfigurationHelper.GetConfiguration();

            string solutionDirectory = GetSolutionDirectory();
            string databaseFileName = configuration["DatabaseFileName"];
            string databaseFilePath = GetDatabaseFilePath(solutionDirectory, databaseFileName);

            if (databaseFilePath == null)
            {
                throw new FileNotFoundException("Database file not found in Debug or Release directories.");
            }

            string testFilename = "SmithPropertyTestFile.txt";
            string testFilePath = Path.Combine(solutionDirectory, "SmartVault.Program", testFilename);
            File.WriteAllText(testFilePath, "Smith Property");

            long testFileSize = new FileInfo(testFilePath).Length;

            using (var connection = new SQLiteConnection($"Data Source={databaseFilePath};"))
            {
                connection.Open();

                var documents = connection.Query<(int Id, string FilePath)>(
                    "SELECT Id, FilePath FROM Document WHERE AccountId = @AccountId ORDER BY Id;",
                    new { AccountId = accountId });

                int totalDocuments = documents.Count();
                int documentsToUpdate = totalDocuments / 2;

                for (int i = 0; i < documentsToUpdate; i++)
                {
                    var document = documents.ElementAt(i);

                    if (document.FilePath != testFilePath)
                    {
                        connection.Execute(
                        "UPDATE Document SET FilePath = @FilePath, Length = @Length WHERE Id = @Id;",
                        new { FilePath = testFilePath, Length = testFileSize, Id = document.Id });
                    }
                }

                Console.WriteLine($"Test file path was inserted into {documentsToUpdate} documents for account ID {accountId}, and their lengths were updated.");
            }
        }
    }
}
