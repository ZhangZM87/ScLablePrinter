# SCLabelPrinter 表格缩放与预览问题复核

## 1. 文档目的

本文基于当前工作区代码对“表格缩放异常”和“快速打印预览越界”两个问题做一次复核，目标是把原先混杂在一起的结论拆成三类：

1. 已确认的实现现状
2. 已确认的问题
3. 需要继续验证的风险点

这样做的原因很直接：原文中有部分判断已经被当前代码实现证伪，如果继续按旧结论推进，会误导后续修复方向。

---

## 2. 已确认的实现现状

### 2.1 表格缩放链路

- `LabelCanvas.GetHitTableResizeHandle(...)` 当前支持三类命中：
  - 内部列分隔线拖动
  - 内部/外侧行分隔线拖动
  - 右下角 `Overall` 拖动
- `Overall` 模式并不是“目标行列不明确”，而是明确绑定为：
  - 最后一列宽度
  - 最后一行高度
- `LabelCanvas.OnMouseMove(...)` 会直接修改当前预览中的 `TableElement` 尺寸数据。
- `LabelCanvas.OnMouseLeftButtonUp(...)` 会通过 `TableCellResizeCommand` 把结果提交回 `EditorViewModel.HandleTableCellResize(...)`。
- `EditorViewModel.HandleTableCellResize(...)` 只有在请求里携带完整 `RowHeights` 时，才会整表覆盖行高；否则只会保存单个行高。

### 2.2 预览布局链路

- `FilePrintView.xaml` 中的 `LabelCanvas` 直接放在 `Border` 内，没有额外包裹 `ScrollViewer`。
- `EditorView.xaml` 中的 `LabelCanvas` 放在 `ScrollViewer` 内，并显式设置了 `HorizontalAlignment="Left"`、`VerticalAlignment="Top"`。
- `LabelCanvas.OnRender(...)` 当前已经对标签区域执行了 `DrawingContext.PushClip(...)`。
- `LabelCanvas.MeasureOverride(...)` 会根据纸张尺寸和 `ZoomFactor` 返回期望大小。
- `LabelCanvas.ArrangeOverride(...)` 当前直接返回 `DesiredSize`，没有根据父容器最终尺寸做收敛。

---

## 3. 已确认的问题

### 3.1 相邻行拖动时，预览状态与最终保存状态可能不一致

这是当前文档里最值得优先保留的问题，而且可以从代码直接确认。

- 当拖动内部横向分隔线时，`LabelCanvas.OnMouseMove(...)` 会同时修改两行：
  - 上方行高度增加或减少
  - 下方相邻行高度反向调整
- 但在松开鼠标时，`LabelCanvas.OnMouseLeftButtonUp(...)` 默认只提交一个 `TableCellResizeRequest`，且没有附带完整 `RowHeights`。
- `EditorViewModel.HandleTableCellResize(...)` 收到这类请求后，只会持久化单个索引对应的行高。

结果是：

- 预览阶段看到的是“两行一起变化”
- 最终保存阶段却可能只保留其中一行的变化
- 后续刷新、重新选择或重新打开模板时，表格行高可能出现不一致

这比原文中“整体拖动保存不稳定”的说法更准确，问题核心不是所有拖动都不稳定，而是“相邻行联动调整时，提交模型无法完整表达当前预览状态”。

### 3.2 `Overall` 模式语义固定，但交互含义不够直观

原文将这个问题描述为“可能未识别具体目标行/列”，这一点与当前实现不符。

当前真实情况是：

- 右下角手柄一旦命中，就固定修改最后一列与最后一行
- 命中目标是明确的，不存在“索引未识别”的直接证据

真正的问题在于：

- 该交互把宽度和高度的联动修改绑定在一个手柄上
- 用户很难从界面上预期“这次拖动会同时影响哪两个维度”
- 后续保存链路仍然拆成两个独立请求，调试时不容易判断异常来自哪一段

因此，这里更适合定义为“交互语义不清晰、排障成本高”，而不是“命中检测失效”。

### 3.3 表格边界约束不是完全缺失，而是分散在多条分支里

原文对边界约束的描述偏重，容易让人误解为“当前几乎没有约束”。现状更接近下面这个判断：

- 列宽调整时，代码使用 `availableWidth` 和其他列总宽度限制当前列的最大值
- 外侧首行/末行调整时，代码使用 `availableHeight` 与其他行总高度做上限控制
- 内部分隔线拖动时，代码通过保持相邻两行总和不变来限制变化范围

所以问题不在于“完全没有边界控制”，而在于：

- 约束逻辑分散在多条 `if/else` 分支中
- 不同分支使用的限制策略并不统一
- 后续一旦新增新的缩放模式或修改保存逻辑，很容易引入回归

换句话说，这里应归类为“维护性风险高”，而不是“现有实现完全失控”。

---

## 4. 快速打印预览问题的准确认定

### 4.1 原文中一条结论已经过时

原文曾将该问题归因为 `LabelCanvas` 缺少裁剪能力。

这条结论与当前实现不一致。`LabelCanvas.OnRender(...)` 已经对标签区域执行了 `PushClip(...)`，因此“标签内部绘制完全不裁剪”这个判断需要删除。

### 4.2 当前更像是“控件布局越界”，而不是“标签内容无裁剪”

结合 `FilePrintView.xaml` 与 `LabelCanvas` 当前实现，预览异常更可能来自以下组合：

- `LabelCanvas` 会根据纸张尺寸和缩放比例返回较大的 `DesiredSize`
- `FilePrintView` 没有像 `EditorView` 那样给它提供 `ScrollViewer`
- 父级容器也没有明确的裁剪或滚动承接机制
- `ArrangeOverride(...)` 直接返回 `DesiredSize`，缺少对父容器最终布局结果的收敛

因此，快速打印页里看到的“预览跑出边框”，更准确的说法是：

- 整个 `LabelCanvas` 控件可能超出承载区域
- 而不是标签内部元素在纸张区域内完全失去裁剪

### 4.3 为什么编辑器视图更稳定

`EditorView.xaml` 的预览区域有两个明显差异：

- 外层使用 `ScrollViewer`
- `LabelCanvas` 明确左上对齐，而不是完全交给父布局容器推断

这意味着同样的 `DesiredSize` 在编辑器视图里有可滚动容器承接，在快速打印视图里则更容易直接挤出卡片区域。

---

## 5. 修复优先级建议

### 5.1 第一优先级：修正行高提交模型

建议先处理表格缩放保存链路，因为这是最容易造成“预览正常、保存后异常”的根因。

建议方向：

1. 当内部行分隔线拖动同时影响两行时，提交完整 `RowHeights`
2. 或者把行缩放命令改为显式支持“联动调整多个行高”
3. 保证 `OnMouseMove(...)` 的临时状态与 `OnMouseLeftButtonUp(...)` 的最终提交模型一致

### 5.2 第二优先级：收敛 `Overall` 模式语义

建议不要再把问题表述为“命中失败”，而是直接从交互语义入手：

1. 保留右下角联动拖动，但在文档和代码里明确它只控制最后一列与最后一行
2. 或者拆成更明确的宽度手柄和高度手柄，降低联动副作用
3. 无论选哪种方案，都应让提交链路与预览链路保持一一对应

### 5.3 第三优先级：修复快速打印页的布局承接

建议先从布局容器入手，而不是继续把精力放在 `PushClip(...)` 上。

建议方向：

1. 为 `FilePrintView` 的图形预览区域增加 `ScrollViewer`
2. 明确 `LabelCanvas` 在该视图中的对齐方式与可见区域边界
3. 如果仍存在越界，再继续审查 `ArrangeOverride(...)` 是否应该返回 `finalSize` 或做更明确的布局收敛

---

## 6. 建议保留为待验证项的内容

以下内容目前更适合保留为“待验证”，不宜直接写成已确认缺陷：

- 某些拖动场景下是否仍可能越过纸张边界
- `Overall` 模式是否应该被完全移除
- 快速打印页是否只靠 `ScrollViewer` 就能彻底解决视觉越界

这些点并非不重要，而是当前代码证据不足以支撑更强结论。

---

## 7. 相关文件

- `SCLabelPrinter\SCLabelPrinter\Controls\LabelCanvas.cs`
- `SCLabelPrinter\SCLabelPrinter\Views\FilePrintView.xaml`
- `SCLabelPrinter\SCLabelPrinter\Views\EditorView.xaml`
- `SCLabelPrinter\SCLabelPrinter\ViewModels\EditorViewModel.cs`
- `SCLabelPrinter\SCLabelPrinter.Core\Models\LabelElements.cs`

---

## 8. 本次复核后的结论

当前最需要修正的不是“再堆更多猜测”，而是把问题表述精确化：

- 表格问题的核心，是“联动行高调整的提交模型不完整”
- 预览问题的核心，是“快速打印页缺少对大尺寸 `LabelCanvas` 的布局承接”
- 原文关于 `PushClip(...)` 缺失的结论已经过时，不应继续作为根因使用

按这个版本继续推进，后续修复会更聚焦，也更容易验证是否真的解决了问题。
