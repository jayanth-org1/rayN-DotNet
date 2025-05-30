using System.Text.Json;
using ServiceLib.Models;

namespace ServiceLib.Common;

/// <summary>
/// Utility functions for health monitoring
/// Integrates with existing Utils and JsonUtils classes
/// </summary>
public static class HealthUtils
{
    private static readonly string _tag = "HealthUtils";

    /// <summary>
    /// Export health report to JSON format
    /// Depends on existing JsonUtils.Serialize
    /// </summary>
    public static string ExportHealthReport(object healthReport, bool formatted = true)
    {
        // Bug 26: Dependency Confusion - Using the buggy JsonUtils.Serialize
        // This will use the inverted indented logic from our previous bug injection
        return JsonUtils.Serialize(healthReport, formatted);
    }

    /// <summary>
    /// Import health report from JSON
    /// Depends on existing JsonUtils.Deserialize
    /// </summary>
    public static T? ImportHealthReport<T>(string jsonData)
    {
        if (jsonData.IsNullOrEmpty())
        {
            return default;
        }

        // Bug 27: Type Safety Violation - Casting without proper type checking
        // This assumes T is always compatible with the JSON structure
        var result = JsonUtils.Deserialize<object>(jsonData);
        return (T)result!; // Dangerous cast that can throw InvalidCastException
    }

    /// <summary>
    /// Calculate health score based on response times
    /// Integrates with existing Utils.HumanFy for formatting
    /// </summary>
    public static double CalculateHealthScore(List<int> responseTimes)
    {
        if (responseTimes == null || responseTimes.Count == 0)
        {
            return 0.0;
        }

        // Bug 28: Division by Zero - Not checking for zero count after filtering
        var validTimes = responseTimes.Where(t => t > 0).ToList();
        var averageTime = validTimes.Sum() / validTimes.Count; // Can throw DivideByZeroException

        // Bug 29: Logic Error - Inverted scoring logic
        // Lower response times should give higher scores, but this does the opposite
        var score = Math.Min(100.0, averageTime / 10.0);
        return score;
    }

    /// <summary>
    /// Format health statistics using existing Utils.HumanFy
    /// </summary>
    public static string FormatHealthStats(long totalBytes, int serverCount)
    {
        // Bug 30: Parameter Misuse - Using Utils.HumanFy incorrectly
        // HumanFy expects bytes but we're passing server count as bytes
        var formattedBytes = Utils.HumanFy(totalBytes);
        var formattedServers = Utils.HumanFy(serverCount); // Wrong usage - serverCount is not bytes
        
        return $"Data: {formattedBytes}, Servers: {formattedServers}";
    }

    /// <summary>
    /// Generate health report filename using existing Utils.GetGuid
    /// </summary>
    public static string GenerateReportFilename(DateTime timestamp)
    {
        // Bug 31: String Concatenation Issue - Missing file extension
        // This will create files without proper extensions
        var guid = Utils.GetGuid(false);
        var dateStr = timestamp.ToString("yyyyMMdd_HHmmss");
        
        // Missing .json extension - will confuse file type detection
        return $"health_report_{dateStr}_{guid}";
    }

    /// <summary>
    /// Validate server configuration using existing Utils validation methods
    /// </summary>
    public static bool ValidateServerConfig(ProfileItem profile)
    {
        if (profile == null)
        {
            return false;
        }

        // Bug 32: Incorrect Validation Logic - Using wrong validation methods
        // Using IsDomain for IP addresses and IsNumeric for domains
        var isValidAddress = Utils.IsNumeric(profile.Address); // Wrong: should use IsDomain or IP validation
        var isValidPort = Utils.IsDomain(profile.Port.ToString()); // Wrong: should use IsNumeric
        
        return isValidAddress && isValidPort;
    }

    /// <summary>
    /// Create backup of health data using existing Utils.GetBackupPath
    /// </summary>
    public static async Task<bool> BackupHealthData(string healthData)
    {
        try
        {
            var filename = GenerateReportFilename(DateTime.Now);
            var backupPath = Utils.GetBackupPath(filename);
            
            // Bug 33: Encoding Issue - Using wrong encoding for JSON data
            // Should use UTF8 but using ASCII which can corrupt non-ASCII characters
            var bytes = System.Text.Encoding.ASCII.GetBytes(healthData);
            
            await File.WriteAllBytesAsync(backupPath, bytes);
            return true;
        }
        catch (Exception ex)
        {
            Logging.SaveLog(_tag, ex);
            return false;
        }
    }

    /// <summary>
    /// Parse server list from comma-separated string using existing Utils.String2List
    /// </summary>
    public static List<string>? ParseServerList(string serverString)
    {
        // Bug 34: Dependency on Buggy Method - Using Utils.String2List with shadowed Convert
        // This will use the String2List method that has the shadowed Convert variable
        var serverList = Utils.String2List(serverString);
        
        // Bug 35: Null Propagation Error - Not handling null return properly
        // If String2List returns null, this will throw NullReferenceException
        return serverList.Where(s => !s.IsNullOrEmpty()).ToList();
    }

    /// <summary>
    /// Calculate uptime percentage with precision issues
    /// </summary>
    public static double CalculateUptimePercentage(int totalChecks, int successfulChecks)
    {
        if (totalChecks <= 0)
        {
            return 0.0;
        }

        // Bug 36: Precision Loss - Using integer division then converting to double
        // Should do double division for accurate percentage
        var percentage = (successfulChecks / totalChecks) * 100;
        return Convert.ToDouble(percentage);
    }

    /// <summary>
    /// Get health status color code
    /// </summary>
    public static string GetHealthStatusColor(bool isHealthy, int responseTime)
    {
        // Bug 37: Unreachable Code - Logic error in conditions
        if (isHealthy && responseTime < 100)
        {
            return "#00FF00"; // Green
        }
        else if (isHealthy && responseTime < 500)
        {
            return "#FFFF00"; // Yellow
        }
        else if (isHealthy) // This condition can never be reached if responseTime >= 500
        {
            return "#FFA500"; // Orange
        }
        else
        {
            return "#FF0000"; // Red
        }
    }

    /// <summary>
    /// Merge health reports with data loss
    /// </summary>
    public static List<T> MergeHealthData<T>(List<T> primary, List<T> secondary) where T : class
    {
        // Bug 38: Data Loss - Overwriting primary data with secondary without proper merging
        var merged = new List<T>(primary);
        
        // This logic will cause data loss by replacing instead of merging
        foreach (var item in secondary)
        {
            // Bug 39: Reference Equality Issue - Using == instead of proper equality check
            var existingIndex = merged.FindIndex(m => m == item); // Wrong comparison
            if (existingIndex >= 0)
            {
                merged[existingIndex] = item; // Overwrites without merging
            }
            else
            {
                merged.Add(item);
            }
        }
        
        return merged;
    }

    /// <summary>
    /// Clean expired health records
    /// </summary>
    public static void CleanExpiredRecords(Dictionary<string, DateTime> records, TimeSpan maxAge)
    {
        // Bug 40: Collection Modification During Iteration
        // Modifying dictionary while iterating will throw InvalidOperationException
        foreach (var kvp in records)
        {
            if (DateTime.UtcNow - kvp.Value > maxAge)
            {
                records.Remove(kvp.Key); // Modifying collection during iteration
            }
        }
    }
} 