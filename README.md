# XAUAT EduApi

西安建筑科技大学教务系统 API 接口项目，提供对学校教务系统的数据访问接口。

## 项目简介

这是一个基于 ASP.NET Core 的 Web API 项目，旨在为西安建筑科技大学（XAUAT）的教务系统提供统一的数据访问接口。该项目通过模拟登录和数据抓取，为移动应用和其他客户端提供标准化的 RESTful API 服务。

## 功能特性

本项目提供了以下主要功能模块：

### 教务相关
- **登录认证** - 用户登录验证
- **课程信息** - 获取学生课程表
- **成绩查询** - 查询各学期考试成绩
- **考试安排** - 获取考试时间安排
- **培养方案** - 查询专业培养计划
- **学业进度** - 查看学业完成情况

### 校园服务
- **校车时刻** - 查询校车运行时间表
- **一卡通消费** - 查询校园卡消费记录

### 应用更新
- **版本检测** - 检查移动端应用最新版本

## 技术架构

- 基于 .NET 9.0 构建
- 使用 Entity Framework Core 进行数据持久化
- 支持 SQLite（开发环境）和 PostgreSQL（生产环境）
- 集成 Redis 缓存提高性能
- 支持 Docker 容器化部署
- 使用 CORS 解决跨域问题

## 环境变量配置

| 变量名 | 说明 | 示例 |
|-------|------|-----|
| SQL | 数据库连接字符串（PostgreSQL） | `Host=localhost;Database=xauat_edu` |
| REDIS | Redis 连接字符串 | `localhost:6379` |

## 部署方式

### Docker 部署（推荐）

```bash
docker build -t xauat-edu-api .
docker run -d -p 8080:8080 xauat-edu-api
```

### 本地运行

```bash
dotnet run
```

## API 接口文档

项目集成了 Scalar API 文档，启动后可通过 `/scalar/v1` 路径访问详细的接口文档。

## 主要依赖

- ASP.NET Core 9.0
- Entity Framework Core
- PostgreSQL / SQLite
- Redis
- HttpClientFactory
- Scalar API 文档工具

## 注意事项

1. 本项目仅用于学习和技术研究目的
2. 使用时需要遵守学校相关规定，不得用于非法用途
3. 开发者不对因使用本项目造成的任何后果负责

## 许可证

本项目采用 MIT 许可证，详情请查看 [LICENSE](LICENSE) 文件。