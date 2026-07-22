# AGENTS.md — Agent Guide (DevDashboard / WinUI 3)

> WinUI 3 (Windows App SDK) 데스크톱 앱. Claude Code의 모든 작업은 이 문서를 우선 따른다.

## Stack
- **언어**: C# / `net10.0-windows10.0.26100.0`
- **UI**: WinUI 3 (Microsoft.WindowsAppSDK 2.0.1), MVVM = `CommunityToolkit.Mvvm` 8.4.2
- **주요 패키지**: `Microsoft.Data.Sqlite` 10.0.7, `CommunityToolkit.WinUI.Controls.SettingsControls`,
  `CommunityToolkit.WinUI.Controls.Primitives`, `WinUIEx` 2.9.0, `System.Drawing.Common`
- **테스트**: **없음** (테스트 프로젝트 부재)

## Build & Test

```
"C:/Program Files/Microsoft Visual Studio/18/Professional/MSBuild/Current/Bin/MSBuild.exe" \
  "DevDashboard_WinUI/DevDashboard.csproj" -t:Build -p:Configuration=Debug -p:Platform=x64
```

- **csproj 파일명은 `DevDashboard.csproj`** — 폴더명(`DevDashboard_WinUI`)과 다르다.
- **`-p:Platform=x64` 필수.** AnyCPU로 빌드하면 MSIX 패키지 오류. (`Platforms`=x86;x64;ARM64)
- **`dotnet build`를 쓰지 말 것.** XamlCompiler가 크래시하면 오류 메시지 없이 실패해 원인 파악이 불가능하다.
  MSBuild는 실제 XamlCompiler 오류를 출력한다.
- 클린 리빌드: `-t:Rebuild`
- 산출물: `DevDashboard_WinUI/bin/x64/Debug/net10.0-windows10.0.26100.0/win-x64/DevDashboard.dll`
- **Test**: 없음. 검증 = 빌드 + 사용자 육안 확인. 테스트 추가는 사용자 승인 후에만.
- **실행**: 패키지형(MSIX). `launchSettings.json` 프로필 `DevDashboard (Package)`(MsixPackage) / `(Unpackaged)`(Project).
  `csproj`에 `WindowsPackageType` 미지정 = 패키지형.

### 상시 존재하는 기존 경고 5건 (신규 경고와 구분할 것)
- `NU1903` — `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 취약점 권고 (1건)
- `CS0612` — 자동 생성 `obj/.../XamlTypeInfo.g.cs`의 'Icon' obsolete (4건)

"경고 0"을 판정할 때는 **이 5건을 제외한 신규 경고 0**을 기준으로 한다.

## 이 레포의 함정 (전부 실제로 겪은 것)

1. **파일 인코딩 혼재.** 레거시 `.resw`/일부 `.cs`는 **BOM 포함**, 신규 파일은 BOM 없음.
   `check-utf8-and-lines` hook이 BOM을 경고하지만 **기존 파일의 BOM은 보존한다**
   (Phase 4에서 BOM 손실이 MAJOR 지적이었음). 전체 통일은 별도 작업(deferred 등재).
2. **다국어는 항상 ko/en 양쪽 등록.** 한쪽만 넣으면 다른 언어에서 빈 문자열이 되고 **빌드는 통과**한다.
3. **resw 키 형식 두 가지 공존.**
   - `x:Uid` 소비 → 속성 접미 필수: `Btn_Save.Content`, `TaskPage_Title.Text`
   - 코드 `LocalizationService.Get()` 소비 → 베어네임: `TaskStatus_Waiting`
   접미 없이 등록하고 `x:Uid`로 쓰면 **빌드 오류 없이 빈 라벨**이 된다.
4. **`Palette.xaml`은 `Default`(다크) 딕셔너리 하나뿐.** 앱이 다크 고정이라 Light/HighContrast 분기를
   의도적으로 두지 않았다(파일 상단 주석에 명시). **새 색은 Default에만 추가.**
5. **`x:Bind` 함수 바인딩은 Converter도 ThemeResource도 받지 못한다.** 색·Visibility가 필요하면
   코드비하인드 정적 헬퍼가 `Brush`/`Visibility`를 **직접 반환**한다
   (`TestPage.StatusBrush`, `TaskPage.PriorityBrush` 선례).
6. **WinUI `Border`는 점선 테두리 미지원.** `StrokeDashArray`는 `Shape` 속성 →
   `Rectangle`로 그린다 (`DashedAddButtonStyle` 선례).
7. **RadioButton 커스텀 템플릿의 GoToState 덮어쓰기.** `CommonStates`와 `CheckStates`가
   같은 요소의 같은 프로퍼티를 건드리면 서로 덮어써 "선택된 항목에 마우스를 올리면 선택 표시가
   사라지는" 버그가 난다. **선택 배경과 호버 오버레이를 별도 요소로 분리**할 것
   (`SettingsNavItemStyle`·`SegmentedToggleStyle` 선례, `Styles.xaml` 상단 주석).
8. **IDE LSP가 WinUI/CommunityToolkit 어셈블리를 로드하지 못해 CS0234/CS0246/CS9248 오탐을
   대량으로 낸다.** LSP 진단이 아니라 **MSBuild 결과를 신뢰**할 것.
9. **`Window` XAML에서 `{x:Bind ... Converter=...}`는 CS1503 빌드 오류.**
   WinUI 3 `Window`는 `FrameworkElement`가 아니다 → `{Binding}`으로 우회.
10. **중첩 ContentDialog 미지원.** 내부에서 다이얼로그를 띄우려면
    Hide → 표시 → `TaskCompletionSource`로 재표시하는 패턴을 쓴다.
11. **공용 DataTemplate 수정 주의.** 하나의 템플릿을 여러 화면이 소비할 수 있다
    (예: 과거 `TaskCardTemplate`이 칸반·목록 공용이었음). 고치기 전에 소비처를 grep으로 전수 확인.

## Conventions

- **아키텍처**: DDD 계열 4계층 (`Domain` / `Infrastructure` / `Presentation` / `Shared`).
  **Application 레이어는 없다** — 유스케이스는 ViewModel이 담당한다.
  - 의존 방향: `Presentation` → `Domain` ← `Infrastructure`.
    `IProjectRepository`가 `Domain/Repositories/`에 있어 의존이 역전돼 있다(DIP).
  - 도메인에 행위를 둔다: `TodoItem.OnStatusChanged`(상태↔완료 동기화),
    `NotificationService`(순수 계산 도메인 서비스).
- **MVVM**: `[ObservableProperty]` partial property + `[RelayCommand]` (CommunityToolkit.Mvvm)
- **DI**: **컨테이너 미사용.** 생성자 주입을 손으로 배선한다
  (`App.xaml.cs` → `DatabaseContext`/`SqliteProjectRepository` 직접 생성).
- **에러 처리**: `Result<T>` 미사용. 예외 + 다이얼로그/`Debug.WriteLine`.
  **실패를 조용히 삼키지 말 것** — 백그라운드 저장은 `QueueSave`처럼 try/catch + 로그.
- **비동기**: `.Result`/`.Wait()` 금지. `async void`는 **이벤트 핸들러만**
  (`Task` 반환 메서드를 `_ =`로 버리면 예외가 무음 처리되므로, 핸들러는 `async void` + `await`).
- **파일**: 1500라인은 분리 "검토" 신호(강제선 아님). 주석은 **한글**, "왜"만 설명.
  **신규 파일은 UTF-8 BOM 없음, 기존 파일은 현재 인코딩 유지**(함정 1).
- **GlobalUsings.cs**가 Domain 네임스페이스를 전역 using으로 등록한다 — 개별 파일에 다시 쓰지 말 것.

## Repository Structure

```
DevDashboard_WinUI/                 # 레포 루트
├── DevDashboard.slnx
├── plan.md                         # 진행 중 계획 (덮어쓰기)
├── docs/
│   ├── prd.md                      # PRD (단일 파일)
│   ├── plans/deferred.md           # Deferred/Follow-up 대장
│   └── screenshots/
└── DevDashboard_WinUI/
    ├── DevDashboard.csproj
    ├── notes.md                    # 작업 내역 영구 기록 (루트 아님 — 이 폴더 안)
    ├── GlobalUsings.cs
    ├── App.xaml(.cs)               # OnLaunched → MainWindow, 언어·테마 적용
    ├── MainWindow.xaml(.cs)        # 진입점 + 페이지 네비(ShowPage/ShowDashboard)
    ├── Domain/{Entities,ValueObjects,Enums,Repositories,Services}/
    ├── Infrastructure/{Persistence,Services}/
    ├── Presentation/{ViewModels,Views,Views/Dialogs,Converters,Controls}/
    ├── Shared/Collections/
    ├── Resources/{Palette.xaml,Styles.xaml,Converters.xaml}
    └── Strings/{ko-KR,en-US}/Resources.resw
```

- **페이지 네비게이션**: `Frame` 미사용. `MainWindow.ShowPage(UIElement)` / `ShowDashboard()`가
  `DashboardContent.Content`를 스왑한다. 전체 페이지(작업/테스트/알림)는 `UserControl`.
- **다이얼로그**: 전부 `ContentDialog` 서브클래스. `ShowAsync()`를 `new`로 섀도잉해
  `XamlRoot = App.MainWindow?.Content?.XamlRoot` 설정 후 `base.ShowAsync()` 호출.

## 산출물·파일 관리
- **빌드 산출물**: `bin/` · `obj/` (gitignore)
- **런타임 생성물**: `ApplicationData.Current.LocalSettings`(앱 설정 JSON), SQLite DB, 아이콘 캐시

## 데이터 접근
- **DB**: SQLite (로컬, `Microsoft.Data.Sqlite`). 스키마 변경은 `DatabaseContext.MigrateSchema`의
  `AddColumnIfNotExists` + `AllowedIdentifiers` 화이트리스트 패턴을 따른다(하위 호환 필수).
- **읽기 시 `HasColumn` 가드**를 두어 구버전 DB에서도 깨지지 않게 한다.
- ⚠️ **INSERT 컬럼을 추가하면 루프 안에서 파라미터 값도 갱신할 것** — 과거 `@method` 바인딩 누락으로
  silent data loss가 발생한 적이 있다.

## DO NOT
- `dotnet build`로 검증 (XamlCompiler 오류가 숨는다) / `-p:Platform` 생략
- `Window.Current` 사용 (UWP 잔재)
- 문구 하드코딩 (resw + ko/en 양쪽 필수)
- 기존 파일의 BOM 제거
- `Palette.xaml`에 Light/HighContrast 딕셔너리 신설 (다크 단일 정책)
- 공용 DataTemplate을 소비처 확인 없이 수정
- `bin/`, `obj/` 커밋
- 코드·문서·notes·plan 등 어떤 파일에도 실제 IP·계정·비밀번호·토큰·DB 연결문자열 기록
- 검증 스크립트에 평문 자격증명·`-WindowStyle Hidden`·과도한 `-ExecutionPolicy Bypass`

## Plan Location

```
Plan Location: plan.md          # 루트, 덮어쓰기. 완료 plan의 Deferred는 docs/plans/deferred.md로 이관
PRD Location:  docs/prd.md      # 단일 파일
```

## Git
- 기본 브랜치 `master`. 원격 `origin` (GitHub `jongcheol-pak/DevDashboard_WinUI`).
- commit 메시지는 한글 `{유형}: {요약}` — 유형: 기능/수정/리팩토링/문서/설정/병합
- 작업 브랜치 `task/<slug>` → `--no-ff` 병합.
