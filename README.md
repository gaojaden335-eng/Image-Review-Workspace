# Image Review Workspace

一个用于图像导入、筛选、复核与结果管理的完整工作台。项目同时提供 Web 管理界面、Windows 桌面筛选工具和独立后端 API，适合在本地或受控服务器环境中部署。

## 项目能做什么

典型工作流如下：

1. 管理员创建项目并导入图片。
2. 用户通过 Web 端或桌面端领取和筛选图片。
3. 筛选结果统一保存到数据库中，便于复核、统计和导出。
4. 管理员在 Web 端管理用户、任务队列与项目进度。

系统支持多项目、批量上传、任务分配、角色权限、筛选结果导出，以及桌面端的快捷键和图片预加载。

## 应用组成

| 目录 | 用途 | 主要技术 |
| --- | --- | --- |
| `Backend/` | REST API、认证授权、数据与文件管理 | .NET 8、Entity Framework Core、MySQL |
| `frontend/` | 项目、用户、队列和图片的 Web 管理界面 | Vue 3、TypeScript、Vite、Pinia |
| `DesktopClient/` | 面向连续筛选任务的 Windows 客户端 | .NET 8、WinForms |
| `Backend.Tests/` | 后端策略和关键行为的自动化测试 | xUnit |

## 当前版本的安全设计

- 仓库不包含数据库密码、JWT 密钥或预设账号。
- Web 登录令牌保存在 `HttpOnly` Cookie 中，前端脚本无法直接读取。
- 图片文件通过受保护的 API 返回，而不是作为公开静态目录暴露。
- 上传会检查扩展名、文件签名、单文件大小、批次大小和文件数量。
- 登录和注册接口启用了基于 IP 的请求频率限制。
- 数据库迁移失败时服务会停止启动，避免在未知结构上继续运行。

## 本地运行

### 环境要求

- .NET 8 SDK
- Node.js 20.19+ 或 22.12+
- MySQL 8.0+
- Windows（仅桌面客户端需要）

### 1. 配置后端

先在当前 PowerShell 会话中设置本地环境变量。以下内容全部是占位符，请替换后使用，不要把真实值提交到 Git：

```powershell
$env:ConnectionStrings__DefaultConnection = "<your-mysql-connection-string>"
$env:JwtSettings__SecretKey = "<at-least-32-random-characters>"
$env:DefaultAdmin__Username = "<optional-initial-admin>"
$env:DefaultAdmin__Password = "<optional-strong-password>"
```

`DefaultAdmin` 两项是可选的；不设置时，应用不会创建初始管理员。数据库迁移会在后端启动时自动执行。

启动 API：

```powershell
dotnet run --project Backend/Backend.csproj
```

开发环境默认地址为 `http://localhost:5097`。

### 2. 启动 Web 界面

另开一个终端并运行：

```powershell
cd frontend
npm ci
npm run dev
```

### 3. 启动桌面客户端

在 Windows 上运行：

```powershell
dotnet run --project DesktopClient/DesktopClient.csproj
```

首次登录前，请在客户端中确认后端 API 地址正确。

## 账号与权限

系统使用三个角色：

- `Admin`：管理项目、用户、任务和全部图片。
- `User`：执行已授权的筛选与复核任务。
- `Guest`：新注册账号的默认状态，需要管理员审核后才能参与任务。

生产环境应使用 HTTPS，并通过安全的服务器配置或密钥管理服务注入连接字符串和 JWT 密钥。

## 验证项目

运行后端测试：

```powershell
dotnet test Backend/Backend.sln
```

验证前端生产构建：

```powershell
cd frontend
npm ci
npm run build
```

GitHub Actions 会在推送到 `main` 或创建拉取请求时执行这两项检查。

## 数据与隐私

- 上传文件默认保存在 `Backend/uploads/`，该目录不会提交到仓库。
- 请勿提交 `.env`、真实连接字符串、访问令牌、账号密码或业务图片。
- 发布日志和问题报告前，请检查其中是否包含本地路径、用户信息或服务地址。
- 如需公开演示，建议使用专门生成的示例图片和独立测试数据库。

## 更多文档

- [后端部署说明](Backend/DEPLOY.md)
- [桌面客户端说明](DesktopClient/README.md)
- [前端部署说明](frontend/DEPLOY.md)

## 默认上传限制

| 限制项 | 默认值 |
| --- | ---: |
| 单个文件 | 100 MB |
| 单次请求总量 | 512 MB |
| 单次文件数量 | 500 |

这些限制可以在后端配置中按部署环境调整。调整服务器限制时，也应同步检查反向代理和数据库的相关配置。
