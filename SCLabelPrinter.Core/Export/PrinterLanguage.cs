namespace SCLabelPrinter.Core.Export;

/// <summary>
/// 定义打印机所使用的指令语言类型，支持未来扩展新的打印机协议。
/// </summary>
public enum PrinterLanguage
{
    /// <summary>TSPL 指令集（TSC 标签打印机）</summary>
    Tspl,

    /// <summary>ZPL 指令集（Zebra 标签打印机）</summary>
    Zpl,

    /// <summary>ESC/POS 指令集（热敏票据打印机）</summary>
    EscPos,

    /// <summary>CPCL 指令集（便携标签打印机）</summary>
    Cpcl,
}