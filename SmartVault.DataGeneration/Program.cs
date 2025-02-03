using Dapper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SmartVault.DataGeneration.Utils;
using SmartVault.Library;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Transactions;
using System.Xml.Serialization;

namespace SmartVault.DataGeneration
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Initializing Data Generation process...");

            try
            {
                var configuration = ConfigurationHelper.GetConfiguration();

                SQLiteConnection.CreateFile(configuration["DatabaseFileName"]);
                File.WriteAllText("TestDoc.txt", string.Join(Environment.NewLine, Enumerable.Repeat("This is my test document", 100)));

                using (var connection = new SQLiteConnection(string.Format(configuration?["ConnectionStrings:DefaultConnection"] ?? "", configuration?["DatabaseFileName"])))
                {
                    connection.Open();

                    var files = Directory.GetFiles(@"..\..\..\..\BusinessObjectSchema");
                    for (int i = 0; i <= 2; i++)
                    {
                        var serializer = new XmlSerializer(typeof(BusinessObject));
                        var businessObject = serializer.Deserialize(new StreamReader(files[i])) as BusinessObject;
                        connection.Execute(businessObject?.Script);
                    }

                    using (var transaction = connection.BeginTransaction())
                    {
                        var documentNumber = 0;
                        var documentPath = new FileInfo("TestDoc.txt").FullName;
                        var documentLength = new FileInfo(documentPath).Length;
                        var currentTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                        for (int i = 0; i < 100; i++)
                        {
                            var randomDay = RandomDay().First();
                            connection.Execute(
                                $"INSERT INTO User (Id, FirstName, LastName, DateOfBirth, AccountId, Username, Password, CreatedOn) " +
                                $"VALUES('{i}','FName{i}','LName{i}','{randomDay.ToString("yyyy-MM-dd")}','{i}','UserName-{i}','e10adc3949ba59abbe56e057f20f883e','{currentTime}')",
                                transaction: transaction);

                            connection.Execute(
                                $"INSERT INTO Account (Id, Name, CreatedOn) " +
                                $"VALUES('{i}','Account{i}','{currentTime}')",
                                transaction: transaction);

                            var documentInserts = new List<string>();
                            for (int d = 0; d < 10000; d++, documentNumber++)
                            {
                                documentInserts.Add($"('{documentNumber}','Document{i}-{d}.txt','{documentPath}','{documentLength}','{i}','{currentTime}')");
                            }

                            var insertCommand = $"INSERT INTO Document (Id, Name, FilePath, Length, AccountId, CreatedOn) VALUES {string.Join(",", documentInserts)}";
                            connection.Execute(insertCommand, transaction: transaction);
                        }

                        transaction.Commit();
                    }

                    var accountData = connection.Query("SELECT COUNT(*) FROM Account;");
                    var documentData = connection.Query("SELECT COUNT(*) FROM Document;");
                    var userData = connection.Query("SELECT COUNT(*) FROM User;");

                    Console.WriteLine($"AccountCount: {JsonConvert.SerializeObject(accountData)}");
                    Console.WriteLine($"DocumentCount: {JsonConvert.SerializeObject(documentData)}");
                    Console.WriteLine($"UserCount: {JsonConvert.SerializeObject(userData)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating data: {ex.Message}");
            }
        }

        static IEnumerable<DateTime> RandomDay()
        {
            DateTime start = new DateTime(1985, 1, 1);
            Random gen = new Random();
            int range = (DateTime.Today - start).Days;
            while (true)
                yield return start.AddDays(gen.Next(range));
        }
    }
}