using DevDashboard.Infrastructure.Services;
using Microsoft.UI.Xaml.Controls;

namespace DevDashboard.Presentation.Views.Dialogs;

/// <summary>테스트 항목의 메모를 입력·수정하는 다이얼로그. 비우고 저장하면 메모가 삭제됩니다.</summary>
public sealed partial class TestNoteDialog : ContentDialog
{
    /// <summary>저장 시 입력된 메모 (취소 시 의미 없음)</summary>
    public string ResultNote => NoteBox.Text;

    /// <param name="testName">메모 대상 테스트 이름 (제목 아래에 표시)</param>
    /// <param name="currentNote">현재 메모 (없으면 빈 문자열)</param>
    public TestNoteDialog(string testName, string currentNote)
    {
        InitializeComponent();

        Title = LocalizationService.Get("TestNote_Title");
        PrimaryButtonText = LocalizationService.Get("Dialog_Save");
        CloseButtonText = LocalizationService.Get("Dialog_Cancel");

        TargetText.Text = testName;
        NoteBox.PlaceholderText = LocalizationService.Get("TestNote_Placeholder");
        NoteBox.Text = currentNote;
    }

    internal new async Task<ContentDialogResult> ShowAsync()
    {
        XamlRoot = App.MainWindow?.Content?.XamlRoot;
        return await base.ShowAsync();
    }
}
