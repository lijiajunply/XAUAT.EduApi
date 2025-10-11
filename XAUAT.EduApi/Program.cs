using EduApi.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using StackExchange.Redis;
using XAUAT.EduApi.Repos;
using XAUAT.EduApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
    });
});

var sql = Environment.GetEnvironmentVariable("SQL", EnvironmentVariableTarget.Process);

// if (string.IsNullOrEmpty(sql) && builder.Environment.IsDevelopment())
// {
//     sql = builder.Configuration["SQL"];
// }

if (string.IsNullOrEmpty(sql))
{
    builder.Services.AddDbContextFactory<EduContext>(opt =>
        opt.UseSqlite("Data Source=Data.db",
            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
}
else
{
    builder.Services.AddDbContextFactory<EduContext>(opt =>
        opt.UseNpgsql(sql,
            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
}

// Add services to the container.
builder.Services.AddScoped<IScoreRepository, ScoreRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<ICodeService, CodeService>();
builder.Services.AddScoped<ILoginService, SSOLoginService>();
builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddScoped<IProgramService, ProgramService>();
builder.Services.AddScoped<IInfoService, InfoService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<CookieCodeService>();

var redis = Environment.GetEnvironmentVariable("REDIS", EnvironmentVariableTarget.Process);
if (string.IsNullOrEmpty(redis) && builder.Environment.IsDevelopment())
{
    redis = builder.Configuration["Redis"];
}

if (!string.IsNullOrEmpty(redis))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redis));
}

// 配置默认的HttpClient（不跳过SSL验证）
builder.Services.AddHttpClient();
// 配置专门用于BusController的HttpClient（跳过SSL验证）
builder.Services.AddHttpClient("BusClient")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    });

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.MapOpenApi();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<IDbContextFactory<EduContext>>().CreateDbContext();

    var pending = context.Database.GetPendingMigrations();
    var enumerable = pending as string[] ?? pending.ToArray();

    if (enumerable.Length != 0)
    {
        Console.WriteLine("Pending migrations: " + string.Join("; ", enumerable));
        try
        {
            await context.Database.MigrateAsync();
            Console.WriteLine("Migrations applied successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Migration error: " + ex);
            throw; // 让异常冒泡，方便定位问题
        }
    }
    else
    {
        Console.WriteLine("No pending migrations.");
    }

    await context.SaveChangesAsync();
    await context.DisposeAsync();
}

app.UseAuthorization();
app.UseCors();
app.MapControllers();
app.MapScalarApiReference();

app.Run();