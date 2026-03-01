# 리팩토링 계획

## 발견된 리팩토링 포인트

### 1. ProjectSettingsDialog — 미사용 변수 제거
**파일**: `Presentation/Views/Dialogs/ProjectSettingsDialog.xaml.cs:35`
```csharp
var manager = WindowManager.Get(this); // 선언 후 아무것도 사용하지 않음
```
CommandScriptDialog와 달리 MinWidth 설정이 없어서 변수가 완전히 죽어있음. 삭제.

---

### 2. ProjectCardViewModel — 슬롯 인덱스 파싱 헬퍼 추출
**파일**: `Presentation/ViewModels/ProjectCardViewModel.cs`

아래 4개 메서드에 동일한 파싱 코드가 반복됨:
- `ExecuteCommandSlot`
- `ConfigureCommandSlot`
- `ClearCommandSlot`
- `ChangeCommandIcon`

```csharp
// 반복되는 코드
if (!int.TryParse(indexStr, out var index) || index < 0 || index >= CommandSlotCount) return;
```

→ `private static bool TryParseSlotIndex(string indexStr, out int index)` 헬퍼로 추출

---

### 3. ProjectCardViewModel — IsShellToolName 중복 체크 통합
**파일**: `Presentation/ViewModels/ProjectCardViewModel.cs`, `ProjectSettingsDialogViewModel.cs`

`ProjectSettingsDialogViewModel.IsShellToolName(name)` static 메서드가 이미 있지만 `private`이라 재사용 불가.
`ProjectCardViewModel` 내 3곳에서 동일 로직을 반복:
- `LaunchDevTool` (PowerShell 분기, Cmd 분기)
- `CheckDevToolValid`
- `CheckProjectPathValid`

```csharp
// 3곳에서 반복되는 코드
DevToolName.Equals(ProjectSettingsDialogViewModel.PowerShellToolName, StringComparison.OrdinalIgnoreCase)
|| DevToolName.Equals(ProjectSettingsDialogViewModel.CmdToolName, StringComparison.OrdinalIgnoreCase)
```

→ `ProjectSettingsDialogViewModel.IsShellToolName`을 `private → internal`로 변경하여 재사용

---

## 구현 체크리스트

- [x] `ProjectSettingsDialog.xaml.cs` — `var manager = WindowManager.Get(this);` 줄 삭제
- [x] `ProjectCardViewModel.cs` — `TryParseSlotIndex` 헬퍼 메서드 추가
- [x] `ProjectCardViewModel.cs` — 4개 메서드에서 파싱 코드를 `TryParseSlotIndex` 호출로 교체
- [x] `ProjectSettingsDialogViewModel.cs` — `IsShellToolName` 접근 제한자를 `private → internal`로 변경
- [x] `ProjectCardViewModel.cs` — `CheckDevToolValid`, `CheckProjectPathValid`에서 `ProjectSettingsDialogViewModel.IsShellToolName` 재사용

## 비고
- XAML 파일 변경 없음
- 동작 변경 없음 — 순수 코드 구조 정리
