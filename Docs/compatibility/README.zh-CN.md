# Simple Mending Yourself 1.6 兼容性清单

[English](README.md)

更新日期：2026-10-05 · Simple Mending Yourself 0.1.2

本清单汇总基础依赖、专用适配和已检查的模组组合。每项说明支持的行为、相关设置和验证范围。反馈组合问题时，欢迎提供模组名称与日志。

## 原版与基础依赖

**RimWorld 1.6 — 兼容**

开启“基础”工作的小人可以修补身上穿着的服装和已装备的武器。修补任务使用选定的工作台，收集并预留材料，在工作完成后恢复耐久。耐久范围、修补台过滤器和允许使用的小人列表共同决定修补目标。

官方 DLC 和其他模组的装备沿用 Simple Mending 提供的修补目标与材料定义。符合条件的服装和武器进入相同的修补流程。

**[Simple Mending](https://steamcommunity.com/sharedfiles/filedetails/?id=3657705987) — 必需依赖与联动**

使用 Simple Mending 的手工、电力修补台，以及物品过滤、材料过滤、修补成本计算和最终修补操作。SMY 增加个人装备选择、允许使用的小人页签和修补速度倍率。储存区中的材料会计入 Simple Mending 的资源可用性检查。

验证：SMY 0.1.2 游戏实测覆盖工作准入、过滤规则、完成修补和准确扣料。参见 [Simpler Sidearms 验证记录](../development/SimplerSidearmsCompatibility.md)与 [Rules of Engagement 验证记录](../development/RulesOfEngagementCompatibility.md)。

**[Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077) — 兼容**

已测试的副武器和战斗模组组合使用 Harmony 加载自身补丁。SMY 通过原版定义和组件添加工作项目与修补台控制。

## 副武器与战斗

**[Simple Sidearms](https://steamcommunity.com/sharedfiles/filedetails/?id=927155256) — 联动**

读取小人的已登记武器列表，将装备栏和背包中匹配的武器纳入修补。登记类型按武器定义与材质匹配。修补台的物品、材料过滤器，以及允许使用的小人列表和耐久阈值同样适用于这些修补任务。

验证：已审查专用登记武器适配代码。

**[Simpler Sidearms](https://steamcommunity.com/sharedfiles/filedetails/?id=3809933830) — 联动，SMY 0.1.2 起支持**

支持修补已装备的武器，以及背包中可装备的近战、远程武器。临时携带的武器也会在符合装备条件、工作台过滤和耐久范围时进入修补候选。修补保持物品当前的持有位置，并扣除本次任务运送的材料。

验证：启用 Simpler Sidearms 的 12 项游戏检查和 5 项基础对照检查通过，覆盖目标选择、过滤规则、背包武器修补完成、装备保留和材料扣除。[验证记录](../development/SimplerSidearmsCompatibility.md)。

**[Rules of Engagement](https://steamcommunity.com/sharedfiles/filedetails/?id=3809985752) — 已装备武器兼容**

支持修补主手和已装备的副手武器。背包中的副武器可以通过 RoE 的武器控制先装备，使其进入修补候选。装备方案、姿态和武器切换继续由 RoE 管理。

RoE 在任务开始时恢复此前使用的武器时，修补任务会保留选定物品的引用。待修物品在恢复操作中移入背包后，也可以完成本次修补。修补台过滤器、耐久阈值和材料成本持续生效。

验证：与 RoE 同时启用的 20 项游戏检查通过，包括副手武器修补完成、双持身份保留、任务开始时的工具恢复和准确扣料。[验证记录](../development/RulesOfEngagementCompatibility.md)。

## 组合使用

以 Simple Mending 与 SMY 作为修补基础，再选择适合殖民地的装备管理系统，并按照该系统的组合说明使用。上面的条目描述 SMY 对各系统的支持范围，验证记录列出实测配置。

使用装备类模组时，可在修补台的物品和材料过滤器中选择目标装备与材料。Simple Mending 的修补定义提供成本和适用规则，SMY 设置提供个人耐久范围和速度倍率。

## 后续维护

本目录的中英文文件同步维护。每项已检查模组都提供直接工坊链接、具体支持说明和验证记录。后续兼容性更新继续通过这两个固定的 GitHub 文件链接提供。

