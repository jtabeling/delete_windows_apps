using System;
using System.Collections.Generic;

namespace WindowsAppsManager.Models
{
    public class BackupInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string AppName { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string OriginalPath { get; set; } = string.Empty;
        public string BackupPath { get; set; } = string.Empty;
        public DateTime BackupDate { get; set; } = DateTime.Now;
        public long BackupSizeBytes { get; set; }
        public BackupStatus Status { get; set; } = BackupStatus.InProgress;
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> BackedUpFiles { get; set; } = new List<string>();
        public List<RegistryBackupInfo> RegistryBackups { get; set; } = new List<RegistryBackupInfo>();
        
        // Display properties
        public string BackupSizeFormatted => FormatBytes(BackupSizeBytes);
        public string BackupDateFormatted => BackupDate.ToString("yyyy-MM-dd HH:mm:ss");
        public string StatusText => Status.ToString();
        
        /// <summary>
        /// Formats bytes into human-readable string
        /// </summary>
        /// <param name="bytes">Number of bytes</param>
        /// <returns>Formatted string (e.g., "1.5 GB")</returns>
        private static string FormatBytes(long bytes)
        {
            if (bytes == 0) return "0 B";
            
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double size = bytes;
            
            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }
            
            return $"{size:N2} {suffixes[suffixIndex]}";
        }
        
        /// <summary>
        /// Checks if this backup can be restored
        /// </summary>
        /// <returns>True if backup is complete and can be restored</returns>
        public bool CanRestore()
        {
            return Status == BackupStatus.Completed && 
                   !string.IsNullOrEmpty(BackupPath) && 
                   System.IO.Directory.Exists(BackupPath);
        }
        
        /// <summary>
        /// Gets a summary description of this backup
        /// </summary>
        /// <returns>Backup summary string</returns>
        public string GetSummary()
        {
            return $"{AppName} v{Version} - {BackupDateFormatted} ({BackupSizeFormatted})";
        }
    }
    
    public class RegistryBackupInfo
    {
        public string KeyPath { get; set; } = string.Empty;
        public string BackupFile { get; set; } = string.Empty;
        public DateTime BackupDate { get; set; } = DateTime.Now;
        public bool IsSuccessful { get; set; }
    }
    
    public enum BackupStatus
    {
        InProgress,
        Completed,
        Failed,
        PartiallyCompleted
    }
} 