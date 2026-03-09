# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

XAUAT EduApi is an ASP.NET Core Web API project that provides unified data access interfaces for Xi'an University of Architecture and Technology's educational system. The project simulates login and data scraping to provide standardized RESTful API services for mobile applications and other clients.

**Target Framework**: .NET 10.0
**Language**: Chinese (project documentation and commit messages are in Chinese)

## Solution Structure

The solution consists of three projects:

- **XAUAT.EduApi**: Main Web API project containing controllers, services, and business logic
- **EduApi.Data**: Data access layer with Entity Framework Core models and DbContext
- **Tests**: Unit tests, integration tests, and performance benchmarks using xUnit and BenchmarkDotNet

## Common Commands

### Build and Run
```bash
# Build the solution
dotnet build

# Run the main API project
dotnet run --project XAUAT.EduApi/XAUAT.EduApi.csproj

# Run with Docker
docker build -t xauat-edu-api .
docker run -d -p 8080:8080 xauat-edu-api
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests in a specific project
dotnet test Tests/XAUAT.EduApi.Tests.csproj

# Run tests with coverage
dotnet test /p:CollectCoverage=true

# Run performance benchmarks
dotnet run --project Tests/XAUAT.EduApi.Tests.csproj -c Release --filter "*Benchmark*"
```

### Database Migrations
```bash
# Add a new migration
dotnet ef migrations add <MigrationName> --project EduApi.Data --startup-project XAUAT.EduApi

# Apply migrations (automatically done on startup)
dotnet ef database update --project EduApi.Data --startup-project XAUAT.EduApi

# Remove last migration
dotnet ef migrations remove --project EduApi.Data --startup-project XAUAT.EduApi
```

## Architecture

### Multi-Tier Architecture

The project follows a layered architecture:

1. **Controllers** (`XAUAT.EduApi/Controllers/`): API endpoints handling HTTP requests
2. **Services** (`XAUAT.EduApi/Services/`): Business logic and external system integration
3. **Repositories** (`XAUAT.EduApi/Repos/`): Data access abstraction
4. **Data Layer** (`EduApi.Data/`): Entity Framework Core models and DbContext

### Key Architectural Components

#### Caching System
The project implements a sophisticated **three-tier caching architecture**:
- **Local Memory Cache**: Fast, in-process caching using MemoryCache
- **Redis Distributed Cache**: Shared cache across instances
- **Multi-Level Coordination**: Automatic fallback and intelligent routing

Key files:
- `XAUAT.EduApi/Caching/CacheService.cs`: Core cache orchestration
- `XAUAT.EduApi/Caching/LocalCacheManager.cs`: Local cache implementation
- `XAUAT.EduApi/Caching/RedisCacheManager.cs`: Redis cache implementation
- `CACHE_STRATEGY.md`: Comprehensive caching strategy documentation

Cache features:
- Hybrid TTL + LRU/LFU eviction strategy
- Cache warmup on startup (async, non-blocking)
- Automatic degradation when Redis is unavailable
- Pre-update mechanism to prevent cache avalanche

#### Service Registration
All services are registered through extension methods in `XAUAT.EduApi/Extensions/ServiceCollectionExtensions.cs`:
- Database services (SQLite for dev, PostgreSQL for prod)
- Redis services
- Cache services
- Repository services
- Business services
- HTTP client services with Polly retry policies
- Health check services
- Service discovery services

#### Dependency Injection Pattern
Services are registered with interfaces to support unit testing:
- `ICodeService` / `CodeService`: Cookie code generation
- `ILoginService` / `SSOLoginService`: SSO authentication
- `ICourseService` / `CourseService`: Course information
- `IScoreService` / `ScoreService`: Grade queries
- `IExamService` / `ExamService`: Exam schedules
- `IProgramService` / `ProgramService`: Academic programs
- `IPaymentService` / `PaymentService`: Payment records
- `ICacheService` / `CacheService`: Caching operations

### Database Design

The project uses Entity Framework Core with:
- **Development**: SQLite (`Data.db`)
- **Production**: PostgreSQL (configured via `SQL` environment variable)

Main entity: `ScoreResponse` with indexes on `UserId`, `Semester`, and composite `(UserId, Semester)`

Database context: `EduApi.Data.EduContext`
- Automatic migrations on startup (see `Program.cs` lines 76-107)
- DbContextFactory pattern for better performance

### HTTP Client Configuration

Three named HTTP clients configured with Polly retry policies:
- **DefaultClient**: Standard HTTP requests with 10s timeout
- **BusClient**: Special client for bus schedule API (skips SSL validation)
- **ExternalApiClient**: For external API integrations

### Middleware Pipeline

Middleware order (configured in `Program.cs`):
1. Error handling (`UseErrorHandling()`)
2. Authorization
3. CORS (`UseCustomCors()`)
4. Metrics collection (`UseMetricsCollection()`)
5. Prometheus monitoring (`UsePrometheusMonitoring()`)

### API Documentation

The project uses **Scalar** for API documentation (not Swagger):
- Access at `/scalar/v1` when running
- Configured via `Scalar.AspNetCore` package

### Monitoring and Observability

- **Logging**: Serilog with file sink
- **Metrics**: Prometheus metrics exposed at `/metrics`
- **Health Checks**: Available at configured health check endpoints
  - Database health check
  - Redis health check

## Configuration

### Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `SQL` | PostgreSQL connection string | `Host=localhost;Database=xauat_edu` |
| `REDIS` | Redis connection string | `localhost:6379` |

### appsettings.json Structure

Key configuration sections:
- `Service:Name` and `Service:InstanceId`: Service discovery
- `Cache`: Caching configuration (see CACHE_STRATEGY.md)
- `Serilog`: Logging configuration

### CORS Policy

Configured to allow:
- All `*.zeabur.app` subdomains
- All `*.xauat.site` subdomains
- `http://localhost` for local development

## Development Guidelines

### Code Organization

- Controllers should be thin, delegating to services
- Services contain business logic and external integrations
- Use interfaces for all services to enable unit testing
- Repository pattern for data access abstraction

### Testing Strategy

The test project includes:
- **Unit Tests** (`Tests/Services/`): Service layer tests with Moq
- **Integration Tests** (`Tests/Integration/`): End-to-end API tests
- **Performance Tests** (`Tests/Performance/`): BenchmarkDotNet benchmarks

When writing tests:
- Use `Microsoft.EntityFrameworkCore.InMemory` for database tests
- Mock external dependencies with Moq
- Follow AAA pattern (Arrange, Act, Assert)

### Caching Best Practices

When implementing new features:
1. Check if data should be cached (see CACHE_STRATEGY.md section 5)
2. Use appropriate cache keys: `{Prefix}:{Module}:{EntityType}:{Id}`
3. Set appropriate TTL based on data volatility
4. Consider cache invalidation strategy when data changes
5. Use `ICacheService` for all caching operations

### Error Handling

- Custom exceptions in `XAUAT.EduApi/Exceptions/`
- Global error handling middleware
- Detailed logging with Serilog
- Cache operations should not break main business flow

### Performance Considerations

- Use `AsNoTracking()` for read-only queries (already applied in repositories)
- Leverage multi-level caching for frequently accessed data
- Use `DbContextFactory` instead of scoped DbContext for better performance
- HTTP clients use connection pooling and retry policies

## Recent Changes

Based on recent commits:
- Refactored `CookieCodeService` to use interface pattern for testability
- Added `AsNoTracking()` to repository queries for performance
- Removed plugin system and event bus functionality
- Added `PlaygroundController` for testing purposes
- Deleted frontend webapp project files

## Important Notes

- This project is for educational and research purposes only
- Must comply with university regulations
- Do not use for illegal purposes
- Developers are not responsible for consequences of misuse
