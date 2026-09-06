# GitHub 自动发布与创意工坊部署

仓库的 `main` 分支和拉取请求只执行构建、XML/翻译校验、打包及负向测试，不会登录 Steam。推送与 `About/About.xml` 中版本完全一致的 `v*` 标签时会创建 GitHub Release；只有仓库变量 `STEAM_PUBLISH_ENABLED` 为 `true` 时，标签流程才会更新现有创意工坊条目 `3671535921`。

构建会匿名下载 Simple Mending 条目 `3657705987` 的 1.6 DLL，并验证固定的 SHA-256。上游 DLL 变化会使构建停止，必须先检查兼容性再更新校验值。

## 首次配置

1. 在 GitHub 仓库 Settings → Environments 创建 `steam-workshop`。
2. 添加环境机密：`STEAM_USERNAME`、`STEAM_PASSWORD`、`STEAM_REFRESH_TOKEN`、`STEAM_CONFIG_VDF_BASE64`。
3. `STEAM_REFRESH_TOKEN` 用本仓库构建出的 `WorkshopOwnerCheck` 在本机交互生成；令牌文件必须位于仓库之外，用完即删除。不要把令牌、SteamCMD 配置、网页响应或登录输出提交到仓库或粘贴到日志。
4. `STEAM_CONFIG_VDF_BASE64` 是完成 Steam Guard 登录后的 SteamCMD `config/config.vdf` 的 Base64 内容。
5. 保持仓库变量 `STEAM_PUBLISH_ENABLED=false`，手动运行 Build and release，勾选 `verify_steam`。只读验证成功后再改为 `true`。

建议给 `steam-workshop` 环境配置审批保护。普通分支推送永远不会上传；实际发布前先更新版本和两份 `Docs/releases/<version>.*.md`，合并到 `main` 后再创建同版本标签。
