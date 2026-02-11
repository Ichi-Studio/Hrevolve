# Docker 一键部署

## 前置条件

- 已安装 Docker Desktop（含 Docker Compose v2）

## 启动

1. 复制环境变量文件：

   - Windows PowerShell：
     - `Copy-Item .env.example .env`

2. 修改 `.env` 里的 `JWT_KEY`（至少 32 字符）

3. 启动：

   - `docker compose up -d --build`

## 访问

- Web：`http://localhost:${WEB_PORT}`（默认 `http://localhost:8080`）
- 后端健康检查：`http://localhost:${WEB_PORT}/api/health`
- Ollama：`http://localhost:${OLLAMA_PORT}`（默认 `http://localhost:11434`）

## CI/CD（GitHub Actions）

### 镜像构建与发布

- 工作流：`.github/workflows/docker.yml`
- 默认推送到：`ghcr.io/<owner>/hrevolve-api` 与 `ghcr.io/<owner>/hrevolve-web`

### 自动部署（可选）

设置以下 Secrets 后，push main 会自动 SSH 到服务器执行 `docker compose -f docker-compose.prod.yml pull/up`：

- `DEPLOY_HOST`
- `DEPLOY_USER`
- `DEPLOY_KEY`（私钥）
- `DEPLOY_PATH`（服务器上包含 `docker-compose.prod.yml` 与 `.env` 的目录）

服务器侧 `.env` 需要至少包含：

- `IMAGE_NAMESPACE`（例如 `my-org`，与镜像仓库路径一致）
- `JWT_KEY`（至少 32 字符）

## 常见问题排查

### 1) IMAGE_NAMESPACE 未生效 / 仍提示未设置

- Docker Compose 只会自动读取“当前目录”的 `.env`，请确认你执行命令的目录里同时存在：
  - `docker-compose.prod.yml`
  - `.env`
- 或显式指定：`docker compose --env-file /path/to/.env -f docker-compose.prod.yml pull`

### 2) invalid reference format / 仓库名必须小写

- GHCR 要求镜像路径全小写：`ghcr.io/<namespace>/<image>:<tag>`
- 请确保 `.env` 里的 `IMAGE_NAMESPACE` 全小写（例如 `ichistudio`，不要 `ICHISTUDIO`）

### 3) error from registry: denied（拉取被拒绝）

- 你的 GHCR 包可能是私有的：需要在服务器上先登录 GHCR
  - `docker login ghcr.io -u <github用户名> -p <PAT>`
  - PAT 至少需要 `read:packages`（同组织/私有仓库还要确保账号有仓库读取权限）
- 或在 GitHub Packages 将 `hrevolve-api/hrevolve-web` 设置为 Public（不推荐生产环境）

## 停止与清理

- 停止：`docker compose down`
- 连同数据卷一起清理：`docker compose down -v`
