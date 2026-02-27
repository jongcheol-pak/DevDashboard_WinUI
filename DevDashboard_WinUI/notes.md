# DevDashboard — 작업 노트 (인덱스)

## 최근 변경 요약

| 날짜 | 내용 |
|------|------|
| 2026-02-27 | 네임스페이스 DevDashboard_WinUI → DevDashboard 전체 변경 |
| 2026-02-27 | [ToolTipService.ToolTip] x:Uid 패턴 전체 수정 (런타임 오류 해결) |
| 2025-01 | WinUI 표준 지역화 방식으로 마이그레이션 (x:Uid + .resw) |

## 미해결 이슈

없음

## 주의 사항

- `LocalizationService.Get(key)`는 내부적으로 `ResourceLoader`를 사용 (C# 코드 변경 불필요)
- `[ToolTipService.ToolTip]` x:Uid 패턴은 WinUI 3에서 런타임 XamlParseException을 발생시킴 — MainWindow는 코드비하인드 `ApplyToolTips()`로, DataTemplate 내부는 직접 XAML 속성으로 처리
- `<Run>` 요소는 x:Uid를 지원하지 않으므로 `MainWindow.xaml.cs`에서 `LocalizationService.Get`으로 처리
- ContentDialog에서 Title을 code-behind로 동적 설정하는 경우 (GroupDialog, ProjectSettingsDialog, HistoryDialog): x:Uid는 PrimaryButtonText, CloseButtonText만 적용
- DashboardView.xaml DataTemplate 내 tooltip은 하드코딩 영문 문자열 (다국어 미지원)

## 상세 로그 링크

- [docs/notes/2026-02.md](docs/notes/2026-02.md)
- [docs/notes/2025-01.md](docs/notes/2025-01.md)
