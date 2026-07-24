using DevDashboard.Infrastructure.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevDashboard.Presentation.Views.Dialogs;

/// <summary>프로젝트 자유 메모를 입력·수정하는 다이얼로그.
/// 비우고 저장하거나 [메모 삭제]를 누르면 메모가 지워집니다(두 경로의 결과가 같습니다).</summary>
public sealed partial class ProjectMemoDialog : ContentDialog
{
    /// <summary>저장 시 입력된 메모 (취소 시 의미 없음). 삭제로 닫으면 빈 문자열</summary>
    public string ResultMemo { get; private set; } = string.Empty;

    /// <summary>[메모 삭제]로 닫혔는지 여부.
    /// 삭제는 Hide()로 닫아 ShowAsync가 None을 반환하므로, 호출부는 이 값으로 저장 여부를 판단합니다.</summary>
    public bool IsDeleteRequested { get; private set; }

    /// <param name="projectName">메모 대상 프로젝트 이름 (제목에 표시)</param>
    /// <param name="currentMemo">현재 메모 (없으면 빈 문자열)</param>
    public ProjectMemoDialog(string projectName, string currentMemo)
    {
        InitializeComponent();

        Title = string.Format(LocalizationService.Get("ProjectMemo_TitleFormat"), projectName);
        PrimaryButtonText = LocalizationService.Get("Dialog_Save");
        CloseButtonText = LocalizationService.Get("Dialog_Cancel");

        MemoBox.PlaceholderText = LocalizationService.Get("ProjectMemo_Placeholder");
        MemoBox.Text = currentMemo;

        DeleteButton.Content = LocalizationService.Get("ProjectMemo_Delete");
        // 지울 메모가 있을 때만 삭제 버튼을 노출한다
        DeleteButton.Visibility = string.IsNullOrWhiteSpace(currentMemo)
            ? Visibility.Collapsed
            : Visibility.Visible;

        PrimaryButtonClick += (_, _) => ResultMemo = MemoBox.Text.Trim();
    }

    internal new async Task<ContentDialogResult> ShowAsync()
    {
        XamlRoot = App.MainWindow?.Content?.XamlRoot;
        return await base.ShowAsync();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        ResultMemo = string.Empty;
        IsDeleteRequested = true;
        Hide();
    }
}
