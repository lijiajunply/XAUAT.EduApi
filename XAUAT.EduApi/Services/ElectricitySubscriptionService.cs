using EduApi.Data.Models;
using XAUAT.EduApi.Repos;

namespace XAUAT.EduApi.Services;

public interface IElectricitySubscriptionService
{
    Task<ElectricitySubscriptionResponse> UpsertAsync(CreateElectricitySubscriptionRequest request,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ElectricitySubscriptionResponse>> GetSubscriptionsAsync(string? email = null,
        CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}

public class ElectricitySubscriptionService(IElectricitySubscriptionRepository repository)
    : IElectricitySubscriptionService
{
    public async Task<ElectricitySubscriptionResponse> UpsertAsync(CreateElectricitySubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedUrl = request.Url.Trim();
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;

        var existing = await repository.GetByEmailAndUrlAsync(normalizedEmail, normalizedUrl, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            existing.Threshold = request.Threshold;
            existing.IsActive = true;
            existing.UpdatedAt = now;
            existing.NextCheckAt = now;

            await repository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            return ElectricitySubscriptionResponse.FromEntity(existing);
        }

        var subscription = new ElectricitySubscription
        {
            ElectricityUrl = normalizedUrl,
            Email = normalizedEmail,
            Threshold = request.Threshold,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            NextCheckAt = now
        };

        await repository.AddAsync(subscription, cancellationToken).ConfigureAwait(false);
        return ElectricitySubscriptionResponse.FromEntity(subscription);
    }

    public async Task<IReadOnlyList<ElectricitySubscriptionResponse>> GetSubscriptionsAsync(string? email = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        var subscriptions = await repository.GetSubscriptionsAsync(normalizedEmail, cancellationToken)
            .ConfigureAwait(false);

        return subscriptions
            .Select(ElectricitySubscriptionResponse.FromEntity)
            .ToList();
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var subscription = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return false;
        }

        await repository.DeleteAsync(subscription, cancellationToken).ConfigureAwait(false);
        return true;
    }
}
