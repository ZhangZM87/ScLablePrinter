# 表格单元格内部元素设计

## 背景

当前 `SCLabelPrinter` 模板编辑器中的 `TableElement` 仅支持每个单元格一个 `TableCell` 内容，表格边框和网格线的样式为实线或默认样式。

当前需求是：
- 表格外框和单元格网格采用虚线显示；
- 每个表格单元格可以包含多个“内部元素”，默认横向排列；
- 每个内部元素可以单独拖拽、调整大小；
- 右键菜单可用于添加、编辑、删除单元格内部元素；
- 内部元素位置不必严格限制在单元格内部，只要视觉上保持在所属单元格范围内即可。

## 目标

实现一个可扩展、分层清晰的表格内部元素系统，满足：
- 可在单元格中添加多个子元素；
- 子元素支持独立逻辑类型（文本、条码、二维码）；
- 子元素可独立拖拽、缩放；
- 表格边框和网格线统一改为虚线样式；
- 模型和序列化兼容现有模板数据结构。

## 设计方案

### 数据模型

引入新的内部元素抽象：

- `TableCellInnerElement`（抽象基类）
  - `Id`
  - `X`, `Y`（相对单元格左上角坐标）
  - `Width`, `Height`
  - `Rotation`
  - `ContentType` 或子类类型标识

派生类型：
- `TableCellTextElement`
- `TableCellBarcodeElement`
- `TableCellQrCodeElement`

现有 `TableCell` 扩展为：
- `List<TableCellInnerElement> InnerElements`
- `int Padding` / 布局参数（可选）

这样可以保持顶部 `LabelElement` 与单元格内部元素的清晰分层，未来可按需扩展更多类型。

### 渲染与布局

在 `LabelCanvas.DrawTableElement` 中：
- 使用虚线 `Pen` 绘制表格外框和所有网格线；
- 按照 `TableElement.Rows/Cols/ColumnWidths/RowHeight` 绘制单元格区域；
- 对每个单元格绘制 `TableCell.InnerElements`：
  - 计算 `cellOrigin = tableOrigin + cellOffset`
  - 内部元素绝对位置为 `cellOrigin + inner.X/Y`
  - 默认新建时横向排列在单元格内部
- 绘制内部元素选中边框和缩放控件。

### 交互与拖拽

现有画布命中检测逻辑分层顺序调整为：
1. 先检测 `TableCellInnerElement` 命中；
2. 再检测 `TableElement` 整体；
3. 最后检测其他顶层元素。

拖拽逻辑：
- 鼠标左键按下命中内部元素时开始拖动；
- `LabelCanvas` 记录内部元素 `Id`、拖动偏移；
- 鼠标移动时按比例更新内部元素 `X/Y`；
- 允许元素移动到单元格外部，但仍以所属单元格坐标为父级参照；
- 在移动结束后更新预览。

缩放逻辑：
- 选中内部元素时绘制四角或边缘缩放控制点；
- 拖动控制点时改变 `Width/Height`，并保持最小值约束；
- 可在后续迭代补充锁定纵横比、网格对齐等高级行为。

### 右键菜单和编辑

将 `TableElement` 右键菜单扩展为：
- 添加单元格内部元素（文本、条码、二维码）
- 编辑单元格内部元素
- 删除单元格内部元素

`TableCellEditorWindow` 或新增对话框负责展示当前单元格内部元素列表，支持：
- 新增内部元素类型；
- 选择并编辑内部元素内容；
- 删除内部元素；
- 预览当前单元格内横向排列结果。

### 打印/序列化兼容

`TableElementTsplWriter` 需要扩展：
- 遍历每个 `TableCell.InnerElements`
- 根据单元格相对坐标计算全局打印坐标
- 生成对应 TSPL 指令

数据模型应继续支持 JSON 多态序列化，避免破坏现有 `LabelTemplateDocument` 模板。

## 扩展性

该方案保留以下扩展能力：
- 未来可支持更多单元格内部元素类型（二维码、图片、富文本等）；
- 未来可增加单元格内部元素对齐策略；
- 未来可支持单元格间“元素拖放转移”。

## 验证标准

- 表格外框和网格线为虚线样式；
- 单元格可容纳多个内部元素，并默认横向排列；
- 内部元素可独立选中、拖拽、调整大小；
- 右键菜单能新增/编辑/删除内部元素；
- 现有模板文件仍可正确加载。
