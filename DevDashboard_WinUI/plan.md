# 100개+ 카드 시작 지연 최적화

## 목적
카드 수 증가 시 앱 시작 지연 개선. git 프로세스/파일 I/O 동시 과다 생성 제한 + UI 먼저 표시.

## 구현 체크리스트

- [x] `ProjectCardViewModel.cs` — `_gitProbeSemaphore(8)`, `_validationSemaphore(16)` static 필드 추가
- [x] `ProjectCardViewModel.cs` — `ProbeGitRepositoryAsync`에 `_gitProbeSemaphore` 적용
- [x] `ProjectCardViewModel.cs` — `ValidatePathsAsync`에 `_validationSemaphore` 적용
- [x] `MainViewModel.cs` — `InitializeAsync()`: AddCardInternal 루프에서 Start* 제거, IsInitializing=false 후 Start* 시작
- [x] `MainViewModel.cs` — `RefreshAsync()`: 동일 패턴 적용
- [x] `MainViewModel.cs` — `HardRefresh()`: 동일 패턴 적용
- [x] `MainWindow.xaml.cs` — `using Microsoft.UI.Dispatching;` 추가
- [x] `MainWindow.xaml.cs` — `CheckLatestVersionAsync()`를 `DispatcherQueuePriority.Low`로 지연 실행

## 검증 결과
- `dotnet build` — 오류 0개, 경고 102개(기존 MVVMTK0045, 무시 가능)
