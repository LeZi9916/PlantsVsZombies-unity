# 冒险核心回归

在 PvZUnity 根目录运行 `dotnet run --project Tools/Tests/AdventureTests/AdventureTests.csproj -c Release`。默认先运行独立断言，再运行真实连续50关冒险，成功退出码为0。自动玩家只调用公开操作，不增加阳光、不注入植物/僵尸、不清场、不调血或改胜利；独立单位测试中明确标记的 fixture 使用内部测试状态，二者分开报告。

连续冒险每关使用固定种子 `level*104729`，通过同一用户GUID、同一AdventureCampaign逐关战斗、拾取奖励、磁盘保存并重读验证。测试过程中自动重开存储进行断言，不替代用户关闭再打开Player的交互测试。失败时退出码非0且CSV记录实际失败，不会强行解锁余下关卡。

参数：`--unit`、`--playthrough`、`--all-levels`、`--level 38`、`--level 38 12345`。单关会产生 trace-<level>.txt 和 playthrough-<level>.csv；完整结果为 playthrough-results.csv。单元/连续进度文件只在本目录 artifacts/<GUID>/ 下生成，旧文件保留用于验证备份。

`--reward-projectiles` 只运行胜利后棋盘投射物回归，覆盖直线、反向、斜向与抛物线运动、出界回收、奖励介绍阶段停止及独立模式胜利；同时检查不恢复伤害、掉落、植物攻击或波次推进。原版依据为 `Board.UpdateGameObjects` 在奖励落地及收取期间继续更新现有 `Projectile`。

`--fume-shroom` 只运行大喷菇回归：原版 340 像素攻击矩形的触边瞄准与正重叠伤害、50 计数器第 49 tick 的瞬时多目标穿盾结算、无 Pea、目标变动后的已排定攻击，以及无目标时不播放攻击动画。默认和 `--unit` 也运行这些断言。

`--multi-head` 只运行三线射手与分裂豌豆的 PC 计时回归：35/26 tick 共用射击计数、有效头部的独立发射、目标消失后的待发射结算，以及旧发射先于新攻击尝试的顺序。默认和 `--unit` 也运行这些断言；目标搜索和动画状态由调用方输入，测试不执行原版 EXE。

`--starfruit` 运行杨桃的原版五方向目标路径、40 计数器与第 39 tick 发射、目标消失后的待发射、射击期间的独立攻击周期和最近被啃食回归。默认和 `--unit` 也运行这些断言；该测试不执行原版 EXE。

`--coin-feedback` checks silver, gold, and diamond collection against PC `Coin::UpdateCollected`
(`0x43C570`): the `(39,558)` wallet target, one twenty-first movement per native tick,
the twelve-pixel scoring threshold, shrinking and opacity, pause and duplicate-click guards,
and retryable arrival-time persistence. It also checks the original currency kind notification
used for the coin Foley or diamond pickup sample. These simulation checks do not verify audio output.

Unity编译能力核对：`dotnet build Tools/Tests/AdventureTests/UnityCoreCompile/UnityCoreCompile.csproj -c Release`；这是netstandard2.1/C#9.0纯核心编译，不启动Unity，不写主工程Build、Library或ProjectSettings。

接线、实现范围与近似项见项目根 ADVENTURE_HANDOFF.md。不能把该测试的通过解释为原版所有随机状态、动画或视觉已一致。

可选本机奖励机器码核对：`python Tools/Tests/AdventureTests/verify_native_awards.py`（需已安装Capstone）。这是解码后受限整数分支解释，不是CPU执行或完整原版游戏回放；外部首周目谓词由测试提供。

可选本机阴影坐标核对：`python Tools/Tests/AdventureTests/verify_native_shadow.py`（需Capstone及仓库父目录中hash匹配的原版EXE）。该工具限域解释原版`0x527D20`图片绘制辅助的整数和x87指令，核对原图86×36、原尺寸半宽高偏移及正常/小体型/反向的最终边界；不运行原版进程，也不写入游戏内存。身体原点使用单独核实的`0x547FF0`公式，输出中明确区分这两类证据。

Run the focused native mine contact/blast/death flags, zombie screen entry, defensive damage,
chomper state transitions, and huge-wave plant retention regressions with
`dotnet run --project Tools/Tests/AdventureTests/AdventureTests.csproj -c Release -- --combat-fidelity`.
These fixtures test the simulation and original image/track selection rules;
they do not validate Unity rendering or platform Player behavior.

The newspaper regressions also exercise the original paper-break gasp, independent
question-mark clock, angry-head selection, and weighted roar admission. Their PC
references are `0x54C7A0` and `0x541770`; Puff-shroom timing uses `0x46D240` and
`0x472C70`. Grave emergence is checked in the Unity EditMode fixtures against
`0x54DA20`, `0x544970`, `0x54990C`, and `0x548DAA`; original stone and soil geometry
uses the local PC `GridItem.DrawGraveStone` at `0x4589E0`. Its source rectangles
retain the full soil atlas cell and apply the shared -9-pixel baseline offset;
the mobile reference implementation's cropped soil parameters do not apply.
The original artwork, sound, and cropping
fixtures and their opt-in resource directory are documented in `Assets/Tests/README.md`.


## 升级植物回归

默认和 `--unit` 额外运行 `UpgradePlantTests.cs` 的240条断言：8种升级的正确/错误前置及消费原子性、双阳光、机枪四连发、忧郁菇四次范围伤害/睡眠继承、香蒲跨行追踪和气球优先、冰西瓜溅射减速、吸金磁实际收钱/金盏花产币、地刺王9次特殊耐久、加农炮两格/南瓜/边界/瞄准取消/发射装填、模仿者目标价格冷却/独立卡槽/变身/被吃/支撑/咖啡效果，以及只读投射物来源元数据。原生数据和8组关键指令字节直接对本机PE核对。

第一轮冒险默认不开放升级卡。测试通过显式 `GameRules.AdventureSeedAccess` 授权，不表示商店或购买已实现。核心API说明见 ADVENTURE_HANDOFF.md。`evidence/upgrade-regression-comparison.json` 证明默认50关当前CSV与升级前所有结果字段一致。
