using Microsoft.EntityFrameworkCore;
using Video.Domain.Entities;
using Video.Domain.IRepository;
using Video.Domain.SeedWork;
using Video.Infrastructure.EntityFramework;

namespace Video.Infrastructure.Repository;

public class VideoHistoryRepository(VideoDbContext dbContext) : IVideoHistoryRepository
{
    public IUnitOfWork UnitOfWork => dbContext;

    public async Task<VideoHistory?> FindByIdAsync(Guid videoHistoryGuid)
    {
        return await dbContext.VideoHistories.FindAsync(videoHistoryGuid);
    }

    public async Task<List<VideoHistory>> FindByUserAsync(Guid userGuid, int page, int pageSize)
    {
        return await dbContext.VideoHistories
            .Where(h => h.UserGuid == userGuid)
            .OrderByDescending(h => h.TimeSpace.CreateAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<VideoHistory>> FindByUserAndVideoAsync(Guid userGuid, Guid videoGuid)
    {
        return await dbContext.VideoHistories
            .Where(h => h.UserGuid == userGuid && h.VideoGuid == videoGuid)
            .OrderByDescending(h => h.TimeSpace.CreateAt)
            .ToListAsync();
    }

    public async Task<VideoHistory?> FindLastByUserAndVideoAsync(Guid userGuid, Guid videoGuid)
    {
        return await dbContext.VideoHistories
            .Where(h => h.UserGuid == userGuid && h.VideoGuid == videoGuid)
            .OrderByDescending(h => h.TimeSpace.CreateAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<VideoHistory>> FindByVideoAsync(Guid videoGuid, int page, int pageSize)
    {
        return await dbContext.VideoHistories
            .Where(h => h.VideoGuid == videoGuid)
            .OrderByDescending(h => h.TimeSpace.CreateAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<long> CountByVideoAsync(Guid videoGuid)
    {
        return await dbContext.VideoHistories
            .LongCountAsync(h => h.VideoGuid == videoGuid);
    }

    public async Task<double> GetCompletionRateAsync(Guid videoGuid)
    {
        var total = await dbContext.VideoHistories
            .LongCountAsync(h => h.VideoGuid == videoGuid);
        if (total == 0) return 0;
        var completed = await dbContext.VideoHistories
            .LongCountAsync(h => h.VideoGuid == videoGuid && h.IsCompleted);
        return (double)completed / total;
    }

    public async Task<TimeSpan> GetAverageDurationAsync(Guid videoGuid)
    {
        var durations = await dbContext.VideoHistories
            .Where(h => h.VideoGuid == videoGuid && h.Duration > TimeSpan.Zero)
            .Select(h => h.Duration)
            .ToListAsync();

        if (durations.Count == 0) return TimeSpan.Zero;
        return TimeSpan.FromTicks((long)durations.Average(d => d.Ticks));
    }

    public async Task AddAsync(VideoHistory history)
    {
        await dbContext.VideoHistories.AddAsync(history);
    }

    public Task UpdateAsync(VideoHistory history)
    {
        dbContext.VideoHistories.Update(history);
        return Task.CompletedTask;
    }

    public async Task DeleteByIdAsync(Guid videoHistoryGuid)
    {
        var history = await dbContext.VideoHistories.FindAsync(videoHistoryGuid);
        if (history is not null)
            dbContext.VideoHistories.Remove(history);
    }

    public async Task DeleteByUserAndVideoAsync(Guid userGuid, Guid videoGuid)
    {
        var histories = await dbContext.VideoHistories
            .Where(h => h.UserGuid == userGuid && h.VideoGuid == videoGuid)
            .ToListAsync();
        dbContext.VideoHistories.RemoveRange(histories);
    }

    public async Task<int> CleanupOlderThanAsync(DateTimeOffset threshold)
    {
        var oldHistories = await dbContext.VideoHistories
            .Where(h => h.TimeSpace.CreateAt < threshold)
            .ToListAsync();
        dbContext.VideoHistories.RemoveRange(oldHistories);
        return oldHistories.Count;
    }

    // Explicit IRepository methods for backward compatibility
    public async Task AddByVideoHistoryAsync(VideoHistory history) => await AddAsync(history);
    public async Task UpdateByVideoHistoryAsync(VideoHistory history) { UpdateAsync(history); }
}
