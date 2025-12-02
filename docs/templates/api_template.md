# API端点文档模板

## 版本信息

| 版本 | 日期 | 更新内容 | 作者 |
|------|------|----------|------|
| 1.0 | YYYY-MM-DD | 初始创建 | 作者名 |

## 接口概述

**接口名称**：[接口名称]

**接口URL**：`[完整URL路径]`

**请求方法**：[GET/POST/PUT/DELETE]

**功能描述**：[简要描述接口功能]

**使用场景**：[说明接口的典型使用场景]

## 请求参数

### 查询参数（GET请求）

| 参数名 | 类型 | 是否必填 | 说明 | 示例值 |
|--------|------|----------|------|--------|
| param1 | string | 是 | 参数1说明 | example |
| param2 | int | 否 | 参数2说明 | 123 |

### 请求头

| 头字段名 | 类型 | 是否必填 | 说明 | 示例值 |
|----------|------|----------|------|--------|
| Authorization | string | 是 | Bearer Token | Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9... |
| Content-Type | string | 是 | 请求体类型 | application/json |

### 请求体（POST/PUT请求）

**请求体格式**：JSON

```json
{
  "field1": "value1",
  "field2": 123,
  "field3": true
}
```

**字段说明**：

| 字段名 | 类型 | 是否必填 | 说明 | 示例值 |
|--------|------|----------|------|--------|
| field1 | string | 是 | 字段1说明 | value1 |
| field2 | int | 否 | 字段2说明 | 123 |
| field3 | boolean | 是 | 字段3说明 | true |

## 响应说明

### 成功响应

**状态码**：200 OK

**响应体格式**：JSON

```json
{
  "code": 0,
  "message": "success",
  "data": {
    "id": 1,
    "name": "示例数据"
  },
  "timestamp": "2024-01-01T00:00:00Z"
}
```

**字段说明**：

| 字段名 | 类型 | 说明 |
|--------|------|------|
| code | int | 响应码（0表示成功，非0表示错误） |
| message | string | 响应消息 |
| data | object/array | 响应数据 |
| timestamp | string | 响应时间 |

### 错误响应

#### 400 Bad Request

**状态码**：400 Bad Request

**响应体格式**：JSON

```json
{
  "code": 400,
  "message": "请求参数错误",
  "errors": {
    "field1": "字段1不能为空"
  },
  "timestamp": "2024-01-01T00:00:00Z"
}
```

#### 401 Unauthorized

**状态码**：401 Unauthorized

**响应体格式**：JSON

```json
{
  "code": 401,
  "message": "未授权访问",
  "timestamp": "2024-01-01T00:00:00Z"
}
```

#### 403 Forbidden

**状态码**：403 Forbidden

**响应体格式**：JSON

```json
{
  "code": 403,
  "message": "禁止访问该资源",
  "timestamp": "2024-01-01T00:00:00Z"
}
```

#### 500 Internal Server Error

**状态码**：500 Internal Server Error

**响应体格式**：JSON

```json
{
  "code": 500,
  "message": "服务器内部错误",
  "timestamp": "2024-01-01T00:00:00Z"
}
```

## 使用示例

### cURL示例

```bash
curl -X [METHOD] \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"field1":"value1"}' \
  [完整API URL]
```

### Python示例

```python
import requests

url = "[完整API URL]"
headers = {
    "Authorization": "Bearer YOUR_TOKEN",
    "Content-Type": "application/json"
}
data = {
    "field1": "value1"
}

response = requests.[method](url, headers=headers, json=data)
print(response.json())
```

### JavaScript示例

```javascript
const url = "[完整API URL]";
const headers = {
    "Authorization": "Bearer YOUR_TOKEN",
    "Content-Type": "application/json"
};
const data = {
    "field1": "value1"
};

fetch(url, {
    method: "[METHOD]",
    headers: headers,
    body: JSON.stringify(data)
})
.then(response => response.json())
.then(data => console.log(data))
.catch(error => console.error('Error:', error));
```

## 分步使用指南

1. **步骤1**：[详细说明第一步操作]
2. **步骤2**：[详细说明第二步操作]
3. **步骤3**：[详细说明第三步操作]
4. **步骤4**：[详细说明第四步操作]

## 常见问题与解决方案

| 问题 | 解决方案 |
|------|----------|
| 问题1 | 解决方案1 |
| 问题2 | 解决方案2 |

## 相关接口

- [相关接口1名称](相关接口1文档链接)
- [相关接口2名称](相关接口2文档链接)
