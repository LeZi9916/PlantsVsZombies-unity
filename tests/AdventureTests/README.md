# 冒险核心回归

在 PvZUnity 根目录运行 `dotnet run --project tests/AdventureTests/AdventureTests.csproj -c Release`。默认先运行独立断言，再运行真实连续50关冒险，成功退出码为0。自动玩家只调用公开操作，不增加阳光、不注入植物/僵尸、不清场、不调血或改胜利；独立单位测试中明确标记的 fixture 使用内部测试状态，二者分开报告。

连续冒险每关使用固定种子 `level*104729`，通过同一用户GUID、同一AdventureCampaign逐关战斗、拾取奖励、磁盘保存并重读验证。测试过程中自动重开存储进行断言，不替代用户关闭再打开Player的交互测试。失败时退出码非0且CSV记录实际失败，不会强行解锁余下关卡。

参数：`--unit`、`--playthrough`、`--all-levels`、`--level 38`、`--level 38 12345`。单关会产生 trace-<level>.txt 和 playthrough-<level>.csv；完整结果为 playthrough-results.csv。单元/连续进度文件只在本目录 artifacts/<GUID>/ 下生成，旧文件保留用于验证备份。

Unity编译能力核对：`dotnet build tests/AdventureTests/UnityCoreCompile/UnityCoreCompile.csproj -c Release`；这是netstandard2.1/C#7.3纯核心编译，不启动Unity，不写主工程Build、Library或ProjectSettings。

接线、实现范围与近似项见项目根 ADVENTURE_HANDOFF.md。不能把该测试的通过解释为原版所有随机状态、动画或视觉已一致。

可选本机奖励机器码核对：`python tests/AdventureTests/verify_native_awards.py`（需已安装Capstone）。这是解码后受限整数分支解释，不是CPU执行或完整原版游戏回放；外部首周目谓词由测试提供。


## 升级植物回归

默认和 `--unit` 额外运行 `UpgradePlantTests.cs` 的240条断言：8种升级的正确/错误前置及消费原子性、双阳光、机枪四连发、忧郁菇四次范围伤害/睡眠继承、香蒲跨行追踪和气球优先、冰西瓜溅射减速、吸金磁实际收钱/金盏花产币、地刺王9次特殊耐久、加农炮两格/南瓜/边界/瞄准取消/发射装填、模仿者目标价格冷却/独立卡槽/变身/被吃/支撑/咖啡效果，以及只读投射物来源元数据。原生数据和8组关键指令字节直接对本机PE核对。

第一轮冒险默认不开放升级卡。测试通过显式 `GameRules.AdventureSeedAccess` 授权，不表示商店或购买已实现。核心API说明见 ADVENTURE_HANDOFF.md。`evidence/upgrade-regression-comparison.json` 证明默认50关当前CSV与升级前所有结果字段一致。
