# Contributing（贡献指南）

感谢你对 Hrevolve 的兴趣。为了让协作更高效，请在提交 Issue / PR 前阅读本文档。

## 行为准则

参与本仓库即表示你同意遵守 [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md)。

## 如何开始

### 1) 先搜再提

- 先搜索已有 Issue / PR，避免重复
- 若是疑问类问题，优先使用 Discussions（如已开启），或在 Issue 中尽量提供上下文

### 2) 选择合适的 Issue 模板

- Bug：请提供复现步骤、期望与实际结果、日志/截图、环境信息
- Feature：请说明业务背景、目标、替代方案与范围
- Docs：请指出文档位置与期望修改内容

## 开发环境（建议）

### 后端（Backend）

- .NET SDK：以仓库 README 与项目文件为准（当前为 .NET 10）
- 数据库/缓存：PostgreSQL、Redis（本地可用 docker-compose）
- 运行方式：以 [Backend/README.md](../Backend/README.md) 与 [Backend/Hrevolve.Web/Properties/launchSettings.json](../Backend/Hrevolve.Web/Properties/launchSettings.json) 为准

常见操作：

- 还原依赖：`dotnet restore`
- 启动 API：`dotnet run --project Backend/Hrevolve.Web`
- 运行测试：`dotnet test Backend`

### 前端（Frontend）

- Node.js：以仓库 README 为准（建议 18+）
- 包管理：npm（仓库已包含 package-lock.json）
- 运行方式：以 [Frontend/README.md](../Frontend/README.md) 与 [Frontend/hrevolve-web/vite.config.ts](../Frontend/hrevolve-web/vite.config.ts) 为准

常见操作：

- 安装依赖：`npm install`（在 `Frontend/hrevolve-web` 下）
- 启动开发：`npm run dev`
- 生产构建：`npm run build`

## 分支与提交规范

- 从 `main`（或默认分支）切出 feature/fix 分支：`feature/<topic>`、`fix/<topic>`、`docs/<topic>`
- 提交信息建议使用清晰的动词开头（例如：`fix: ...`、`feat: ...`、`docs: ...`）
- 尽量保持 PR 小而聚焦：一次 PR 解决一个问题

## 代码质量与风格

- 遵循现有代码结构与命名约定（Clean Architecture / DDD 分层）
- 不要在 PR 中混入无关的格式化改动
- 新增/修改功能需包含对应测试或可复现验证说明
- 涉及 API 变更：同步更新 OpenAPI/前端调用与相关文档

## PR 提交流程

1. 确保本地测试通过（后端 `dotnet test`；前端 `npm run build` 或相关 lint/测试）
2. 更新文档（如有用户可见变更）
3. 提交 PR：
   - 清晰描述改动动机与方案
   - 关联 Issue（例如：`Closes #123`）
   - 提供截图/录屏（如涉及 UI）
4. 维护者 Review 后可能要求：
   - 调整代码风格/结构
   - 补充测试/边界情况
   - 更新文档或迁移说明

## 安全相关

如果你发现潜在安全漏洞，请不要公开发 Issue。请按 [SECURITY.md](SECURITY.md) 的流程进行私密披露。

