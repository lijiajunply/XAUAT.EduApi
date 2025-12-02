using EduApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace XAUAT.EduApi.HealthChecks;

/// <summary>
/// 数据库健康检查
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDbContextFactory<EduContext> _dbContextFactory;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContextFactory">数据库上下文工厂</param>
    /// <param name="logger">日志记录器</param>
    public DatabaseHealthCheck(IDbContextFactory<EduContext> dbContextFactory, ILogger<DatabaseHealthCheck> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    /// <summary>
    /// 执行健康检查
    /// </summary>
    /// <param name="context">健康检查上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>健康检查结果</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            // 检查数据库连接
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

            if (!canConnect)
            {
                _logger.LogWarning("数据库连接失败");
                return HealthCheckResult.Unhealthy("数据库连接失败");
            }

            // 检查数据库迁移
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            var migrations = pendingMigrations as string[] ?? pendingMigrations.ToArray();
            if (migrations.Any())
            {
                _logger.LogWarning("存在待执行的数据库迁移: {Migrations}", string.Join(", ", migrations));
                return HealthCheckResult.Degraded($"存在待执行的数据库迁移: {string.Join(", ", migrations)}");
            }

            _logger.LogInformation("数据库健康检查通过");
            return HealthCheckResult.Healthy("数据库连接正常，所有迁移已执行");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据库健康检查失败");
            return HealthCheckResult.Unhealthy("数据库健康检查失败", ex);
        }
    }
}