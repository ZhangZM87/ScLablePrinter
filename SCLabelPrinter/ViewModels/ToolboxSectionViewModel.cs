using System.Windows.Input;

namespace SCLabelPrinter.ViewModels;

/// <summary>
/// 表示工具箱中的单个可执行工具项，用于将新增元素入口从硬编码 XAML 中抽离出来。
/// </summary>
public sealed class ToolboxItemViewModel
{
    /// <summary>
    /// 创建一个包含标题、说明和命令的工具项。
    /// </summary>
    public ToolboxItemViewModel(string title, string description, ICommand command)
    {
        Title = title;
        Description = description;
        Command = command;
    }

    public string Title { get; }

    public string Description { get; }

    public ICommand Command { get; }
}

/// <summary>
/// 表示工具箱中的一个分组，用于按职责组织可插入元素。
/// </summary>
public sealed class ToolboxSectionViewModel
{
    /// <summary>
    /// 创建一个包含标题和工具项集合的工具箱分组。
    /// </summary>
    public ToolboxSectionViewModel(string title, IReadOnlyList<ToolboxItemViewModel> items)
    {
        Title = title;
        Items = items;
    }

    public string Title { get; }

    public IReadOnlyList<ToolboxItemViewModel> Items { get; }
}