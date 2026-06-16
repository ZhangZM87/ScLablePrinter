using SCLabelPrinter.Core.Models;

namespace SCLabelPrinter.Core.Export;

/// <summary>
/// 虚线段落计算器，根据线条样式计算出需要绘制的各个线段坐标。
/// 供各打印机语言的 LineWriter 和 TableWriter 共用。
/// </summary>
public static class DashSegmentCalculator
{
    /// <summary>
    /// 计算水平虚线的各段起止坐标。
    /// </summary>
    /// <param name="startX">起始 X 坐标</param>
    /// <param name="y">Y 坐标</param>
    /// <param name="totalLength">线条总长度</param>
    /// <param name="thickness">线条粗细</param>
    /// <param name="style">线条样式</param>
    /// <param name="dashLength">虚线模式线段长度</param>
    /// <param name="gapLength">虚线模式间隔长度</param>
    /// <returns>每个线段的 (X, Y, Width, Height) 元组列表</returns>
    public static List<(int X, int Y, int Width, int Height)> CalculateHorizontalSegments(
        int startX, int y, int totalLength, int thickness,
        TableLineStyle style, int dashLength = 8, int gapLength = 4)
    {
        if (style == TableLineStyle.Solid)
        {
            return [( startX, y, totalLength, thickness )];
        }

        var segmentLength = style == TableLineStyle.Dotted ? Math.Max(1, thickness) : dashLength;
        var segmentGap = style == TableLineStyle.Dotted ? Math.Max(1, gapLength) : gapLength;

        var segments = new List<(int X, int Y, int Width, int Height)>();
        var currentX = startX;
        var endX = startX + totalLength;

        while (currentX < endX)
        {
            var segWidth = Math.Min(segmentLength, endX - currentX);
            segments.Add((currentX, y, segWidth, thickness));
            currentX += segmentLength + segmentGap;
        }

        return segments;
    }

    /// <summary>
    /// 计算垂直虚线的各段起止坐标。
    /// </summary>
    /// <param name="x">X 坐标</param>
    /// <param name="startY">起始 Y 坐标</param>
    /// <param name="totalLength">线条总长度</param>
    /// <param name="thickness">线条粗细</param>
    /// <param name="style">线条样式</param>
    /// <param name="dashLength">虚线模式线段长度</param>
    /// <param name="gapLength">虚线模式间隔长度</param>
    /// <returns>每个线段的 (X, Y, Width, Height) 元组列表</returns>
    public static List<(int X, int Y, int Width, int Height)> CalculateVerticalSegments(
        int x, int startY, int totalLength, int thickness,
        TableLineStyle style, int dashLength = 8, int gapLength = 4)
    {
        if (style == TableLineStyle.Solid)
        {
            return [( x, startY, thickness, totalLength )];
        }

        var segmentLength = style == TableLineStyle.Dotted ? Math.Max(1, thickness) : dashLength;
        var segmentGap = style == TableLineStyle.Dotted ? Math.Max(1, gapLength) : gapLength;

        var segments = new List<(int X, int Y, int Width, int Height)>();
        var currentY = startY;
        var endY = startY + totalLength;

        while (currentY < endY)
        {
            var segHeight = Math.Min(segmentLength, endY - currentY);
            segments.Add((x, currentY, thickness, segHeight));
            currentY += segmentLength + segmentGap;
        }

        return segments;
    }
}