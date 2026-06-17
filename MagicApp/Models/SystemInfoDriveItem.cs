namespace MagicApp.Models
{
    public class SystemInfoDriveItem
    {
        public string Name { get; set; } = string.Empty;
        public double TotalBytes { get; set; }
        public double FreeBytes { get; set; }
        public double UsedBytes { get; set; }
        public string TotalSpaceStr { get; set; } = string.Empty;
        public string FreeSpaceStr { get; set; } = string.Empty;
        public double UsagePercent { get; set; }
        public bool IsHighUsage { get; set; }
    }
}
