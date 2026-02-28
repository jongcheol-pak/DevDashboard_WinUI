using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace DevDashboard.Presentation.Converters;

/// <summary>태그/개발 도구 이름(string)을 배지 배경색으로 변환합니다.</summary>
public class TagColorConverter : IValueConverter
{
    private static readonly SolidColorBrush WhiteBrush = new(Colors.White);

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        // ConverterParameter="text" 인 경우 항상 흰색 반환
        if (parameter is string p && p == "text")
            return WhiteBrush;

        // 배경색: 도구 이름 해시 기반 HSL 색상, 명도 0.30 고정
        if (value is not string name || string.IsNullOrWhiteSpace(name))
            return new SolidColorBrush(Colors.Gray);

        var hash = GetFnv1aHash(name);
        var hue = hash % 360;
        var color = HslToRgb(hue, 0.55, 0.30);
        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();

    /// <summary>도구 이름을 대소문자 무시하여 FNV-1a 결정론적 해시로 변환합니다.</summary>
    private static uint GetFnv1aHash(string name)
    {
        const uint fnvPrime = 16777619;
        const uint offsetBasis = 2166136261;
        uint hash = offsetBasis;
        foreach (var ch in name.ToUpperInvariant())
        {
            hash ^= ch;
            hash *= fnvPrime;
        }
        return hash;
    }

    /// <summary>HSL 값을 RGB Color로 변환합니다.</summary>
    private static Windows.UI.Color HslToRgb(double h, double s, double l)
    {
        var c = (1 - Math.Abs(2 * l - 1)) * s;
        var x = c * (1 - Math.Abs(h / 60 % 2 - 1));
        var m = l - c / 2;

        var (r1, g1, b1) = h switch
        {
            < 60 => (c, x, 0.0),
            < 120 => (x, c, 0.0),
            < 180 => (0.0, c, x),
            < 240 => (0.0, x, c),
            < 300 => (x, 0.0, c),
            _ => (c, 0.0, x)
        };

        return Windows.UI.Color.FromArgb(
            255,
            (byte)((r1 + m) * 255),
            (byte)((g1 + m) * 255),
            (byte)((b1 + m) * 255));
    }
}

/// <summary>개발 도구 이름(string)을 그대로 표시합니다.</summary>
public class DevToolNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is string name && !string.IsNullOrWhiteSpace(name)
            ? name
            : string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
