# KEDA Processing Center V2

## 项目简介
本项目为 KEDA 设备数据处理中心，基于 .NET 8，支持 MQTT 通信、设备数据采集与处理。

## 环境要求
- .NET 8 SDK
- MySQL 数据库
- MQTT Broker

## 配置说明

### 数据库配置
数据库连接字符串请在 `appsettings.json` 的 `ConnectionStrings:WorkstationDb` 配置。

**示例：**
```json
"ConnectionStrings": {
  "WorkstationDb": "Server=127.0.0.1;Port=3306;Database=workstation;User=root;Password=keda;SslMode=none;"
}
```

### MQTT 连接配置
MQTT 相关参数请在 `appsettings.json` 的 `Mqtt` 节点配置。

**示例：**
```json
"Mqtt": {
  "Server": "localhost",
  "Port": 2030,
  "Username":  "USER001",
  "Password": "USER001"
}
```

**参数说明：**
- `Server`: MQTT Broker 服务器地址
- `Port`: MQTT Broker 端口号
- `Username`: MQTT 连接用户名
- `Password`: MQTT 连接密码

### MQTT 主题配置
MQTT 主题前缀请在 `appsettings.json` 的 `MqttTopics` 节点配置。

**示例：**
```json
"MqttTopics": {
  "EdgePrefix": "edge/",
  "ControlPrefix":  "control/",
  "WorkstationStatusPrefix": "workstation/status/",
  "WorkstationDataPrefix": "workstation/data/",
  "WorkstationConfigPrefix": "workstation/config/"
}
```



**参数说明：**

| 参数 | 说明 | 示例 |
|------|------|------|
| `EdgePrefix` | 边缘协议原始数据主题前缀 | `edge/{protocolId}` |
| `ControlPrefix` | 设备控制/清洗后数据主题前缀 | `control/{deviceId}` |
| `WorkstationStatusPrefix` | 工作站状态主题前缀 | `workstation/status/{edgeId}` |
| `WorkstationDataPrefix` | 工作站数据主题前缀 | `workstation/data/{deviceId}` |
| `WorkstationConfigRequestPrefix` | 工作站配置下发请求主题前缀 | `workstation/config/send/` |
| `WorkstationConfigResponsePrefix` | 工作站配置下发响应主题前缀 | `workstation/config/response/{edgeId}` |

**主题使用场景举例：**

- **EdgePrefix**  
  - 订阅/发布：`edge/{protocolId}`  
  - 用于协议原始数据采集

- **ControlPrefix**  
  - 订阅/发布：`control/{deviceId}`  
  - 用于设备级别的数据清洗、转换

- **WorkstationStatusPrefix**  
  - 订阅/发布：`workstation/status/{edgeId}`  
  - 用于工作站状态（如在线、健康等）

- **WorkstationDataPrefix**  
  - 订阅/发布：`workstation/data/{deviceId}`  
  - 用于设备采集数据

- **WorkstationConfigRequestPrefix**  
  - 订阅/发布：`workstation/config/send/`  
  - 用于下发工作站配置请求

- **WorkstationConfigResponsePrefix**  
  - 订阅/发布：`workstation/config/response/{edgeId}`  
  - 用于下发工作站配置响应

**注意事项：**
- 所有前缀建议以 `/` 结尾，便于主题拼接。
- 推荐使用 `{edgeId}`、`{deviceId}`、`{protocolId}` 等变量占位符。
- 响应主题建议带上 EdgeID，便于设备端精准订阅。



### 日志配置
日志配置请在 `appsettings.json` 的 `Serilog` 节点配置，支持控制台和 Seq 输出。

**示例：**
```json
"Serilog": {
  "MinimumLevel": {
    "Default": "Error",
    "Override": {
      "Microsoft. Hosting.Lifetime": "Information"
    }
  },
  "WriteTo": [
    {
      "Name": "Console",
      "Args": {
        "outputTemplate": "{Timestamp: yyyy-MM-dd HH: mm:ss.fff} [{Level:u3}] [{MachineName}] [{EnvironmentUserName}] [{LocalIp}] {Message:lj}{NewLine}{Exception}"
      }
    },
    {
      "Name": "Seq",
      "Args": {
        "serverUrl": "http://localhost:5341",
        "apiKey":  "$A40C86UyXAJCv4IslOC9"
      }
    }
  ],
  "Enrich": [ "FromLogContext", "WithMachineName", "WithEnvironmentUserName" ]
}
```

## 运行方法
1. 安装 .NET 8 SDK
2. 配置数据库和 MQTT
3. 配置 `appsettings.json` 文件
4. 使用 Visual Studio 2022 打开解决方案
5. 运行项目

## 常见问题

### 启动失败
- 检查数据库连接字符串是否正确
- 检查 MQTT Broker 服务是否正常运行
- 检查防火墙是否开放相应端口

### MQTT 连接失败
- 确认 MQTT Broker 地址和端口配置正确
- 确认用户名密码正确
- 检查网络连接是否正常

### 消息收发异常
- 检查 MQTT 主题前缀配置是否正确
- 确认设备 ID 或工作站 ID 是否存在
- 查看日志输出排查具体错误

### 日志查看
- 控制台：直接在运行窗口查看
- Seq：访问 http://localhost:5341 查看结构化日志

## 联系方式
如有问题请联系 xxx@yourdomain.com