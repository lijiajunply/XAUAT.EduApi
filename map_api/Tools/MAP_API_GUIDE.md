# 校园地图POI数据导入使用指南

## 📋 功能概述

已为项目添加完整的校园地图地理坐标API接口和数据导入功能：

### ✅ 已实现功能

1. **API接口层** - 10个RESTful接口
2. **数据模型** - Entity Framework Core POI实体
3. **业务逻辑** - 带缓存的数据查询服务
4. **导入工具** - 支持CSV/JSON批量导入

---

## 🔌 API接口列表

### 查询接口（GET）

| 接口 | 方法 | 说明 | 示例 |
|------|------|------|------|
| `/Map` | GET | 获取所有POI | `GET /Map` |
| `/Map/category/{category}` | GET | 按分类查询 | `GET /Map/category/教学楼` |
| `/Map/campus/{campus}` | GET | 按校区查询 | `GET /Map/campus/雁塔` |
| `/Map/{id}` | GET | 根据ID查询详情 | `GET /Map/1` |
| `/Map/search?keyword=xx` | GET | 关键词搜索 | `GET /Map/search?keyword=图书馆` |
| `/Map/categories` | GET | 获取所有分类 | `GET /Map/categories` |
| `/Map/campuses` | GET | 获取所有校区 | `GET /Map/campuses` |

### 管理接口（POST/DELETE）

| 接口 | 方法 | 说明 |
|------|------|------|
| `/Map/import` | POST | 导入单个POI |
| `/Map/import/batch` | POST | 批量导入POI |
| `/Map/clear` | DELETE | 清空所有POI |

---

## 📊 数据模型结构

```json
{
  "id": 1,
  "name": "图书馆",
  "category": "教学建筑",
  "latitude": 34.245000,
  "longitude": 108.990000,
  "description": "主图书馆",
  "address": "雁塔校区",
  "campus": "雁塔",
  "icon": "library.png",
  "is_active": true,
  "sort_order": 1,
  "created_at": "2026-05-24T10:30:00Z",
  "updated_at": "2026-05-24T10:30:00Z"
}
```

### 字段说明

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| name | string | ✓ | POI名称（如：图书馆） |
| category | string | ✓ | 分类（如：教学建筑、餐饮服务） |
| latitude | decimal(10,7) | ✓ | 纬度坐标 |
| longitude | decimal(10,7) | ✓ | 经度坐标 |
| description | string | 描述信息 | |
| address | string | 详细地址 | |
| campus | string | 所属校区（雁塔/草堂） | |
| icon | string | 图标文件名 | |
| sort_order | int | 排序权重（默认0） | |

---

## 🚀 快速开始

### 方式一：使用PowerShell脚本导入（推荐）

#### 1. 生成数据模板

```powershell
cd XAUAT.EduApi/Tools
.\Import-MapData.ps1 -Action template
```

将生成两个模板文件：
- `poi_template_时间戳.csv`
- `poi_template_时间戳.json`

#### 2. 编辑数据文件

**CSV格式示例** (`poi_data.csv`)：
```csv
名称,分类,纬度,经度,描述,地址,校区,图标,排序
图书馆,教学建筑,34.245000,108.990000,主图书馆,雁塔校区,雁塔,library.png,1
教学楼A,教学建筑,34.246000,108.991000,A栋教学楼,雁塔校区,雁塔,building.png,2
学生食堂,餐饮服务,34.247000,108.992000,第一食堂,雁塔校区,雁塔,canteen.png,3
```

**JSON格式示例** (`poi_data.json`)：
```json
[
  {
    "name": "图书馆",
    "category": "教学建筑",
    "latitude": 34.245000,
    "longitude": 108.990000,
    "description": "主图书馆",
    "address": "雁塔校区",
    "campus": "雁塔",
    "icon": "library.png",
    "sort_order": 1
  }
]
```

#### 3. 导入数据

```powershell
# 导入CSV文件
.\Import-MapData.ps1 -FilePath poi_data.csv -Action import

# 导入JSON文件
.\Import-MapData.ps1 -FilePath poi_data.json -Action import

# 查看统计
.\Import-MapData.ps1 -Action stats

# 清空数据（慎用）
.\Import-MapData.ps1 -Action clear
```

### 方式二：直接调用API接口

#### 批量导入示例（cURL）

```bash
# 批量导入JSON数据
curl -X POST http://localhost:5000/Map/import/batch \
  -H "Content-Type: application/json" \
  -d '[
    {
      "name": "图书馆",
      "category": "教学建筑",
      "latitude": 34.245000,
      "longitude": 108.990000,
      "campus": "雁塔"
    },
    {
      "name": "教学楼A",
      "category": "教学建筑",
      "latitude": 34.246000,
      "longitude": 108.991000,
      "campus": "雁塔"
    }
  ]'
```

#### 单个导入示例

```bash
curl -X POST http://localhost:5000/Map/import \
  -H "Content-Type: application/json" \
  -d '{
    "name": "体育馆",
    "category": "体育设施",
    "latitude": 34.250000,
    "longitude": 108.995000,
    "campus": "雁塔"
  }'
```

### 方式三：使用C#控制台工具

```bash
cd XAUAT.EduApi
dotnet run --project Tools/MapDataImporter.csproj
```

交互式菜单支持：
1. CSV导入
2. JSON导入
3. Excel导入（需EPPlus包）
4. 生成模板
5. 清空数据
6. 统计信息

---

## 📝 数据准备建议

### 从数据库导出坐标数据

如果你已有数据库中的坐标数据，可以使用SQL导出：

```sql
-- PostgreSQL示例
COPY (
  SELECT 
    name,
    category,
    latitude::text,
    longitude::text,
    description,
    address,
    campus,
    icon,
    sort_order::text
  FROM your_poi_table
) TO '/tmp/poi_export.csv' WITH CSV HEADER;
```

### 从Excel转换

1. 打开Excel文件
2. 另存为 **CSV (逗号分隔)(*.csv)**
3. 确保列名与模板一致
4. 使用PowerShell脚本导入

### 常用分类建议

```
教学建筑 (教学楼、实验楼、图书馆)
餐饮服务 (食堂、餐厅)
住宿服务 (宿舍楼)
办公建筑 (行政楼、办公楼)
体育设施 (体育馆、操场)
生活服务 (超市、银行、医院)
交通设施 (校门、车站)
景观景点 (广场、花园)
```

---

## 🔍 API调用示例

### 获取所有POI

```http
GET /Map HTTP/1.1
Host: your-api-domain.com
Accept: application/json
```

**响应示例**：
```json
[
  {
    "id": 1,
    "name": "图书馆",
    "category": "教学建筑",
    "latitude": 34.2450000,
    "longitude": 108.9900000,
    "description": "主图书馆",
    "address": "雁塔校区",
    "campus": "雁塔",
    "icon": "library.png",
    "is_active": true,
    "sort_order": 1,
    "created_at": "2026-05-24T10:30:00Z",
    "updated_at": "2026-05-24T10:30:00Z"
  }
]
```

### 搜索POI

```http
GET /Map/search?keyword=图书馆 HTTP/1.1
```

### 按分类筛选

```http
GET /Map/category/教学建筑 HTTP/1.1
```

---

## ⚙️ 配置说明

### 缓存策略

地图数据采用**24小时缓存**，自动更新：
- L1: 本地内存缓存（毫秒级响应）
- L2: Redis分布式缓存（可选）
- 导入操作会自动清除相关缓存

### 性能优化

已添加数据库索引优化查询性能：
- `category` 分类索引
- `campus` 校区索引
- `is_active` 状态索引
- `(latitude, longitude)` 地理坐标复合索引

---

## 🛠️ 故障排除

### 常见问题

**Q: 导入时报"经纬度不能为0"？**
A: 检查CSV/JSON中的坐标值是否正确，确保使用小数点格式（34.245而非34,245）

**Q: 中文乱码？**
A: 确保文件保存为UTF-8编码，CSV文件使用BOM头

**Q: API返回404？**
A: 确认项目已重新编译并启动，检查路由配置

**Q: 缓存未更新？**
A: 导入后会自动清除缓存，或等待24小时过期

---

## 📁 文件结构

```
XAUAT.EduApi/
├── EduApi.Data/
│   └── Models/
│       └── MapPoiModel.cs          # POI数据模型
├── XAUAT.EduApi/
│   ├── Controllers/
│   │   └── MapController.cs        # API控制器（10个接口）
│   ├── Services/
│   │   ├── IMapService.cs          # 服务接口
│   │   └── MapService.cs           # 业务逻辑实现
│   ├── Extensions/
│   │   └── RedisExtensions.cs      # 新增地图缓存键
│   └── Tools/
│       ├── MapDataImporter.cs      # C#导入工具
│       └── Import-MapData.ps1      # PowerShell导入脚本
└── EduApi.Data/
    └── EduContext.cs               # 更新：新增MapPois表
```

---

## ✅ 验证检查清单

- [ ] 项目编译成功（`dotnet build`）
- [ ] 数据库迁移成功（自动创建map_pois表）
- [ ] API启动正常（访问 `/Map/categories` 测试）
- [ ] 使用模板生成功能测试
- [ ] 导入测试数据成功
- [ ] 查询接口返回正确JSON格式
- [ ] 前端可以正常渲染地图标记点

---

## 🎯 下一步建议

1. **前端集成**：使用Leaflet/高德地图渲染POI点位
2. **图标资源**：准备各分类的图标PNG文件
3. **权限控制**：对import/clear接口添加认证
4. **批量编辑**：提供管理后台UI进行POI管理
5. **路径规划**：集成导航算法实现路径规划功能

---

**技术支持**：如有问题请查看日志或联系开发团队
