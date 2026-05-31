# 苏丹的游戏 修改器

苏丹的游戏 MelonLoader Mod — 游戏内修改面板，按 F1 打开。

## 安装方法

### 1. 安装 MelonLoader

下载 MelonLoader.Installer：

[MelonLoader.Installer.exe 直接下载](https://github.com/LavaGang/MelonLoader.Installer/releases/download/4.3.0/MelonLoader.Installer.exe)

> 或用浏览器打开 [Release 页面](https://github.com/LavaGang/MelonLoader.Installer/releases/tag/4.3.0) 自行选择版本。

打开 MelonLoader.Installer，按以下步骤操作：

1. 在列表中找到 **Sultan's Game**
2. **Install v0.7.2**（越新越好，但看体质；开发环境为 v0.7.2）
3. 安装完成后，**运行一次游戏**，务必进入游戏主界面再退出（首次运行会生成必要文件，速度看网络）
4. 游戏根目录出现 `MelonLoader/` 和 `Mods/` 文件夹即安装成功

> **如果控制台报错或安装失败**：卸载后尝试降低 MelonLoader 版本（例如 v0.7.1）。

### 2. 安装 Mod

将 `SultansGameMod.dll` 放入游戏根目录的 `Mods/` 文件夹。

### 3. 启动

启动游戏，进入主界面后按下 **F1** 打开修改面板。

## Steam 游戏根目录快速定位

Steam 库 → 右键 **Sultan's Game** → 管理 → 浏览本地文件。

## 功能面板

| Tab | 功能 |
|---|---|
| 回合控制 | 回合跳转/冻结、游戏变速、游戏结束、重载存档 |
| 卡牌操作 | 选中卡牌属性编辑、标签编辑、注入新卡牌（搜索/分类筛选） |
| 事件仪式 | 活跃事件管理、事件搜索与触发、仪式搜索与添加 |
| 自定义事件 | 声望/计数器数值选择、卡牌注入、特殊机会（交换苏丹牌/回到上一回合） |
| 关于 | 作者信息与链接 |

## 构建

```bash
dotnet build "SultansGameMod/SultansGameMod.csproj" -c Release -o "输出目录"
```

需要 .NET 6 SDK + MelonLoader v0.7.2 依赖（`MelonLoader/net6/` 和 `MelonLoader/Il2CppAssemblies/`）。

---

**作者**: Mizuof  
**B站**: https://space.bilibili.com/516995192/dynamic  
**QQ群**: 624594852  
**网站**: www.mizu7.top  

*本修改器完全免费，请勿用于商业用途。*
