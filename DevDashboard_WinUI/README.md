# DevDashboard

개발 프로젝트를 관리하고 빠르게 실행할 수 있는 WinUI 3 기반 대시보드 앱

## 현재 기능 목록

- 프로젝트 카드 관리 (추가/수정/삭제/핀 고정)
- 그룹 탭으로 프로젝트 분류
- 개발 도구 연동 실행 (커맨드 스크립트 포함)
- Git 상태 확인
- To-Do 목록 관리
- 작업 기록 관리 및 마크다운 내보내기
- 아이콘 선택 (Segoe MDL2 Assets)
- 다국어 지원 (ko-KR, en-US) — `.resw` + `x:Uid` 방식

## 실행/사용 방법

```
dotnet build
dotnet run
```

또는 Visual Studio에서 `F5` 실행

## 설정/외부 인터페이스 핵심 요약

- 설정 저장: `JsonStorageService` (로컬 JSON 파일)
- 프로젝트 DB: SQLite (`SqliteProjectRepository`)
- 버전 확인: GitHub Releases API
- 지역화: `Strings/ko-KR/Resources.resw`, `Strings/en-US/Resources.resw`
- 다이얼로그: `DialogWindowHost.ShowAsync(dialog)` — 독립 창에 표시, `MainWindow.OnRootGridLoaded`에서 `SetOwnerWindow(this)` 등록 필요

## 알려진 제한 사항

- `<Run>` 요소는 x:Uid 미지원 → code-behind에서 `LocalizationService.Get`으로 직접 설정
- `[ToolTipService.ToolTip]` x:Uid 패턴은 런타임 오류 발생 → MainWindow는 code-behind `ApplyToolTips()`로, DataTemplate 내부는 직접 XAML 속성으로 처리
- DashboardView DataTemplate 내 tooltip은 하드코딩 영문 문자열 (다국어 미지원)
- WinUI 3 비패키지 모드에서 ResourceLoader 동작 주의

## 상세 문서 링크

- [아키텍처](docs/readme/architecture.md)
- [동작 흐름](docs/readme/flow.md)
- [변경 이력](docs/readme/changelog.md)
