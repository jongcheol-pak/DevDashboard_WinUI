using Microsoft.UI.Xaml.Data;

namespace DevDashboard.Converters;

/// <summary>DateTime? 값을 지정된 형식 문자열로 변환합니다. null이면 "-"를 반환합니다.</summary>
public class NullableDateTimeToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTime dt)
        {
            var format = parameter as string ?? "yyyy-MM-dd HH:mm";
            return dt.ToString(format);
        }

        return "-";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
