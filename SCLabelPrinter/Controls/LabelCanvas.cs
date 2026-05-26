using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SCLabelPrinter.Core.Models;
using ZXing;
using ZXing.Common;
using ZXing.QrCode.Internal;
using ZXing.Rendering;

namespace SCLabelPrinter.Controls;

/// <summary>
/// 提供标签内容的轻量级预览控件，复用同一份模板数据进行可视化渲染。
/// </summary>
public sealed class LabelCanvas : FrameworkElement
{
    private const double DotsPerMillimeter = 8.0;
    private const int HitTestPadding = 8;
    private readonly Brush _backgroundBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFC));
    private readonly Brush _eraseBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFC));
    private readonly Pen _borderPen = new(new SolidColorBrush(Color.FromRgb(0xD9, 0xD2, 0xC3)), 1);
    private readonly Brush _foregroundBrush = Brushes.Black;
    private readonly Pen _selectionPen = new(new SolidColorBrush(Color.FromRgb(0x1D, 0x4E, 0x8E)), 1)
    {
        DashStyle = DashStyles.Dash
    };
    private readonly Brush _tableBackgroundBrush = new SolidColorBrush(Color.FromRgb(0xFC, 0xFC, 0xFF));
    private readonly Brush _tableAlternateRowBrush = new SolidColorBrush(Color.FromArgb(24, 0xEA, 0xEA, 0xF0));
    private readonly Pen _tableBorderPen = new(new SolidColorBrush(Color.FromRgb(0x25, 0x27, 0x2E)), 1.6);
    private readonly Pen _tableGridPen = new(new SolidColorBrush(Color.FromRgb(0x9A, 0x9E, 0xAC)), 0.7)
    {
        DashStyle = new DashStyle(new double[] { 2, 2 }, 0),
    };
    private string? _draggingElementId;
    private Point _dragOffset;

    public LabelCanvas()
    {
        Focusable = true;
    }

    public static readonly DependencyProperty TemplateProperty = DependencyProperty.Register(
        nameof(Template),
        typeof(LabelTemplateDocument),
        typeof(LabelCanvas),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SelectedElementIdProperty = DependencyProperty.Register(
        nameof(SelectedElementId),
        typeof(string),
        typeof(LabelCanvas),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ElementSelectedCommandProperty = DependencyProperty.Register(
        nameof(ElementSelectedCommand),
        typeof(ICommand),
        typeof(LabelCanvas),
        new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty ElementDragStartedCommandProperty = DependencyProperty.Register(
        nameof(ElementDragStartedCommand),
        typeof(ICommand),
        typeof(LabelCanvas),
        new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty ElementMovedCommandProperty = DependencyProperty.Register(
        nameof(ElementMovedCommand),
        typeof(ICommand),
        typeof(LabelCanvas),
        new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty TableCellContextMenuCommandProperty = DependencyProperty.Register(
        nameof(TableCellContextMenuCommand),
        typeof(ICommand),
        typeof(LabelCanvas),
        new FrameworkPropertyMetadata(null));

    public LabelTemplateDocument? Template
    {
        get => (LabelTemplateDocument?)GetValue(TemplateProperty);
        set => SetValue(TemplateProperty, value);
    }

    public string? SelectedElementId
    {
        get => (string?)GetValue(SelectedElementIdProperty);
        set => SetValue(SelectedElementIdProperty, value);
    }

    public ICommand? ElementSelectedCommand
    {
        get => (ICommand?)GetValue(ElementSelectedCommandProperty);
        set => SetValue(ElementSelectedCommandProperty, value);
    }

    public ICommand? ElementDragStartedCommand
    {
        get => (ICommand?)GetValue(ElementDragStartedCommandProperty);
        set => SetValue(ElementDragStartedCommandProperty, value);
    }

    public ICommand? ElementMovedCommand
    {
        get => (ICommand?)GetValue(ElementMovedCommandProperty);
        set => SetValue(ElementMovedCommandProperty, value);
    }

    public ICommand? TableCellContextMenuCommand
    {
        get => (ICommand?)GetValue(TableCellContextMenuCommandProperty);
        set => SetValue(TableCellContextMenuCommandProperty, value);
    }

    /// <summary>
    /// 在控件表面绘制标签边界和全部元素预览。
    /// </summary>
    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var bounds = new Rect(new Point(0, 0), RenderSize);
        drawingContext.DrawRectangle(new SolidColorBrush(Color.FromRgb(0xF7, 0xF1, 0xE3)), null, bounds);

        if (Template is null)
        {
            return;
        }

        var surface = CreateSurfaceRect();
        drawingContext.DrawRoundedRectangle(_backgroundBrush, _borderPen, surface, 16, 16);

        var (scale, origin) = CalculateScale(surface, Template.Label);
        foreach (var element in Template.Elements)
        {
            DrawElement(drawingContext, element, scale, origin);
        }
    }

    /// <summary>
    /// 开始鼠标拖动元素时进行选择并捕获鼠标。
    /// </summary>
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (Template is null)
        {
            return;
        }

        var mousePoint = e.GetPosition(this);
        var surface = CreateSurfaceRect();
        var (scale, origin) = CalculateScale(surface, Template.Label);
        var hitElement = Template.Elements.AsEnumerable().Reverse().FirstOrDefault(el => IsPointOverElement(el, mousePoint, scale, origin));
        if (hitElement is null)
        {
            return;
        }

        _draggingElementId = hitElement.Id;
        var elementBounds = GetElementBounds(hitElement, scale, origin);
        _dragOffset = new Point(mousePoint.X - elementBounds.X, mousePoint.Y - elementBounds.Y);
        CaptureMouse();
        SelectedElementId = hitElement.Id;

        // 画布元素被点击时通知外部视图模型，选择和拖动由外部命令处理。
        //var selectedCommand = ElementSelectedCommand;
        //if (selectedCommand is not null && selectedCommand.CanExecute(hitElement.Id))
        //{
        //    selectedCommand.Execute(hitElement.Id);
        //}

        //var dragStartedCommand = ElementDragStartedCommand;
        //if (dragStartedCommand is not null && dragStartedCommand.CanExecute(hitElement.Id))
        //{
        //    dragStartedCommand.Execute(hitElement.Id);
        //}

        e.Handled = true;
    }

    /// <summary>
    /// 鼠标右键单击时显示表格单元格上下文菜单。
    /// </summary>
    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);

        if (Template is null)
        {
            return;
        }

        var mousePoint = e.GetPosition(this);
        var surface = CreateSurfaceRect();
        var (scale, origin) = CalculateScale(surface, Template.Label);
        var hitElement = Template.Elements.AsEnumerable().Reverse().FirstOrDefault(el => IsPointOverElement(el, mousePoint, scale, origin));
        if (hitElement is not TableElement tableElement)
        {
            return;
        }

        var cell = GetTableCellFromPoint(tableElement, mousePoint, scale, origin);
        if (cell is null)
        {
            return;
        }

        SelectedElementId = tableElement.Id;
        var contextMenu = new ContextMenu();
        contextMenu.PlacementTarget = this;
        contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;

        contextMenu.Items.Add(CreateMenuItem("添加当前行上方", TableCellContextMenuAction.AddRowAbove, tableElement, cell.Value.Row, cell.Value.Column));
        contextMenu.Items.Add(CreateMenuItem("添加当前行下方", TableCellContextMenuAction.AddRowBelow, tableElement, cell.Value.Row, cell.Value.Column));
        var removeRowItem = CreateMenuItem("删除当前行", TableCellContextMenuAction.RemoveRow, tableElement, cell.Value.Row, cell.Value.Column);
        removeRowItem.IsEnabled = tableElement.Rows > 1;
        contextMenu.Items.Add(removeRowItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(CreateMenuItem("添加当前列左侧", TableCellContextMenuAction.AddColumnLeft, tableElement, cell.Value.Row, cell.Value.Column));
        contextMenu.Items.Add(CreateMenuItem("添加当前列右侧", TableCellContextMenuAction.AddColumnRight, tableElement, cell.Value.Row, cell.Value.Column));
        var removeColumnItem = CreateMenuItem("删除当前列", TableCellContextMenuAction.RemoveColumn, tableElement, cell.Value.Row, cell.Value.Column);
        removeColumnItem.IsEnabled = tableElement.Cols > 1;
        contextMenu.Items.Add(removeColumnItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(CreateMenuItem("编辑当前单元格", TableCellContextMenuAction.EditCell, tableElement, cell.Value.Row, cell.Value.Column));

        contextMenu.IsOpen = true;

        e.Handled = true;
    }

    private MenuItem CreateMenuItem(string header, TableCellContextMenuAction action, TableElement table, int row, int column)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) =>
        {
            var command = TableCellContextMenuCommand;
            if (command is not null)
            {
                var request = new TableCellContextMenuRequest
                {
                    TableElementId = table.Id,
                    Row = row,
                    Column = column,
                    Action = action,
                };
                if (command.CanExecute(request))
                {
                    command.Execute(request);
                }
            }
        };

        return item;
    }

    private static (int Row, int Column)? GetTableCellFromPoint(TableElement table, Point point, double scale, Point origin)
    {
        var left = origin.X + table.X * scale;
        var top = origin.Y + table.Y * scale;
        var totalWidth = table.ColumnWidths.Sum() * scale;
        var totalHeight = table.Rows * table.RowHeight * scale;
        var localX = point.X - left;
        var localY = point.Y - top;

        if (localX < 0 || localY < 0 || localX > totalWidth || localY > totalHeight)
        {
            return null;
        }

        var row = Math.Min(table.Rows - 1, (int)(localY / (table.RowHeight * scale)));
        var column = 0;
        var accumulated = 0.0;
        foreach (var width in table.ColumnWidths)
        {
            accumulated += width * scale;
            if (localX <= accumulated)
            {
                break;
            }
            column++;
        }

        if (column < 0 || column >= table.Cols)
        {
            return null;
        }

        return (row, column);
    }

    /// <summary>
    /// 鼠标移动时更新拖动元素位置。
    /// </summary>
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_draggingElementId is null || !IsMouseCaptured || Template is null)
        {
            return;
        }

        var mousePoint = e.GetPosition(this);
        var surface = CreateSurfaceRect();
        var (scale, origin) = CalculateScale(surface, Template.Label);
        var targetPoint = new Point(mousePoint.X - _dragOffset.X, mousePoint.Y - _dragOffset.Y);
        var targetX = (int)Math.Round((targetPoint.X - origin.X) / scale);
        var targetY = (int)Math.Round((targetPoint.Y - origin.Y) / scale);
        targetX = Math.Max(0, targetX);
        targetY = Math.Max(0, targetY);

        var draggingElement = Template.Elements.FirstOrDefault(e => e.Id == _draggingElementId);
        if (draggingElement is not null)
        {
            var maxX = GetMaxDragCoordinate(draggingElement, Template.Label, scale, true);
            var maxY = GetMaxDragCoordinate(draggingElement, Template.Label, scale, false);
            targetX = Math.Min(targetX, maxX);
            targetY = Math.Min(targetY, maxY);
        }

        var moveRequest = new ElementMoveRequest(_draggingElementId, targetX, targetY);
        var movedCommand = ElementMovedCommand;
        if (movedCommand is not null && movedCommand.CanExecute(moveRequest))
        {
            movedCommand.Execute(moveRequest);
        }

        e.Handled = true;
    }

    /// <summary>
    /// 释放鼠标时结束拖动。
    /// </summary>
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (_draggingElementId is not null && IsMouseCaptured)
        {
            ReleaseMouseCapture();
            _draggingElementId = null;
            e.Handled = true;
        }
    }

    /// <summary>
    /// 鼠标捕获丢失时清理拖动状态。
    /// </summary>
    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        _draggingElementId = null;
    }

    /// <summary>
    /// 测量控件希望占据的默认空间。
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(
            double.IsInfinity(availableSize.Width) ? 560 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? 420 : availableSize.Height);
    }

    /// <summary>
    /// 创建标签可视化表面的边距矩形。
    /// </summary>
    private Rect CreateSurfaceRect()
    {
        return new Rect(20, 20, Math.Max(0, RenderSize.Width - 40), Math.Max(0, RenderSize.Height - 40));
    }

    /// <summary>
    /// 根据标签尺寸计算绘制缩放比例和原点位置。
    /// </summary>
    private static (double scale, Point origin) CalculateScale(Rect surface, LabelDefinition definition)
    {
        var widthDots = definition.Unit == LabelUnit.Millimeter ? definition.Width * DotsPerMillimeter : definition.Width;
        var heightDots = definition.Unit == LabelUnit.Millimeter ? definition.Height * DotsPerMillimeter : definition.Height;

        if (widthDots <= 0 || heightDots <= 0)
        {
            return (1.0, surface.Location);
        }

        var scale = Math.Min(surface.Width / Math.Max(widthDots, 1), surface.Height / Math.Max(heightDots, 1));
        var offsetX = surface.X + (surface.Width - widthDots * scale) / 2;
        var offsetY = surface.Y + (surface.Height - heightDots * scale) / 2;
        return (scale, new Point(offsetX, offsetY));
    }

    private static int GetMaxDragCoordinate(LabelElement element, LabelDefinition definition, double scale, bool isHorizontal)
    {
        var widthDots = definition.Unit == LabelUnit.Millimeter ? definition.Width * DotsPerMillimeter : definition.Width;
        var heightDots = definition.Unit == LabelUnit.Millimeter ? definition.Height * DotsPerMillimeter : definition.Height;
        var bounds = GetElementBounds(element, scale, new Point(0, 0));
        var sizeInUnits = isHorizontal ? bounds.Width / scale : bounds.Height / scale;
        var limit = isHorizontal ? widthDots : heightDots;
        var max = (int)Math.Floor(Math.Max(0, limit - sizeInUnits));
        return max;
    }

    /// <summary>
    /// 根据元素类型将对应图形绘制到预览表面。
    /// </summary>
    private void DrawElement(DrawingContext drawingContext, LabelElement element, double scale, Point origin)
    {
        switch (element)
        {
            case TextElement textElement:
                DrawTextElement(drawingContext, textElement, scale, origin);
                break;
            case BarcodeElement barcodeElement:
                DrawBarcodeElement(drawingContext, barcodeElement, scale, origin);
                break;
            case QrCodeElement qrCodeElement:
                DrawQrCodeElement(drawingContext, qrCodeElement, scale, origin);
                break;
            case BoxElement boxElement:
                DrawBoxElement(drawingContext, boxElement, scale, origin);
                break;
            case LineElement lineElement:
                DrawLineElement(drawingContext, lineElement, scale, origin);
                break;
            case EraseElement eraseElement:
                DrawEraseElement(drawingContext, eraseElement, scale, origin);
                break;
            case TableElement tableElement:
                DrawTableElement(drawingContext, tableElement, scale, origin);
                break;
        }

        if (element.Id == SelectedElementId)
        {
            DrawSelectionHighlight(drawingContext, element, scale, origin);
        }
    }

    private bool IsPointOverElement(LabelElement element, Point point, double scale, Point origin)
    {
        var hitBounds = GetElementHitBounds(element, scale, origin);
        if (element.Rotation % 360 != 0)
        {
            var rotatedPoint = RotatePoint(point, hitBounds.TopLeft, -element.Rotation);
            return hitBounds.Contains(rotatedPoint);
        }

        return hitBounds.Contains(point);
    }

    private static Rect GetElementHitBounds(LabelElement element, double scale, Point origin)
    {
        var bounds = GetElementBounds(element, scale, origin);
        bounds.Inflate(HitTestPadding, HitTestPadding);
        return bounds;
    }

    private static Rect GetElementBounds(LabelElement element, double scale, Point origin)
    {
        return element switch
        {
            TextElement textElement => GetTextBounds(textElement, scale, origin),
            BarcodeElement barcodeElement => GetBarcodeBounds(barcodeElement, scale, origin),
            QrCodeElement qrCodeElement => GetQrBounds(qrCodeElement, scale, origin),
            BoxElement boxElement => GetBoxBounds(boxElement, scale, origin),
            LineElement lineElement => new Rect(origin.X + lineElement.X * scale, origin.Y + lineElement.Y * scale, Math.Max(1, lineElement.Width * scale), Math.Max(1, lineElement.Height * scale)),
            EraseElement eraseElement => new Rect(origin.X + eraseElement.X * scale, origin.Y + eraseElement.Y * scale, Math.Max(1, eraseElement.Width * scale), Math.Max(1, eraseElement.Height * scale)),
            TableElement tableElement => new Rect(origin.X + tableElement.X * scale, origin.Y + tableElement.Y * scale, Math.Max(1, tableElement.TotalWidth * scale), Math.Max(1, tableElement.Rows * tableElement.RowHeight * scale)),
            _ => new Rect(origin.X + element.X * scale, origin.Y + element.Y * scale, 1, 1),
        };
    }

    private void DrawSelectionHighlight(DrawingContext drawingContext, LabelElement element, double scale, Point origin)
    {
        var bounds = GetElementHitBounds(element, scale, origin);
        if (element.Rotation % 360 != 0)
        {
            var actualBounds = GetElementBounds(element, scale, origin);
            var rotateTransform = new RotateTransform(element.Rotation % 360, actualBounds.X, actualBounds.Y);
            drawingContext.PushTransform(rotateTransform);
            drawingContext.DrawRectangle(null, _selectionPen, bounds);
            drawingContext.Pop();
            return;
        }

        drawingContext.DrawRectangle(null, _selectionPen, bounds);
    }

    private static Point RotatePoint(Point point, Point center, int angle)
    {
        var radians = angle * Math.PI / 180.0;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        var dx = point.X - center.X;
        var dy = point.Y - center.Y;
        return new Point(center.X + dx * cos - dy * sin, center.Y + dx * sin + dy * cos);
    }

    private static Rect GetTextBounds(TextElement element, double scale, Point origin)
    {
        var width = Math.Max(20, MapTsplFontSize(element.Font) * scale * element.XScale * element.Content.Length * 0.5);
        var height = Math.Max(16, MapTsplFontSize(element.Font) * scale * element.YScale);
        return new Rect(origin.X + element.X * scale, origin.Y + element.Y * scale, width, height);
    }

    private static Rect GetBarcodeBounds(BarcodeElement element, double scale, Point origin)
    {
        var width = EstimateBarcodeWidth(element) * scale;
        var height = Math.Max(20, element.Height) * scale;
        return new Rect(origin.X + element.X * scale, origin.Y + element.Y * scale, width, height);
    }

    private static Rect GetQrBounds(QrCodeElement element, double scale, Point origin)
    {
        var size = Math.Max(21 * Math.Max(1, element.CellWidth), 84) * scale;
        return new Rect(origin.X + element.X * scale, origin.Y + element.Y * scale, size, size);
    }

    private static Rect GetBoxBounds(BoxElement element, double scale, Point origin)
    {
        return new Rect(
            origin.X + element.X * scale,
            origin.Y + element.Y * scale,
            Math.Max(1, (element.EndX - element.X) * scale),
            Math.Max(1, (element.EndY - element.Y) * scale));
    }

    /// <summary>
    /// 绘制文本元素预览。
    /// </summary>
    private void DrawTextElement(DrawingContext drawingContext, TextElement element, double scale, Point origin)
    {
        var anchor = new Point(origin.X + element.X * scale, origin.Y + element.Y * scale);
        var formattedText = new FormattedText(
            element.Content,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface("Microsoft YaHei UI"),
            Math.Max(12, MapTsplFontSize(element.Font) * scale * element.YScale),
            _foregroundBrush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        DrawWithRotation(drawingContext, element.Rotation, anchor, () =>
        {
            drawingContext.DrawText(formattedText, anchor);
        });
    }

    /// <summary>
    /// 以简化条纹方式绘制条码元素预览，突出布局和占位而非完全可扫读内容。
    /// </summary>
    private void DrawBarcodeElement(DrawingContext drawingContext, BarcodeElement element, double scale, Point origin)
    {
        var startX = origin.X + element.X * scale;
        var startY = origin.Y + element.Y * scale;
        var width = EstimateBarcodeWidth(element) * scale;
        var barHeight = Math.Max(20, element.Height) * scale;
        var image = CreateBarcodeImage(MapBarcodeFormat(element.CodeType), element.Content, Math.Max(1, (int)Math.Ceiling(width)), Math.Max(1, (int)Math.Ceiling(barHeight)));
        var imageRect = new Rect(startX, startY, width, barHeight);

        DrawWithRotation(drawingContext, element.Rotation, new Point(startX, startY), () =>
        {
            drawingContext.DrawImage(image, imageRect);
        });

        if (element.Readable)
        {
            var readableText = new FormattedText(
                element.Content,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface("Bahnschrift"),
                Math.Max(10, 12 * scale),
                _foregroundBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            DrawWithRotation(drawingContext, element.Rotation, new Point(startX, startY), () =>
            {
                drawingContext.DrawText(readableText, new Point(startX, startY + barHeight + 4));
            });
        }
    }

    /// <summary>
    /// 以哈希矩阵方式绘制二维码元素预览，帮助用户校对位置和面积。
    /// </summary>
    private void DrawQrCodeElement(DrawingContext drawingContext, QrCodeElement element, double scale, Point origin)
    {
        var startX = origin.X + element.X * scale;
        var startY = origin.Y + element.Y * scale;
        var size = Math.Max(21 * Math.Max(1, element.CellWidth), 84) * scale;
        var image = CreateQrCodeImage(element.Content, Math.Max(1, (int)Math.Ceiling(size)), element.ErrorCorrectionLevel);
        var imageRect = new Rect(startX, startY, size, size);

        DrawWithRotation(drawingContext, element.Rotation, new Point(startX, startY), () =>
        {
            drawingContext.DrawImage(image, imageRect);
        });
    }

    /// <summary>
    /// 绘制矩形框元素预览。
    /// </summary>
    private void DrawBoxElement(DrawingContext drawingContext, BoxElement element, double scale, Point origin)
    {
        var rect = new Rect(
            origin.X + element.X * scale,
            origin.Y + element.Y * scale,
            Math.Max(1, (element.EndX - element.X) * scale),
            Math.Max(1, (element.EndY - element.Y) * scale));

        DrawWithRotation(drawingContext, element.Rotation, rect.TopLeft, () =>
        {
            drawingContext.DrawRectangle(null, new Pen(_foregroundBrush, Math.Max(1, element.Thickness * scale)), rect);
        });
    }

    /// <summary>
    /// 绘制线条元素预览。
    /// </summary>
    private void DrawLineElement(DrawingContext drawingContext, LineElement element, double scale, Point origin)
    {
        var rect = new Rect(origin.X + element.X * scale, origin.Y + element.Y * scale, Math.Max(1, element.Width * scale), Math.Max(1, element.Height * scale));

        DrawWithRotation(drawingContext, element.Rotation, rect.TopLeft, () =>
        {
            drawingContext.DrawRectangle(_foregroundBrush, null, rect);
        });
    }

    /// <summary>
    /// 绘制挖空区域预览。
    /// </summary>
    private void DrawEraseElement(DrawingContext drawingContext, EraseElement element, double scale, Point origin)
    {
        var rect = new Rect(origin.X + element.X * scale, origin.Y + element.Y * scale, Math.Max(1, element.Width * scale), Math.Max(1, element.Height * scale));

        DrawWithRotation(drawingContext, element.Rotation, rect.TopLeft, () =>
        {
            drawingContext.DrawRectangle(_eraseBrush, null, rect);
        });
    }

    /// <summary>
    /// 绘制表格元素预览。
    /// </summary>
    private void DrawTableElement(DrawingContext drawingContext, TableElement element, double scale, Point origin)
    {
        var left = origin.X + element.X * scale;
        var top = origin.Y + element.Y * scale;
        var rowHeight = element.RowHeight * scale;
        var totalWidth = Math.Max(1, element.ColumnWidths.Sum() * scale);
        var totalHeight = Math.Max(1, element.Rows * rowHeight);
        var tableRect = new Rect(left, top, totalWidth, totalHeight);

        drawingContext.DrawRoundedRectangle(_tableBackgroundBrush, _tableBorderPen, tableRect, 4, 4);

        for (var rowIndex = 0; rowIndex < element.Rows; rowIndex++)
        {
            if (rowIndex % 2 == 1)
            {
                var rowRect = new Rect(left, top + rowIndex * rowHeight, totalWidth, rowHeight);
                drawingContext.DrawRectangle(_tableAlternateRowBrush, null, rowRect);
            }
        }

        var currentX = left;
        for (var colIndex = 1; colIndex < element.Cols; colIndex++)
        {
            currentX += element.GetColumnWidth(colIndex - 1) * scale;
            drawingContext.DrawLine(_tableGridPen, new Point(currentX, top), new Point(currentX, top + totalHeight));
        }

        for (var rowIndex = 1; rowIndex < element.Rows; rowIndex++)
        {
            var y = top + rowIndex * rowHeight;
            drawingContext.DrawLine(_tableGridPen, new Point(left, y), new Point(left + totalWidth, y));
        }

        for (var rowIndex = 0; rowIndex < element.Rows; rowIndex++)
        {
            for (var colIndex = 0; colIndex < element.Cols; colIndex++)
            {
                var cellIndex = rowIndex * element.Cols + colIndex;
                if (cellIndex >= element.Cells.Count)
                {
                    continue;
                }

                var cell = element.Cells[cellIndex];
                var cellLeft = left + element.ColumnWidths.Take(colIndex).Sum() * scale;
                var cellTop = top + rowIndex * rowHeight;
                var cellWidth = element.GetColumnWidth(colIndex) * scale;
                var cellHeight = rowHeight;

                switch (cell.ContentType)
                {
                    case TableCellContentType.Text:
                    {
                        var contentRect = new Rect(cellLeft + 8, cellTop + 6, Math.Max(1, cellWidth - 16), Math.Max(1, cellHeight - 12));
                        var formattedText = new FormattedText(
                            cell.Content,
                            CultureInfo.CurrentUICulture,
                            FlowDirection.LeftToRight,
                            new Typeface("Microsoft YaHei UI"),
                            Math.Max(10, 12 * scale),
                            _foregroundBrush,
                            VisualTreeHelper.GetDpi(this).PixelsPerDip)
                        {
                            MaxTextWidth = contentRect.Width,
                            MaxTextHeight = contentRect.Height,
                            TextAlignment = TextAlignment.Center,
                        };
                        var textX = contentRect.X + (contentRect.Width - formattedText.Width) / 2;
                        var textY = contentRect.Y + (contentRect.Height - formattedText.Height) / 2;
                        drawingContext.DrawText(formattedText, new Point(Math.Max(contentRect.X, textX), Math.Max(contentRect.Y, textY)));
                        break;
                    }
                    case TableCellContentType.Barcode:
                    {
                        var availableWidth = Math.Max(1, cellWidth - 16);
                        var availableHeight = Math.Max(1, cellHeight - 18);
                        var desiredHeight = Math.Min(availableHeight, 48 * scale);
                        var barcodeImage = CreateBarcodeImage(MapBarcodeFormat(cell.BarcodeType), string.IsNullOrWhiteSpace(cell.Content) ? " " : cell.Content, (int)Math.Max(1, availableWidth), (int)Math.Max(1, desiredHeight));
                        var imageWidth = Math.Min(availableWidth, barcodeImage.PixelWidth);
                        var imageHeight = Math.Min(availableHeight, barcodeImage.PixelHeight);
                        var drawX = cellLeft + 8 + (availableWidth - imageWidth) / 2;
                        var drawY = cellTop + 8 + (availableHeight - imageHeight) / 2;
                        drawingContext.DrawImage(barcodeImage, new Rect(drawX, drawY, imageWidth, imageHeight));
                        break;
                    }
                    case TableCellContentType.QrCode:
                    {
                        var availableSize = Math.Min(cellWidth - 16, cellHeight - 16);
                        var qrSize = Math.Min((int)Math.Floor(availableSize), 120);
                        var qrImage = CreateQrCodeImage(string.IsNullOrWhiteSpace(cell.Content) ? " " : cell.Content, Math.Max(1, qrSize), cell.QrErrorCorrectionLevel);
                        var drawX = cellLeft + 8 + (cellWidth - 16 - qrSize) / 2;
                        var drawY = cellTop + 8 + (cellHeight - 16 - qrSize) / 2;
                        drawingContext.DrawImage(qrImage, new Rect(drawX, drawY, qrSize, qrSize));
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 在绘制指定元素时应用 TSPL 旋转角度。
    /// </summary>
    private static void DrawWithRotation(DrawingContext drawingContext, int rotation, Point anchor, Action drawAction)
    {
        if (rotation % 360 == 0)
        {
            drawAction();
            return;
        }

        drawingContext.PushTransform(new RotateTransform(rotation % 360, anchor.X, anchor.Y));
        drawAction();
        drawingContext.Pop();
    }

    /// <summary>
    /// 估算条码在预览画布上的实际宽度，尽量贴近打印时的占位尺寸。
    /// </summary>
    private static double EstimateBarcodeWidth(BarcodeElement element)
    {
        var baseWidth = element.CodeType switch
        {
            BarcodeType.Code39 => element.Content.Length * (element.Narrow + element.Wide) * 8,
            BarcodeType.Code128 => element.Content.Length * (element.Narrow + element.Wide) * 6,
            BarcodeType.Ean13 => 180,
            _ => 180,
        };

        return Math.Max(120, baseWidth);
    }

    /// <summary>
    /// 将 TSPL 文本字体编号映射为预览所使用的字号。
    /// </summary>
    private static double MapTsplFontSize(string font)
    {
        return font switch
        {
            "1" => 12,
            "2" => 16,
            "3" => 20,
            "4" => 26,
            "5" => 34,
            "6" => 42,
            _ => 18,
        };
    }

    /// <summary>
    /// 根据条码类型枚举映射 ZXing 的条码格式。
    /// </summary>
    private static BarcodeFormat MapBarcodeFormat(BarcodeType barcodeType)
    {
        return barcodeType switch
        {
            BarcodeType.Code39 => BarcodeFormat.CODE_39,
            BarcodeType.Code128 => BarcodeFormat.CODE_128,
            BarcodeType.Ean13 => BarcodeFormat.EAN_13,
            _ => BarcodeFormat.CODE_128,
        };
    }

    /// <summary>
    /// 创建条码位图，供预览控件直接绘制。
    /// </summary>
    private static BitmapSource CreateBarcodeImage(BarcodeFormat barcodeFormat, string content, int width, int height)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = barcodeFormat,
            Options = new EncodingOptions
            {
                Width = width,
                Height = height,
                Margin = 0,
                PureBarcode = true,
            },
        };

        return CreateBitmapSource(writer.Write(string.IsNullOrWhiteSpace(content) ? " " : content));
    }

    /// <summary>
    /// 创建二维码位图，供预览控件直接绘制。
    /// </summary>
    private static BitmapSource CreateQrCodeImage(string content, int size, string errorCorrectionLevel)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new EncodingOptions
            {
                Width = size,
                Height = size,
                Margin = 0,
                PureBarcode = true,
                Hints =
                {
                    [EncodeHintType.ERROR_CORRECTION] = MapQrErrorCorrectionLevel(errorCorrectionLevel),
                },
            },
        };

        return CreateBitmapSource(writer.Write(string.IsNullOrWhiteSpace(content) ? " " : content));
    }

    /// <summary>
    /// 将 ZXing 生成的像素数据转换为 WPF 可绘制的位图对象。
    /// </summary>
    private static BitmapSource CreateBitmapSource(PixelData pixelData)
    {
        var bitmap = BitmapSource.Create(pixelData.Width, pixelData.Height, 96, 96, PixelFormats.Bgra32, null, pixelData.Pixels, pixelData.Width * 4);
        bitmap.Freeze();
        return bitmap;
    }

    /// <summary>
    /// 将 TSPL 二维码纠错级别转换为 ZXing 所需的枚举值。
    /// </summary>
    private static ErrorCorrectionLevel MapQrErrorCorrectionLevel(string level)
    {
        return level.ToUpperInvariant() switch
        {
            "L" => ErrorCorrectionLevel.L,
            "M" => ErrorCorrectionLevel.M,
            "Q" => ErrorCorrectionLevel.Q,
            "H" => ErrorCorrectionLevel.H,
            _ => ErrorCorrectionLevel.L,
        };
    }
}