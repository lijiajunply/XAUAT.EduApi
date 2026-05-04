using EduApi.Data.Models;

namespace XAUAT.EduApi.Repos;

public interface IElectricitySubscriptionRepository
{
    Task<ElectricitySubscription?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<ElectricitySubscription?> GetByEmailAndUrlAsync(string email, string electricityUrl,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ElectricitySubscription>> GetSubscriptionsAsync(string? email = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ElectricitySubscription>> GetDueSubscriptionsAsync(DateTime utcNow, int take,
        CancellationToken cancellationToken = default);
    Task AddAsync(ElectricitySubscription subscription, CancellationToken cancellationToken = default);
    Task UpdateAsync(ElectricitySubscription subscription, CancellationToken cancellationToken = default);
    Task DeleteAsync(ElectricitySubscription subscription, CancellationToken cancellationToken = default);
    Task AddNotificationLogAsync(ElectricityNotificationLog log, CancellationToken cancellationToken = default);
}
