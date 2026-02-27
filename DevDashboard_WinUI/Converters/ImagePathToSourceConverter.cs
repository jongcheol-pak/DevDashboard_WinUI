using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DevDashboard.Converters;

/// <summary>이미지 파일 경로를 BitmapImage URI로 변환합니다. 파일이 없거나 경로가 비어있으면 null을 반환합니다.</summary>
public class ImagePathToSourceConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        try
        {
            var bitmap = new BitmapImage(new Uri(path, UriKind.Absolute));
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
