# 2026-02-28 코드 리뷰 및 수정

## 수행한 작업
전체 코드 리뷰 (메모리 누수, 예외 처리, 최적화) 후 7개 항목 수정.
다이얼로그 표시 시 카드 컨트롤 깜빡임 원인 분석 및 수정 (EnableWindow → XAML 오버레이).

### 8. [높음] 다이얼로그 표시 시 카드 깜빡임
- **문제:** `EnableWindow(hwnd, false)` → `WM_ENABLE` → MicaBackdrop 비활성 전환 + 모든 Button VisualState 전이 + ThemeResource 브러시 재평가 → 깜빡임
- **수정:** `EnableWindow` 제거, XAML 투명 Grid 오버레이로 입력 차단 방식으로 전환
- `_modalDepth` 카운터로 중첩 다이얼로그 대응
- `GWLP_HWNDPARENT` 소유 관계가 유지되므로 MicaBackdrop은 활성 상태 유지
- **변경 파일:** `Services/DialogWindowHost.cs`

## 변경된 파일 목록

| 파일 | 변경 내용 |
|------|----------|
| `ViewModels/ProjectCardViewModel.cs` | `ClearIconCache()` 정적 메서드 추가, `ClearCommandSlot`의 `RemoveAt` → `null` 할당 |
| `ViewModels/MainViewModel.cs` | `RefreshAsync()`, `HardRefresh()`에서 `ProjectCardViewModel.ClearIconCache()` 호출 추가 |
| `Views/DashboardView.xaml.cs` | `_subscribedCards` HashSet 추가, `Reset` 액션 시 이벤트 구독 누수 수정 |
| `Views/Dialogs/GitStatusDialog.xaml.cs` | `CancellationTokenSource` 추가, `Closed`에서 취소/해제, `LoadGitStatusAsync`에 토큰 전달 |
| `Services/DialogService.cs` | `App.MainWindow?.Content?.XamlRoot` null 체크 추가 |
| `Services/DevToolDetector.cs` | double-checked locking + `volatile` 적용 |
| `Services/VersionCheckService.cs` | `ReadAsStreamAsync()` 반환 Stream에 `using` 추가 |

## 검증 결과
- **빌드:** 오류 0개, 경고는 기존 MVVMTK0045만 (변경 없음)

## 상세 이슈 및 수정 내용

### 1. [높음] 정적 아이콘 캐시 메모리 누적
- **문제:** `ProjectCardViewModel._iconLoadTasks` (static ConcurrentDictionary)가 BitmapImage를 영구 보관
- **수정:** `ClearIconCache()` 메서드 추가, Refresh/HardRefresh 시 호출

### 2. [높음] DashboardView ResetWith() 이벤트 구독 누수
- **문제:** `BulkObservableCollection.ResetWith()`는 `Reset` 액션 발생 → `OldItems`가 null → 기존 카드의 `UnsubscribeCardEvents()` 미호출
- **수정:** `_subscribedCards` HashSet으로 구독 대상 추적, Reset 시 전체 해제 후 재구독

### 3. [높음] GitStatusDialog 취소 토큰 미사용
- **문제:** 다이얼로그 닫아도 Git 프로세스가 30초까지 계속 실행
- **수정:** `CancellationTokenSource` 필드 추가, `Closed`에서 `Cancel()`+`Dispose()`, `LoadGitStatusAsync`에 토큰 전달

### 4. [중간] DialogService NullReferenceException 가능
- **문제:** `App.MainWindow!.Content.XamlRoot`이 Window 종료 후 null일 수 있음
- **수정:** null 체크 후 null이면 조기 반환

### 5. [중간] ClearCommandSlot의 RemoveAt 슬롯 매핑 어긋남
- **문제:** `RemoveAt(index)` 사용 시 뒤 슬롯이 한 칸씩 당겨짐
- **수정:** `_item.CommandScripts[index] = null`로 변경

### 6. [낮음] DevToolDetector 캐시 경합
- **문제:** lock 밖에서 캐시 null 체크 → 두 스레드가 동시에 파일 시스템 탐색
- **수정:** double-checked locking + `volatile` 적용

### 7. [낮음] VersionCheckService Stream 미해제
- **문제:** `ReadAsStreamAsync()` 반환 Stream을 `using`으로 감싸지 않음
- **수정:** `using var stream = ...` 추가
