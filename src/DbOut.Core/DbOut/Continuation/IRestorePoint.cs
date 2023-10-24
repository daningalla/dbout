using DbOut.Options;
using DbOut.Reporting;

namespace DbOut.Continuation;

/// <summary>
/// Represents a restore point manager.
/// </summary>
public interface IRestorePoint
{
    Task AddCacheFileDescriptorAsync(CacheFileDescriptor fileDescriptor);
    Task<IReadOnlyCollection<CacheFileDescriptor>> GetCacheFileDescriptorsAsync(string queryHash);
    Task InitializeAsync();
    Task SaveTelemetryAsync(SummaryTelemetryData telemetryData);

    Task BulkUpdateCacheFileDescriptorsAsync(
        IEnumerable<CacheFileDescriptor> insertDescriptors,
        IEnumerable<CacheFileDescriptor> deleteDescriptors);

    Task SaveRuntimeParametersAsync(RuntimeOptions options);
    Task<RestorePointParameters?> GetRuntimeParametersAsync(); 
    Task UpdateCommittedOffsetAsync(int value);
    Task<int?> GetCommittedOffsetAsync();
    RuntimeOptions CreateCriticalOptions(RuntimeOptions options);
    Task<SummaryTelemetryData> LoadTelemetryAsync();
}