<#
.SYNOPSIS
    校园地图POI数据导入工具（PowerShell版本）
.DESCRIPTION
    支持从CSV/JSON文件批量导入地理坐标数据到数据库
.NOTES
    文件: Import-MapData.ps1
    作者: Auto-generated
#>

param(
    [Parameter(Mandatory=$false, HelpMessage="数据文件路径 (CSV或JSON)")]
    [string]$FilePath,

    [Parameter(Mandatory=$false, HelpMessage="操作类型: import|template|stats|clear")]
    [string]$Action = "import",

    [Parameter(Mandatory=$false, HelpMessage="API基础URL")]
    [string]$ApiBaseUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Stop"

Write-Host "=== 校园地图POI数据导入工具 ===" -ForegroundColor Cyan
Write-Host ""

function Test-ApiConnection {
    Write-Host "测试API连接..." -NoNewline
    try {
        $response = Invoke-RestMethod -Uri "$ApiBaseUrl/Map/categories" -Method Get -TimeoutSec 5
        Write-Host " ✓ 成功" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host " ✗ 失败: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

function Import-CsvData {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        Write-Host "错误: 文件不存在 - $Path" -ForegroundColor Red
        return
    }

    Write-Host "读取CSV文件: $Path" -ForegroundColor Yellow
    $csvData = Import-Csv -Path $Path -Encoding UTF8

    if ($csvData.Count -eq 0) {
        Write-Host "错误: CSV文件为空" -ForegroundColor Red
        return
    }

    Write-Host "发现 $($csvData.Count) 条记录" -ForegroundColor Cyan
    Write-Host ""

    $successCount = 0
    $errorCount = 0

    foreach ($row in $csvData) {
        try {
            # 构建POI对象
            $poi = @{
                name = $row.名称 -or $row.Name -or $row.name
                category = $row.分类 -or $row.Category -or $row.category
                latitude = [decimal]($row.纬度 -or $row.Latitude -or $row.latitude)
                longitude = [decimal]($row.经度 -or $row.Longitude -or $row.longitude)
                description = $row.描述 -or $row.Description -or $row.description
                address = $row.地址 -or $row.Address -or $row.address
                campus = $row.校区 -or $row.Campus -or $row.campus
                icon = $row.图标 -or $row.Icon -or $row.icon
                sort_order = if ($row.排序 -or $row.SortOrder -or $row.sort_order) { [int]($row.排序 -or $row.SortOrder -or $row.sort_order) } else { 0 }
                is_active = $true
            }

            # 转换为JSON并发送到API
            $body = $poi | ConvertTo-Json -Compress
            $response = Invoke-RestMethod -Uri "$ApiBaseUrl/Map/import" -Method Post -Body $body -ContentType "application/json" -ErrorAction Stop

            $successCount++
            Write-Host "  [$successCount] ✓ $($poi.name)" -ForegroundColor Green
        }
        catch {
            $errorCount++
            Write-Host "  ✗ 导入失败: $($_.Exception.Message)" -ForegroundColor Red
        }
    }

    Write-Host ""
    Write-Host "导入完成:" -ForegroundColor Cyan
    Write-Host "  成功: $successCount 条" -ForegroundColor Green
    Write-Host "  失败: $errorCount 条" -ForegroundColor Red
}

function Import-JsonData {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        Write-Host "错误: 文件不存在 - $Path" -ForegroundColor Red
        return
    }

    Write-Host "读取JSON文件: $Path" -ForegroundColor Yellow
    $jsonData = Get-Content -Path $Path -Raw | ConvertFrom-Json

    if ($jsonData.Count -eq 0) {
        Write-Host "错误: JSON文件为空" -ForegroundColor Red
        return
    }

    Write-Host "发现 $($jsonData.Count) 条记录" -ForegroundColor Cyan
    Write-Host ""

    # 批量导入JSON数组
    try {
        $body = $jsonData | ConvertTo-Json -Depth 10 -Compress
        $response = Invoke-RestMethod -Uri "$ApiBaseUrl/Map/import/batch" -Method Post -Body $body -ContentType "application/json" -ErrorAction Stop

        Write-Host "✓ 批量导入成功: $($response.imported_count) 条" -ForegroundColor Green
    }
    catch {
        Write-Host "✗ 批量导入失败: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "尝试逐条导入..." -ForegroundColor Yellow

        $successCount = 0
        foreach ($item in $jsonData) {
            try {
                $itemBody = $item | ConvertTo-Json -Compress
                Invoke-RestMethod -Uri "$ApiBaseUrl/Map/import" -Method Post -Body $itemBody -ContentType "application/json" -ErrorAction Stop
                $successCount++
                Write-Host "  [$successCount] ✓ $($item.name)" -ForegroundColor Green
            }
            catch {
                Write-Host "  ✗ $($item.name): $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }
}

function New-SampleTemplate {
    $timestamp = Get-Date -Format "yyyyMMddHHmmss"
    $csvFile = "poi_template_$timestamp.csv"
    $jsonFile = "poi_template_$timestamp.json"

    # CSV模板
    $csvTemplate = @"
名称,分类,纬度,经度,描述,地址,校区,图标,排序
图书馆,教学建筑,34.245000,108.990000,主图书馆,雁塔校区,雁塔,library.png,1
教学楼A,教学建筑,34.246000,108.991000,A栋教学楼,雁塔校区,雁塔,building.png,2
学生食堂,餐饮服务,34.247000,108.992000,第一食堂,雁塔校区,雁塔,canteen.png,3
学生宿舍1,住宿服务,34.248000,108.993000,1号宿舍楼,雁塔校区,雁塔,dormitory.png,4
行政楼,办公建筑,34.249000,108.994000,学校办公楼,雁塔校区,雁塔,office.png,5
体育馆,体育设施,34.250000,108.995000,综合体育馆,雁塔校区,雁塔,gym.png,6
实验楼,教学建筑,34.251000,108.996000,理工实验楼,雁塔校区,雁塔,laboratory.png,7
"@

    Set-Content -Path $csvFile -Value $csvTemplate -Encoding UTF8

    # JSON模板
    $jsonTemplate = @[
        @{
            name = "图书馆"
            category = "教学建筑"
            latitude = 34.245000
            longitude = 108.990000
            description = "主图书馆"
            address = "雁塔校区"
            campus = "雁塔"
            icon = "library.png"
            sort_order = 1
        },
        @{
            name = "教学楼A"
            category = "教学建筑"
            latitude = 34.246000
            longitude = 108.991000
            description = "A栋教学楼"
            address = "雁塔校区"
            campus = "雁塔"
            icon = "building.png"
            sort_order = 2
        }
    ] | ConvertTo-Json -Depth 10

    Set-Content -Path $jsonFile -Value $jsonTemplate -Encoding UTF8

    Write-Host "✓ 已生成示例模板:" -ForegroundColor Green
    Write-Host "  CSV模板: $csvFile" -ForegroundColor Cyan
    Write-Host "  JSON模板: $jsonFile" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "请编辑模板文件填入实际坐标数据后重新运行导入" -ForegroundColor Yellow
}

function Show-Statistics {
    Write-Host "获取统计数据..." -NoNewline
    try {
        $pois = Invoke-RestMethod -Uri "$ApiBaseUrl/Map" -Method Get
        $categories = Invoke-RestMethod -Uri "$ApiBaseUrl/Map/categories" -Method Get
        $campuses = Invoke-RestMethod -Uri "$ApiBaseUrl/Map/campuses" -Method Get

        Write-Host " ✓" -ForegroundColor Green
        Write-Host ""
        Write-Host "=== POI数据统计 ===" -ForegroundColor Cyan
        Write-Host "总数量: $($pois.Count)" -ForegroundColor White
        Write-Host ""
        Write-Host "--- 分类统计 ---" -ForegroundColor Yellow
        foreach ($cat in $categories) {
            $count = ($pois | Where-Object { $_.category -eq $cat }).Count
            Write-Host "  $cat : $count 个" -ForegroundColor White
        }
        Write-Host ""
        Write-Host "--- 校区统计 ---" -ForegroundColor Yellow
        foreach ($campus in $campuses) {
            $count = ($pois | Where-Object { $_.campus -eq $campus }).Count
            Write-Host "  $campus : $count 个" -ForegroundColor White
        }
    }
    catch {
        Write-Host " ✗ 获取统计失败: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# 主逻辑
switch ($Action.ToLower()) {
    "import" {
        if ([string]::IsNullOrEmpty($FilePath)) {
            Write-Host "请指定要导入的文件路径" -ForegroundColor Red
            Write-Host "用法: .\Import-MapData.ps1 -FilePath data.csv -Action import" -ForegroundColor Yellow
            exit 1
        }

        if (-not (Test-ApiConnection)) {
            exit 1
        }

        $extension = [System.IO.Path]::GetExtension($FilePath).ToLower()
        switch ($extension) {
            ".csv" { Import-CsvData -Path $FilePath }
            ".json" { Import-JsonData -Path $FilePath }
            default {
                Write-Host "不支持的文件格式: $extension (仅支持.csv和.json)" -ForegroundColor Red
                exit 1
            }
        }
    }

    "template" {
        New-SampleTemplate
    }

    "stats" {
        if (-not (Test-ApiConnection)) {
            exit 1
        }
        Show-Statistics
    }

    "clear" {
        Write-Host "警告: 此操作将清空所有POI数据!" -ForegroundColor Red
        $confirm = Read-Host "确认继续? (y/N)"
        if ($confirm -eq 'y') {
            try {
                Invoke-RestMethod -Uri "$ApiBaseUrl/Map/clear" -Method Delete -ErrorAction Stop
                Write-Host "✓ 已清空所有POI数据" -ForegroundColor Green
            }
            catch {
                Write-Host "✗ 清空失败: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
        else {
            Write-Host "已取消操作" -ForegroundColor Yellow
        }
    }

    default {
        Write-Host "未知操作: $Action" -ForegroundColor Red
        Write-Host "支持的操作: import, template, stats, clear" -ForegroundColor Yellow
        exit 1
    }
}

Write-Host ""
Write-Host "操作完成" -ForegroundColor Cyan
