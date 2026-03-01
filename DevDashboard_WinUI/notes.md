# DevDashboard — 작업 노트 (인덱스)

## 최근 변경 요약

| 날짜 | 내용 |
|------|------|
| 2026-07 | **CommandScripts DB 삭제 버그 수정** — `ProjectSettingsDialog` 저장 시 `ToProjectItem()`이 `CommandScripts`를 포함하지 않아 `Update()`에서 전부 삭제되던 문제. `AddOrUpdateProject` 업데이트 분기에서 기존 카드의 `CommandScripts`를 보존하도록 수정 |
| 2026-07 | **CommandScriptDialog 실행폴더 UI 개선 + 저장 버그 수정**
| 2026-07 | **MSIX StartupTask + LocalSettings 마이그레이션**
| 2026-07 | **저장 경로 마이그레이션** — `AppContext.BaseDirectory`(MSIX 설치폴더, 읽기전용) → `ApplicationData.Current.LocalFolder.Path`로 변경 (settings.json → LocalSettings, projects.db → LocalFolder) |
| 2026-07 | **ProjectHistoryDialog Closed 예외 미처리 수정** — `Vm.SaveAll()` 실패 시 `_closedTcs.TrySetResult()` 미호출 → 무한 대기 문제 수정. `try/catch/finally`로 항상 TCS 완료 보장 |
| 2026-07 | **HistoryDialog/ProjectHistoryDialog 목록 미표시 및 팝업 오류 수정** — 중첩 DataTemplate 내 `{Binding HasDescription, Converter={StaticResource BoolToVisibility}}` → `{x:Bind DescriptionVisibility}` 변경 + `ItemsSource` 한 번만 설정, `RefreshList()` 단순화 |
| 2026-07 | **HistoryDialog 아코디언 UI + 저장 즉시 표시** — 목록 클릭 시 상세 설명 펼침/접힘 구현, 삭제 버튼 Tapped 이벤트 전파 차단, `ExpandedVisibility`·`ExpandChevron` 프로퍼티 추가 |
| 2026-07 | **HistoryDialog 삭제 버튼 ToolTip 다국어 수정** — DataTemplate 내 `x:Uid=[ToolTipService.ToolTip]` 패턴 → `HistoryEntryViewModel.DeleteTooltip` 프로퍼티 + `{x:Bind DeleteTooltip}` 직접 바인딩으로 변경 (XamlParseException 해결) |
| 2026-07 | **HistoryDialog/ProjectHistoryDialog 저장 버튼 오류 수정** — `DatePicker.SelectedDate`(존재하지 않는 속성) → XAML `CalendarDatePicker`로 변경, 코드비하인드 `.SelectedDate` → `.Date` 수정 |
| 2026-07 | **그리드/리스트 뷰 전환 기능 제거** — 헤더 GridViewButton/ListViewButton 삭제, ViewMode enum·AppSettings 프로퍼티·ViewModel 코드 전체 제거 |
| 2026-07 | **DashboardView 개발 도구 아이콘 표시 방식 변경** — `&#xEC7A;` 항상 표시(보조색), `&#xE7BA;` 경고 아이콘을 도구 파일 미존재 시 추가 표시 (cmd/powershell 제외) |
| 2026-07 | **Git 버튼 미표시 버그 수정** — MainViewModel.AddOrUpdateProject() 새 카드 추가 분기에서 StartGitStatusLoad() + StartIconLoad() 누락 수정 |
| 2026-07 | **DashboardView Git 버튼 아이콘 교체** — FontIcon(Segoe MDL2 Assets) → GitHub 공식 SVG Path 요소로 변경 |
| 2026-07 | **ProjectSettingsDialog 아이콘 미리보기 추가**
| 2026-07 | **ProjectSettingsDialog 버그 수정** — 수정 모드에서 자기 자신 이름 중복 체크 문제 수정(SetExistingNames 호출 순서), 유효성 오류 메시지 ContentDialog 팝업으로 변경 |
| 2026-07 | **Dialog 타이틀 바 스타일 통일** — 9개 다이얼로그에 AppTitleBar Border(40px) 추가, ExtendsContentIntoTitleBar + SetTitleBar 적용, MainWindow 스타일 통일 |
| 2026-07 | **Dialog 모달 동작 추가** — DialogWindowHost.Show()에서 소유자 창 EnableWindow(false), 닫힐 때 EnableWindow(true) + SetForegroundWindow 복원 |
| 2026-07 | **Dialog WindowEx 전환** — 9개 다이얼로그(AppSettings/CommandScript/GitStatus/Group/History/IconPicker/ProjectHistory/ProjectSettings/Todo) Window → WindowEx 변경 |
| 2026-07 | **MainWindow WindowEx 전환** — WinUIEx `WindowEx` 베이스 클래스로 변경, `MinWidth=900`, `MinHeight=600` 적용 |
| 2026-07 | **Window.Title x:Uid 버그 수정** — 5개 Dialog Window에서 x:Uid 제거, 코드비하인드에서 LocalizationService.Get으로 Title 설정 |
| 2026-07 | **ContentDialog → Window 전환 완료** — 9개 Dialog 전면 교체, DialogWindowHost 단순화 |
| 2026-07 | ContentDialog 독립 창 표시 — DialogWindowHost 추가, 창 크기 변경 시 ContentDialog 크기 동기화 추가 |
| 2026-02-27 | 네임스페이스 DevDashboard_WinUI → DevDashboard 전체 변경 |
| 2026-02-27 | [ToolTipService.ToolTip] x:Uid 패턴 전체 수정 (런타임 오류 해결) |
| 2025-01 | WinUI 표준 지역화 방식으로 마이그레이션 (x:Uid + .resw) |

## 미해결 이슈

없음

## 주의 사항

- `LocalizationService.Get(key)`는 내부적으로 `ResourceLoader`를 사용 (C# 코드 변경 불필요)
- `[ToolTipService.ToolTip]` x:Uid 패턴은 WinUI 3에서 런타임 XamlParseException을 발생시킴 — MainWindow는 코드비하인드 `ApplyToolTips()`로, DataTemplate 내부는 직접 XAML 속성으로 처리
- `<Run>` 요소는 x:Uid를 지원하지 않으므로 `MainWindow.xaml.cs`에서 `LocalizationService.Get`으로 처리
- DashboardView.xaml DataTemplate 내 tooltip은 하드코딩 영문 문자열 (다국어 미지원)
- **WinUI 3 Window에서 `{x:Bind Prop, Converter={StaticResource ...}}`는 CS1503 빌드 오류 발생** → `{Binding Prop, Converter=...}` + `DataContext="{x:Bind Vm}"`으로 대체 (Window ≠ FrameworkElement)
- Dialog Window에서 `FileSavePicker` hwnd: `WindowNative.GetWindowHandle(this)` 사용 (App.MainWindow 아님)
- `DialogWindowHost.Show(dialog, w, h)`: 창 속성 설정 후 `Activate()` 호출 순서 필수
- **WinUI 3 Window.Title은 x:Uid로 설정 불가** — `Window.Title`은 CLR 속성으로 x:Uid DependencyProperty 할당 메커니즘 미지원 → `XamlParseException` 발생. Dialog Title은 코드비하인드에서 `LocalizationService.Get("욬XxxDialogTitle")`로 설정
- `ContentDialog`는 `App.MainWindow!.Content.XamlRoot` 사용 (DialogService, 오류 메시지용)
- **중첩 DataTemplate 내 `{Binding ..., Converter={StaticResource ...}}`는 리소스 조회 실패 위험** — `x:DataType` 지정 DataTemplate에서는 `{x:Bind ComputedProperty}` 패턴 사용 (ViewModel에 `Visibility` 계산 속성 추가)
- `ObservableCollection`이 `ItemsControl.ItemsSource`에 연결된 경우 `Clear()` + `Add()` 만으로 자동 갱신됨 — ItemsSource 재할당(null → 컬렉션) 불필요

## 상세 로그 링크

- [docs/notes/2026-07.md](docs/notes/2026-07.md)
- [docs/notes/2026-02.md](docs/notes/2026-02.md)
- [docs/notes/2025-01.md](docs/notes/2025-01.md)
