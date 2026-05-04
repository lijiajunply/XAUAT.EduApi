using EduApi.Data;
using EduApi.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace XAUAT.EduApi.Repos;

public class ElectricitySubscriptionRepository(IDbContextFactory<EduContext> contextFactory)
    : IElectricitySubscriptionRepository
{
    public async Task<ElectricitySubscription?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await context.ElectricitySubscriptions
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ElectricitySubscription?> GetByEmailAndUrlAsync(string email, string electricityUrl,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await context.ElectricitySubscriptions
            .FirstOrDefaultAsync(x => x.Email == email && x.ElectricityUrl == electricityUrl, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ElectricitySubscription>> GetSubscriptionsAsync(string? email = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var query = context.ElectricitySubscriptions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(email))
        {
            query = query.Where(x => x.Email == email);
        }

        return await query
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ElectricitySubscription>> GetDueSubscriptionsAsync(DateTime utcNow, int take,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await context.ElectricitySubscriptions
            .Where(x => x.IsActive && x.NextCheckAt <= utcNow)
            .OrderBy(x => x.NextCheckAt)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(ElectricitySubscription subscription, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await context.ElectricitySubscriptions.AddAsync(subscription, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(ElectricitySubscription subscription, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        context.ElectricitySubscriptions.Update(subscription);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(ElectricitySubscription subscription, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        context.ElectricitySubscriptions.Remove(subscription);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddNotificationLogAsync(ElectricityNotificationLog log,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await context.ElectricityNotificationLogs.AddAsync(log, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
