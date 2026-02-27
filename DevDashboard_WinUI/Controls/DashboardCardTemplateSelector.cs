using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevDashboard.Models;
using DevDashboard.ViewModels;

namespace DevDashboard.Controls;

/// <summary>ProjectCardViewModel이면 프로젝트 카드, DropPlaceholder이면 드롭 위치, AddCardPlaceholder이면 추가 카드 템플릿을 선택합니다.</summary>
public class DashboardCardTemplateSelector : DataTemplateSelector
{
    public DataTemplate? ProjectCardTemplate { get; set; }
    public DataTemplate? AddCardTemplate { get; set; }
    public DataTemplate? DropPlaceholderTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
        => item switch
        {
            ProjectCardViewModel => ProjectCardTemplate,
            DropPlaceholder => DropPlaceholderTemplate,
            _ => AddCardTemplate
        };

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
        => SelectTemplateCore(item);
}
