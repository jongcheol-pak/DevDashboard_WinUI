using DevDashboard.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevDashboard.Presentation.Controls;

/// <summary>
/// 런처 사이드바 아이템/플레이스홀더 템플릿 선택기.
/// </summary>
public sealed class LauncherItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? IconTemplate { get; set; }
    public DataTemplate? PlaceholderTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
        => item switch
        {
            LauncherItemViewModel => IconTemplate,
            DropPlaceholder => PlaceholderTemplate,
            _ => IconTemplate
        };
}
