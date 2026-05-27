# SCLabelPrinter 标签预览问题汇总

## 1. 问题背景

当前问题集中在 `SCLabelPrinter` 应用的文件预览功能，特别是对 TSPL 文件的解析与 `LabelCanvas` 预览渲染。

主要目标：
- 让 `.txt` / `.prn` / `.bin` 文件能够正确识别为 TSPL 并显示图形预览
- 修复 `test2.txt` 等样本的文本渲染混乱问题
- 识别 `test1.txt` 类文件是否为可预览内容

## 2. 核心发现

### 2.1 `test2.txt` 的真实格式

- `test2.txt` 不是普通可读的 TSPL 文本。
- 它本质上是 TSPL 指令的 ASCII 十六进制转储。
- 解码后内容是：
  - `SIZE 128 mm,150 mm`
  - `GAP 10 mm`
  - `CLS`
  - `DENSITY 5`
  - 一系列 `BOX`、`ERASE`、`TEXT`、`BARCODE` 指令
- 其中 `TEXT` 命令参数包含非常长的字符串。

结论：`test2.txt` 预览混乱，主要原因不是“错误解析后才变成这样”，而是“文件内容本身就包含极长的 `TEXT` 文本”。

### 2.2 `test1.txt` 的类型

- `test1.txt` 更像是原始二进制打印包，而非 TSPL 文本或十六进制 TSPL 转储。
- 当前实现逻辑对纯二进制打印包的识别能力不足，因此 `test1` 不能稳定进入可视化预览流程。

## 3. 已尝试过的改动

### 3.1 解析链路改动

- `TsplTextDecoder.TryDecodeHexDump` 现在支持两种 hex dump：
  - 带空格分隔的十六进制字节
  - 连续无空格的十六进制字符串
- 增加了 `BITMAP` 命令解析支持
- 将 `BITMAP` 转换为 `BitmapElement`，并尝试在 `LabelCanvas` 中渲染

### 3.2 预览控件改动

- 在 `LabelCanvas` 中新增 `BitmapElement` 绘制函数
- 对 `TextElement` 做了简单的换行逻辑
- 加入了最大显示行数限制
- 对第 4 行进行了尾部省略处理（`...`）

## 4. 当前未解决的关键点

### 4.1 文本布局仍不符合 TSPL 显示逻辑

- 当前 `LabelCanvas` 的 `TEXT` 渲染仅按固定宽度折行，并没有与 TSPL 的真实显示边界完全对齐。
- 对于超长 `TEXT`，仍然可能出现：
  - 视觉上混乱
  - 长文本重叠
  - 位置错位
- 可能需要进一步实现：
  - 精确计算 TSPL 文字宽度
  - 按真实打印机行宽换行
  - 统一 `TEXT` 元素高度与预览边界

### 4.2 二进制文件可视化策略不明确

- `test1.txt` 类型的二进制文件是否应该被预览，当前未确定。
- 需要决定：
  - 是继续解析为 TSPL 文本/指令？
  - 还是只对可识别 TSPL 文本 hex dump 进行预览，其他二进制仅展示摘要？

### 4.3 现有测试不覆盖真实展示问题

- 当前单元测试覆盖了解码和解析逻辑，但没有覆盖 `LabelCanvas` 的实际显示效果。
- 这意味着即便编译通过，也可能无法保证长文本渲染正确。

## 5. 重要文件

- `SCLabelPrinter\SCLabelPrinter.Core\Printing\TsplTextDecoder.cs`
- `SCLabelPrinter\SCLabelPrinter.Core\Printing\TsplParser.cs`
- `SCLabelPrinter\SCLabelPrinter.Core\Models\LabelElements.cs`
- `SCLabelPrinter\SCLabelPrinter\Controls\LabelCanvas.cs`
- `SCLabelPrinter\SCLabelPrinter.Tests\Printing\TsplParserTests.cs`

## 6. 建议给其他 AI 的处理方向

### 6.1 先确定输入类型

- 先对 `test2.txt` 这种文件确认：它是十六进制转储的 TSPL，而不是原始 TSPL。
- 对 `test1.txt` 这样的文件确认：它是否属于可视化 TSPL/打印指令，还是仅能作为二进制打印包发送。

### 6.2 重点修复对象：`LabelCanvas` 文本渲染

- 以 TSPL `TEXT` 指令为基础，重新设计 `TextElement` 的布局方式。
- 需要做到：
  - 可拆分单行文本为多行
  - 能够根据图形空间限制裁剪
  - 支持文字长度、字体大小与缩放比例的综合计算
  - 避免文本旋转后边界错位

### 6.3 预览策略决策

- 对不可解析的二进制文件采用“摘要展示”策略；
- 对可解析的 `hex dump` TSPL 采用真实预览；
- 若无法直接解析，则不要误导性地显示错乱画面。

## 7. 建议直接交给别的 AI 的问题描述

请基于以上问题，优先处理：

1. `LabelCanvas` 的 `TextElement` 预览绘制算法应当如何改进？
2. `test2.txt` 应当如何在预览中更合理地折行、截断并避免混乱？
3. 是否需要新增 `TEXT` 元素显示约束或 `LabelCanvas` 绘制边界管理？
4. `test1.txt` 是否本质上属于不可视化二进制打印包？如果是，应当如何在 UI 中做出合理fallback？

---

> 备注：目前已经尝试过源码层面的折行和最大行数截断，但 `test2` 的实际内容仍然使预览显得混乱，说明问题更偏向渲染策略而非简单格式化。