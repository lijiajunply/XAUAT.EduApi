using Scalar.AspNetCore;
using StackExchange.Redis;
using XAUAT.EduApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
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
builder.Services.AddScoped<ICodeService, CodeService>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddScoped<IProgramService, ProgramService>();

var redis = Environment.GetEnvironmentVariable("REDIS", EnvironmentVariableTarget.Process);
if (string.IsNullOrEmpty(redis) && builder.Environment.IsDevelopment())
{
    redis = builder.Configuration["Redis"];
}
if (!string.IsNullOrEmpty(redis))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redis));
}

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.MapOpenApi();

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseCors();
app.MapControllers();
app.MapScalarApiReference();

app.Run();