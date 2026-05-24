using System.Data;
using System.Globalization;
using EduApi.Data;
using EduApi.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace XAUAT.EduApi.Tools;

/// <summary>
/// 地图POI数据导入工具
/// 用于从CSV/Excel/JSON等格式导入校园地理坐标数据
/// </summary>
public class MapDataImporter
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=== 校园地图POI数据导入工具 ===\n");

        try
        {
            // 构建配置
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            // 配置依赖注入
            var services = new ServiceCollection();
            var sqlConnectionString = configuration.GetConnectionString("SQL") ??
                                      Environment.GetEnvironmentVariable("SQL");

            if (string.IsNullOrEmpty(sqlConnectionString))
            {
                // 开发环境使用SQLite
                services.AddDbContext<EduContext>(options =>
                    options.UseSqlite("Data Source=Data.db"));
                Console.WriteLine("使用 SQLite 数据库");
            }
            else
            {
                // 生产环境使用PostgreSQL
                services.AddDbContext<EduContext>(options =>
                    options.UseNpgsql(sqlConnectionString));
                Console.WriteLine("使用 PostgreSQL 数据库");
            }

            var serviceProvider = services.BuildServiceProvider();

            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EduContext>();

            // 确保数据库已创建
            await dbContext.Database.EnsureCreatedAsync();
            Console.WriteLine("数据库连接成功\n");

            // 显示菜单
            Console.WriteLine("请选择操作：");
            Console.WriteLine("1. 从CSV文件导入POI数据");
            Console.WriteLine("2. 从JSON文件导入POI数据");
            Console.WriteLine("3. 从Excel文件导入POI数据");
            Console.WriteLine("4. 生成示例数据模板");
            Console.WriteLine("5. 清空所有POI数据");
            Console.WriteLine("6. 查看当前POI统计信息");
            Console.WriteLine("0. 退出");
            Console.Write("\n请输入选项 (0-6): ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await ImportFromCsvAsync(dbContext);
                    break;
                case "2":
                    await ImportFromJsonAsync(dbContext);
                    break;
                case "3":
                    await ImportFromExcelAsync(dbContext);
                    break;
                case "4":
                    GenerateSampleTemplate();
                    break;
                case "5":
                    await ClearAllPoisAsync(dbContext);
                    break;
                case "6":
                    await ShowStatisticsAsync(dbContext);
                    break;
                case "0":
                    Console.WriteLine("退出程序");
                    return 0;
                default:
                    Console.WriteLine("无效选项");
                    break;
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n错误: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }

    /// <summary>
    /// 从CSV文件导入POI数据
    /// CSV格式: 名称,分类,纬度,经度,描述,地址,校区,图标,排序
    /// </summary>
    private static async Task ImportFromCsvAsync(EduContext dbContext)
    {
        Console.Write("\n请输入CSV文件路径: ");
        var filePath = Console.ReadLine()?.Trim('"');

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            Console.WriteLine("文件不存在!");
            return;
        }

        Console.WriteLine($"\n正在从CSV文件导入: {filePath}");
        var lines = await File.ReadAllLinesAsync(filePath);
        var count = 0;
        var errors = new List<string>();

        for (int i = 1; i < lines.Length; i++) // 跳过标题行
        {
            try
            {
                var parts = lines[i].Split(',');
                if (parts.Length < 4)
                {
                    errors.Add($"第{i + 1}行: 列数不足");
                    continue;
                }

                var poi = new MapPoiModel
                {
                    Name = parts[0].Trim(),
                    Category = parts[1].Trim(),
                    Latitude = decimal.Parse(parts[2].Trim(), CultureInfo.InvariantCulture),
                    Longitude = decimal.Parse(parts[3].Trim(), CultureInfo.InvariantCulture),
                    Description = parts.Length > 4 ? parts[4].Trim() : null,
                    Address = parts.Length > 5 ? parts[5].Trim() : null,
                    Campus = parts.Length > 6 ? parts[6].Trim() : null,
                    Icon = parts.Length > 7 ? parts[7].Trim() : null,
                    SortOrder = parts.Length > 8 ? int.Parse(parts[8].Trim()) : 0,
                    IsActive = true
                };

                dbContext.MapPois.Add(poi);
                count++;
            }
            catch (Exception ex)
            {
                errors.Add($"第{i + 1}行: {ex.Message}");
            }
        }

        if (count > 0)
        {
            await dbContext.SaveChangesAsync();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✓ 成功导入 {count} 条POI数据");
            Console.ResetColor();
        }

        if (errors.Any())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n⚠ 跳过 {errors.Count} 条错误记录:");
            foreach (var error in errors.Take(10))
            {
                Console.WriteLine($"  - {error}");
            }
            Console.ResetColor();
        }
    }

    /// <summary>
    /// 从JSON文件导入POI数据
    /// JSON格式: [{name, category, latitude, longitude, ...}, ...]
    /// </summary>
    private static async Task ImportFromJsonAsync(EduContext dbContext)
    {
        Console.Write("\n请输入JSON文件路径: ");
        var filePath = Console.ReadLine()?.Trim('"');

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            Console.WriteLine("文件不存在!");
            return;
        }

        Console.WriteLine($"\n正在从JSON文件导入: {filePath}");
        var jsonContent = await File.ReadAllTextAsync(filePath);
        var pois = System.Text.Json.JsonSerializer.Deserialize<List<MapPoiModel>>(jsonContent,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (pois == null || !pois.Any())
        {
            Console.WriteLine("未找到有效的POI数据或JSON格式错误");
            return;
        }

        foreach (var poi in pois)
        {
            poi.IsActive = true;
            dbContext.MapPois.Add(poi);
        }

        await dbContext.SaveChangesAsync();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n✓ 成功导入 {pois.Count} 条POI数据");
        Console.ResetColor();
    }

    /// <summary>
    /// 从Excel文件导入POI数据（需要安装 EPPlus 包）
    /// </summary>
    private static async Task ImportFromExcelAsync(EduContext dbContext)
    {
        Console.Write("\n请输入Excel文件路径: ");
        var filePath = Console.ReadLine()?.Trim('"');

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            Console.WriteLine("文件不存在!");
            return;
        }

        Console.WriteLine($"\n注意: Excel导入需要安装EPPlus包");
        Console.WriteLine("运行命令: dotnet add package EPPlus");

        try
        {
            // 这里需要根据实际使用的Excel库来实现
            // 示例使用简单的CSV转换方式
            Console.WriteLine("建议将Excel导出为CSV后使用CSV导入功能");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Excel导入失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 生成示例数据模板
    /// </summary>
    private static void GenerateSampleTemplate()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var csvPath = $"poi_template_{timestamp}.csv";
        var jsonPath = $"poi_template_{timestamp}.json";

        // 生成CSV模板
        var csvContent = @"名称,分类,纬度,经度,描述,地址,校区,图标,排序
图书馆,教学建筑,34.245000,108.990000,主图书馆,雁塔校区,雁塔,library.png,1
教学楼A,教学建筑,34.246000,108.991000,A栋教学楼,雁塔校区,雁塔,building.png,2
学生食堂,餐饮服务,34.247000,108.992000,第一食堂,雁塔校区,雁塔,canteen.png,3
学生宿舍1,住宿服务,34.248000,108.993000,1号宿舍楼,雁塔校区,雁塔,dormitory.png,4
行政楼,办公建筑,34.249000,108.994000,学校办公楼,雁塔校区,雁塔,office.png,5
体育馆,体育设施,34.250000,108.995000,综合体育馆,雁塔校区,雁塔,gym.png,6
实验楼,教学建筑,34.251000,108.996000,理工实验楼,雁塔校区,雁塔,laboratory.png,7";

        File.WriteAllText(csvPath, csvContent, System.Text.Encoding.UTF8);

        // 生成JSON模板
        var samplePois = new List<object>
        {
            new
            {
                name = "图书馆",
                category = "教学建筑",
                latitude = 34.245000m,
                longitude = 108.990000m,
                description = "主图书馆",
                address = "雁塔校区",
                campus = "雁塔",
                icon = "library.png",
                sort_order = 1
            },
            new
            {
                name = "教学楼A",
                category = "教学建筑",
                latitude = 34.246000m,
                longitude = 108.991000m,
                description = "A栋教学楼",
                address = "雁塔校区",
                campus = "雁塔",
                icon = "building.png",
                sort_order = 2
            }
        };

        var jsonContent = System.Text.Json.JsonSerializer.Serialize(samplePois, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });

        File.WriteAllText(jsonPath, jsonContent, System.Text.Encoding.UTF8);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n✓ 已生成示例模板文件:");
        Console.WriteLine($"  - CSV模板: {csvPath}");
        Console.WriteLine($"  - JSON模板: {jsonPath}");
        Console.WriteLine("\n请编辑模板文件填入实际数据，然后使用导入功能");
        Console.ResetColor();
    }

    /// <summary>
    /// 清空所有POI数据
    /// </summary>
    private static async Task ClearAllPoisAsync(EduContext dbContext)
    {
        Console.Write("\n确认要清空所有POI数据吗？(y/N): ");
        var confirm = Console.ReadLine()?.ToLower();

        if (confirm != "y")
        {
            Console.WriteLine("已取消操作");
            return;
        }

        var count = await dbContext.MapPois.CountAsync();
        dbContext.MapPois.RemoveRange(dbContext.MapPois);
        await dbContext.SaveChangesAsync();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n✓ 已清空 {count} 条POI数据");
        Console.ResetColor();
    }

    /// <summary>
    /// 显示当前POI统计信息
    /// </summary>
    private static async Task ShowStatisticsAsync(EduContext dbContext)
    {
        var totalCount = await dbContext.MapPois.CountAsync();
        var activeCount = await dbContext.MapPois.CountAsync(p => p.IsActive);
        var categories = await dbContext.MapPois
            .Where(p => p.IsActive)
            .GroupBy(p => p.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToListAsync();

        var campuses = await dbContext.MapPois
            .Where(p => p.IsActive && p.Campus != null)
            .GroupBy(p => p.Campus!)
            .Select(g => new { Campus = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToListAsync();

        Console.WriteLine("\n=== POI数据统计 ===");
        Console.WriteLine($"总数量: {totalCount}");
        Console.WriteLine($"启用数量: {activeCount}");
        Console.WriteLine($"禁用数量: {totalCount - activeCount}");

        Console.WriteLine("\n--- 按分类统计 ---");
        foreach (var cat in categories)
        {
            Console.WriteLine($"  {cat.Category}: {cat.Count} 个");
        }

        Console.WriteLine("\n--- 按校区统计 ---");
        foreach (var campus in campuses)
        {
            Console.WriteLine($"  {campus.Campus}: {campus.Count} 个");
        }
    }
}
