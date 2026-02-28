using Microsoft.UI.Xaml.Data;

namespace DevDashboard.Presentation.Converters;

/// <summary>
/// ThemeMode enum과 RadioButton.IsChecked를 양방향 변환합니다.
/// ConverterParameter에 "Light" / "Dark" / "System" 문자열을 전달합니다.
/// </summary>
public class ThemeModeRadioConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ThemeMode mode && parameter is string paramStr &&
            Enum.TryParse<ThemeMode>(paramStr, out var target))
        {
            return mode == target;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is true && parameter is string paramStr &&
            Enum.TryParse<ThemeMode>(paramStr, out var target))
        {
            return target;
        }
        // WinUI에서는 DoNothing 대신 UnsetValue 사용
        return Microsoft.UI.Xaml.DependencyProperty.UnsetValue;
    }
}
