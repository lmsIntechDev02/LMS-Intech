using System;
using System.IO;
 
using System.Threading.Tasks;
 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace LMSDatabaseBackup
{
    class Program
    {
        private static async Task<int> Main(string[] args)
        {
            Console.WriteLine("=== SQL Server Backup Utility ===");
            Console.WriteLine($"Started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            try
            {
                // ---- 1. Load configuration (appsettings.json + optional command-line overrides) ----
                var config = new  ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                    .AddCommandLine(args)
                    .Build();

                var settings = config.GetSection("BackupSettings");

                string connectionString = settings["ConnectionString"]
                    ?? throw new InvalidOperationException("ConnectionString is missing in appsettings.json");
                string databaseName = settings["DatabaseName"]
                    ?? throw new InvalidOperationException("DatabaseName is missing in appsettings.json");
                string backupFolder = settings["BackupFolder"]
                    ?? throw new InvalidOperationException("BackupFolder is missing in appsettings.json");
                int retentionDays = int.TryParse(settings["RetentionDays"], out var rd) ? rd : 7;
                string backupType = settings["BackupType"] ?? "Full"; // "Full" or "Differential"

                // ---- 2. Run the backup ----
                string backupFilePath = await BackupDatabaseAsync(connectionString, databaseName, backupFolder, backupType);
                Console.WriteLine($"Backup completed successfully: {backupFilePath}");
                Console.WriteLine();

                // ---- 3. Clean up backups older than the retention window ----
                CleanupOldBackups(backupFolder, databaseName, retentionDays);

                Console.WriteLine();
                Console.WriteLine("=== Done ===");
                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        
        private static async Task<string> BackupDatabaseAsync(
            string connectionString,
            string databaseName,
            string backupFolder,
            string backupType)
        {
            // Ensure the target folder exists (this only works if this app runs on
            // the same machine as SQL Server; for remote servers, pre-create the share).
            if (!Directory.Exists(backupFolder))
            {
                Directory.CreateDirectory(backupFolder);
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"{databaseName}_{backupType}_{timestamp}.bak";
            string fullPath = Path.Combine(backupFolder, fileName);

            string sql = backupType.Equals("Differential", StringComparison.OrdinalIgnoreCase)
                ? $@"BACKUP DATABASE [{databaseName}]
              TO DISK = @BackupPath
              WITH DIFFERENTIAL, NOFORMAT, INIT, NAME = @BackupName,
              SKIP, NOREWIND, NOUNLOAD, STATS = 10"
                : $@"BACKUP DATABASE [{databaseName}]
              TO DISK = @BackupPath
              WITH NOFORMAT, INIT, NAME = @BackupName,
              SKIP, NOREWIND, NOUNLOAD, STATS = 10";

            await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new Microsoft.Data.SqlClient.SqlCommand(sql, connection)
            {
                CommandTimeout = 0 // no timeout - large DBs can take a while
            };
            command.Parameters.AddWithValue("@BackupPath", fullPath);
            command.Parameters.AddWithValue("@BackupName", $"{databaseName}-{backupType} Backup");

            Console.WriteLine($"Backing up '{databaseName}' to: {fullPath}");
            await command.ExecuteNonQueryAsync();

            return fullPath;
        }

       
        private static void CleanupOldBackups(string backupFolder, string databaseName, int retentionDays)
        {
            Console.WriteLine($"Cleaning up backups older than {retentionDays} day(s)...");

            if (!Directory.Exists(backupFolder))
            {
                Console.WriteLine("Backup folder does not exist, nothing to clean up.");
                return;
            }

            var cutoff = DateTime.Now.AddDays(-retentionDays);
            var searchPattern = $"{databaseName}_*.bak";
            var deletedCount = 0;

            foreach (var file in Directory.GetFiles(backupFolder, searchPattern))
            {
                var info = new FileInfo(file);
                if (info.LastWriteTime < cutoff)
                {
                    try
                    {
                        info.Delete();
                        Console.WriteLine($"  Deleted old backup: {info.Name} (created {info.LastWriteTime:yyyy-MM-dd})");
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Could not delete {info.Name}: {ex.Message}");
                    }
                }
            }

            Console.WriteLine(deletedCount == 0
                ? "No old backups needed to be removed."
                : $"Removed {deletedCount} old backup file(s).");
        }
    }
}
