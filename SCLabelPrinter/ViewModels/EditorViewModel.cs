using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCLabelPrinter.Core.Models;
using SCLabelPrinter.Core.Printing;
using SCLabelPrinter.Core.Printers;
using SCLabelPrinter.Core.Serialization;
using SCLabelPrinter.Core.Storage;
using SCLabelPrinter.Services;
using SCLabelPrinter.Views;

namespace SCLabelPrinter.ViewModels;

/// <summary>
/// 提供标签模板编辑、预览、保存和打印功能。
/// </summary>
public partial class EditorViewModel : ObservableObject
{
    private readonly ILabelTemplateStorageService _storageService;
    private readonly IPrinterService _printerService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IUserNotificationService _notificationService;
    private readonly StatusCenter _statusCenter;
    private readonly TsplGenerator _tsplGenerator;
    private readonly LabelTemplateSerializer _serializer;
    private readonly Stack<string> _undoStack = new();
    private readonly Stack<string> _redoStack = new();
    private IAsyncRelayCommand<TableCellContextMenuRequest?>? _tableCellContextMenuCommand;
    private IRelayCommand<TableCellInnerElementMoveRequest?>? _tableCellInnerElementMoveCommand;
    private IRelayCommand<TableCellResizeRequest?>? _tableCellResizeCommand;
    private bool _isApplyingSnapshot;

    public IAsyncRelayCommand<TableCellContextMenuRequest?> TableCellContextMenuCommand => _tableCellContextMenuCommand ??= new AsyncRelayCommand<TableCellContextMenuRequest?>(HandleTableCellContextMenuAsync);

    public IRelayCommand<TableCellInnerElementMoveRequest?> TableCellInnerElementMoveCommand => _tableCellInnerElementMoveCommand ??= new RelayCommand<TableCellInnerElementMoveRequest?>(MoveSelectedCellInnerElement);

    public IRelayCommand<TableCellResizeRequest?> TableCellResizeCommand => _tableCellResizeCommand ??= new RelayCommand<TableCellResizeRequest?>(HandleTableCellResize);

    /// <summary>
    /// 创建标签编辑器视图模型。
    /// </summary>
    public EditorViewModel(ILabelTemplateStorageService storageService, IPrinterService printerService, IFileDialogService fileDialogService, IUserNotificationService notificationService, StatusCenter statusCenter, TsplGenerator tsplGenerator, LabelTemplateSerializer serializer)
    {
        _storageService = storageService;
        _printerService = printerService;
        _fileDialogService = fileDialogService;
        _notificationService = notificationService;
        _statusCenter = statusCenter;
        _tsplGenerator = tsplGenerator;
        _serializer = serializer;

        ResetDocument(false);
    }

    public ObservableCollection<LabelElement> Elements { get; } = [];

    public ObservableCollection<string> RecentFiles { get; } = [];

    [ObservableProperty]
    private LabelTemplateDocument previewTemplate = new();

    [ObservableProperty]
    private string tsplPreview = string.Empty;

    [ObservableProperty]
    private string? currentFilePath;

    [ObservableProperty]
    private double labelWidth = 60;

    [ObservableProperty]
    private double labelHeight = 40;

    [ObservableProperty]
    private double labelGap = 2;

    [ObservableProperty]
    private double previewZoom = 2.0;

    [ObservableProperty]
    private int density = 8;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveSelectedElementCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApplySelectedElementChangesCommand))]
    private LabelElement? selectedElement;

    [ObservableProperty]
    private string? selectedElementId;

    [ObservableProperty]
    private int elementX;

    [ObservableProperty]
    private int elementY;

    [ObservableProperty]
    private int elementRotation;

    [ObservableProperty]
    private string selectedTextContent = string.Empty;

    [ObservableProperty]
    private string selectedFont = "3";

    [ObservableProperty]
    private string selectedBarcodeContent = string.Empty;

    [ObservableProperty]
    private BarcodeType selectedBarcodeType = BarcodeType.Code128;

    [ObservableProperty]
    private int selectedBarcodeHeight = 80;

    [ObservableProperty]
    private bool selectedBarcodeReadable = true;

    [ObservableProperty]
    private int selectedBarcodeNarrow = 2;

    [ObservableProperty]
    private int selectedBarcodeWide = 2;

    [ObservableProperty]
    private string selectedQrContent = string.Empty;

    [ObservableProperty]
    private string selectedQrErrorCorrectionLevel = "L";

    [ObservableProperty]
    private int selectedQrCellWidth = 5;

    [ObservableProperty]
    private string selectedQrMode = "A";

    [ObservableProperty]
    private int selectedBoxEndX = 200;

    [ObservableProperty]
    private int selectedTableRowHeight = 100;

    [ObservableProperty]
    private int selectedTableColumnWidthA = 260;

    [ObservableProperty]
    private int selectedTableColumnWidthB = 260;

    [ObservableProperty]
    private TableCellContentType selectedTableCell11Type = TableCellContentType.Text;

    [ObservableProperty]
    private string selectedTableCell11Content = string.Empty;

    [ObservableProperty]
    private TableCellContentType selectedTableCell12Type = TableCellContentType.Text;

    [ObservableProperty]
    private string selectedTableCell12Content = string.Empty;

    [ObservableProperty]
    private TableCellContentType selectedTableCell21Type = TableCellContentType.Text;

    [ObservableProperty]
    private string selectedTableCell21Content = string.Empty;

    [ObservableProperty]
    private TableCellContentType selectedTableCell22Type = TableCellContentType.Text;

    [ObservableProperty]
    private string selectedTableCell22Content = string.Empty;

    [ObservableProperty]
    private int selectedBoxEndY = 120;

    [ObservableProperty]
    private int selectedBoxThickness = 2;

    [ObservableProperty]
    private int selectedLineWidth = 240;

    [ObservableProperty]
    private int selectedLineHeight = 4;

    [ObservableProperty]
    private int selectedEraseWidth = 120;

    [ObservableProperty]
    private int selectedEraseHeight = 20;

    [ObservableProperty]
    private string editorHint = "从左侧工具栏添加元素，然后在右侧属性面板调整内容。";

    public bool IsTextElementSelected => SelectedElement is TextElement;

    public bool IsBarcodeElementSelected => SelectedElement is BarcodeElement;

    public bool IsQrElementSelected => SelectedElement is QrCodeElement;

    public bool IsBoxElementSelected => SelectedElement is BoxElement;

    public bool IsLineElementSelected => SelectedElement is LineElement;

    public bool IsEraseElementSelected => SelectedElement is EraseElement;

    /// <summary>
    /// 处理标签尺寸或打印浓度更新后的预览刷新。
    /// </summary>
    partial void OnLabelWidthChanged(double value)
    {
        RefreshPreview();
    }

    /// <summary>
    /// 处理标签高度更新后的预览刷新。
    /// </summary>
    partial void OnLabelHeightChanged(double value)
    {
        RefreshPreview();
    }

    /// <summary>
    /// 处理标签间距更新后的预览刷新。
    /// </summary>
    partial void OnLabelGapChanged(double value)
    {
        RefreshPreview();
    }

    /// <summary>
    /// 处理打印浓度更新后的预览刷新。
    /// </summary>
    partial void OnDensityChanged(int value)
    {
        RefreshPreview();
    }

    /// <summary>
    /// 当选中元素变化时同步右侧属性面板。
    /// </summary>
    partial void OnSelectedElementChanged(LabelElement? value)
    {
        SelectedElementId = value?.Id;
        LoadSelectedElementEditor(value);
        OnPropertyChanged(nameof(IsTextElementSelected));
        OnPropertyChanged(nameof(IsBarcodeElementSelected));
        OnPropertyChanged(nameof(IsQrElementSelected));
        OnPropertyChanged(nameof(IsBoxElementSelected));
        OnPropertyChanged(nameof(IsLineElementSelected));
        OnPropertyChanged(nameof(IsEraseElementSelected));
    }

    partial void OnSelectedElementIdChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            SelectedElement = null;
            return;
        }

        var element = Elements.FirstOrDefault(e => e.Id == value);
        if (element is not null)
        {
            SelectedElement = element;
        }
    }

    /// <summary>
    /// 新建一个空白标签模板。
    /// </summary>
    [RelayCommand]
    private void NewTemplate()
    {
        CaptureUndoSnapshot();
        ResetDocument(true);
    }

    [RelayCommand]
    private void ZoomIn()
    {
        PreviewZoom = Math.Min(4.0, PreviewZoom + 0.2);
    }

    [RelayCommand]
    private void ZoomOut()
    {
        PreviewZoom = Math.Max(0.5, PreviewZoom - 0.2);
    }

    [RelayCommand]
    private void ResetZoom()
    {
        PreviewZoom = 2.0;
    }

    /// <summary>
    /// 打开已有的标签模板文件。
    /// </summary>
    [RelayCommand]
    private async Task OpenTemplateAsync()
    {
        if (!_fileDialogService.TryOpenFile("标签模板|*.sclabel|所有文件|*.*", out var path))
        {
            return;
        }

        try
        {
            CaptureUndoSnapshot();
            var template = await _storageService.LoadAsync(path);
            ApplyTemplate(template, path, true);
            _statusCenter.SetActivityMessage($"已打开模板 {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"打开模板失败: {ex.Message}");
            _statusCenter.SetActivityMessage("打开模板失败");
        }
    }

    /// <summary>
    /// 保存当前模板到现有路径或新路径。
    /// </summary>
    [RelayCommand]
    private async Task SaveTemplateAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentFilePath))
        {
            await SaveTemplateAsAsync();
            return;
        }

        try
        {
            await _storageService.SaveAsync(CurrentFilePath, BuildTemplateSnapshot());
            AddRecentFile(CurrentFilePath);
            _statusCenter.SetActivityMessage($"模板已保存到 {Path.GetFileName(CurrentFilePath)}");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"保存模板失败: {ex.Message}");
            _statusCenter.SetActivityMessage("保存模板失败");
        }
    }

    /// <summary>
    /// 另存当前模板到新的路径。
    /// </summary>
    [RelayCommand]
    private async Task SaveTemplateAsAsync()
    {
        if (!_fileDialogService.TrySaveFile("标签模板|*.sclabel", string.IsNullOrWhiteSpace(CurrentFilePath) ? "template.sclabel" : Path.GetFileName(CurrentFilePath), out var path))
        {
            return;
        }

        try
        {
            CurrentFilePath = path;
            await _storageService.SaveAsync(path, BuildTemplateSnapshot());
            AddRecentFile(path);
            _statusCenter.SetActivityMessage($"模板已另存为 {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"另存模板失败: {ex.Message}");
            _statusCenter.SetActivityMessage("另存模板失败");
        }
    }

    /// <summary>
    /// 将当前模板生成 TSPL 并发送到打印机。
    /// </summary>
    [RelayCommand]
    private async Task PrintTemplateAsync()
    {
        if (!_printerService.IsConnected)
        {
            _statusCenter.SetActivityMessage("请先连接打印机再执行模板打印");
            return;
        }

        try
        {
            var command = _tsplGenerator.Generate(BuildTemplateSnapshot(), 1);
            await _printerService.SendDataAsync(Encoding.GetEncoding(54936).GetBytes(command));
            _statusCenter.SetActivityMessage("模板内容已发送到打印机");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"打印模板失败: {ex.Message}");
            _statusCenter.SetActivityMessage("打印模板失败");
        }
    }

    /// <summary>
    /// 添加默认文本元素。
    /// </summary>
    [RelayCommand]
    private void AddTextElement()
    {
        AddElement(new TextElement
        {
            X = 40,
            Y = 40,
            Content = "新文本",
        }, "已添加文本元素");
    }

    /// <summary>
    /// 添加默认条码元素。
    /// </summary>
    [RelayCommand]
    private void AddBarcodeElement()
    {
        AddElement(new BarcodeElement
        {
            X = 40,
            Y = 120,
            Content = "1234567890",
        }, "已添加条码元素");
    }

    /// <summary>
    /// 添加默认二维码元素。
    /// </summary>
    [RelayCommand]
    private void AddQrCodeElement()
    {
        AddElement(new QrCodeElement
        {
            X = 320,
            Y = 40,
            Content = "https://ceratron.example",
        }, "已添加二维码元素");
    }

    /// <summary>
    /// 添加默认矩形框元素。
    /// </summary>
    [RelayCommand]
    private void AddBoxElement()
    {
        AddElement(new BoxElement
        {
            X = 20,
            Y = 20,
            EndX = 420,
            EndY = 220,
            Thickness = 2,
        }, "已添加矩形框元素");
    }

    /// <summary>
    /// 添加默认线条元素。
    /// </summary>
    [RelayCommand]
    private void AddLineElement()
    {
        AddElement(new LineElement
        {
            X = 20,
            Y = 260,
            Width = 420,
            Height = 4,
        }, "已添加线条元素");
    }

    /// <summary>
    /// 添加默认挖空元素。
    /// </summary>
    [RelayCommand]
    private void AddEraseElement()
    {
        AddElement(new EraseElement
        {
            X = 20,
            Y = 300,
            Width = 180,
            Height = 24,
        }, "已添加挖空元素");
    }

    /// <summary>
    /// 添加默认表格元素。
    /// </summary>
    [RelayCommand]
    private void AddTableElement()
    {
        var dotsPerMillimeter = 8.0;
        var labelWidthDots = (int)Math.Round(LabelWidth * dotsPerMillimeter);
        var labelHeightDots = (int)Math.Round(LabelHeight * dotsPerMillimeter);
        var margin = 24;
        var availableWidth = Math.Max(120, labelWidthDots - margin * 2);
        var availableHeight = Math.Max(120, labelHeightDots - margin * 2);
        var columnWidthA = Math.Max(40, availableWidth / 2);
        var columnWidthB = Math.Max(40, availableWidth - columnWidthA);
        var rowHeight = Math.Max(40, availableHeight / 2);
        var tableWidth = columnWidthA + columnWidthB;
        var tableHeight = rowHeight * 2;
        var x = Math.Max(0, (labelWidthDots - tableWidth) / 2);
        var y = Math.Max(0, (labelHeightDots - tableHeight) / 2);

        AddElement(new TableElement
        {
            X = x,
            Y = y,
            Rows = 2,
            Cols = 2,
            RowHeight = rowHeight,
            ColumnWidths = new List<int> { columnWidthA, columnWidthB },
        }, "已添加表格元素");
    }

    [RelayCommand]
    private async Task HandleTableCellContextMenuAsync(TableCellContextMenuRequest? request)
    {
        if (request is null)
        {
            return;
        }

        var tableElement = Elements.OfType<TableElement>().FirstOrDefault(e => e.Id == request.TableElementId);
        if (tableElement is null)
        {
            return;
        }

        if (request.Action != TableCellContextMenuAction.EditCell)
        {
            CaptureUndoSnapshot();
        }

        switch (request.Action)
        {
            case TableCellContextMenuAction.AddRowAbove:
                tableElement.InsertRowAt(request.Row);
                break;
            case TableCellContextMenuAction.AddRowBelow:
                tableElement.InsertRowAt(request.Row + 1);
                break;
            case TableCellContextMenuAction.AddColumnLeft:
                tableElement.InsertColumnAt(request.Column);
                break;
            case TableCellContextMenuAction.AddColumnRight:
                tableElement.InsertColumnAt(request.Column + 1);
                break;
            case TableCellContextMenuAction.RemoveRow:
                tableElement.RemoveRowAt(request.Row);
                break;
            case TableCellContextMenuAction.RemoveColumn:
                tableElement.RemoveColumnAt(request.Column);
                break;
            case TableCellContextMenuAction.AddCellTextElement:
                AddCellInnerElement(tableElement, request.Row, request.Column, new TableCellTextElement { Content = "文本", Width = 120, Height = 40 });
                break;
            case TableCellContextMenuAction.AddCellBarcodeElement:
                AddCellInnerElement(tableElement, request.Row, request.Column, new TableCellBarcodeElement { Content = "12345678", Width = 140, Height = 40 });
                break;
            case TableCellContextMenuAction.AddCellQrCodeElement:
                AddCellInnerElement(tableElement, request.Row, request.Column, new TableCellQrCodeElement { Content = "https://example.com", Width = 80, Height = 80 });
                break;
            case TableCellContextMenuAction.EditCellInnerElement:
                await EditTableCellInnerElementAsync(tableElement, request.Row, request.Column);
                return;
            case TableCellContextMenuAction.RemoveCellInnerElement:
                RemoveCellInnerElement(tableElement, request.Row, request.Column);
                break;
            case TableCellContextMenuAction.EditCell:
                await EditTableCellAsync(tableElement, request.Row, request.Column);
                return;
            default:
                return;
        }

        tableElement.EnsureCellCount();
        RefreshPreview();
        _statusCenter.SetActivityMessage("表格已更新");
    }

    private async Task EditTableCellAsync(TableElement tableElement, int row, int column)
    {
        if (row < 0 || row >= tableElement.Rows || column < 0 || column >= tableElement.Cols)
        {
            return;
        }

        tableElement.EnsureCellCount();
        var cellIndex = row * tableElement.Cols + column;
        var cell = tableElement.Cells[cellIndex];

        var editor = new TableCellEditorViewModel(cell);
        var dialog = new TableCellEditorWindow
        {
            DataContext = editor,
            Owner = Application.Current?.MainWindow,
        };

        if (dialog.ShowDialog() == true)
        {
            CaptureUndoSnapshot();
            tableElement.Cells[cellIndex] = editor.BuildTableCell();
            RefreshPreview();
            _statusCenter.SetActivityMessage($"单元格 ({row + 1},{column + 1}) 已更新");
        }
    }

    private void AddCellInnerElement(TableElement tableElement, int row, int column, TableCellInnerElement innerElement)
    {
        if (row < 0 || row >= tableElement.Rows || column < 0 || column >= tableElement.Cols)
        {
            return;
        }

        CaptureUndoSnapshot();
        tableElement.EnsureCellCount();
        var cellIndex = row * tableElement.Cols + column;
        var cell = tableElement.Cells[cellIndex];
        cell.MigrateLegacyContentToInnerElements();
        cell.InnerElements.Add(innerElement);
        RefreshPreview();
        _statusCenter.SetActivityMessage("已向单元格添加内部元素");
    }

    private async Task EditTableCellInnerElementAsync(TableElement tableElement, int row, int column)
    {
        if (row < 0 || row >= tableElement.Rows || column < 0 || column >= tableElement.Cols)
        {
            return;
        }

        tableElement.EnsureCellCount();
        var cellIndex = row * tableElement.Cols + column;
        var cell = tableElement.Cells[cellIndex];
        if (cell.InnerElements.Count == 0)
        {
            await EditTableCellAsync(tableElement, row, column);
            return;
        }

        var editor = new TableCellInnerElementEditorViewModel(cell);
        var dialog = new TableCellInnerElementEditorWindow
        {
            DataContext = editor,
            Owner = Application.Current?.MainWindow,
        };

        if (dialog.ShowDialog() == true)
        {
            CaptureUndoSnapshot();
            tableElement.Cells[cellIndex] = editor.BuildTableCell();
            RefreshPreview();
            _statusCenter.SetActivityMessage($"单元格 ({row + 1},{column + 1}) 内部元素已更新");
        }
    }

    private void RemoveCellInnerElement(TableElement tableElement, int row, int column)
    {
        if (row < 0 || row >= tableElement.Rows || column < 0 || column >= tableElement.Cols)
        {
            return;
        }

        tableElement.EnsureCellCount();
        var cellIndex = row * tableElement.Cols + column;
        var cell = tableElement.Cells[cellIndex];
        if (cell.InnerElements.Count == 0)
        {
            return;
        }

        CaptureUndoSnapshot();
        cell.InnerElements.RemoveAt(cell.InnerElements.Count - 1);
        RefreshPreview();
        _statusCenter.SetActivityMessage("已删除单元格内的最后一个内部元素");
    }

    [RelayCommand(CanExecute = nameof(CanEditSelectedElement))]
    private void MoveSelectedCellInnerElement(TableCellInnerElementMoveRequest? request)
    {
        if (request is null)
        {
            return;
        }

        var tableElement = Elements.OfType<TableElement>().FirstOrDefault(e => e.Id == request.TableElementId);
        if (tableElement is null)
        {
            return;
        }

        var cellIndex = request.Row * tableElement.Cols + request.Column;
        if (cellIndex < 0 || cellIndex >= tableElement.Cells.Count)
        {
            return;
        }

        var cell = tableElement.Cells[cellIndex];
        var innerElement = cell.InnerElements.FirstOrDefault(e => e.Id == request.InnerElementId);
        if (innerElement is null)
        {
            return;
        }

        innerElement.X = request.X;
        innerElement.Y = request.Y;
        innerElement.Width = request.Width;
        innerElement.Height = request.Height;

        RefreshPreview();
    }

    private void HandleTableCellResize(TableCellResizeRequest? request)
    {
        if (request is null)
        {
            return;
        }

        var tableElement = Elements.OfType<TableElement>().FirstOrDefault(e => e.Id == request.TableElementId);
        if (tableElement is null)
        {
            return;
        }

        switch (request.Mode)
        {
            case TableCellResizeMode.Column:
                if (request.Index >= 0 && request.Index < tableElement.ColumnWidths.Count)
                {
                    tableElement.ColumnWidths[request.Index] = request.NewSize;
                }
                break;
            case TableCellResizeMode.Row:
                if (request.RowHeights is not null && request.RowHeights.Count == tableElement.Rows)
                {
                    tableElement.RowHeights.Clear();
                    tableElement.RowHeights.AddRange(request.RowHeights);
                }
                else if (request.Index >= 0 && request.Index < tableElement.RowHeights.Count)
                {
                    tableElement.RowHeights[request.Index] = request.NewSize;
                }
                else
                {
                    tableElement.RowHeight = request.NewSize;
                }
                break;
        }

        if (SelectedElement is TableElement selectedTable && selectedTable.Id == tableElement.Id)
        {
            SelectedTableRowHeight = tableElement.RowHeight;
            SelectedTableColumnWidthA = tableElement.ColumnWidths.ElementAtOrDefault(0);
            SelectedTableColumnWidthB = tableElement.ColumnWidths.ElementAtOrDefault(1);
        }

        RefreshPreview();
    }

    /// <summary>
    /// 删除当前选中的元素。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEditSelectedElement))]
    private void RemoveSelectedElement()
    {
        if (SelectedElement is null)
        {
            return;
        }

        CaptureUndoSnapshot();
        Elements.Remove(SelectedElement);
        SelectedElement = Elements.LastOrDefault();
        RefreshPreview();
        _statusCenter.SetActivityMessage("已删除选中元素");
    }

    /// <summary>
    /// 将右侧属性面板中的修改应用到当前元素。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEditSelectedElement))]
    private void ApplySelectedElementChanges()
    {
        if (SelectedElement is null)
        {
            return;
        }

        CaptureUndoSnapshot();

        SelectedElement.X = ElementX;
        SelectedElement.Y = ElementY;
        SelectedElement.Rotation = ElementRotation;

        switch (SelectedElement)
        {
            case TextElement textElement:
                textElement.Content = SelectedTextContent;
                textElement.Font = SelectedFont;
                break;
            case BarcodeElement barcodeElement:
                barcodeElement.Content = SelectedBarcodeContent;
                barcodeElement.CodeType = SelectedBarcodeType;
                barcodeElement.Height = SelectedBarcodeHeight;
                barcodeElement.Readable = SelectedBarcodeReadable;
                barcodeElement.Narrow = SelectedBarcodeNarrow;
                barcodeElement.Wide = SelectedBarcodeWide;
                break;
            case QrCodeElement qrCodeElement:
                qrCodeElement.Content = SelectedQrContent;
                qrCodeElement.ErrorCorrectionLevel = SelectedQrErrorCorrectionLevel;
                qrCodeElement.CellWidth = SelectedQrCellWidth;
                qrCodeElement.Mode = SelectedQrMode;
                break;
            case BoxElement boxElement:
                boxElement.EndX = SelectedBoxEndX;
                boxElement.EndY = SelectedBoxEndY;
                boxElement.Thickness = SelectedBoxThickness;
                break;
            case LineElement lineElement:
                lineElement.Width = SelectedLineWidth;
                lineElement.Height = SelectedLineHeight;
                break;
            case EraseElement eraseElement:
                eraseElement.Width = SelectedEraseWidth;
                eraseElement.Height = SelectedEraseHeight;
                break;
            case TableElement tableElement:
                tableElement.RowHeight = SelectedTableRowHeight;
                if (tableElement.ColumnWidths.Count == 0)
                {
                    tableElement.ColumnWidths.Add(SelectedTableColumnWidthA);
                    tableElement.ColumnWidths.Add(SelectedTableColumnWidthB);
                }
                else
                {
                    tableElement.ColumnWidths[0] = SelectedTableColumnWidthA;
                    if (tableElement.ColumnWidths.Count > 1)
                    {
                        tableElement.ColumnWidths[1] = SelectedTableColumnWidthB;
                    }
                    else
                    {
                        tableElement.ColumnWidths.Add(SelectedTableColumnWidthB);
                    }
                }
                tableElement.Cells = new List<TableCell>
                {
                    new TableCell { ContentType = SelectedTableCell11Type, Content = SelectedTableCell11Content, BarcodeType = BarcodeType.Code128, QrCellWidth = SelectedQrCellWidth, QrErrorCorrectionLevel = SelectedQrErrorCorrectionLevel, QrMode = SelectedQrMode },
                    new TableCell { ContentType = SelectedTableCell12Type, Content = SelectedTableCell12Content, BarcodeType = BarcodeType.Code128, QrCellWidth = SelectedQrCellWidth, QrErrorCorrectionLevel = SelectedQrErrorCorrectionLevel, QrMode = SelectedQrMode },
                    new TableCell { ContentType = SelectedTableCell21Type, Content = SelectedTableCell21Content, BarcodeType = BarcodeType.Code128, QrCellWidth = SelectedQrCellWidth, QrErrorCorrectionLevel = SelectedQrErrorCorrectionLevel, QrMode = SelectedQrMode },
                    new TableCell { ContentType = SelectedTableCell22Type, Content = SelectedTableCell22Content, BarcodeType = BarcodeType.Code128, QrCellWidth = SelectedQrCellWidth, QrErrorCorrectionLevel = SelectedQrErrorCorrectionLevel, QrMode = SelectedQrMode },
                };
                break;
        }

        RefreshPreview();
        EditorHint = "属性已应用并刷新预览";
        _statusCenter.SetActivityMessage("元素属性已更新");
    }

    /// <summary>
    /// 在画布上选中指定元素。
    /// </summary>
    [RelayCommand]
    private void SelectElementById(string elementId)
    {
        if (string.IsNullOrWhiteSpace(elementId))
        {
            return;
        }

        var element = Elements.FirstOrDefault(e => e.Id == elementId);
        if (element is null)
        {
            return;
        }

        SelectedElement = element;
        _statusCenter.SetActivityMessage("已选中画布元素");
    }

    /// <summary>
    /// 在画布开始拖动元素之前记录快照。
    /// </summary>
    [RelayCommand]
    private void BeginSelectedElementMove(string elementId)
    {
        if (string.IsNullOrWhiteSpace(elementId))
        {
            return;
        }

        SelectElementById(elementId);
        CaptureUndoSnapshot();
    }

    /// <summary>
    /// 接收画布拖动后更新的元素坐标。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEditSelectedElement))]
    private void MoveSelectedElement(ElementMoveRequest request)
    {
        if (request is null)
        {
            return;
        }

        var element = Elements.FirstOrDefault(e => e.Id == request.ElementId);
        if (element is null)
        {
            return;
        }

        var deltaX = request.X - element.X;
        var deltaY = request.Y - element.Y;

        switch (element)
        {
            case BoxElement boxElement:
                boxElement.X += deltaX;
                boxElement.Y += deltaY;
                boxElement.EndX += deltaX;
                boxElement.EndY += deltaY;
                break;
            default:
                element.X = request.X;
                element.Y = request.Y;
                break;
        }

        if (SelectedElement?.Id == request.ElementId)
        {
            ElementX = request.X;
            ElementY = request.Y;
        }

        RefreshPreview();
        _statusCenter.SetActivityMessage("元素位置已更新");
    }

    /// <summary>
    /// 撤销上一次编辑操作。
    /// </summary>
    [RelayCommand]
    private void Undo()
    {
        if (_undoStack.Count == 0)
        {
            return;
        }

        _redoStack.Push(_serializer.Serialize(BuildTemplateSnapshot()));
        var snapshot = _undoStack.Pop();
        ApplyTemplate(_serializer.Deserialize(snapshot), CurrentFilePath, false);
        _statusCenter.SetActivityMessage("已撤销上一步操作");
    }

    /// <summary>
    /// 重做最近一次撤销的编辑操作。
    /// </summary>
    [RelayCommand]
    private void Redo()
    {
        if (_redoStack.Count == 0)
        {
            return;
        }

        _undoStack.Push(_serializer.Serialize(BuildTemplateSnapshot()));
        var snapshot = _redoStack.Pop();
        ApplyTemplate(_serializer.Deserialize(snapshot), CurrentFilePath, false);
        _statusCenter.SetActivityMessage("已重做撤销的操作");
    }

    /// <summary>
    /// 判断是否存在可编辑的选中元素。
    /// </summary>
    private bool CanEditSelectedElement()
    {
        return SelectedElement is not null;
    }

    /// <summary>
    /// 重置为新的空白模板。
    /// </summary>
    private void ResetDocument(bool clearHistory)
    {
        _isApplyingSnapshot = true;
        LabelWidth = 60;
        LabelHeight = 40;
        LabelGap = 2;
        Density = 8;
        PreviewZoom = 2.0;
        Elements.Clear();
        SelectedElement = null;
        CurrentFilePath = null;
        EditorHint = "从左侧工具栏添加元素，然后在右侧属性面板调整内容。";

        if (clearHistory)
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }

        _isApplyingSnapshot = false;
        RefreshPreview();
        _statusCenter.SetDocumentMessage($"标签: {LabelWidth}x{LabelHeight} mm");
        _statusCenter.SetActivityMessage("已新建空白标签模板");
    }

    /// <summary>
    /// 将新元素加入编辑区并立即刷新预览。
    /// </summary>
    private void AddElement(LabelElement element, string message)
    {
        CaptureUndoSnapshot();
        Elements.Add(element);
        SelectedElement = element;
        RefreshPreview();
        _statusCenter.SetActivityMessage(message);
    }

    /// <summary>
    /// 从当前编辑状态构建一份独立的模板快照。
    /// </summary>
    private LabelTemplateDocument BuildTemplateSnapshot()
    {
        return new LabelTemplateDocument
        {
            Version = "1.0",
            Label = new LabelDefinition
            {
                Width = LabelWidth,
                Height = LabelHeight,
                Gap = LabelGap,
                Density = Density,
                Unit = LabelUnit.Millimeter,
            },
            Elements = Elements.Select(CloneElement).ToList(),
        };
    }

    /// <summary>
    /// 刷新预览模板、TSPL 文本和状态栏中的文档尺寸描述。
    /// </summary>
    private void RefreshPreview()
    {
        if (_isApplyingSnapshot)
        {
            return;
        }

        PreviewTemplate = BuildTemplateSnapshot();
        TsplPreview = _tsplGenerator.Generate(PreviewTemplate, 1);
        _statusCenter.SetDocumentMessage($"标签: {LabelWidth}x{LabelHeight} mm | 元素: {Elements.Count}");
    }

    /// <summary>
    /// 将模板对象应用到当前编辑器状态中。
    /// </summary>
    private void ApplyTemplate(LabelTemplateDocument template, string? filePath, bool pushRecentFile)
    {
        _isApplyingSnapshot = true;
        LabelWidth = template.Label.Width;
        LabelHeight = template.Label.Height;
        LabelGap = template.Label.Gap;
        Density = template.Label.Density;

        Elements.Clear();
        foreach (var element in template.Elements.Select(CloneElement))
        {
            Elements.Add(element);
        }

        CurrentFilePath = filePath;
        SelectedElement = Elements.FirstOrDefault();
        if (pushRecentFile && !string.IsNullOrWhiteSpace(filePath))
        {
            AddRecentFile(filePath);
        }

        _redoStack.Clear();
        _isApplyingSnapshot = false;
        RefreshPreview();
    }

    /// <summary>
    /// 将当前模板快照压入撤销栈，便于后续执行 Undo。
    /// </summary>
    private void CaptureUndoSnapshot()
    {
        if (_isApplyingSnapshot)
        {
            return;
        }

        var snapshot = _serializer.Serialize(BuildTemplateSnapshot());
        if (_undoStack.Count == 0 || _undoStack.Peek() != snapshot)
        {
            _undoStack.Push(snapshot);
        }
        _redoStack.Clear();
    }

    /// <summary>
    /// 将最近操作的模板路径加入最近文件列表。
    /// </summary>
    private void AddRecentFile(string path)
    {
        if (RecentFiles.Contains(path))
        {
            RecentFiles.Remove(path);
        }

        RecentFiles.Insert(0, path);
        while (RecentFiles.Count > 6)
        {
            RecentFiles.RemoveAt(RecentFiles.Count - 1);
        }
    }

    /// <summary>
    /// 将选中元素的属性同步到属性面板编辑字段。
    /// </summary>
    private void LoadSelectedElementEditor(LabelElement? element)
    {
        if (element is null)
        {
            EditorHint = "请选择一个元素后再编辑属性。";
            return;
        }

        ElementX = element.X;
        ElementY = element.Y;
        ElementRotation = element.Rotation;

        switch (element)
        {
            case TextElement textElement:
                SelectedTextContent = textElement.Content;
                SelectedFont = textElement.Font;
                break;
            case BarcodeElement barcodeElement:
                SelectedBarcodeContent = barcodeElement.Content;
                SelectedBarcodeType = barcodeElement.CodeType;
                SelectedBarcodeHeight = barcodeElement.Height;
                SelectedBarcodeReadable = barcodeElement.Readable;
                SelectedBarcodeNarrow = barcodeElement.Narrow;
                SelectedBarcodeWide = barcodeElement.Wide;
                break;
            case QrCodeElement qrCodeElement:
                SelectedQrContent = qrCodeElement.Content;
                SelectedQrErrorCorrectionLevel = qrCodeElement.ErrorCorrectionLevel;
                SelectedQrCellWidth = qrCodeElement.CellWidth;
                SelectedQrMode = qrCodeElement.Mode;
                break;
            case BoxElement boxElement:
                SelectedBoxEndX = boxElement.EndX;
                SelectedBoxEndY = boxElement.EndY;
                SelectedBoxThickness = boxElement.Thickness;
                break;
            case LineElement lineElement:
                SelectedLineWidth = lineElement.Width;
                SelectedLineHeight = lineElement.Height;
                break;
            case EraseElement eraseElement:
                SelectedEraseWidth = eraseElement.Width;
                SelectedEraseHeight = eraseElement.Height;
                break;
            case TableElement tableElement:
                SelectedTableRowHeight = tableElement.RowHeight;
                SelectedTableColumnWidthA = tableElement.ColumnWidths.ElementAtOrDefault(0);
                SelectedTableColumnWidthB = tableElement.ColumnWidths.ElementAtOrDefault(1);

                SelectedTableCell11Type = tableElement.Cells.ElementAtOrDefault(0)?.ContentType ?? TableCellContentType.Text;
                SelectedTableCell11Content = tableElement.Cells.ElementAtOrDefault(0)?.Content ?? string.Empty;
                SelectedTableCell12Type = tableElement.Cells.ElementAtOrDefault(1)?.ContentType ?? TableCellContentType.Text;
                SelectedTableCell12Content = tableElement.Cells.ElementAtOrDefault(1)?.Content ?? string.Empty;
                SelectedTableCell21Type = tableElement.Cells.ElementAtOrDefault(2)?.ContentType ?? TableCellContentType.Text;
                SelectedTableCell21Content = tableElement.Cells.ElementAtOrDefault(2)?.Content ?? string.Empty;
                SelectedTableCell22Type = tableElement.Cells.ElementAtOrDefault(3)?.ContentType ?? TableCellContentType.Text;
                SelectedTableCell22Content = tableElement.Cells.ElementAtOrDefault(3)?.Content ?? string.Empty;
                break;
        }

        EditorHint = $"正在编辑 {element.GetType().Name}";
    }

    /// <summary>
    /// 通过序列化往返复制元素，避免编辑快照之间共享同一引用。
    /// </summary>
    private LabelElement CloneElement(LabelElement element)
    {
        var document = new LabelTemplateDocument
        {
            Label = new LabelDefinition(),
            Elements = [element],
        };

        return _serializer.Deserialize(_serializer.Serialize(document)).Elements.Single();
    }
}