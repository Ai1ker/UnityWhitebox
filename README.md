# VECTOR — 方向试验场

Unity 2D 解谜玩法白盒。可改写物体速度方向与四向重力，包含平台跳跃、压力板、感应门、炮塔、检查点和关卡传送。

## 打开与运行

1. 在 Unity Hub 中安装 **Unity 6000.6.1f1**。
2. 克隆仓库，通过 Unity Hub 的“添加项目”选择仓库根目录。
3. 首次打开时等待 Unity 导入资源和解析 Packages。
4. 打开 `Assets/Whitebox/Scenes/MainMenu.unity`，点击 Play。

也可直接打开某个关卡测试。第一关提供教学，其余关卡用于自行设计；提交前请在 Unity 中保存场景及预制体修改。继续游戏的个人存档和按键设置保存在本机，不随仓库同步。

## 修改项目

- `Assets/Whitebox/Scenes`：主菜单、六个关卡和结束场景。
- `Assets/Whitebox/Prefabs`：玩家、环境、机关及 UI 预制体。
- `Assets/Whitebox/Scripts`：运行时玩法代码。
- `Assets/Whitebox/ArtAssets`：预留美术素材目录。
- [玩法、Inspector 参数和机关摆放说明](Assets/Whitebox/README.md)。

提交 `Assets`、`Packages`、`ProjectSettings` 及资源对应的 `.meta` 文件。不要手动删除 `.meta`，资源之间的引用依赖其中的 GUID。缓存、IDE 文件及个人设置已通过 `.gitignore` 排除。

MCP for Unity 工具源码已嵌入 `Packages/com.coplaydev.unity-mcp`，不依赖原作者电脑上的绝对路径。无需启动 MCP 服务也可打开和运行游戏；要用 AI 控制 Unity 时，再为自己的客户端配置 MCP 服务。该第三方包的许可证保留在其目录中。
