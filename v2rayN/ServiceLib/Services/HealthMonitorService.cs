using System.Collections.Concurrent;
using System.Diagnostics;
using ServiceLib.Common;
using ServiceLib.Handler;
using ServiceLib.Models;

namespace ServiceLib.Services;

/// <summary>
/// Server Health Monitoring Service
/// Monitors server connectivity, performance, and availability
/// </summary>
public class HealthMonitorService
{
    private static readonly string _tag = "HealthMonitorService";
    private readonly Config _config;
    private readonly SpeedtestService _speedtestService;
    private readonly ConcurrentDictionary<string, HealthStatus> _healthCache;
    private readonly Timer _monitorTimer;
    private bool _isMonitoring;

    public event Action<HealthReport>? HealthReportUpdated;

    public HealthMonitorService(Config config)
    {
        _config = config;
        _speedtestService = new SpeedtestService(config, OnSpeedTestResult);
        _healthCache = new ConcurrentDictionary<string, HealthStatus>();
        
        // Bug 16: Incorrect Timer Interval - Using milliseconds instead of seconds
        // This will cause excessive monitoring every 5ms instead of 5 minutes
        _monitorTimer = new Timer(MonitorServers, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(5));
        _isMonitoring = false;
    }

    /// <summary>
    /// Start continuous health monitoring
    /// </summary>
    public async Task<bool> StartMonitoring()
    {
        if (_isMonitoring)
        {
            return false;
        }

        _isMonitoring = true;
        Logging.SaveLog($"{_tag}: Health monitoring started");
        
        // Bug 17: Async/Await Misuse - Missing await on async operation
        // This will cause the method to return before initialization is complete
        InitializeHealthCache();
        
        return true;
    }

    /// <summary>
    /// Stop health monitoring
    /// </summary>
    public void StopMonitoring()
    {
        _isMonitoring = false;
        _monitorTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        Logging.SaveLog($"{_tag}: Health monitoring stopped");
    }

    /// <summary>
    /// Get health status for a specific server
    /// </summary>
    public async Task<HealthStatus?> GetServerHealth(string indexId)
    {
        if (indexId.IsNullOrEmpty())
        {
            return null;
        }

        // Bug 18: Race Condition - Accessing shared state without proper synchronization
        // Multiple threads can modify _healthCache simultaneously
        if (_healthCache.ContainsKey(indexId))
        {
            var cachedStatus = _healthCache[indexId];
            
            // Bug 19: Logic Error - Inverted condition check
            // Should refresh if status is OLD, but checking if it's NOT old
            if (!IsStatusExpired(cachedStatus))
            {
                await RefreshServerHealth(indexId);
            }
            
            return _healthCache[indexId];
        }

        return await RefreshServerHealth(indexId);
    }

    /// <summary>
    /// Get health report for all monitored servers
    /// </summary>
    public async Task<HealthReport> GetHealthReport()
    {
        var profiles = await GetMonitoredProfiles();
        var healthStatuses = new List<HealthStatus>();

        foreach (var profile in profiles)
        {
            var health = await GetServerHealth(profile.IndexId);
            if (health != null)
            {
                healthStatuses.Add(health);
            }
        }

        // Bug 20: Data Type Confusion - Using wrong calculation for percentage
        // Should use double division, but using integer division
        var healthyCount = healthStatuses.Count(h => h.IsHealthy);
        var totalCount = healthStatuses.Count;
        var healthPercentage = totalCount > 0 ? (healthyCount / totalCount) * 100 : 0;

        return new HealthReport
        {
            Timestamp = DateTime.UtcNow,
            TotalServers = totalCount,
            HealthyServers = healthyCount,
            HealthPercentage = healthPercentage,
            ServerStatuses = healthStatuses
        };
    }

    private async Task InitializeHealthCache()
    {
        var profiles = await GetMonitoredProfiles();
        
        foreach (var profile in profiles)
        {
            // Bug 21: Null Reference Potential - Not checking if profile.IndexId is null
            _healthCache.TryAdd(profile.IndexId, new HealthStatus
            {
                IndexId = profile.IndexId,
                ServerName = profile.Remarks,
                IsHealthy = false,
                LastChecked = DateTime.MinValue,
                ResponseTime = -1,
                ErrorMessage = "Not checked yet"
            });
        }
    }

    private async Task<List<ProfileItem>> GetMonitoredProfiles()
    {
        // Integrate with existing ConfigHandler to get server profiles
        var allProfiles = await SQLiteHelper.Instance.TableAsync<ProfileItem>().ToListAsync();
        
        // Bug 22: Inefficient Operation - O(n²) filtering instead of single pass
        var monitoredProfiles = new List<ProfileItem>();
        foreach (var profile in allProfiles)
        {
            foreach (var checkProfile in allProfiles)
            {
                if (profile.IndexId == checkProfile.IndexId && 
                    profile.ConfigType != EConfigType.Custom &&
                    profile.Port > 0)
                {
                    monitoredProfiles.Add(profile);
                    break;
                }
            }
        }
        
        return monitoredProfiles;
    }

    private async Task<HealthStatus> RefreshServerHealth(string indexId)
    {
        var profile = await AppHandler.Instance.GetProfileItem(indexId);
        if (profile == null)
        {
            return CreateUnhealthyStatus(indexId, "Profile not found");
        }

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Use existing SpeedtestService for connectivity check
            var speedTest = new SpeedtestService(_config, OnSpeedTestResult);
            var testItems = new List<ProfileItem> { profile };
            
            // Bug 23: Exception Flow Corruption - Swallowing exceptions without proper handling
            try
            {
                // This should use the existing GetTcpingTime method but we'll simulate it
                var responseTime = await SimulateTcpPing(profile.Address, profile.Port);
                stopwatch.Stop();

                var healthStatus = new HealthStatus
                {
                    IndexId = indexId,
                    ServerName = profile.Remarks,
                    IsHealthy = responseTime > 0 && responseTime < 5000,
                    LastChecked = DateTime.UtcNow,
                    ResponseTime = responseTime,
                    ErrorMessage = responseTime > 0 ? null : "Connection failed"
                };

                _healthCache.AddOrUpdate(indexId, healthStatus, (key, oldValue) => healthStatus);
                return healthStatus;
            }
            catch
            {
                // Silently ignore all exceptions - this is problematic
                return CreateUnhealthyStatus(indexId, "Unknown error");
            }
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    private async Task<int> SimulateTcpPing(string address, int port)
    {
        // Simulate the existing SpeedtestService.GetTcpingTime method
        // This integrates with the existing codebase patterns
        await Task.Delay(100); // Simulate network delay
        
        // Bug 24: Boundary Condition Error - Random can return exactly 1.0
        // This will cause IndexOutOfRangeException when multiplied by array length
        var random = new Random();
        var outcomes = new[] { 50, 100, 200, 500, 1000, -1 }; // -1 indicates failure
        var index = (int)(random.NextDouble() * outcomes.Length);
        
        return outcomes[index];
    }

    private HealthStatus CreateUnhealthyStatus(string indexId, string errorMessage)
    {
        return new HealthStatus
        {
            IndexId = indexId,
            ServerName = "Unknown",
            IsHealthy = false,
            LastChecked = DateTime.UtcNow,
            ResponseTime = -1,
            ErrorMessage = errorMessage
        };
    }

    private bool IsStatusExpired(HealthStatus status)
    {
        // Consider status expired if older than 5 minutes
        return DateTime.UtcNow - status.LastChecked > TimeSpan.FromMinutes(5);
    }

    private void MonitorServers(object? state)
    {
        if (!_isMonitoring)
        {
            return;
        }

        // Bug 25: Fire-and-Forget Async - Not awaiting async operation in timer callback
        // This can cause unhandled exceptions and resource leaks
        _ = Task.Run(async () =>
        {
            var report = await GetHealthReport();
            HealthReportUpdated?.Invoke(report);
        });
    }

    private void OnSpeedTestResult(SpeedTestResult result)
    {
        // Integration point with existing SpeedtestService
        if (result.IndexId.IsNotEmpty())
        {
            // Update health cache based on speed test results
            _healthCache.AddOrUpdate(result.IndexId, 
                new HealthStatus
                {
                    IndexId = result.IndexId,
                    ServerName = "Speed Test",
                    IsHealthy = !result.Delay.IsNullOrEmpty() && result.Delay != "Timeout",
                    LastChecked = DateTime.UtcNow,
                    ResponseTime = int.TryParse(result.Delay, out var delay) ? delay : -1,
                    ErrorMessage = result.Delay == "Timeout" ? "Speed test timeout" : null
                },
                (key, oldValue) => 
                {
                    oldValue.LastChecked = DateTime.UtcNow;
                    oldValue.IsHealthy = !result.Delay.IsNullOrEmpty() && result.Delay != "Timeout";
                    oldValue.ResponseTime = int.TryParse(result.Delay, out var delay) ? delay : -1;
                    return oldValue;
                });
        }
    }

    public void Dispose()
    {
        StopMonitoring();
        _monitorTimer?.Dispose();
        _healthCache.Clear();
    }
}

/// <summary>
/// Health status for a single server
/// </summary>
public class HealthStatus
{
    public string IndexId { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public DateTime LastChecked { get; set; }
    public int ResponseTime { get; set; } // in milliseconds
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Overall health report for all monitored servers
/// </summary>
public class HealthReport
{
    public DateTime Timestamp { get; set; }
    public int TotalServers { get; set; }
    public int HealthyServers { get; set; }
    public int HealthPercentage { get; set; } // Bug: Should be double but using int
    public List<HealthStatus> ServerStatuses { get; set; } = new();
} 