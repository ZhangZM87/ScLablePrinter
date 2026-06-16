# 单元格文本元素问题总结

## 问题背景
- 在 SCLabelPrinter WPF 预览编辑器中，表格右键菜单可以添加单元格内部元素。
- 添加条码和二维码元素正常显示。
- 添加文本元素虽然能进入模型，但在预览中不可见或行为异常。

## 已定位点
1. **命令执行路径**
   - `LabelCanvas.cs` 中 `CreateMenuItem(...)` 为右键菜单创建 `MenuItem`。
   - 曾经修改为使用 `Command` / `CommandParameter` 并增加回退 `Click` 执行，确认命令能发出。

2. **模型创建逻辑**
   - `EditorViewModel.HandleTableCellContextMenuAsync(...)` 处理 `TableCellContextMenuAction.AddCellTextElement`。
   - `AddCellInnerElement(...)` 将生成的 `TableCellTextElement` 插入目标单元格。
   - 文本模型已有，但显示异常，说明命令与模型插入基本正常。

3. **默认文本元素大小**
   - `TableCellInnerElementFactory.CreateTextElement(...)` 负责创建默认文本内部元素尺寸。
   - 调整过默认宽高以排查可见性问题，但最终恢复为原始默认值。

4. **渲染与测量逻辑**
   - `LabelCanvas.cs` 中 `GetTableCellInnerElementScreenRect(...)`、`GetTextElementVisualBounds(...)` 和 `DrawTableCellInnerElements(...)` 是关键怀疑区域。
   - 条码/二维码正常表明问题更可能集中在 `TableCellTextElement` 的测量/绘制分支。

5. **用户交互与反馈**
   - 原先状态提示过于通用，无法区分“文本添加失败”与“表格更新成功”。
   - 已改为更精确提示，如 `已向单元格添加文本元素`，方便判断执行路径。

## 当前状态
- 现在已确认“添加文本元素”命令已触发并进入模型。
- 但预览中仍未显示文本元素。
- 问题最可能发生在文本内部元素的可视范围计算或绘制绘制层。

## 后续建议
- 重点检查 `LabelCanvas.cs` 中：
  - `GetTableCellInnerElementScreenRect(...)`
  - `GetTextElementVisualBounds(...)`
  - `DrawTableCellInnerElements(...)` 中的 `case TableCellTextElement`
- 避免继续改右键菜单命令路径，先保留条码/二维码已正常的处理方式。
- 如果需要，可以直接对文本绘制进行最小化替换，先画一个固定边框占位，确认文本元素确实可见。
