# 部署指南

## 版本信息

| 版本 | 日期 | 更新内容 | 作者 |
|------|------|----------|------|
| 1.0 | 2024-12-02 | 初始创建 | 开发者团队 |

## 文档概述

本文档提供了西安建筑科技大学教务系统API（XAUAT EduApi）的详细部署指南，包括开发环境搭建、测试环境部署、生产环境配置及自动化部署流程。本文档旨在帮助运维人员和开发人员快速部署和配置系统，确保系统的稳定运行。

## 目录

- [1. 环境准备](#1-环境准备)
  - [1.1 硬件要求](#11-硬件要求)
  - [1.2 软件要求](#12-软件要求)
  - [1.3 系统依赖](#13-系统依赖)
- [2. 开发环境搭建](#2-开发环境搭建)
  - [2.1 安装.NET SDK](#21-安装net-sdk)
  - [2.2 克隆项目代码](#22-克隆项目代码)
  - [2.3 配置开发环境变量](#23-配置开发环境变量)
  - [2.4 初始化数据库](#24-初始化数据库)
  - [2.5 启动开发服务器](#25-启动开发服务器)
- [3. 测试环境部署](#3-测试环境部署)
  - [3.1 使用Docker Compose部署](#31-使用docker-compose部署)
  - [3.2 手动部署](#32-手动部署)
  - [3.3 测试环境配置](#33-测试环境配置)
- [4. 生产环境配置](#4-生产环境配置)
  - [4.1 服务器配置](#41-服务器配置)
  - [4.2 数据库配置](#42-数据库配置)
  - [4.3 缓存配置](#43-缓存配置)
  - [4.4 环境变量配置](#44-环境变量配置)
  - [4.5 负载均衡配置](#45-负载均衡配置)
  - [4.6 监控与日志配置](#46-监控与日志配置)
- [5. 自动化部署流程](#5-自动化部署流程)
  - [5.1 CI/CD配置](#51-cicd配置)
  - [5.2 自动化构建流程](#52-自动化构建流程)
  - [5.3 自动化部署流程](#53-自动化部署流程)
  - [5.4 回滚机制](#54-回滚机制)
- [6. 环境变量说明](#6-环境变量说明)
  - [6.1 基础环境变量](#61-基础环境变量)
  - [6.2 数据库环境变量](#62-数据库环境变量)
  - [6.3 缓存环境变量](#63-缓存环境变量)
  - [6.4 服务注册环境变量](#64-服务注册环境变量)
  - [6.5 第三方服务环境变量](#65-第三方服务环境变量)
- [7. 第三方服务集成指南](#7-第三方服务集成指南)
  - [7.1 Redis集成](#71-redis集成)
  - [7.2 Prometheus集成](#72-prometheus集成)
  - [7.3 日志系统集成](#73-日志系统集成)
- [8. 常见问题与解决方案](#8-常见问题与解决方案)

## 1. 环境准备

### 1.1 硬件要求

| 环境 | CPU | 内存 | 磁盘 | 网络 |
|------|-----|------|------|------|
| 开发环境 | 2核 | 4GB | 50GB | 100Mbps |
| 测试环境 | 4核 | 8GB | 100GB | 1Gbps |
| 生产环境 | 8核 | 16GB+ | 200GB+ | 1Gbps+ |

### 1.2 软件要求

| 环境 | .NET SDK | 数据库 | 缓存 | 容器化 | CI/CD |
|------|----------|--------|------|--------|-------|
| 开发环境 | 10.0+ | SQLite | 可选Redis | 可选Docker | 可选GitHub Actions |
| 测试环境 | 10.0+ | PostgreSQL | Redis | Docker | GitHub Actions |
| 生产环境 | 10.0+ | PostgreSQL | Redis | Docker/K8s | GitHub Actions |

### 1.3 系统依赖

- **开发工具**：Visual Studio 2022/2023、Rider或VS Code
- **版本控制**：Git
- **容器化**：Docker、Docker Compose
- **CI/CD**：GitHub Actions、GitLab CI或Jenkins
- **监控工具**：Prometheus、Grafana
- **日志工具**：ELK Stack（Elasticsearch + Logstash + Kibana）

## 2. 开发环境搭建

### 2.1 安装.NET SDK

1. **Windows**：
   - 访问 [.NET 下载页面](https://dotnet.microsoft.com/zh-cn/download)
   - 下载并安装 .NET SDK 10.0
   - 验证安装：打开命令提示符，运行 `dotnet --version`

2. **macOS**：
   - 使用 Homebrew 安装：`brew install dotnet-sdk@10`
   - 或访问 [.NET 下载页面](https://dotnet.microsoft.com/zh-cn/download) 下载安装包
   - 验证安装：打开终端，运行 `dotnet --version`

3. **Linux**：
   - 使用包管理器安装，以 Ubuntu 为例：
     ```bash
     wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
     sudo dpkg -i packages-microsoft-prod.deb
     rm packages-microsoft-prod.deb
     sudo apt-get update
     sudo apt-get install -y dotnet-sdk-10.0
     ```
   - 验证安装：运行 `dotnet --version`

### 2.2 克隆项目代码

1. 克隆项目代码：
   ```bash
   git clone https://github.com/your-repo/XAUAT.EduAp.git
   cd XAUAT.EduAp
   ```

2. 切换到开发分支：
   ```bash
   git checkout develop
   ```

### 2.3 配置开发环境变量

1. 复制 `appsettings.Development.json` 文件：
   ```bash
   cp XAUAT.EduApi/appsettings.Development.json XAUAT.EduApi/appsettings.Development.local.json
   ```

2. 编辑 `appsettings.Development.local.json` 文件，配置开发环境变量：
   ```json
   {
     "Service": {
       "Name": "XAUAT.EduApi",
       "InstanceId": "dev-instance-1",
       "Host": "localhost",
       "Port": 8080,
       "IsHttps": false
     },
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     }
   }
   ```

### 2.4 初始化数据库

开发环境使用 SQLite 数据库，无需手动创建数据库，系统会自动创建。

### 2.5 启动开发服务器

1. 进入项目目录：
   ```bash
   cd XAUAT.EduApi
   ```

2. 启动开发服务器：
   ```bash
   dotnet run
   ```

3. 验证服务是否启动成功：
   - 访问 `http://localhost:8080/health`，返回健康检查结果
   - 访问 `http://localhost:8080/scalar/v1`，查看 API 文档

## 3. 测试环境部署

### 3.1 使用Docker Compose部署

1. 编写 `docker-compose.yml` 文件：
   ```yaml
   version: '3.8'
   
   services:
     app:
       build:
         context: .
         dockerfile: Dockerfile
       ports:
         - "8080:8080"
       environment:
         - ASPNETCORE_ENVIRONMENT=Test
         - SQL=Host=db;Port=5432;Database=xauat_edu_test;Username=postgres;Password=your_password
         - REDIS=redis:6379
       depends_on:
         - db
         - redis
     
     db:
       image: postgres:16
       environment:
         - POSTGRES_DB=xauat_edu_test
         - POSTGRES_USER=postgres
         - POSTGRES_PASSWORD=your_password
       volumes:
         - postgres_data:/var/lib/postgresql/data
     
     redis:
       image: redis:7.2
       volumes:
         - redis_data:/data
   
   volumes:
     postgres_data:
     redis_data:
   ```

2. 启动服务：
   ```bash
   docker-compose up -d
   ```

3. 验证服务是否启动成功：
   - 访问 `http://localhost:8080/health`
   - 访问 `http://localhost:8080/scalar/v1`

### 3.2 手动部署

1. 构建项目：
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. 复制发布文件到服务器：
   ```bash
   scp -r ./publish user@server_ip:/opt/xauat-eduapi
   ```

3. 配置环境变量：
   ```bash
   export ASPNETCORE_ENVIRONMENT=Test
   export SQL=Host=localhost;Port=5432;Database=xauat_edu_test;Username=postgres;Password=your_password
   export REDIS=localhost:6379
   ```

4. 启动服务：
   ```bash
   cd /opt/xauat-eduapi
   dotnet XAUAT.EduApi.dll
   ```

### 3.3 测试环境配置

- **数据库**：PostgreSQL 16
- **缓存**：Redis 7.2
- **日志级别**：Information
- **API文档**：启用
- **监控**：启用 Prometheus 监控

## 4. 生产环境配置

### 4.1 服务器配置

- **操作系统**：Ubuntu 22.04 LTS 或 CentOS 8
- **Web服务器**：使用 Nginx 作为反向代理
- **进程管理**：使用 Systemd 管理服务进程
- **安全配置**：
  - 启用防火墙，开放必要端口
  - 配置 SSL/TLS 证书
  - 禁用 root 登录
  - 配置 SSH 密钥认证

### 4.2 数据库配置

- **数据库类型**：PostgreSQL 16
- **部署方式**：主从复制集群
- **配置建议**：
  - 设置合理的连接池大小
  - 配置定期备份策略
  - 启用慢查询日志
  - 优化数据库性能参数

### 4.3 缓存配置

- **缓存类型**：Redis 7.2
- **部署方式**：Redis 集群
- **配置建议**：
  - 设置合理的内存上限
  - 配置持久化策略
  - 启用 Redis 监控
  - 配置密码认证

### 4.4 环境变量配置

生产环境使用环境变量配置系统，主要环境变量如下：

```bash
# 基础配置
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS=http://*:8080
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# 服务配置
export Service__Name=XAUAT.EduApi
export Service__InstanceId=prod-instance-1
export Service__Host=0.0.0.0
export Service__Port=8080
export Service__IsHttps=false

# 数据库配置
export SQL=Host=db;Port=5432;Database=xauat_edu_prod;Username=postgres;Password=your_secure_password

# 缓存配置
export REDIS=redis-cluster:6379,password=your_redis_password

# 日志配置
export Serilog__MinimumLevel__Default=Warning
export Serilog__WriteTo__0__Args__path=/var/log/xauat-eduapi/log-.txt

# 第三方服务配置
export GITEE_ACCESS_TOKEN=your_gitee_token
```

### 4.5 负载均衡配置

使用 Nginx 作为负载均衡器，配置示例：

```nginx
upstream xauat_eduapi {
    server 10.0.0.1:8080;
    server 10.0.0.2:8080;
    server 10.0.0.3:8080;
}

server {
    listen 80;
    server_name api.xauat.edu.cn;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl;
    server_name api.xauat.edu.cn;
    
    ssl_certificate /etc/nginx/ssl/api.xauat.edu.cn.crt;
    ssl_certificate_key /etc/nginx/ssl/api.xauat.edu.cn.key;
    
    location / {
        proxy_pass http://xauat_eduapi;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
    
    location /metrics {
        proxy_pass http://xauat_eduapi/metrics;
    }
    
    location /health {
        proxy_pass http://xauat_eduapi/health;
    }
    
    location /scalar {
        proxy_pass http://xauat_eduapi/scalar;
    }
}
```

### 4.6 监控与日志配置

- **监控系统**：
  - 使用 Prometheus 收集监控指标
  - 使用 Grafana 可视化监控数据
  - 配置告警规则，监控系统运行状态

- **日志系统**：
  - 使用 Serilog 记录结构化日志
  - 日志输出到文件和 Elasticsearch
  - 使用 Kibana 分析和查询日志
  - 配置日志轮转和清理策略

## 5. 自动化部署流程

### 5.1 CI/CD配置

使用 GitHub Actions 配置 CI/CD 流程，示例 `.github/workflows/ci-cd.yml` 文件：

```yaml
name: CI/CD

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 10.0.x
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore --configuration Release
    
    - name: Test
      run: dotnet test --no-build --configuration Release --verbosity normal
    
    - name: Publish
      run: dotnet publish --no-build --configuration Release --output ./publish
    
    - name: Upload artifact
      uses: actions/upload-artifact@v4
      with:
        name: xauat-eduapi
        path: ./publish

  deploy-dev:
    needs: build
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/develop'
    
    steps:
    - name: Download artifact
      uses: actions/download-artifact@v4
      with:
        name: xauat-eduapi
        path: ./publish
    
    # 部署到开发环境的步骤
    # ...

  deploy-prod:
    needs: build
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
    - name: Download artifact
      uses: actions/download-artifact@v4
      with:
        name: xauat-eduapi
        path: ./publish
    
    # 部署到生产环境的步骤
    # ...
```

### 5.2 自动化构建流程

1. **代码提交**：开发者提交代码到 GitHub 仓库
2. **触发 CI**：GitHub Actions 自动触发 CI 流程
3. **依赖还原**：还原项目依赖
4. **构建项目**：构建 Release 版本
5. **运行测试**：运行单元测试和集成测试
6. **发布构建**：生成发布包
7. **上传工件**：将发布包上传到 GitHub Actions 工件存储

### 5.3 自动化部署流程

1. **下载工件**：从 GitHub Actions 工件存储下载发布包
2. **部署到服务器**：使用 SSH 或其他方式将发布包部署到服务器
3. **配置环境变量**：设置生产环境变量
4. **启动服务**：使用 Systemd 启动服务
5. **验证服务**：检查服务是否正常运行
6. **发送通知**：发送部署成功或失败的通知

### 5.4 回滚机制

1. **备份当前版本**：在部署新版本前，备份当前版本的发布包和配置文件
2. **记录部署历史**：记录每次部署的版本信息和时间
3. **快速回滚**：如果新版本出现问题，使用备份的旧版本快速回滚
4. **验证回滚**：验证回滚后的服务是否正常运行

## 6. 环境变量说明

### 6.1 基础环境变量

| 变量名 | 类型 | 说明 | 示例值 |
|--------|------|------|--------|
| ASPNETCORE_ENVIRONMENT | string | 环境名称 | Development/Test/Production |
| ASPNETCORE_URLS | string | 服务监听地址 | http://*:8080 |
| DOTNET_SYSTEM_GLOBALIZATION_INVARIANT | bool | 是否启用全球化不变模式 | false |

### 6.2 数据库环境变量

| 变量名 | 类型 | 说明 | 示例值 |
|--------|------|------|--------|
| SQL | string | 数据库连接字符串 | Host=db;Port=5432;Database=xauat_edu;Username=postgres;Password=your_password |

### 6.3 缓存环境变量

| 变量名 | 类型 | 说明 | 示例值 |
|--------|------|------|--------|
| REDIS | string | Redis连接字符串 | localhost:6379,password=your_redis_password |

### 6.4 服务注册环境变量

| 变量名 | 类型 | 说明 | 示例值 |
|--------|------|------|--------|
| Service__Name | string | 服务名称 | XAUAT.EduApi |
| Service__InstanceId | string | 服务实例ID | prod-instance-1 |
| Service__Host | string | 服务主机地址 | 0.0.0.0 |
| Service__Port | int | 服务端口 | 8080 |
| Service__IsHttps | bool | 是否使用HTTPS | false |

### 6.5 第三方服务环境变量

| 变量名 | 类型 | 说明 | 示例值 |
|--------|------|------|--------|
| GITEE_ACCESS_TOKEN | string | Gitee API访问令牌 | your_gitee_token |

## 7. 第三方服务集成指南

### 7.1 Redis集成

1. **安装Redis**：
   - 参考 [Redis 官方文档](https://redis.io/docs/getting-started/installation/) 安装Redis
   - 配置Redis密码和监听地址

2. **配置Redis连接字符串**：
   ```bash
   export REDIS=redis-host:6379,password=your_redis_password
   ```

3. **验证Redis连接**：
   - 启动服务后，查看日志是否有Redis连接成功的记录
   - 使用Redis客户端工具连接Redis，检查是否有缓存数据

### 7.2 Prometheus集成

1. **安装Prometheus**：
   - 参考 [Prometheus 官方文档](https://prometheus.io/docs/prometheus/latest/installation/) 安装Prometheus

2. **配置Prometheus**：
   ```yaml
   scrape_configs:
     - job_name: 'xauat-eduapi'
       static_configs:
         - targets: ['api-server:8080']
       metrics_path: '/metrics'
   ```

3. **启动Prometheus**：
   ```bash
   prometheus --config.file=prometheus.yml
   ```

4. **访问Prometheus**：
   - 访问 `http://prometheus-server:9090`，查看监控数据

### 7.3 日志系统集成

1. **安装ELK Stack**：
   - 参考 [ELK Stack 官方文档](https://www.elastic.co/guide/en/elk-stack/current/index.html) 安装ELK Stack

2. **配置Serilog**：
   ```json
   {
     "Serilog": {
       "MinimumLevel": {
         "Default": "Information",
         "Override": {
           "Microsoft": "Warning",
           "System": "Warning"
         }
       },
       "WriteTo": [
         {
           "Name": "File",
           "Args": {
             "path": "/var/log/xauat-eduapi/log-.txt",
             "rollingInterval": "Day",
             "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Exception}{NewLine}"
           }
         },
         {
           "Name": "Elasticsearch",
           "Args": {
             "nodeUris": "http://elasticsearch:9200",
             "indexFormat": "xauat-eduapi-{0:yyyy.MM.dd}",
             "autoRegisterTemplate": true
           }
         }
       ],
       "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
     }
   }
   ```

3. **访问Kibana**：
   - 访问 `http://kibana-server:5601`，查看和分析日志

## 8. 常见问题与解决方案

| 问题 | 解决方案 |
|------|----------|
| 服务启动失败，提示端口被占用 | 检查端口是否被其他进程占用，使用 `netstat -tuln | grep 8080` 查看，或修改服务端口 |
| 数据库连接失败 | 检查数据库连接字符串是否正确，数据库服务是否正常运行，防火墙是否开放端口 |
| Redis连接失败 | 检查Redis连接字符串是否正确，Redis服务是否正常运行，防火墙是否开放端口 |
| API文档无法访问 | 检查Scalar API文档是否启用，服务是否正常运行，访问地址是否正确 |
| 监控指标无法收集 | 检查Prometheus配置是否正确，服务是否正常运行，监控端点是否可访问 |
| 日志无法写入 | 检查日志目录是否存在，服务是否有写入权限，日志配置是否正确 |

## 更新日志

| 日期 | 版本 | 更新内容 | 作者 |
|------|------|----------|------|
| 2024-12-02 | 1.0 | 初始创建 | 开发者团队 |

## 相关资源

- [项目GitHub仓库](https://github.com/your-repo/XAUAT.EduAp)
- [API文档](/scalar/v1)（部署后可访问）
- [架构设计文档](../architecture/architecture.md)
- [开发指南](../development/development.md)
