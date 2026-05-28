using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCLabelPrinter.Core.Models;

namespace SCLabelPrinter.ViewModels;

/// <summary>
/// 提供单元格内部元素编辑对话框的数据和行为。
/// </summary>
public sealed partial class TableCellInnerElementEditorViewModel : ObservableObject
{
    public TableCellInnerElementEditorViewModel(TableCell cell)
    {
        InnerElements = new ObservableCollection<TableCellInnerElement>(cell.InnerElements.Select(CloneInnerElement));
        SelectedInnerElement = InnerElements.FirstOrDefault();
    }

    [ObservableProperty]
    private ObservableCollection<TableCellInnerElement> innerElements = new();

    [ObservableProperty]
    private TableCellInnerElement? selectedInnerElement;

    [ObservableProperty]
    private string selectedContent = string.Empty;

    [ObservableProperty]
    private int selectedX;

    [ObservableProperty]
    private int selectedY;

    [ObservableProperty]
    private int selectedWidth = 120;

    [ObservableProperty]
    private int selectedHeight = 40;

    [ObservableProperty]
    private BarcodeType selectedBarcodeType = BarcodeType.Code128;

    [ObservableProperty]
    private bool selectedBarcodeReadable = true;

    [ObservableProperty]
    private int selectedBarcodeNarrow = 2;

    [ObservableProperty]
    private int selectedBarcodeWide = 2;

    [ObservableProperty]
    private string selectedQrErrorCorrectionLevel = "L";

    [ObservableProperty]
    private int selectedQrCellWidth = 5;

    [ObservableProperty]
    private string selectedQrMode = "A";

    partial void OnSelectedInnerElementChanged(TableCellInnerElement? value)
    {
        RefreshSelectedInnerProperties(value);
    }

    private void RefreshSelectedInnerProperties(TableCellInnerElement? inner)
    {
        if (inner is null)
        {
            SelectedContent = string.Empty;
            SelectedX = 0;
            SelectedY = 0;
            SelectedWidth = 120;
            SelectedHeight = 40;
            SelectedBarcodeType = BarcodeType.Code128;
            SelectedBarcodeReadable = true;
            SelectedBarcodeNarrow = 2;
            SelectedBarcodeWide = 2;
            SelectedQrErrorCorrectionLevel = "L";
            SelectedQrCellWidth = 5;
            SelectedQrMode = "A";
            return;
        }

        SelectedX = inner.X;
        SelectedY = inner.Y;
        SelectedWidth = inner.Width;
        SelectedHeight = inner.Height;

        switch (inner)
        {
            case TableCellTextElement textElement:
                SelectedContent = textElement.Content;
                break;
            case TableCellBarcodeElement barcodeElement:
                SelectedContent = barcodeElement.Content;
                SelectedBarcodeType = barcodeElement.BarcodeType;
                SelectedBarcodeReadable = barcodeElement.Readable;
                SelectedBarcodeNarrow = barcodeElement.Narrow;
                SelectedBarcodeWide = barcodeElement.Wide;
                break;
            case TableCellQrCodeElement qrCodeElement:
                SelectedContent = qrCodeElement.Content;
                SelectedQrErrorCorrectionLevel = qrCodeElement.ErrorCorrectionLevel;
                SelectedQrCellWidth = qrCodeElement.CellWidth;
                SelectedQrMode = qrCodeElement.Mode;
                break;
            default:
                SelectedContent = string.Empty;
                break;
        }
    }

    [RelayCommand]
    private void AddTextElement()
    {
        var inner = new TableCellTextElement
        {
            Content = "新文本",
            X = 8,
            Y = 8,
            Width = 120,
            Height = 40,
        };
        InnerElements.Add(inner);
        SelectedInnerElement = inner;
    }

    [RelayCommand]
    private void AddBarcodeElement()
    {
        var inner = new TableCellBarcodeElement
        {
            Content = "12345678",
            X = 8,
            Y = 8,
            Width = 140,
            Height = 40,
        };
        InnerElements.Add(inner);
        SelectedInnerElement = inner;
    }

    [RelayCommand]
    private void AddQrCodeElement()
    {
        var inner = new TableCellQrCodeElement
        {
            Content = "https://example.com",
            X = 8,
            Y = 8,
            Width = 80,
            Height = 80,
        };
        InnerElements.Add(inner);
        SelectedInnerElement = inner;
    }

    [RelayCommand(CanExecute = nameof(CanModifySelectedInnerElement))]
    private void RemoveSelectedInnerElement()
    {
        if (SelectedInnerElement is null)
        {
            return;
        }

        var index = InnerElements.IndexOf(SelectedInnerElement);
        if (index < 0)
        {
            return;
        }

        InnerElements.RemoveAt(index);
        SelectedInnerElement = InnerElements.ElementAtOrDefault(Math.Max(0, index - 1));
    }

    [RelayCommand(CanExecute = nameof(CanModifySelectedInnerElement))]
    private void ApplySelectedInnerElementChanges()
    {
        if (SelectedInnerElement is null)
        {
            return;
        }

        SelectedInnerElement.X = SelectedX;
        SelectedInnerElement.Y = SelectedY;
        SelectedInnerElement.Width = SelectedWidth;
        SelectedInnerElement.Height = SelectedHeight;

        switch (SelectedInnerElement)
        {
            case TableCellTextElement textElement:
                textElement.Content = SelectedContent;
                break;
            case TableCellBarcodeElement barcodeElement:
                barcodeElement.Content = SelectedContent;
                barcodeElement.BarcodeType = SelectedBarcodeType;
                barcodeElement.Readable = SelectedBarcodeReadable;
                barcodeElement.Narrow = SelectedBarcodeNarrow;
                barcodeElement.Wide = SelectedBarcodeWide;
                break;
            case TableCellQrCodeElement qrCodeElement:
                qrCodeElement.Content = SelectedContent;
                qrCodeElement.ErrorCorrectionLevel = SelectedQrErrorCorrectionLevel;
                qrCodeElement.CellWidth = SelectedQrCellWidth;
                qrCodeElement.Mode = SelectedQrMode;
                break;
        }

        SelectedInnerElement = SelectedInnerElement;
    }

    public TableCell BuildTableCell()
    {
        var cell = new TableCell
        {
            ContentType = InnerElements.FirstOrDefault() switch
            {
                TableCellTextElement => TableCellContentType.Text,
                TableCellBarcodeElement => TableCellContentType.Barcode,
                TableCellQrCodeElement => TableCellContentType.QrCode,
                _ => TableCellContentType.Text,
            },
            Content = InnerElements.FirstOrDefault() switch
            {
                TableCellTextElement textElement => textElement.Content,
                TableCellBarcodeElement barcodeElement => barcodeElement.Content,
                TableCellQrCodeElement qrCodeElement => qrCodeElement.Content,
                _ => string.Empty,
            },
            BarcodeType = InnerElements.FirstOrDefault() is TableCellBarcodeElement barcode ? barcode.BarcodeType : BarcodeType.Code128,
            QrCellWidth = InnerElements.FirstOrDefault() is TableCellQrCodeElement qr ? qr.CellWidth : 5,
            QrErrorCorrectionLevel = InnerElements.FirstOrDefault() is TableCellQrCodeElement qr2 ? qr2.ErrorCorrectionLevel : "L",
            QrMode = InnerElements.FirstOrDefault() is TableCellQrCodeElement qr3 ? qr3.Mode : "A",
            InnerElements = InnerElements.Select(CloneInnerElement).ToList(),
        };

        return cell;
    }

    private bool CanModifySelectedInnerElement()
    {
        return SelectedInnerElement is not null;
    }

    private static TableCellInnerElement CloneInnerElement(TableCellInnerElement element)
    {
        return element switch
        {
            TableCellTextElement textElement => new TableCellTextElement
            {
                Id = textElement.Id,
                X = textElement.X,
                Y = textElement.Y,
                Width = textElement.Width,
                Height = textElement.Height,
                Rotation = textElement.Rotation,
                Content = textElement.Content,
                Font = textElement.Font,
                XScale = textElement.XScale,
                YScale = textElement.YScale,
            },
            TableCellBarcodeElement barcodeElement => new TableCellBarcodeElement
            {
                Id = barcodeElement.Id,
                X = barcodeElement.X,
                Y = barcodeElement.Y,
                Width = barcodeElement.Width,
                Height = barcodeElement.Height,
                Rotation = barcodeElement.Rotation,
                Content = barcodeElement.Content,
                BarcodeType = barcodeElement.BarcodeType,
                Narrow = barcodeElement.Narrow,
                Wide = barcodeElement.Wide,
                Readable = barcodeElement.Readable,
            },
            TableCellQrCodeElement qrCodeElement => new TableCellQrCodeElement
            {
                Id = qrCodeElement.Id,
                X = qrCodeElement.X,
                Y = qrCodeElement.Y,
                Width = qrCodeElement.Width,
                Height = qrCodeElement.Height,
                Rotation = qrCodeElement.Rotation,
                Content = qrCodeElement.Content,
                ErrorCorrectionLevel = qrCodeElement.ErrorCorrectionLevel,
                CellWidth = qrCodeElement.CellWidth,
                Mode = qrCodeElement.Mode,
            },
            _ => throw new InvalidOperationException("Unsupported cell inner element type."),
        };
    }
}
