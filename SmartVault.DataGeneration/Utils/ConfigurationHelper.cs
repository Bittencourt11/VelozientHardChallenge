using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartVault.DataGeneration.Utils
{
    public static class ConfigurationHelper
    {
        private static IConfigurationRoot _configuration;

        static ConfigurationHelper()
        {
            string solutionDirectory = GetSolutionDirectory();
            string appSettingsPath = Path.Combine(solutionDirectory, "SmartVault.DataGeneration");

            _configuration = new ConfigurationBuilder()
                .SetBasePath(appSettingsPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        public static IConfigurationRoot GetConfiguration()
        {
            return _configuration;
        }

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

    }
}
