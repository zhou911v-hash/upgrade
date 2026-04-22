using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SmartBuildingLighting.Core.Enums;
using System.Windows;

namespace SmartBuildingLighting.App.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isOn)
        {
            return isOn
                ? new SolidColorBrush(Color.FromRgb(0, 255, 136))  // #00FF88 荧光绿
                : new SolidColorBrush(Color.FromRgb(74, 104, 128)); // #4A6880 暗蓝灰
        }
        return new SolidColorBrush(Color.FromRgb(74, 104, 128));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToStatusTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool isOn ? (isOn ? "开启" : "关闭") : "未知";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToOnOffColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isOn)
            return isOn
                ? new SolidColorBrush(Color.FromArgb(40, 0, 255, 136))   // 荧光绿半透明
                : new SolidColorBrush(Color.FromArgb(20, 30, 58, 95));   // 暗蓝半透明
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool inverse = string.Equals(parameter?.ToString(), "Inverse", StringComparison.OrdinalIgnoreCase);
        bool hasValue = value switch
        {
            null => false,
            string text => !string.IsNullOrWhiteSpace(text),
            _ => true
        };
        if (inverse)
            hasValue = !hasValue;

        return hasValue ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class CircuitStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not CircuitStatus status)
            return new SolidColorBrush(Colors.Gray);

        return status switch
        {
            CircuitStatus.On => new SolidColorBrush(Color.FromRgb(0, 255, 136)),       // #00FF88 荧光绿
            CircuitStatus.Fault => new SolidColorBrush(Color.FromRgb(255, 59, 92)),    // #FF3B5C 警示红
            CircuitStatus.Offline => new SolidColorBrush(Color.FromRgb(255, 176, 32)), // #FFB020 工业橙
            _ => new SolidColorBrush(Color.FromRgb(74, 104, 128))                      // #4A6880 暗蓝
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class CircuitStatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not CircuitStatus status)
            return "未知";

        return status switch
        {
            CircuitStatus.On => "运行中",
            CircuitStatus.Off => "已关闭",
            CircuitStatus.Fault => "故障",
            CircuitStatus.Offline => "离线",
            _ => "未知"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class CommunicationModeDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is CommunicationMode mode
            ? mode switch
            {
                CommunicationMode.Simulator => "模拟器",
                CommunicationMode.ModbusTcp => "Modbus TCP",
                _ => "未知"
            }
            : "未知";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class ScheduleTargetTypeDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is ScheduleTargetType scheduleTargetType
            ? scheduleTargetType switch
            {
                ScheduleTargetType.Circuit => "单回路",
                ScheduleTargetType.Group => "分组",
                ScheduleTargetType.SceneMode => "情景模式",
                ScheduleTargetType.AllCircuits => "全楼",
                _ => "未知"
            }
            : "未知";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class PageKeyStateBrushConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var pageKey = values.Length > 0 ? values[0]?.ToString() : null;
        var currentPageKey = values.Length > 1 ? values[1]?.ToString() : null;
        bool isActive = !string.IsNullOrWhiteSpace(pageKey) && string.Equals(pageKey, currentPageKey, StringComparison.Ordinal);
        string mode = parameter?.ToString() ?? string.Empty;

        return mode switch
        {
            "Background" => isActive
                ? new SolidColorBrush(Color.FromRgb(21, 41, 66))      // #152942 active 背景
                : new SolidColorBrush(Colors.Transparent),
            "Border" => isActive
                ? new SolidColorBrush(Color.FromRgb(0, 229, 255))     // #00E5FF 青色霓虹指示条
                : new SolidColorBrush(Colors.Transparent),
            "Foreground" => isActive
                ? new SolidColorBrush(Color.FromRgb(0, 229, 255))     // #00E5FF active 文字
                : new SolidColorBrush(Color.FromRgb(138, 169, 201)),  // #8AA9C9 默认文字
            _ => new SolidColorBrush(Colors.Transparent)
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
