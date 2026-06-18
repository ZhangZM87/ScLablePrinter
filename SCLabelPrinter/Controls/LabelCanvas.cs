using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SCLabelPrinter.Core.Models;
using SCLabelPrinter.Core.Printing;
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
    private const int TableInnerElementContentPadding = 2;
    private static readonly ITsplTextPreviewLayoutPlanner TextPreviewLayoutPlanner = new TsplTextPreviewLayoutPlanner();
    private static readonly ITableCellInnerElementInteractionService TableCellInnerElementInteractionService = new TableCellInnerElementInteractionService();
    private static readonly ITableCellInnerElementVisualLayoutService TableCellInnerElementVisualLayoutService = new TableCellInnerElementVisualLayoutService();
    private static readonly ITableCellTextPreviewMetricsService TableCellTextPreviewMetricsService = new TableCellTextPreviewMetricsService();
    private readonly Brush _canvasBackdropBrush = new SolidColorBrush(Color.FromRgb(0xF3, 0xE7, 0xD4));
    private readonly Brush _workspaceBrush = new SolidColorBrush(Color.FromRgb(0xEC, 0xDE, 0xC8));
    private readonly Brush _backgroundBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFC));
    private readonly Brush _eraseBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFC));
    private readonly Pen _borderPen = new(new SolidColorBrush(Color.FromRgb(0xD9, 0xD2, 0xC3)), 1);
    private readonly Pen _workspaceBorderPen = new(new SolidColorBrush(Color.FromRgb(0xD0, 0xBF, 0xA5)), 1);
    private readonly Pen _workspaceGridPen = new(new SolidColorBrush(Color.FromArgb(0x40, 0xB0, 0xA8, 0x88)), 0.6)
    {
        DashStyle = new DashStyle(new double[] { 2, 4 }, 0),
    };
    private readonly Pen _paperBorderPen = new(new SolidColorBrush(Color.FromRgb(0xB7, 0xA7, 0x92)), 1.4);
    private readonly Pen _paperGuidePen = new(new SolidColorBrush(Color.FromArgb(0x82, 0xC7, 0xB8, 0xA2)), 0.9)
    {
        DashStyle = new DashStyle(new double[] { 4, 3 }, 0),
    };
    private readonly Brush _paperShadowBrush = new SolidColorBrush(Color.FromArgb(0x24, 0x2A, 0x1D, 0x12));
    private readonly Brush _foregroundBrush = Brushes.Black;
    private readonly Pen _selectionPen = new(new SolidColorBrush(Color.FromRgb(0x1D, 0x4E, 0x8E)), 1)
    {
        DashStyle = DashStyles.Dash
    };
    private readonly Brush _tableBackgroundBrush = new SolidColorBrush(Color.FromRgb(0xFC, 0xFC, 0xFF));
    private readonly Brush _tableAlternateRowBrush = new SolidColorBrush(Color.FromArgb(24, 0xEA, 0xEA, 0xF0));
    private readonly Pen _tableBorderPen = new(new SolidColorBrush(Color.FromRgb(0x25, 0x27, 0x2E)), 1.6)
    {
        DashStyle = new DashStyle(new double[] { 4, 2 }, 0),
        DashCap = PenLineCap.Flat,
    };
    private readonly Pen _tableGridPen = new(new SolidColorBrush(Color.FromRgb(0x9A, 0x9E, 0xAC)), 0.7)
    {
        DashStyle = new DashStyle(new double[] { 4, 2 }, 0),
        DashCap = PenLineCap.Flat,
    };
    private string? _draggingElementId;

    /// <summary>
    /// 框选起始点（屏幕坐标）。
    /// </summary>
    /// <summary>
    /// 正在拖拽缩放的普通元素 ID。
    /// </summary>
    private string? _resizingElementId;
    private Point _elementResizeStart;
    private int _elementResizeOriginalWidth;
    private int _elementResizeOriginalHeight;
    private int _elementResizeOriginalX;
    private int _elementResizeOriginalY;
    private int _elementResizeCorner; // 0=右下 1=右上 2=左下 3=左上
    private const double ResizeHandleSize = 8.0;
    private static readonly Brush _resizeHandleBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x4E, 0x8E));

    private Point? _marqueeStart;

    /// <summary>
    /// 框选当前点（屏幕坐标）。
    /// </summary>
    private Point _marqueeCurrent;

    /// <summary>
    /// 当前被多选中的元素 ID 集合。
    /// </summary>
    private readonly HashSet<string> _multiSelectedIds = new();

    /// <summary>
    /// 多选拖拽起始点。
    /// </summary>
    private Point? _multiDragStart;

    /// <summary>
    /// 多选拖拽时各元素的原始位置。
    /// </summary>
    private readonly Dictionary<string, (int X, int Y)> _multiDragOriginalPositions = new();

    /// <summary>
    /// 框选矩形画笔。
    /// </summary>
    private static readonly Pen _marqueePen = new(new SolidColorBrush(Color.FromRgb(0x33, 0x99, 0xFF)), 1.0)
    {
        DashStyle = DashStyles.Dash,
    };
    private static readonly Brush _marqueeFill = new SolidColorBrush(Color.FromArgb(0x20, 0x33, 0x99, 0xFF));
    private static readonly Pen _multiSelectPen = new(new SolidColorBrush(Color.FromRgb(0x33, 0x99, 0xFF)), 1.5)
    {
        DashStyle = DashStyles.Dash,
    };
    private Point _dragOffset;
    private TableCellInnerElementHit? _draggingCellInnerElement;
    private TableInteractionPoint _dragOffsetInner;
    private string? _selectedTableCellInnerElementId;
    private const double TableResizeHandleThreshold = 6.0;
    private const double TableInnerElementHandleSize = 8.0;
    private const double TableElementCornerHandleSize = 12.0;
    private TableResizeMode _tableResizeMode = TableResizeMode.None;
    private TableElement? _resizingTableElement;
    private int _resizingTableIndex;
    private int _resizingRowIndex;
    private bool _resizingOuterHandle;
    private Point _resizeStartPoint;
    private int _resizeOriginalSize;
    private int _resizeOriginalNextColumnWidth;
    private int _resizeOriginalRowHeight;
    private int _resizeOriginalNextRowHeight;
    private int _resizeOriginalTableX;
    private int _resizeOriginalTableY;
    private InnerElementResizeMode _innerElementResizeMode = InnerElementResizeMode.None;
    private TableCellInnerElementResizeHit? _resizingCellInnerElement;
    private Point _resizeStartPointInner;
    private int _resizeOriginalInnerWidth;
    private int _resizeOriginalInnerHeight;

    private sealed record TableCellInnerElementHit(TableElement Table, TableCell Cell, int Row, int Column, TableCellInnerElement InnerElement);
    private sealed record TableCellInnerElementResizeHit(TableElement Table, TableCell Cell, int Row, int Column, TableCellInnerElement InnerElement);
    private sealed record TableResizeHandleHit(TableElement Table, TableResizeMode Mode, int Index, int OriginalSize, bool IsOuterHandle = false);
    private enum InnerElementResizeMode
    {
        None,
        Resize,
    }

    private enum TableResizeMode
    {
        None,
        Column,
        Row,
        Overall,
    }

    public LabelCanvas()
    {
        Focusable = true;
    }

    public static readonly DependencyProperty TemplateProperty = DependencyProperty.Register(
        nameof(Template),
        typeof(LabelTemplateDocument),
        typeof(LabelCanvas),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty SelectedElementIdProperty = DependencyProperty.Register(
        nameof(SelectedElementId),
        typeof(string),
        typeof(LabelCanvas),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SelectedElementIdsProperty = DependencyProperty.Register(
        nameof(SelectedElementIds),
        typeof(IReadOnlySet<string>),
        typeof(LabelCanvas),
        new PropertyMetadata(null));

    /// <summary>
    /// 当前多选中的元素 ID 集合，供 ViewModel 绑定。
    /// </summary>
    public IReadOnlySet<string>? SelectedElementIds
    {
        get => (IReadOnlySet<string>?)GetValue(SelectedElementIdsProperty);
        set => SetValue(SelectedElementIdsProperty, value);
    }

    public static readonly DependencyProperty MultiElementMovedCommandProperty = DependencyProperty.Register(
        nameof(MultiElementMovedCommand),
        typeof(ICommand),
        typeof(LabelCanvas));

    /// <summary>
    /// 多选元素整体移动后的通知命令。
    /// </summary>
    public ICommand? MultiElementMovedCommand
    {
        get => (ICommand?)GetValue(MultiElementMovedCommandProperty);
        set => SetValue(MultiElementMovedCommandProperty, value);
    }

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

    public static readonly DependencyProperty TableCellInnerElementMovedCommandProperty = DependencyProperty.Register(
        nameof(TableCellInnerElementMovedCommand),
        typeof(ICommand),
        typeof(LabelCanvas),
        new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty TableCellResizeCommandProperty = DependencyProperty.Register(
        nameof(TableCellResizeCommand),
        typeof(ICommand),
        typeof(LabelCanvas),
        new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty IsPreviewOnlyProperty = DependencyProperty.Register(
        nameof(IsPreviewOnly),
        typeof(bool),
        typeof(LabelCanvas),
        new PropertyMetadata(false));

    /// <summary>
    /// 预览模式下禁止选中、拖拽、编辑等所有交互操作。
    /// </summary>
    public bool IsPreviewOnly
    {
        get => (bool)GetValue(IsPreviewOnlyProperty);
        set => SetValue(IsPreviewOnlyProperty, value);
    }

    public static readonly DependencyProperty ZoomFactorProperty = DependencyProperty.Register(
        nameof(ZoomFactor),
        typeof(double),
        typeof(LabelCanvas),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

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

    public ICommand? TableCellInnerElementMovedCommand
    {
        get => (ICommand?)GetValue(TableCellInnerElementMovedCommandProperty);
        set => SetValue(TableCellInnerElementMovedCommandProperty, value);
    }

    public ICommand? TableCellResizeCommand
    {
        get => (ICommand?)GetValue(TableCellResizeCommandProperty);
        set => SetValue(TableCellResizeCommandProperty, value);
    }

    public double ZoomFactor
    {
        get => (double)GetValue(ZoomFactorProperty);
        set => SetValue(ZoomFactorProperty, value);
    }

    /// <summary>
    /// 在控件表面绘制标签边界和全部元素预览。
    /// </summary>
    /// <summary>
    /// 获取普通元素的宽高（用于缩放手柄计算）。
    /// </summary>
    private static (int W, int H) GetElementSize(LabelElement el)
    {
        return el switch
        {
            LineElement l => (l.Width, l.Height),
            EraseElement e => (e.Width, e.Height),
            BitmapElement b => (b.Width, b.Height),
            BoxElement box => (box.EndX - box.X, box.EndY - box.Y),
            TableElement t => (t.TotalWidth, t.TotalHeight),
            BarcodeElement bc => (Math.Max(80, bc.Narrow * 60), bc.Height),
            QrCodeElement qr => (qr.CellWidth * 20, qr.CellWidth * 20),
            TextElement txt => (Math.Max(40, txt.XScale * 60), Math.Max(20, txt.YScale * 24)),
            _ => (40, 20),
        };
    }

    /// <summary>
    /// 设置普通元素的宽高。
    /// </summary>
    private static void SetElementSize(LabelElement el, int w, int h)
    {
        switch (el)
        {
            case LineElement l: l.Width = w; l.Height = Math.Max(1, h); break;
            case EraseElement e: e.Width = w; e.Height = h; break;
            case BitmapElement b: b.Width = w; b.Height = h; break;
            case BoxElement box: box.EndX = box.X + w; box.EndY = box.Y + h; break;
            case BarcodeElement bc: bc.Height = Math.Max(10, h); break;
            case QrCodeElement qr: qr.CellWidth = Math.Max(1, Math.Min(w, h) / 20); break;
            case TextElement txt: txt.XScale = Math.Max(1, w / 60); txt.YScale = Math.Max(1, h / 24); break;
        }
    }

    /// <summary>
    /// 检测鼠标是否命中普通元素的缩放手柄，返回命中的角索引（0-3）或 -1。
    /// </summary>
    private int HitTestElementResizeHandle(Point mouse, LabelElement el, double scale, Point origin)
    {
        var bounds = GetElementBounds(el, scale, origin);
        var hs = ResizeHandleSize;
        var corners = new[]
        {
            new Rect(bounds.Right - hs, bounds.Bottom - hs, hs, hs),
            new Rect(bounds.Right - hs, bounds.Top, hs, hs),
            new Rect(bounds.Left, bounds.Bottom - hs, hs, hs),
            new Rect(bounds.Left, bounds.Top, hs, hs),
        };
        for (var i = 0; i < corners.Length; i++)
        {
            if (corners[i].Contains(mouse)) return i;
        }
        return -1;
    }

    /// <summary>
    /// 绘制元素的四角缩放手柄。
    /// </summary>
    private void DrawElementResizeHandles(DrawingContext dc, LabelElement el, double scale, Point origin)
    {
        var bounds = GetElementBounds(el, scale, origin);
        var hs = ResizeHandleSize;
        var corners = new[]
        {
            new Rect(bounds.Right - hs, bounds.Bottom - hs, hs, hs),
            new Rect(bounds.Right - hs, bounds.Top, hs, hs),
            new Rect(bounds.Left, bounds.Bottom - hs, hs, hs),
            new Rect(bounds.Left, bounds.Top, hs, hs),
        };
        foreach (var corner in corners)
        {
            dc.DrawRectangle(_resizeHandleBrush, null, corner);
        }
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var bounds = new Rect(new Point(0, 0), RenderSize);
        drawingContext.DrawRectangle(_canvasBackdropBrush, null, bounds);

        if (Template is null)
        {
            return;
        }

        var surface = CreateSurfaceRect();
        DrawWorkspaceSurface(drawingContext, surface);

        var (scale, origin) = CalculateScale(surface, Template.Label, ZoomFactor);
        var labelRect = CreateLabelRect(Template.Label, scale, origin);
        DrawLabelSurface(drawingContext, labelRect);

        var clipRect = CreateLabelClipRect(labelRect);
        drawingContext.PushClip(new RectangleGeometry(clipRect, 12, 12));
        foreach (var element in Template.Elements)
        {
            DrawElement(drawingContext, element, scale, origin);
        }
        drawingContext.Pop();

        if (_multiSelectedIds.Count > 0 && Template is not null)
        {
            foreach (var id in _multiSelectedIds)
            {
                var el = Template.Elements.FirstOrDefault(x => x.Id == id);
                if (el is not null)
                {
                    var elBounds = GetElementBounds(el, scale, origin);
                    drawingContext.DrawRectangle(null, _multiSelectPen, elBounds);
                }
            }
        }

        if (_multiSelectedIds.Count == 0 && SelectedElementId is not null && Template is not null)
        {
            var selEl = Template.Elements.FirstOrDefault(x => x.Id == SelectedElementId);
            if (selEl is not null)
            {
                var selBounds = GetElementBounds(selEl, scale, origin);
                drawingContext.DrawRectangle(null, _selectionPen, selBounds);
                DrawElementResizeHandles(drawingContext, selEl, scale, origin);
            }
        }

        if (_marqueeStart is not null)
        {
            var marqueeRect = new Rect(_marqueeStart.Value, _marqueeCurrent);
            drawingContext.DrawRectangle(_marqueeFill, _marqueePen, marqueeRect);
        }
    }

    /// <summary>
    /// 开始鼠标拖动元素时进行选择并捕获鼠标。
    /// </summary>
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (Template is null || IsPreviewOnly)
        {
            return;
        }

        var mousePoint = e.GetPosition(this);
        var surface = CreateSurfaceRect();
        var (scale, origin) = CalculateScale(surface, Template.Label, ZoomFactor);
        var resizeHandle = GetHitTableResizeHandle(mousePoint, scale, origin);
        if (resizeHandle is not null)
        {
            _tableResizeMode = resizeHandle.Mode;
            _resizingTableElement = resizeHandle.Table;
            _resizingTableIndex = resizeHandle.Index;
            _resizingOuterHandle = resizeHandle.IsOuterHandle;
            _resizeOriginalSize = resizeHandle.OriginalSize;
            _resizeStartPoint = mousePoint;
            if (_tableResizeMode == TableResizeMode.Column)
            {
                _resizeOriginalNextColumnWidth = !_resizingOuterHandle && resizeHandle.Index + 1 < resizeHandle.Table.Cols
                    ? resizeHandle.Table.GetColumnWidth(resizeHandle.Index + 1)
                    : 0;
            }
            if (_tableResizeMode == TableResizeMode.Row || _tableResizeMode == TableResizeMode.Overall)
            {
                _resizingRowIndex = _tableResizeMode == TableResizeMode.Overall
                    ? resizeHandle.Table.Rows - 1
                    : resizeHandle.Index;
                _resizeOriginalRowHeight = resizeHandle.Table.GetRowHeight(_resizingRowIndex);
                _resizeOriginalNextRowHeight = _resizingRowIndex + 1 < resizeHandle.Table.Rows
                    ? resizeHandle.Table.GetRowHeight(_resizingRowIndex + 1)
                    : 0;
                if (_resizingOuterHandle && _tableResizeMode == TableResizeMode.Row && _resizingRowIndex == 0)
                {
                    _resizeOriginalTableY = resizeHandle.Table.Y;
                }
            }
            if (_tableResizeMode == TableResizeMode.Column && _resizingOuterHandle && resizeHandle.Index == 0)
            {
                _resizeOriginalTableX = resizeHandle.Table.X;
            }
            CaptureMouse();
            SelectedElementId = resizeHandle.Table.Id;
            e.Handled = true;
            return;
        }

        var innerResizeHit = GetHitTableCellInnerElementResizeHandle(mousePoint, scale, origin);
        if (innerResizeHit is not null)
        {
            _innerElementResizeMode = InnerElementResizeMode.Resize;
            _resizingCellInnerElement = innerResizeHit;
            _resizeStartPointInner = mousePoint;
            _resizeOriginalInnerWidth = innerResizeHit.InnerElement.Width;
            _resizeOriginalInnerHeight = innerResizeHit.InnerElement.Height;
            _selectedTableCellInnerElementId = innerResizeHit.InnerElement.Id;
            CaptureMouse();
            SelectedElementId = innerResizeHit.Table.Id;
            e.Handled = true;
            return;
        }

        if (SelectedElementId is not null)
        {
            var selForResize = Template.Elements.FirstOrDefault(x => x.Id == SelectedElementId);
            if (selForResize is not null && selForResize is not TableElement)
            {
                var corner = HitTestElementResizeHandle(mousePoint, selForResize, scale, origin);
                if (corner >= 0)
                {
                    var sz = GetElementSize(selForResize);
                    _resizingElementId = selForResize.Id;
                    _elementResizeCorner = corner;
                    _elementResizeStart = mousePoint;
                    _elementResizeOriginalWidth = sz.W;
                    _elementResizeOriginalHeight = sz.H;
                    _elementResizeOriginalX = selForResize.X;
                    _elementResizeOriginalY = selForResize.Y;
                    CaptureMouse();
                    e.Handled = true;
                    return;
                }
            }
        }

        var hitInner = GetHitTableCellInnerElement(mousePoint, scale, origin);
        if (hitInner is not null)
        {
            _draggingCellInnerElement = hitInner;
            var pointerPosition = GetPointerPositionInCell(mousePoint, hitInner.Table, hitInner.Row, hitInner.Column, scale, origin);
            _dragOffsetInner = TableCellInnerElementInteractionService.CaptureDragOffset(
                hitInner.Table,
                hitInner.Row,
                hitInner.Column,
                hitInner.InnerElement,
                pointerPosition);
            _selectedTableCellInnerElementId = hitInner.InnerElement.Id;
            CaptureMouse();
            SelectedElementId = hitInner.Table.Id;
            e.Handled = true;
            return;
        }

        var hitElement = GetHitElement(mousePoint, scale, origin);
        if (hitElement is null)
        {
            _marqueeStart = mousePoint;
            _marqueeCurrent = mousePoint;
            _multiSelectedIds.Clear();
            CaptureMouse();
            e.Handled = true;
            return;
        }

        if (_multiSelectedIds.Count > 1 && _multiSelectedIds.Contains(hitElement.Id))
        {
            _multiDragStart = mousePoint;
            _multiDragOriginalPositions.Clear();
            foreach (var id in _multiSelectedIds)
            {
                var el = Template.Elements.FirstOrDefault(x => x.Id == id);
                if (el is not null)
                {
                    _multiDragOriginalPositions[id] = (el.X, el.Y);
                }
            }
            CaptureMouse();
            SelectedElementId = hitElement.Id;
            e.Handled = true;
            return;
        }

        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
        {
            if (_multiSelectedIds.Contains(hitElement.Id))
            {
                _multiSelectedIds.Remove(hitElement.Id);
            }
            else
            {
                _multiSelectedIds.Add(hitElement.Id);
            }
            SelectedElementIds = new HashSet<string>(_multiSelectedIds);
            SelectedElementId = hitElement.Id;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        _multiSelectedIds.Clear();
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

        if (Template is null || IsPreviewOnly)
        {
            return;
        }

        var mousePoint = e.GetPosition(this);
        var surface = CreateSurfaceRect();
        var (scale, origin) = CalculateScale(surface, Template.Label, ZoomFactor);
        var hitInner = GetHitTableCellInnerElement(mousePoint, scale, origin);
        if (hitInner is not null)
        {
            SelectedElementId = hitInner.Table.Id;
            var innerContextMenu = new ContextMenu
            {
                PlacementTarget = this,
                Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint,
            };

            innerContextMenu.Items.Add(CreateMenuItem("编辑单元格内部元素", TableCellContextMenuAction.EditCellInnerElement, hitInner.Table, hitInner.Row, hitInner.Column));
            innerContextMenu.Items.Add(CreateMenuItem("删除单元格内部元素", TableCellContextMenuAction.RemoveCellInnerElement, hitInner.Table, hitInner.Row, hitInner.Column));
            innerContextMenu.IsOpen = true;
            e.Handled = true;
            return;
        }

        var hitElement = GetHitElement(mousePoint, scale, origin);
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
        var contextMenu = new ContextMenu
        {
            PlacementTarget = this,
            Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint,
        };

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
        contextMenu.Items.Add(CreateMenuItem("添加单元格文本元素", TableCellContextMenuAction.AddCellTextElement, tableElement, cell.Value.Row, cell.Value.Column));
        contextMenu.Items.Add(CreateMenuItem("添加单元格条码元素", TableCellContextMenuAction.AddCellBarcodeElement, tableElement, cell.Value.Row, cell.Value.Column));
        contextMenu.Items.Add(CreateMenuItem("添加单元格二维码元素", TableCellContextMenuAction.AddCellQrCodeElement, tableElement, cell.Value.Row, cell.Value.Column));

        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(CreateMenuItem("向右合并单元格", TableCellContextMenuAction.MergeCellRight, tableElement, cell.Value.Row, cell.Value.Column));
        contextMenu.Items.Add(CreateMenuItem("向下合并单元格", TableCellContextMenuAction.MergeCellDown, tableElement, cell.Value.Row, cell.Value.Column));
        contextMenu.Items.Add(CreateMenuItem("拆分合并", TableCellContextMenuAction.UnmergeCell, tableElement, cell.Value.Row, cell.Value.Column));

        contextMenu.IsOpen = true;

        e.Handled = true;
    }

    private MenuItem CreateMenuItem(string header, TableCellContextMenuAction action, TableElement table, int row, int column)
    {
        var request = new TableCellContextMenuRequest
        {
            TableElementId = table.Id,
            Row = row,
            Column = column,
            Action = action,
        };

        var item = new MenuItem
        {
            Header = header,
            Command = TableCellContextMenuCommand,
            CommandParameter = request,
            CommandTarget = this,
        };

        return item;
    }

    private static (int Row, int Column)? GetTableCellFromPoint(TableElement table, Point point, double scale, Point origin)
    {
        var left = origin.X + table.X * scale;
        var top = origin.Y + table.Y * scale;
        var totalWidth = table.ColumnWidths.Sum() * scale;
        var totalHeight = table.TotalHeight * scale;
        var localX = point.X - left;
        var localY = point.Y - top;

        if (localX < 0 || localY < 0 || localX > totalWidth || localY > totalHeight)
        {
            return null;
        }

        var row = 0;
        var rowAccumulated = 0.0;
        for (var rowIndex = 0; rowIndex < table.Rows; rowIndex++)
        {
            rowAccumulated += table.GetRowHeight(rowIndex) * scale;
            if (localY <= rowAccumulated)
            {
                row = rowIndex;
                break;
            }
        }
        var column = 0;
        var columnAccumulated = 0.0;
        foreach (var width in table.ColumnWidths)
        {
            columnAccumulated += width * scale;
            if (localX <= columnAccumulated)
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

    private TableResizeHandleHit? GetHitTableResizeHandle(Point point, double scale, Point origin)
    {
        foreach (var table in Template?.Elements.OfType<TableElement>() ?? Enumerable.Empty<TableElement>())
        {
            var left = origin.X + table.X * scale;
            var top = origin.Y + table.Y * scale;
            var totalWidth = table.ColumnWidths.Sum() * scale;
            var totalHeight = table.TotalHeight * scale;
            var right = left + totalWidth;
            var bottom = top + totalHeight;

            if (point.X < left - TableResizeHandleThreshold || point.X > right + TableResizeHandleThreshold || point.Y < top - TableResizeHandleThreshold || point.Y > bottom + TableResizeHandleThreshold)
            {
                continue;
            }

            var columnLeft = left;
            for (var columnIndex = 0; columnIndex < table.Cols - 1; columnIndex++)
            {
                columnLeft += table.GetColumnWidth(columnIndex) * scale;
                if (Math.Abs(point.X - columnLeft) <= TableResizeHandleThreshold && point.Y >= top && point.Y <= bottom)
                {
                    return new TableResizeHandleHit(table, TableResizeMode.Column, columnIndex, table.GetColumnWidth(columnIndex));
                }
            }

            if (Math.Abs(point.X - left) <= TableResizeHandleThreshold && point.Y >= top && point.Y <= bottom)
            {
                return new TableResizeHandleHit(table, TableResizeMode.Column, 0, table.GetColumnWidth(0), true);
            }

            if (Math.Abs(point.X - right) <= TableResizeHandleThreshold && point.Y >= top && point.Y <= bottom)
            {
                return new TableResizeHandleHit(table, TableResizeMode.Column, table.Cols - 1, table.GetColumnWidth(table.Cols - 1), true);
            }

            var rowTop = top;
            for (var rowIndex = 1; rowIndex < table.Rows; rowIndex++)
            {
                rowTop += table.GetRowHeight(rowIndex - 1) * scale;
                if (Math.Abs(point.Y - rowTop) <= TableResizeHandleThreshold && point.X >= left && point.X <= right)
                {
                    return new TableResizeHandleHit(table, TableResizeMode.Row, rowIndex - 1, table.GetRowHeight(rowIndex - 1));
                }
            }

            if (Math.Abs(point.Y - top) <= TableResizeHandleThreshold && point.X >= left && point.X <= right)
            {
                return new TableResizeHandleHit(table, TableResizeMode.Row, 0, table.GetRowHeight(0), true);
            }

            if (Math.Abs(point.Y - bottom) <= TableResizeHandleThreshold && point.X >= left && point.X <= right)
            {
                return new TableResizeHandleHit(table, TableResizeMode.Row, table.Rows - 1, table.GetRowHeight(table.Rows - 1), true);
            }

            var cornerHandleRect = new Rect(right - TableElementCornerHandleSize, bottom - TableElementCornerHandleSize, TableElementCornerHandleSize, TableElementCornerHandleSize);
            if (cornerHandleRect.Contains(point))
            {
                return new TableResizeHandleHit(table, TableResizeMode.Overall, table.Cols - 1, table.GetColumnWidth(table.Cols - 1));
            }
        }

        return null;
    }

    /// <summary>
    /// 鼠标移动时更新拖动元素位置。
    /// </summary>
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!IsMouseCaptured || Template is null)
        {
            return;
        }

        var mousePoint = e.GetPosition(this);
        var surface = CreateSurfaceRect();
        var (scale, origin) = CalculateScale(surface, Template.Label, ZoomFactor);

        if (_marqueeStart is not null)
        {
            _marqueeCurrent = mousePoint;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_multiDragStart is not null && _multiDragOriginalPositions.Count > 0)
        {
            var deltaX = (int)Math.Round((mousePoint.X - _multiDragStart.Value.X) / scale);
            var deltaY = (int)Math.Round((mousePoint.Y - _multiDragStart.Value.Y) / scale);
            foreach (var kvp in _multiDragOriginalPositions)
            {
                var el = Template.Elements.FirstOrDefault(x => x.Id == kvp.Key);
                if (el is not null)
                {
                    el.X = Math.Max(0, kvp.Value.X + deltaX);
                    el.Y = Math.Max(0, kvp.Value.Y + deltaY);
                }
            }
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_resizingElementId is not null && Template is not null)
        {
            var resEl = Template.Elements.FirstOrDefault(x => x.Id == _resizingElementId);
            if (resEl is not null)
            {
                var dx = (int)Math.Round((mousePoint.X - _elementResizeStart.X) / scale);
                var dy = (int)Math.Round((mousePoint.Y - _elementResizeStart.Y) / scale);
                var newW = _elementResizeOriginalWidth;
                var newH = _elementResizeOriginalHeight;
                var newX = _elementResizeOriginalX;
                var newY = _elementResizeOriginalY;
                switch (_elementResizeCorner)
                {
                    case 0: newW += dx; newH += dy; break;
                    case 1: newW += dx; newH -= dy; newY += dy; break;
                    case 2: newW -= dx; newH += dy; newX += dx; break;
                    case 3: newW -= dx; newH -= dy; newX += dx; newY += dy; break;
                }
                newW = Math.Max(10, newW);
                newH = Math.Max(10, newH);
                newX = Math.Max(0, newX);
                newY = Math.Max(0, newY);
                resEl.X = newX;
                resEl.Y = newY;
                SetElementSize(resEl, newW, newH);
                InvalidateVisual();
                e.Handled = true;
                return;
            }
        }

        if (_innerElementResizeMode != InnerElementResizeMode.None && _resizingCellInnerElement is not null)
        {
            var deltaX = mousePoint.X - _resizeStartPointInner.X;
            var deltaY = mousePoint.Y - _resizeStartPointInner.Y;
            var newWidth = _resizeOriginalInnerWidth + (int)Math.Round(deltaX / scale);
            var newHeight = _resizeOriginalInnerHeight + (int)Math.Round(deltaY / scale);
            var clampedSize = TableCellLayoutCalculator.ClampInnerElementSize(
                _resizingCellInnerElement.Table,
                _resizingCellInnerElement.Row,
                _resizingCellInnerElement.Column,
                _resizingCellInnerElement.InnerElement.X,
                _resizingCellInnerElement.InnerElement.Y,
                newWidth,
                newHeight);
            newWidth = clampedSize.Width;
            newHeight = clampedSize.Height;

            _resizingCellInnerElement.InnerElement.Width = newWidth;
            _resizingCellInnerElement.InnerElement.Height = newHeight;
            _selectedTableCellInnerElementId = _resizingCellInnerElement.InnerElement.Id;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_tableResizeMode != TableResizeMode.None && _resizingTableElement is not null)
        {
            var deltaX = mousePoint.X - _resizeStartPoint.X;
            var deltaY = mousePoint.Y - _resizeStartPoint.Y;
            var deltaWidth = (int)Math.Round(deltaX / scale);
            var deltaHeight = (int)Math.Round(deltaY / scale);

            if (_tableResizeMode == TableResizeMode.Column)
            {
                var columnIndexToResize = _resizingTableIndex >= 0 ? _resizingTableIndex : 0;
                if (columnIndexToResize >= 0 && columnIndexToResize < _resizingTableElement.ColumnWidths.Count)
                {
                    if (_resizingOuterHandle && columnIndexToResize == 0)
                    {
                        var newWidth = Math.Max(20, _resizeOriginalSize - deltaWidth);
                        var shift = _resizeOriginalSize - newWidth;
                        _resizingTableElement.ColumnWidths[columnIndexToResize] = newWidth;
                        _resizingTableElement.X = Math.Max(0, _resizeOriginalTableX + shift);
                    }
                    else
                    {
                        _resizingTableElement.ColumnWidths[columnIndexToResize] = Math.Max(20, _resizeOriginalSize + deltaWidth);
                    }
                }
            }
            else if (_tableResizeMode == TableResizeMode.Row)
            {
                var rowIndexToResize = _resizingRowIndex >= 0 ? _resizingRowIndex : 0;
                if (rowIndexToResize >= 0 && rowIndexToResize < _resizingTableElement.RowHeights.Count)
                {
                    if (_resizingOuterHandle && rowIndexToResize == 0)
                    {
                        var newHeight = Math.Max(20, _resizeOriginalRowHeight - deltaHeight);
                        var shift = _resizeOriginalRowHeight - newHeight;
                        _resizingTableElement.RowHeights[rowIndexToResize] = newHeight;
                        _resizingTableElement.Y = Math.Max(0, _resizeOriginalTableY + shift);
                    }
                    else
                    {
                        _resizingTableElement.RowHeights[rowIndexToResize] = Math.Max(20, _resizeOriginalRowHeight + deltaHeight);
                    }
                }
            }
            else if (_tableResizeMode == TableResizeMode.Overall)
            {
                var targetColumnIndex = _resizingTableIndex >= 0 ? _resizingTableIndex : _resizingTableElement.Cols - 1;
                if (targetColumnIndex >= 0 && targetColumnIndex < _resizingTableElement.ColumnWidths.Count)
                {
                    _resizingTableElement.ColumnWidths[targetColumnIndex] = Math.Max(20, _resizeOriginalSize + deltaWidth);
                }

                var targetRowIndex = _resizingRowIndex >= 0 ? _resizingRowIndex : _resizingTableElement.Rows - 1;
                if (targetRowIndex >= 0 && targetRowIndex < _resizingTableElement.RowHeights.Count)
                {
                    _resizingTableElement.RowHeights[targetRowIndex] = Math.Max(20, _resizeOriginalRowHeight + deltaHeight);
                }
            }

            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_draggingCellInnerElement is not null)
        {
            var pointerPosition = GetPointerPositionInCell(mousePoint, _draggingCellInnerElement.Table, _draggingCellInnerElement.Row, _draggingCellInnerElement.Column, scale, origin);
            var clampedPosition = TableCellInnerElementInteractionService.ResolveDragPosition(
                _draggingCellInnerElement.Table,
                _draggingCellInnerElement.Row,
                _draggingCellInnerElement.Column,
                _draggingCellInnerElement.InnerElement,
                pointerPosition,
                _dragOffsetInner);

            _draggingCellInnerElement.InnerElement.X = clampedPosition.X;
            _draggingCellInnerElement.InnerElement.Y = clampedPosition.Y;
            _selectedTableCellInnerElementId = _draggingCellInnerElement.InnerElement.Id;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_draggingElementId is null)
        {
            return;
        }

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

        if (IsMouseCaptured)
        {
            if (_marqueeStart is not null)
            {
                var surface = CreateSurfaceRect();
                var (scale, origin) = CalculateScale(surface, Template.Label, ZoomFactor);
                var marqueeRect = new Rect(_marqueeStart.Value, _marqueeCurrent);
                _multiSelectedIds.Clear();
                foreach (var element in Template.Elements)
                {
                    var bounds = GetElementBounds(element, scale, origin);
                    if (marqueeRect.IntersectsWith(bounds))
                    {
                        _multiSelectedIds.Add(element.Id);
                    }
                }
                _marqueeStart = null;
                SelectedElementIds = new HashSet<string>(_multiSelectedIds);
                if (_multiSelectedIds.Count == 1)
                {
                    SelectedElementId = _multiSelectedIds.First();
                }
                ReleaseMouseCapture();
                InvalidateVisual();
                e.Handled = true;
                return;
            }

            if (_multiDragStart is not null)
            {
                var cmd = MultiElementMovedCommand;
                if (cmd is not null)
                {
                    var moves = _multiDragOriginalPositions.Keys
                        .Select(id => Template.Elements.FirstOrDefault(x => x.Id == id))
                        .Where(el => el is not null)
                        .Select(el => new ElementMoveRequest(el!.Id, el.X, el.Y))
                        .ToList();
                    if (cmd.CanExecute(moves))
                    {
                        cmd.Execute(moves);
                    }
                }
                _multiDragStart = null;
                _multiDragOriginalPositions.Clear();
                ReleaseMouseCapture();
                InvalidateVisual();
                e.Handled = true;
                return;
            }

            if (_resizingElementId is not null)
            {
                var resEl = Template?.Elements.FirstOrDefault(x => x.Id == _resizingElementId);
                if (resEl is not null)
                {
                    var sz = GetElementSize(resEl);
                    var moveCmd = ElementMovedCommand;
                    if (moveCmd is not null)
                    {
                        var req = new ElementMoveRequest(resEl.Id, resEl.X, resEl.Y);
                        if (moveCmd.CanExecute(req)) moveCmd.Execute(req);
                    }
                }
                _resizingElementId = null;
                ReleaseMouseCapture();
                InvalidateVisual();
                e.Handled = true;
                return;
            }

            if (_tableResizeMode != TableResizeMode.None && _resizingTableElement is not null)
            {
                var resizeCommand = TableCellResizeCommand;
                if (resizeCommand is not null)
                {
                    if (_tableResizeMode == TableResizeMode.Overall)
                    {
                        var targetColumnIndex = _resizingTableIndex >= 0 ? _resizingTableIndex : _resizingTableElement.Cols - 1;
                        var targetRowIndex = _resizingRowIndex >= 0 ? _resizingRowIndex : _resizingTableElement.Rows - 1;
                        var overallRequests = TableCellResizeRequestFactory.CreateOverallRequests(_resizingTableElement, targetColumnIndex, targetRowIndex);

                        foreach (var request in overallRequests)
                        {
                            if (resizeCommand.CanExecute(request))
                            {
                                resizeCommand.Execute(request);
                            }
                        }
                    }
                    else
                    {
                        var request = _tableResizeMode == TableResizeMode.Column
                            ? TableCellResizeRequestFactory.CreateColumnRequest(_resizingTableElement, _resizingTableIndex)
                            : TableCellResizeRequestFactory.CreateRowRequest(_resizingTableElement, _resizingRowIndex);

                        if (resizeCommand.CanExecute(request))
                        {
                            resizeCommand.Execute(request);
                        }
                    }
                }
            }

            ReleaseMouseCapture();
            if (_draggingCellInnerElement is not null)
            {
                var innerMoveRequest = new TableCellInnerElementMoveRequest(
                    _draggingCellInnerElement.Table.Id,
                    _draggingCellInnerElement.Row,
                    _draggingCellInnerElement.Column,
                    _draggingCellInnerElement.InnerElement.Id,
                    _draggingCellInnerElement.InnerElement.X,
                    _draggingCellInnerElement.InnerElement.Y,
                    _draggingCellInnerElement.InnerElement.Width,
                    _draggingCellInnerElement.InnerElement.Height);
                var innerMovedCommand = TableCellInnerElementMovedCommand;
                if (innerMovedCommand is not null && innerMovedCommand.CanExecute(innerMoveRequest))
                {
                    innerMovedCommand.Execute(innerMoveRequest);
                }
            }
            else if (_resizingCellInnerElement is not null)
            {
                var innerMoveRequest = new TableCellInnerElementMoveRequest(
                    _resizingCellInnerElement.Table.Id,
                    _resizingCellInnerElement.Row,
                    _resizingCellInnerElement.Column,
                    _resizingCellInnerElement.InnerElement.Id,
                    _resizingCellInnerElement.InnerElement.X,
                    _resizingCellInnerElement.InnerElement.Y,
                    _resizingCellInnerElement.InnerElement.Width,
                    _resizingCellInnerElement.InnerElement.Height);
                var innerMovedCommand = TableCellInnerElementMovedCommand;
                if (innerMovedCommand is not null && innerMovedCommand.CanExecute(innerMoveRequest))
                {
                    innerMovedCommand.Execute(innerMoveRequest);
                }
            }

            _draggingElementId = null;
            _draggingCellInnerElement = null;
            _tableResizeMode = TableResizeMode.None;
            _resizingTableElement = null;
            _innerElementResizeMode = InnerElementResizeMode.None;
            _resizingCellInnerElement = null;
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
        _draggingCellInnerElement = null;
        _tableResizeMode = TableResizeMode.None;
        _resizingTableElement = null;
        _innerElementResizeMode = InnerElementResizeMode.None;
        _resizingCellInnerElement = null;
    }

    /// <summary>
    /// 测量控件希望占据的默认空间。
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Template is null)
        {
            return new Size(
                double.IsInfinity(availableSize.Width) ? 560 : availableSize.Width,
                double.IsInfinity(availableSize.Height) ? 420 : availableSize.Height);
        }

        var widthDots = Template.Label.Unit == LabelUnit.Millimeter ? Template.Label.Width * DotsPerMillimeter : Template.Label.Width;
        var heightDots = Template.Label.Unit == LabelUnit.Millimeter ? Template.Label.Height * DotsPerMillimeter : Template.Label.Height;
        var desiredWidth = Math.Max(0, widthDots * ZoomFactor + 40);
        var desiredHeight = Math.Max(0, heightDots * ZoomFactor + 40);

        return new Size(desiredWidth, desiredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        return DesiredSize;
    }

    /// <summary>
    /// 创建标签可视化表面的边距矩形。
    /// </summary>
    private Rect CreateSurfaceRect()
    {
        return new Rect(20, 20, Math.Max(0, RenderSize.Width - 40), Math.Max(0, RenderSize.Height - 40));
    }

    /// <summary>
    /// 绘制标签预览外层的工作区背景和边框。
    /// </summary>
    private void DrawWorkspaceSurface(DrawingContext drawingContext, Rect surface)
    {
        drawingContext.DrawRoundedRectangle(_workspaceBrush, _workspaceBorderPen, surface, 18, 18);

        const double gridSpacing = 24.0;
        for (var x = surface.X + gridSpacing; x < surface.Right; x += gridSpacing)
        {
            drawingContext.DrawLine(_workspaceGridPen, new Point(x, surface.Y), new Point(x, surface.Bottom));
        }

        for (var y = surface.Y + gridSpacing; y < surface.Bottom; y += gridSpacing)
        {
            drawingContext.DrawLine(_workspaceGridPen, new Point(surface.X, y), new Point(surface.Right, y));
        }
    }

    /// <summary>
    /// 绘制真实标签纸张边界，增强编辑态的纸张感和可视化边界。
    /// </summary>
    private void DrawLabelSurface(DrawingContext drawingContext, Rect labelRect)
    {
        if (labelRect.Width <= 0 || labelRect.Height <= 0)
        {
            return;
        }

        var shadowRect = new Rect(labelRect.X + 6, labelRect.Y + 6, labelRect.Width, labelRect.Height);
        drawingContext.DrawRoundedRectangle(_paperShadowBrush, null, shadowRect, 14, 14);
        drawingContext.DrawRoundedRectangle(_backgroundBrush, _paperBorderPen, labelRect, 14, 14);

        var inset = Math.Min(12, Math.Min(labelRect.Width, labelRect.Height) / 8);
        if (inset > 4)
        {
            var guideRect = Rect.Inflate(labelRect, -inset, -inset);
            drawingContext.DrawRoundedRectangle(null, _paperGuidePen, guideRect, 10, 10);
        }
    }

    /// <summary>
    /// 根据实际标签尺寸计算纸张在预览控件中的可视区域。
    /// </summary>
    private static Rect CreateLabelRect(LabelDefinition definition, double scale, Point origin)
    {
        var widthDots = definition.Unit == LabelUnit.Millimeter ? definition.Width * DotsPerMillimeter : definition.Width;
        var heightDots = definition.Unit == LabelUnit.Millimeter ? definition.Height * DotsPerMillimeter : definition.Height;
        return new Rect(origin.X, origin.Y, Math.Max(1, widthDots * scale), Math.Max(1, heightDots * scale));
    }

    /// <summary>
    /// 生成用于裁剪内容的标签内边界，避免图形元素超出纸张边框。
    /// </summary>
    private static Rect CreateLabelClipRect(Rect labelRect)
    {
        return new Rect(
            labelRect.X,
            labelRect.Y,
            Math.Max(1, labelRect.Width),
            Math.Max(1, labelRect.Height));
    }

    /// <summary>
    /// 根据标签尺寸计算绘制缩放比例和原点位置。
    /// </summary>
    private static (double scale, Point origin) CalculateScale(Rect surface, LabelDefinition definition, double zoomFactor)
    {
        var widthDots = definition.Unit == LabelUnit.Millimeter ? definition.Width * DotsPerMillimeter : definition.Width;
        var heightDots = definition.Unit == LabelUnit.Millimeter ? definition.Height * DotsPerMillimeter : definition.Height;

        if (widthDots <= 0 || heightDots <= 0)
        {
            return (zoomFactor, surface.Location);
        }

        return (zoomFactor, surface.Location);
    }

    private int GetMaxDragCoordinate(LabelElement element, LabelDefinition definition, double scale, bool isHorizontal)
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
            case BitmapElement bitmapElement:
                DrawBitmapElement(drawingContext, bitmapElement, scale, origin);
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

    private Rect GetElementHitBounds(LabelElement element, double scale, Point origin)
    {
        var bounds = GetElementBounds(element, scale, origin);
        bounds.Inflate(HitTestPadding, HitTestPadding);
        return bounds;
    }

    /// <summary>
    /// 将表格坐标系中的单元格边界转换为画布坐标，统一单元格绘制、命中和拖动的基准。
    /// </summary>
    private static Rect GetTableCellScreenBounds(TableElement table, int rowIndex, int columnIndex, double scale, Point origin)
    {
        var bounds = TableCellLayoutCalculator.GetCellBounds(table, rowIndex, columnIndex);
        return ConvertTableBoundsToScreenRect(table, bounds, scale, origin);
    }

    /// <summary>
    /// 将表格坐标系中的内部元素边界转换为画布坐标，不考虑元素旋转。
    /// </summary>
    private Rect GetTableCellInnerElementScreenRect(TableElement table, int rowIndex, int columnIndex, TableCellInnerElement innerElement, double scale, Point origin)
    {
        var layout = GetTableCellInnerElementVisualLayout(table, rowIndex, columnIndex, innerElement);
        return ConvertTableBoundsToScreenRect(table, layout.FrameBounds, scale, origin);
    }

    /// <summary>
    /// 将表格内部元素内容区域转换为画布坐标，保证文本和图像都使用同一内容边界。
    /// </summary>
    private Rect GetTableCellInnerElementContentScreenRect(TableElement table, int rowIndex, int columnIndex, TableCellInnerElement innerElement, double scale, Point origin)
    {
        var layout = GetTableCellInnerElementVisualLayout(table, rowIndex, columnIndex, innerElement);
        return ConvertTableBoundsToScreenRect(table, layout.ContentBounds, scale, origin);
    }

    /// <summary>
    /// 为表格内部元素创建屏幕空间几何体，包含旋转信息。
    /// </summary>
    private Geometry GetTableCellInnerElementScreenGeometry(TableElement table, int rowIndex, int columnIndex, TableCellInnerElement innerElement, double scale, Point origin)
    {
        var rect = GetTableCellInnerElementScreenRect(table, rowIndex, columnIndex, innerElement, scale, origin);
        var geometry = new RectangleGeometry(rect);
        if (innerElement.Rotation % 360 != 0)
        {
            var rotationAnchor = GetTableCellInnerElementScreenRotationAnchor(table, rowIndex, columnIndex, innerElement, scale, origin);
            geometry.Transform = new RotateTransform(innerElement.Rotation % 360, rotationAnchor.X, rotationAnchor.Y);
        }

        return geometry;
    }

    /// <summary>
    /// 获取表格内部元素在屏幕上的旋转锚点，保证绘制与命中测试使用相同的旋转中心。
    /// </summary>
    private Point GetTableCellInnerElementScreenRotationAnchor(TableElement table, int rowIndex, int columnIndex, TableCellInnerElement innerElement, double scale, Point origin)
    {
        var layout = GetTableCellInnerElementVisualLayout(table, rowIndex, columnIndex, innerElement);
        return ConvertTablePointToScreenPoint(table, layout.RotationAnchor, scale, origin);
    }

    /// <summary>
    /// 创建表格内部元素在表格坐标系中的统一视觉布局，供绘制和命中共享。
    /// </summary>
    private static TableCellInnerElementVisualLayout GetTableCellInnerElementVisualLayout(TableElement table, int rowIndex, int columnIndex, TableCellInnerElement innerElement)
    {
        return TableCellInnerElementVisualLayoutService.CreateLayout(table, rowIndex, columnIndex, innerElement, TableInnerElementContentPadding);
    }

    /// <summary>
    /// 将表格坐标系中的矩形边界映射到画布坐标，统一单元格和内部元素的显示基准。
    /// </summary>
    private static Rect ConvertTableBoundsToScreenRect(TableElement table, TableCellBounds bounds, double scale, Point origin)
    {
        return new Rect(
            origin.X + (table.X + bounds.X) * scale,
            origin.Y + (table.Y + bounds.Y) * scale,
            Math.Max(1, bounds.Width * scale),
            Math.Max(1, bounds.Height * scale));
    }

    /// <summary>
    /// 将表格坐标系中的点映射到画布坐标，保证旋转锚点与边界换算保持一致。
    /// </summary>
    private static Point ConvertTablePointToScreenPoint(TableElement table, TableInteractionPoint point, double scale, Point origin)
    {
        return new Point(
            origin.X + (table.X + point.X) * scale,
            origin.Y + (table.Y + point.Y) * scale);
    }

    /// <summary>
    /// 将画布屏幕坐标转换为表格局部坐标，统一内部元素拖拽与缩放的输入坐标系。
    /// </summary>
    private static TableInteractionPoint GetPointerPositionInCell(Point point, TableElement table, int rowIndex, int columnIndex, double scale, Point origin)
    {
        var pointerX = (point.X - origin.X) / scale - table.X;
        var pointerY = (point.Y - origin.Y) / scale - table.Y;
        var cellBounds = TableCellLayoutCalculator.GetCellBounds(table, rowIndex, columnIndex);
        return new TableInteractionPoint(pointerX - cellBounds.X, pointerY - cellBounds.Y);
    }


    private TableCellInnerElementHit? GetHitTableCellInnerElement(Point point, double scale, Point origin)
    {
        if (Template is null)
        {
            return null;
        }

        foreach (var tableElement in Template.Elements.OfType<TableElement>())
        {
            var tableLeft = origin.X + tableElement.X * scale;
            var tableTop = origin.Y + tableElement.Y * scale;
            var totalWidth = tableElement.ColumnWidths.Sum() * scale;
            var totalHeight = tableElement.TotalHeight * scale;
            var localX = point.X - tableLeft;
            var localY = point.Y - tableTop;

            if (localX < 0 || localY < 0 || localX > totalWidth || localY > totalHeight)
            {
                continue;
            }

            for (var rowIndex = 0; rowIndex < tableElement.Rows; rowIndex++)
            {
                for (var colIndex = 0; colIndex < tableElement.Cols; colIndex++)
                {
                    var cellIndex = rowIndex * tableElement.Cols + colIndex;
                    if (cellIndex >= tableElement.Cells.Count)
                    {
                        continue;
                    }

                    var cell = tableElement.Cells[cellIndex];

                    foreach (var inner in cell.InnerElements)
                    {
                        var geometry = GetTableCellInnerElementScreenGeometry(tableElement, rowIndex, colIndex, inner, scale, origin);
                        if (geometry.FillContains(point))
                        {
                            return new TableCellInnerElementHit(tableElement, cell, rowIndex, colIndex, inner);
                        }

                        var paddedRect = GetTableCellInnerElementScreenRect(tableElement, rowIndex, colIndex, inner, scale, origin);
                        paddedRect.Inflate(HitTestPadding, HitTestPadding);
                        var paddedGeometry = new RectangleGeometry(paddedRect);
                        if (inner.Rotation % 360 != 0)
                        {
                            var rotationAnchor = GetTableCellInnerElementScreenRotationAnchor(tableElement, rowIndex, colIndex, inner, scale, origin);
                            paddedGeometry.Transform = new RotateTransform(inner.Rotation % 360, rotationAnchor.X, rotationAnchor.Y);
                        }

                        if (paddedGeometry.FillContains(point))
                        {
                            return new TableCellInnerElementHit(tableElement, cell, rowIndex, colIndex, inner);
                        }
                    }
                }
            }
        }

        return null;
    }

    private TableCellInnerElementResizeHit? GetHitTableCellInnerElementResizeHandle(Point point, double scale, Point origin)
    {
        if (Template is null)
        {
            return null;
        }

        foreach (var tableElement in Template.Elements.OfType<TableElement>())
        {
            var tableLeft = origin.X + tableElement.X * scale;
            var tableTop = origin.Y + tableElement.Y * scale;
            var totalWidth = tableElement.ColumnWidths.Sum() * scale;
            var totalHeight = tableElement.TotalHeight * scale;
            var localX = point.X - tableLeft;
            var localY = point.Y - tableTop;

            if (localX < 0 || localY < 0 || localX > totalWidth || localY > totalHeight)
            {
                continue;
            }

            for (var rowIndex = 0; rowIndex < tableElement.Rows; rowIndex++)
            {
                for (var colIndex = 0; colIndex < tableElement.Cols; colIndex++)
                {
                    var cellIndex = rowIndex * tableElement.Cols + colIndex;
                    if (cellIndex >= tableElement.Cells.Count)
                    {
                        continue;
                    }

                    var cell = tableElement.Cells[cellIndex];

                    foreach (var inner in cell.InnerElements)
                    {
                        var innerRect = GetTableCellInnerElementScreenRect(tableElement, rowIndex, colIndex, inner, scale, origin);
                        var handleRect = new Rect(
                            innerRect.Right - TableInnerElementHandleSize,
                            innerRect.Bottom - TableInnerElementHandleSize,
                            TableInnerElementHandleSize * 2,
                            TableInnerElementHandleSize * 2);
                        var handleGeometry = new RectangleGeometry(handleRect);
                        if (inner.Rotation % 360 != 0)
                        {
                            var rotationAnchor = GetTableCellInnerElementScreenRotationAnchor(tableElement, rowIndex, colIndex, inner, scale, origin);
                            handleGeometry.Transform = new RotateTransform(inner.Rotation % 360, rotationAnchor.X, rotationAnchor.Y);
                        }

                        if (handleGeometry.FillContains(point))
                        {
                            return new TableCellInnerElementResizeHit(tableElement, cell, rowIndex, colIndex, inner);
                        }
                    }
                }
            }
        }

        return null;
    }

    private LabelElement? GetHitElement(Point point, double scale, Point origin)
    {
        if (Template is null)
        {
            return null;
        }

        var candidates = Template.Elements
            .Select((element, index) => new
            {
                Element = element,
                Index = index,
                Bounds = GetElementHitBounds(element, scale, origin),
            })
            .Where(item => item.Bounds.Contains(point))
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates
            .OrderBy(item => item.Bounds.Width * item.Bounds.Height)
            .ThenByDescending(item => item.Index)
            .First().Element;
    }

    private Rect GetElementBounds(LabelElement element, double scale, Point origin)
    {
        return element switch
        {
            TextElement textElement => GetTextBounds(textElement, scale, origin),
            BarcodeElement barcodeElement => GetBarcodeBounds(barcodeElement, scale, origin),
            QrCodeElement qrCodeElement => GetQrBounds(qrCodeElement, scale, origin),
            BoxElement boxElement => GetBoxBounds(boxElement, scale, origin),
            LineElement lineElement => new Rect(origin.X + lineElement.X * scale, origin.Y + lineElement.Y * scale, Math.Max(1, lineElement.Width * scale), Math.Max(1, lineElement.Height * scale)),
            EraseElement eraseElement => new Rect(origin.X + eraseElement.X * scale, origin.Y + eraseElement.Y * scale, Math.Max(1, eraseElement.Width * scale), Math.Max(1, eraseElement.Height * scale)),
            BitmapElement bitmapElement => new Rect(origin.X + bitmapElement.X * scale, origin.Y + bitmapElement.Y * scale, Math.Max(1, bitmapElement.Width * scale), Math.Max(1, bitmapElement.Height * scale)),
            TableElement tableElement => new Rect(origin.X + tableElement.X * scale, origin.Y + tableElement.Y * scale, Math.Max(1, tableElement.TotalWidth * scale), Math.Max(1, tableElement.TotalHeight * scale)),
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

    private Rect GetTextBounds(TextElement element, double scale, Point origin)
    {
        if (Template is null)
        {
            return new Rect(origin.X + element.X * scale, origin.Y + element.Y * scale, 1, 1);
        }

        var layout = TextPreviewLayoutPlanner.Plan(Template.Label, element);
        var anchor = new Point(origin.X + element.X * scale, origin.Y + element.Y * scale);
        var fontSize = Math.Max(8, layout.FontSizeDots * scale);
        var maxWidth = Math.Max(1, layout.MaxWidthDots * scale - 2);
        var maxHeight = Math.Max(fontSize * 1.1, layout.MaxHeightDots * scale - 2);
        var typeface = new Typeface("Microsoft YaHei UI");
        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var formattedText = new FormattedText(
            string.IsNullOrEmpty(element.Content) ? " " : element.Content,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            _foregroundBrush,
            pixelsPerDip)
        {
            MaxTextWidth = maxWidth,
            MaxTextHeight = maxHeight,
            Trimming = TextTrimming.CharacterEllipsis,
        };

        return new Rect(
            anchor.X,
            anchor.Y,
            Math.Max(1, formattedText.Width),
            Math.Max(1, formattedText.Height));
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
    /// 绘制文本元素预览 — 所见即所得，不换行不裁剪，按 TSPL 实际尺寸渲染。
    /// </summary>
    private void DrawTextElement(DrawingContext drawingContext, TextElement element, double scale, Point origin)
    {
        if (Template is null)
        {
            return;
        }

        var (charWidth, charHeight) = GetTsplFontDotSize(element.Font);
        var xMul = Math.Max(1, element.XScale);
        var yMul = Math.Max(1, element.YScale);
        var dotW = charWidth * xMul * scale;
        var dotH = charHeight * yMul * scale;
        var anchor = new Point(origin.X + element.X * scale, origin.Y + element.Y * scale);
        var content = string.IsNullOrEmpty(element.Content) ? " " : element.Content;
        var isChinese = IsCjkFont(element.Font);
        var typeface = new Typeface(isChinese ? "SimSun" : "Consolas");
        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var fontSize = dotH * 0.85;

        var formattedText = new FormattedText(
            content,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            typeface,
            Math.Max(6, fontSize),
            _foregroundBrush,
            pixelsPerDip);

        var expectedWidth = content.Length * dotW;
        if (isChinese)
        {
            var cjkCount = 0;
            var asciiCount = 0;
            foreach (var ch in content)
            {
                if (ch > 127) cjkCount++; else asciiCount++;
            }
            expectedWidth = (cjkCount * dotW + asciiCount * dotW * 0.5);
        }

        DrawWithRotation(drawingContext, element.Rotation, anchor, () =>
        {
            var actualWidth = formattedText.WidthIncludingTrailingWhitespace;
            var scaleX = actualWidth > 0.1 ? expectedWidth / actualWidth : 1.0;
            var scaleY = formattedText.Height > 0.1 ? dotH / formattedText.Height : 1.0;
            var transform = new ScaleTransform(scaleX, scaleY, anchor.X, anchor.Y);
            drawingContext.PushTransform(transform);
            drawingContext.DrawText(formattedText, anchor);
            drawingContext.Pop();
        });
    }

    private static (double Width, double Height) GetTsplFontDotSize(string font)
    {
        return font switch
        {
            "1" => (8, 12),
            "2" => (12, 20),
            "3" => (16, 24),
            "4" => (24, 32),
            "5" => (32, 48),
            "6" => (14, 19),
            "7" => (21, 27),
            "8" => (14, 25),
            "9" => (9, 17),
            "10" => (12, 24),
            "TSS16.BF2" => (16, 16),
            "TSS20.BF2" => (20, 20),
            "TST24.BF2" => (24, 24),
            "TSS24.BF2" => (24, 24),
            "K" => (24, 24),
            "TSS32.BF2" => (32, 32),
            _ => (12, 20),
        };
    }

    private static bool IsCjkFont(string font)
    {
        return font is "TSS16.BF2" or "TSS20.BF2" or "TST24.BF2" or "TSS24.BF2" or "K" or "TSS32.BF2";
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
    /// 绘制位图元素预览。
    /// </summary>
    private void DrawBitmapElement(DrawingContext drawingContext, BitmapElement element, double scale, Point origin)
    {
        var startX = origin.X + element.X * scale;
        var startY = origin.Y + element.Y * scale;
        var width = Math.Max(1, element.Width * scale);
        var height = Math.Max(1, element.Height * scale);
        var image = CreateBitmapPreview(element);
        var imageRect = new Rect(startX, startY, width, height);

        DrawWithRotation(drawingContext, element.Rotation, new Point(startX, startY), () =>
        {
            drawingContext.DrawImage(image, imageRect);
        });
    }

    /// <summary>
    /// 绘制表格元素预览。
    /// </summary>
    private void DrawTableElement(DrawingContext drawingContext, TableElement element, double scale, Point origin)
    {
        var left = origin.X + element.X * scale;
        var top = origin.Y + element.Y * scale;
        var totalWidth = Math.Max(1, element.ColumnWidths.Sum() * scale);
        var totalHeight = Math.Max(1, element.TotalHeight * scale);
        var tableRect = new Rect(left, top, totalWidth, totalHeight);
        var borderPen = element.BorderStyle == TableLineStyle.Dashed ? _tableBorderPen : new Pen(_tableBorderPen.Brush, _tableBorderPen.Thickness);

        drawingContext.DrawRectangle(_tableBackgroundBrush, borderPen, tableRect);

        for (var rowIndex = 0; rowIndex < element.Rows; rowIndex++)
        {
            var currentRowHeight = element.GetRowHeight(rowIndex) * scale;
            if (rowIndex % 2 == 1)
            {
                var altRowOffset = element.GetRowHeights().Take(rowIndex).Sum() * scale;
                var rowRect = new Rect(left, top + altRowOffset, totalWidth, currentRowHeight);
                drawingContext.DrawRectangle(_tableAlternateRowBrush, null, rowRect);
            }
        }

        var gridPen = element.GridStyle == TableLineStyle.Dashed ? _tableGridPen : new Pen(_tableGridPen.Brush, _tableGridPen.Thickness);
        var currentX = left;
        for (var colIndex = 1; colIndex < element.Cols; colIndex++)
        {
            currentX += element.GetColumnWidth(colIndex - 1) * scale;
            var colLineY = top;
            for (var row = 0; row < element.Rows; row++)
            {
                var rowH = element.GetRowHeight(row) * scale;
                var skipSegment = false;
                // 只有确认存在 ColSpan 跨越此列边界时才跳过竖线
                for (var scanCol = colIndex - 1; scanCol >= 0; scanCol--)
                {
                    var scanIdx = row * element.Cols + scanCol;
                    if (scanIdx >= 0 && scanIdx < element.Cells.Count)
                    {
                        var scanCell = element.Cells[scanIdx];
                        if (!scanCell.IsMerged && scanCell.ColSpan > 1)
                        {
                            if (scanCol + scanCell.ColSpan > colIndex) skipSegment = true;
                            break;
                        }
                        if (!scanCell.IsMerged) break;
                    }
                }
                if (!skipSegment)
                {
                    drawingContext.DrawLine(gridPen, new Point(currentX, colLineY), new Point(currentX, colLineY + rowH));
                }
                colLineY += rowH;
            }
        }

        var rowOffset = 0.0;
        for (var rowIndex = 0; rowIndex < element.Rows - 1; rowIndex++)
        {
            rowOffset += element.GetRowHeight(rowIndex) * scale;
            var rowLineX = left;
            for (var col = 0; col < element.Cols; col++)
            {
                var colW = element.GetColumnWidth(col) * scale;
                var skipSegment = false;
                // 只有确认存在 RowSpan 跨越此行边界时才跳过横线
                for (var scanRow = rowIndex; scanRow >= 0; scanRow--)
                {
                    var scanIdx = scanRow * element.Cols + col;
                    if (scanIdx >= 0 && scanIdx < element.Cells.Count)
                    {
                        var scanCell = element.Cells[scanIdx];
                        if (!scanCell.IsMerged && scanCell.RowSpan > 1)
                        {
                            if (scanRow + scanCell.RowSpan > rowIndex + 1) skipSegment = true;
                            break;
                        }
                        if (!scanCell.IsMerged) break;
                    }
                }
                if (!skipSegment)
                {
                    drawingContext.DrawLine(gridPen, new Point(rowLineX, top + rowOffset), new Point(rowLineX + colW, top + rowOffset));
                }
                rowLineX += colW;
            }
        }

        if (element.Id == SelectedElementId)
        {
            var handleRect = new Rect(
                left + totalWidth - TableElementCornerHandleSize,
                top + totalHeight - TableElementCornerHandleSize,
                TableElementCornerHandleSize,
                TableElementCornerHandleSize);
            drawingContext.DrawRectangle(_backgroundBrush, _selectionPen, handleRect);
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
                if (cell.IsMerged) continue;
                var mergedBounds = TableCellLayoutCalculator.GetMergedCellBounds(element, rowIndex, colIndex);
                if (cell.ColSpan > 1 || cell.RowSpan > 1)
                {
                    var mergeFillRect = new Rect(
                        origin.X + (element.X + mergedBounds.X) * scale + 1,
                        origin.Y + (element.Y + mergedBounds.Y) * scale + 1,
                        Math.Max(1, mergedBounds.Width * scale - 2),
                        Math.Max(1, mergedBounds.Height * scale - 2));
                    drawingContext.DrawRectangle(_tableBackgroundBrush, null, mergeFillRect);
                }
                var cellBounds = new Rect(
                    origin.X + (element.X + mergedBounds.X) * scale,
                    origin.Y + (element.Y + mergedBounds.Y) * scale,
                    Math.Max(1, mergedBounds.Width * scale),
                    Math.Max(1, mergedBounds.Height * scale));
                var cellLeft = cellBounds.X;
                var cellTop = cellBounds.Y;
                var cellWidth = cellBounds.Width;
                var cellHeight = cellBounds.Height;

                if (cell.InnerElements?.Count > 0)
                {
                    DrawTableCellInnerElements(drawingContext, element, cell, rowIndex, colIndex, scale, origin);
                }
                else
                {
                    switch (cell.ContentType)
                    {
                        case TableCellContentType.Text:
                            {
                                var contentRect = new Rect(cellLeft + 8, cellTop + 6, Math.Max(1, cellWidth - 16), Math.Max(1, cellHeight - 12));
                                var cellFontSize = Math.Max(8, 14 * scale);
                                var formattedText = new FormattedText(
                                    cell.Content,
                                    CultureInfo.CurrentUICulture,
                                    FlowDirection.LeftToRight,
                                    new Typeface("Microsoft YaHei UI"),
                                    cellFontSize,
                                    _foregroundBrush,
                                    VisualTreeHelper.GetDpi(this).PixelsPerDip)
                                {
                                    MaxTextWidth = contentRect.Width,
                                    MaxTextHeight = contentRect.Height,
                                    TextAlignment = cell.Alignment switch
                                    {
                                        LabelTextAlignment.Center => TextAlignment.Center,
                                        LabelTextAlignment.Right => TextAlignment.Right,
                                        _ => TextAlignment.Left,
                                    },
                                };
                                var textY = contentRect.Y + (contentRect.Height - formattedText.Height) / 2;
                                drawingContext.DrawText(formattedText, new Point(contentRect.X, Math.Max(contentRect.Y, textY)));
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
    }

    private void DrawTableCellInnerElements(DrawingContext drawingContext, TableElement table, TableCell cell, int rowIndex, int columnIndex, double scale, Point origin)
    {
        var cellBounds = GetTableCellScreenBounds(table, rowIndex, columnIndex, scale, origin);
        var cellLeft = cellBounds.X;
        var cellTop = cellBounds.Y;
        var cellWidth = cellBounds.Width;
        var cellHeight = cellBounds.Height;
        var cellClip = new Rect(cellLeft, cellTop, cellWidth, cellHeight);
        drawingContext.PushClip(new RectangleGeometry(cellClip));

        foreach (var innerElement in cell.InnerElements)
        {
            var innerRect = GetTableCellInnerElementScreenRect(table, rowIndex, columnIndex, innerElement, scale, origin);
            var contentRect = GetTableCellInnerElementContentScreenRect(table, rowIndex, columnIndex, innerElement, scale, origin);
            var rotationAnchor = GetTableCellInnerElementScreenRotationAnchor(table, rowIndex, columnIndex, innerElement, scale, origin);
            var startX = innerRect.X;
            var startY = innerRect.Y;
            var innerWidth = innerRect.Width;
            var innerHeight = innerRect.Height;

            DrawWithRotation(drawingContext, innerElement.Rotation, rotationAnchor, () =>
            {
                var selectionRect = innerRect;
                if (_selectedTableCellInnerElementId == innerElement.Id)
                {
                    drawingContext.DrawRectangle(null, _selectionPen, innerRect);
                }

                switch (innerElement)
                {
                    case TableCellTextElement textElement:
                        {
                            var fontSize = Math.Max(8, TableCellTextPreviewMetricsService.GetPreviewFontSize(textElement.Font, textElement.YScale) * scale);
                            var xScale = Math.Max(1, textElement.XScale);
                            var minimumTextHeight = TableCellTextPreviewMetricsService.GetMinimumContentHeight(textElement.Font, textElement.YScale) * scale;
                            var formattedText = new FormattedText(
                                string.IsNullOrEmpty(textElement.Content) ? " " : textElement.Content,
                                CultureInfo.CurrentUICulture,
                                FlowDirection.LeftToRight,
                                new Typeface("Microsoft YaHei UI"),
                                fontSize,
                                _foregroundBrush,
                                VisualTreeHelper.GetDpi(this).PixelsPerDip)
                            {
                                MaxTextWidth = Math.Max(1, contentRect.Width / xScale),
                                MaxTextHeight = Math.Max(contentRect.Height, minimumTextHeight),
                                Trimming = TextTrimming.CharacterEllipsis,
                                TextAlignment = TextAlignment.Left,
                            };

                            var renderedWidth = Math.Max(1, Math.Min(contentRect.Width, formattedText.Width * xScale));
                            var renderedHeight = Math.Max(1, Math.Min(contentRect.Height, formattedText.Height));
                            var textX = contentRect.X + Math.Max(0, (contentRect.Width - renderedWidth) / 2);
                            var textY = contentRect.Y + Math.Max(0, (contentRect.Height - renderedHeight) / 2);

                            drawingContext.PushClip(new RectangleGeometry(contentRect));

                            if (xScale != 1)
                            {
                                var transformGroup = new TransformGroup();
                                transformGroup.Children.Add(new ScaleTransform(xScale, 1));
                                transformGroup.Children.Add(new TranslateTransform(textX, textY));
                                drawingContext.PushTransform(transformGroup);
                                drawingContext.DrawText(formattedText, new Point(0, 0));
                                drawingContext.Pop();
                            }
                            else
                            {
                                drawingContext.DrawText(formattedText, new Point(textX, textY));
                            }

                            drawingContext.Pop();
                            break;
                        }
                    case TableCellBarcodeElement barcodeElement:
                        {
                            var availableWidth = Math.Max(1, innerWidth - 8);
                            var availableHeight = Math.Max(1, innerHeight - 8);
                            var desiredHeight = Math.Min(availableHeight, 48 * scale);
                            var barcodeImage = CreateBarcodeImage(MapBarcodeFormat(barcodeElement.BarcodeType), string.IsNullOrWhiteSpace(barcodeElement.Content) ? " " : barcodeElement.Content, (int)Math.Max(1, availableWidth), (int)Math.Max(1, desiredHeight));
                            var imageWidth = Math.Min(availableWidth, barcodeImage.PixelWidth);
                            var imageHeight = Math.Min(availableHeight, barcodeImage.PixelHeight);
                            var drawX = startX + 4 + (availableWidth - imageWidth) / 2;
                            var drawY = startY + 4 + (availableHeight - imageHeight) / 2;

                            drawingContext.DrawImage(barcodeImage, new Rect(drawX, drawY, imageWidth, imageHeight));
                            break;
                        }
                    case TableCellQrCodeElement qrCodeElement:
                        {
                            var availableSize = Math.Min(innerWidth - 8, innerHeight - 8);
                            var qrSize = Math.Max(1, (int)Math.Min(availableSize, 120));
                            var qrImage = CreateQrCodeImage(string.IsNullOrWhiteSpace(qrCodeElement.Content) ? " " : qrCodeElement.Content, qrSize, qrCodeElement.ErrorCorrectionLevel);
                            var drawX = startX + 4 + (innerWidth - 8 - qrSize) / 2;
                            var drawY = startY + 4 + (innerHeight - 8 - qrSize) / 2;

                            drawingContext.DrawImage(qrImage, new Rect(drawX, drawY, qrSize, qrSize));
                            break;
                        }
                }

                if (_selectedTableCellInnerElementId == innerElement.Id)
                {
                    var handleRect = new Rect(
                        selectionRect.Right - TableInnerElementHandleSize,
                        selectionRect.Bottom - TableInnerElementHandleSize,
                        TableInnerElementHandleSize,
                        TableInnerElementHandleSize);
                    drawingContext.DrawRectangle(_backgroundBrush, _selectionPen, handleRect);
                }
            });
        }

        drawingContext.Pop();
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
    private static string[] WrapTextLines(string text, double maxWidth, Typeface typeface, double fontSize, double pixelsPerDip, int maxLines)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0)
        {
            return new[] { string.Empty };
        }

        var lines = new List<string>();
        var currentLine = string.Empty;
        var words = text.Split(' ');

        foreach (var word in words)
        {
            var candidate = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            var formatted = new FormattedText(
                candidate,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.Black,
                pixelsPerDip);

            if (formatted.Width <= maxWidth)
            {
                currentLine = candidate;
                continue;
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
                currentLine = word;
            }
            else
            {
                lines.AddRange(BreakWordToLines(word, maxWidth, typeface, fontSize, pixelsPerDip));
                currentLine = string.Empty;
            }

            if (lines.Count >= maxLines)
            {
                break;
            }
        }

        if (!string.IsNullOrEmpty(currentLine) && lines.Count < maxLines)
        {
            lines.Add(currentLine);
        }

        if (lines.Count == 0)
        {
            lines.Add(string.Empty);
        }

        if (lines.Count > maxLines)
        {
            lines = lines.Take(maxLines).ToList();
        }

        if (lines.Count == maxLines)
        {
            lines[^1] = TruncateLineToWidth(lines[^1], maxWidth, typeface, fontSize, pixelsPerDip);
        }

        return lines.ToArray();
    }

    private static string TruncateLineToWidth(string text, double maxWidth, Typeface typeface, double fontSize, double pixelsPerDip)
    {
        const string ellipsis = "...";
        var candidate = text;
        while (!string.IsNullOrEmpty(candidate))
        {
            var formatted = new FormattedText(
                candidate + ellipsis,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.Black,
                pixelsPerDip);

            if (formatted.Width <= maxWidth)
            {
                return candidate + ellipsis;
            }

            candidate = candidate[..^1];
        }

        return ellipsis;
    }

    private static IEnumerable<string> BreakWordToLines(string word, double maxWidth, Typeface typeface, double fontSize, double pixelsPerDip)
    {
        var lines = new List<string>();
        var builder = string.Empty;

        foreach (var ch in word)
        {
            var candidate = builder + ch;
            var formatted = new FormattedText(
                candidate,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.Black,
                pixelsPerDip);

            if (formatted.Width > maxWidth && builder.Length > 0)
            {
                lines.Add(builder);
                builder = ch.ToString();
                continue;
            }

            builder = candidate;
        }

        if (!string.IsNullOrEmpty(builder))
        {
            lines.Add(builder);
        }

        return lines;
    }

    private static BitmapSource CreateBitmapPreview(BitmapElement element)
    {
        var width = Math.Max(1, element.Width);
        var height = Math.Max(1, element.Height);
        var bytesPerRow = Math.Max(1, (width + 7) / 8);
        var pixels = new byte[width * height * 4];

        for (var row = 0; row < height; row++)
        {
            for (var col = 0; col < width; col++)
            {
                var byteIndex = row * bytesPerRow + (col / 8);
                var bitMask = (byte)(0x80 >> (col % 8));
                var isBlack = byteIndex < element.Data.Length && (element.Data[byteIndex] & bitMask) != 0;
                var pixelIndex = (row * width + col) * 4;
                if (isBlack)
                {
                    pixels[pixelIndex] = 0x00;
                    pixels[pixelIndex + 1] = 0x00;
                    pixels[pixelIndex + 2] = 0x00;
                    pixels[pixelIndex + 3] = 0xFF;
                }
                else
                {
                    pixels[pixelIndex] = 0xFF;
                    pixels[pixelIndex + 1] = 0xFF;
                    pixels[pixelIndex + 2] = 0xFF;
                    pixels[pixelIndex + 3] = 0xFF;
                }
            }
        }

        var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
        bitmap.Freeze();
        return bitmap;
    }

    /// <summary>
    /// 估算条码在预览画布上的实际宽度，尽量贴近打印时的占位尺寸。
    /// </summary>
    private static double EstimateBarcodeWidth(BarcodeElement element)
    {
        var contentLength = Math.Max(1, element.Content.Length);
        return element.CodeType switch
        {
            // Code39: start(13) + data*13 + stop(13) modules, each module = narrow
            BarcodeType.Code39 => (26 + contentLength * 13) * element.Narrow,
            // Code128: start(11) + data*11 + checksum(11) + stop(13) modules
            BarcodeType.Code128 => (35 + contentLength * 11) * element.Narrow,
            // EAN13 固定宽度: 95 modules, narrow=1 时约 95 dots
            BarcodeType.Ean13 => 95 * element.Narrow,
            _ => (35 + contentLength * 11) * element.Narrow,
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