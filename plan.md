# plan.md — Phase 2: 작업(칸반) — 페이지 네비게이션 컨테이너 + 작업 도메인 확장 + 칸반/목록 페이지

**PRD**: docs/prd.md (Phase 2 = FR-C3, FR-T1, FR-T2, FR-T3, FR-T4, FR-T5, FR-T6, FR-T8)
**전체 목표**: DevDashboard 5개 영역 리디자인(§PRD). 이 plan은 **Phase 2(작업)** 만 다룬다.
**이전 plan**: Phase 1(설정) — 완료·커밋(`3eb995f`). notes 보존.
**다음 plan**: Phase 3(테스트) — 별도 승인·실행.

## 요구 이해

> 원문(PRD §4 작업 Phase 2 발췌): "FR-T1 TodoItem에 상태 4값(예정/진행 중/완료/보류)·카테고리·우선순위(높음/보통/낮음)·시작/종료일 추가+영속화(마이그레이션). FR-T2 작업을 전체 페이지로 전환, 칸반(4열)/목록 뷰 전환. FR-T3 칸반 카드 드래그앤드롭 상태 변경. FR-T4 카테고리 필터·그룹핑·상태별 개수. FR-T5 새 작업/편집 다이얼로그+삭제 확인. FR-T6 테스트 추가 토글(작업↔테스트 연결). FR-T8 작업 카드 연결 테스트 상태 배지. FR-C3 DashboardContent 앵커에 페이지 네비게이션 컨테이너 도입." + 사용자 스코핑(2026-07-18): 작업 페이지=프로젝트별만(교차 집계 Deferred), 담당자(FR-T7) 제외, FR-T6/T8 이번 포함, 시각은 PRD §3 추출값으로 진행(시안 HTML 부재).

이해한 요구(Phase 2):
- 작업(To-Do)을 **프로젝트별 전체 페이지(칸반 4열 / 목록 토글)** 로 전환하고, 현재 다이얼로그(TodoDialog)를 대체한다.
- `TodoItem`에 **보류 상태·카테고리·우선순위·시작/종료일·연결 테스트 ID**를 추가하고 하위호환 마이그레이션으로 영속화한다.
- 칸반 **드래그로 상태 변경**, **카테고리 필터·상태별 개수·카테고리 그룹핑**, **새 작업/편집 다이얼로그·삭제 확인**을 제공한다.
- 새 작업의 **"테스트 추가" 토글**로 프로젝트 "작업" 테스트 카테고리에 테스트를 자동 등록하고(작업↔테스트 연결), **작업 카드에 연결 테스트 상태 배지**를 표시한다.
- 셸의 `DashboardContent` 앵커에 **대시보드 ↔ 페이지 콘텐츠 스왑 네비게이션**(뒤로가기 "대시보드")을 도입하되, 첫 실제 페이지(작업)와 함께 구축한다.

## Goal
`DashboardContent`(MainWindow.xaml:345) 콘텐츠 스왑 기반의 경량 페이지 네비게이션을 도입하고(Frame 미도입 — 기존 `.Content =` idiom), 프로젝트 카드의 "작업" 버튼이 다이얼로그 대신 **작업 칸반 페이지**를 연다. `TodoItem`/`TodoStatus`/신규 `TaskPriority`를 확장하고 SQLite 하위호환 마이그레이션으로 영속화한다. 기존 `TodoDialogViewModel`의 필터/카운트/그룹 로직을 신규 `TaskPageViewModel`로 이전하고, 구 다이얼로그 자산과 고아(`TodoDetailDialogViewModel`)를 정리한다.

## Investigation Log (근거)

### 네비게이션·셸 (explorer 확인 + 직접 Read)
- **스왑 앵커**: `ContentControl x:Name="DashboardContent"`(MainWindow.xaml:345, 우측 컬럼 2행 Grid의 Row 1). 그룹 탭은 별도 `<Border Grid.Row="0">`(MainWindow.xaml:234, **x:Name 없음**).
- **콘텐츠 주입**: `DashboardContent.Content = new DashboardView { DataContext = _viewModel }` — MainWindow.xaml.cs:118(최초), :500(언어 변경 재주입). **Frame/NavigationView/Navigate 전무**(코드베이스 grep 0). **DI 컨테이너 없음** — App.xaml.cs → MainWindow 생성자 수동 주입, VM/View는 `new`로 조립.
- **작업 다이얼로그 진입(유일 경로)**: 카드 `Command="{Binding OpenTodoCommand}"`(DashboardView.xaml:490) → `ProjectCardViewModel.OpenTodo()`가 `OpenTodoRequested` 이벤트 발생 → `DashboardView.xaml.cs OnOpenTodoRequested`(178-193)가 `new TodoDialog(card.CreateTodoDialogViewModel()).ShowAsync()` 후 `card.OnTodoDialogClosed(...)`. History/TestList/GitStatus도 동일 이벤트 릴레이 패턴(DashboardView.xaml.cs:163-227). **알림 진입점 없음**(Phase 5).
- **카드 버튼군**: DashboardView.xaml:472 StackPanel — Git(478)/작업(490)/작업기록(499)/테스트(508), 모두 `[RelayCommand]` → `Open*Requested` 이벤트.

### 도메인·영속성 (직접 Read)
- **TodoItem**(Domain/Entities/TodoItem.cs): Id/Text/IsCompleted/Status(TodoStatus)/CompletedAt/Description/CreatedAt. `OnStatusChanged`가 IsCompleted/CompletedAt 동기화. **카테고리·우선순위·시작/종료일·연결 없음**(전무 재확인).
- **TodoStatus**(TodoStatus.cs): Waiting/Active/Completed **3값**(보류 없음).
- **TestItem**(TestItem.cs): 상태 문자열 상수 `Testing`/`Fix`/`Done`, `IsCompleted => Status==Done`. TestCategory 하위. Phase 3에서 통과/실패/미실행 전환 예정(이번 미전환).
- **DatabaseContext**(Infrastructure/Persistence/DatabaseContext.cs): `Todos` 테이블 = Id/ProjectId/Text/Description/IsCompleted/Status/CompletedAt/CreatedAt(219-229). 마이그레이션 = `MigrateSchema`(114-123)가 `AddColumnIfNotExists` + `AllowedIdentifiers` 화이트리스트(142-152) 검증. `Todos.Status`는 이미 마이그레이션 선례(121-122). **신규 컬럼은 화이트리스트 등록 필수**(미등록 시 ArgumentException). `Category`는 이미 화이트리스트에 존재(Projects.Category용, 재사용 가능).
- **SqliteProjectRepository**: Todos read/write **3경로** — `GetTodos`(81-113, 메인), `InsertTodos`(519-552, 저장), `ReadTodosForProject`(784-815, 가져오기용). `HasColumn` 하위호환 가드 패턴(90,664) — 신규 컬럼 read 시 동일 적용. 저장은 `SaveTodos`(192-201, delete+reinsert). 테스트 로드 `GetTestCategories`(74-78)/저장 `SaveTestCategories`(214-224, TestItems+TestCategories delete 후 reinsert).
- **저장 플로우(현행)**: 다이얼로그 close → `TodoDialogViewModel.SaveToModel`(model.Todos 교체) → `ProjectCardViewModel.OnTodoDialogClosed`(TodoChanged 이벤트) → `MainViewModel.OnTodoChanged`(MainViewModel.cs:345 `SaveTodos`). 페이지 전환 시 **증분 저장**(변경마다 SaveTodos)으로 재설계 — Design 참조.

### 재사용·고아 (explorer 확인)
- **TodoDialogViewModel**(Presentation/ViewModels/TodoDialogViewModel.cs): 상태 필터(`RefreshFilter` 76-101)·카운트(`UpdateTabCounts` 104-118)·날짜 그룹화·`ChangeStatus`(183-195, 완료 시 콜백)·`StatusOptions`. → TaskPageViewModel로 이전 대상.
- **TagColorConverter**(Presentation/Converters/DevToolConverters.cs:8-69): 이름→FNV-1a 해시→hue→`HslToRgb(hue, S=0.45, L=0.36)`. 문자열만으로 결정론적 색 → 카테고리 dot 색에 재사용 가능(작은 dot은 L 상향 파라미터화 여지).
- **드래그 패턴**: 대시보드 카드 핀 정렬(DashboardView.xaml:86-91 + xaml.cs:296-409) — `DragStarting`(SetText(id), RequestedOperation=Move)·`DragOver`(AcceptedOperation=Move + 플레이스홀더)·`Drop`(VM 호출). **드래그 컨테이너는 Grid 사용**(런처 주의사항 notes:26 — Button은 포인터 캡처로 드래그 불가). 칸반 열 간 이동에 참고.
- **기본 카테고리 소스**: `AppSettingsDialogViewModel.DefaultTaskCategories`(AppSettingsDialogViewModel.cs:218 = ["UI·UX","프론트엔드","백엔드"]). **`ProjectSettingsDialogViewModel`이 이미 `AppSettingsDialogViewModel.DefaultTechStackTags`를 정적 참조**(cs:120,298) → 칸반 VM도 동일 cross-VM 정적 참조 패턴으로 재사용(신규 공유 상수 도입 불요).
- **설정 접근**: `MainViewModel._settings`(AppSettings), `ProjectCardViewModel`가 생성자로 `_settings` 수령(MainViewModel.cs:150). `_settings.TaskCategories`(Phase 1 추가) 접근 가능.
- **고아(D-6)**: `TodoDetailDialogViewModel`(사용처 grep 0 — 정의부+PRD만) + 대응 View 부재. `TodoItem.Description`은 현재 UI 미연결(이번 새 작업 다이얼로그의 "설명"으로 연결 → 고아 해소).
- **완료→작업기록 팝업**: `ShowWorkLogPopupOnTodoComplete`(AppSettings.cs:43) → 현재 `TodoDialog.xaml.cs`에서 완료 전환 시 `HistoryDialog` 표시. 페이지 전환 후에도 보존(페이지는 다이얼로그가 아니므로 HistoryDialog 직접 표시).
- **확인 다이얼로그**: `DialogService.ShowConfirmAsync`(Infrastructure/Services/DialogService.cs) 재사용.

### 위키·시안
- 위키 참조: vault 미설정 — 코드 1차 출처로 진행.
- 리디자인 시안: 목업 HTML(claude.ai/design)이 레포에 없음. `docs/screenshots/todo-and-test.png`는 **현재 앱**(Phase 0 적용) 캡처이지 리디자인 시안 아님. → 시각 명세는 PRD §3 추출값 기준(사용자 결정).

## 시각 요소 분해 (작업 칸반 페이지 — 출처 PRD §3, 목업 부재로 픽셀값은 사용자 확인 필요)

| 요소 | 속성 | 디자인 값 | 확인 방법 |
|------|------|----------|-----------|
| 칸반 열 | 개수·라벨 | 4열: 예정 / 진행 중 / 완료 / 보류 | PRD §3 "칸반 상태(4)" |
| 열 헤더 | 개수 배지 | 각 열에 항목 개수 표시 | PRD §4 FR-T4 "상태별 개수 표시" |
| 뷰 전환 | 컨트롤 | 칸반 ↔ 목록 토글 | PRD §4 FR-T2 |
| 필터 | 컨트롤 | 카테고리 필터(전체+각 카테고리) | PRD §4 FR-T4 |
| 작업 카드 | 카테고리 | 색 dot + 카테고리 이름 | PRD §3 "각 색상 dot" |
| 작업 카드 | 우선순위 | 높음/보통/낮음 표시(기본 보통) | PRD §3 "작업 우선순위(3)" |
| 작업 카드 | 날짜 | 시작일·종료일 | PRD §4 FR-T1 |
| 작업 카드 | 테스트 배지 | 연결 테스트 상태(미실행/통과/실패 = 현재 Testing/Done/Fix 매핑) | PRD §4 FR-T8 |
| 작업 카드 | 액션 | 수정 / 삭제 | PRD §4 FR-T5 |
| 팔레트·폰트 | — | 배경 #131316·카드 #1c1c20·강조 코랄 #f0716a·본문 #e8e6e3, Malgun Gothic | PRD §3(Phase 0에서 전역 적용 완료) |
| 뒤로가기 | 컨트롤 | "대시보드" 복귀 | PRD §4 FR-C3 |
> 팔레트·폰트·SettingsCard/pill 스타일은 Phase 0 + 기존 idiom으로 이미 확정. 세부 레이아웃(간격·정렬)은 목업 부재 → WinUI idiom으로 구현하고 빌드 후 사용자 확인.

## Tasks

### 진행 체크리스트
- [x] T1 — 작업 도메인 확장
- [x] T2 — Todos 영속화 마이그레이션
- [x] T3 — 페이지 네비게이션 컨테이너(FR-C3)
- [x] T4 — TaskPageViewModel
- [x] T5 — 작업 칸반 페이지 View
- [x] T6 — 칸반 드래그앤드롭
- [x] T7 — 새 작업/편집 다이얼로그 (T5보다 먼저 구현 — 아래 순서 조정)
- [x] T8 — 진입점 재배선 + 고아 정리
- [x] T9 — 작업↔테스트 연결 + 카드 배지

### T1 — 작업 도메인 확장 (Type C)
- **내용**: (a) `TodoStatus`에 `Hold` 추가(예정=Waiting·진행중=Active·완료=Completed·보류=Hold — 열거자명 유지, 표시 라벨만 "예정"/"보류"). (b) 신규 `TaskPriority` enum(`High`/`Normal`/`Low`, 기본 Normal) → `Domain/Enums/Enums.cs`에 추가(SortOrder 등과 동일 파일). (c) `TodoItem`에 필드 추가: `[ObservableProperty] string Category`(빈=미분류), `[ObservableProperty] TaskPriority Priority = Normal`, `[ObservableProperty] DateTime? StartDate`, `[ObservableProperty] DateTime? EndDate`, `string LinkedTestId`(빈=연결없음). 한글 XML 주석.
- **Design**: 배치 = Domain(TodoStatus.cs/Enums.cs/TodoItem.cs). 신규 심볼 = `TaskPriority` enum(작업 우선순위 3값), `TodoStatus.Hold`(보류 상태), TodoItem 5필드. 의존 = TodoItem이 TaskPriority/TodoStatus 참조. 참조하는 곳 = T2 영속화·T4 VM·T7 다이얼로그. 비추상화 = 우선순위를 값 객체가 아닌 단순 enum, 카테고리/연결은 문자열 ID(기존 Category/CategoryId 관례와 동형) — 별도 엔티티 만들지 않음.
- **Files**: `Domain/Entities/TodoStatus.cs`, `Domain/Enums/Enums.cs`, `Domain/Entities/TodoItem.cs`
- **Edge**: 기존 데이터엔 신규 필드 없음 → 기본값(Category 빈·Priority Normal·날짜 null·LinkedTestId 빈). Hold는 신규라 기존 데이터에 없음.
- **Halt Forecast**: 없음(비파괴 필드/열거자 추가).
- **Acceptance**: 빌드 0. TodoStatus 4값·TaskPriority 3값 존재, TodoItem에 5필드 존재(기본값 확인). 기존 코드 컴파일 유지(OnStatusChanged 등 무영향).

### T2 — Todos 영속화 마이그레이션 (Type D)
- **내용**: (a) `DatabaseContext`: `AllowedIdentifiers`에 `Priority`,`StartDate`,`EndDate`,`LinkedTestId` 추가(`Category`는 기존재). `MigrateSchema`에 `AddColumnIfNotExists(conn,"Todos",...)` 5개(Category TEXT DEFAULT ''·Priority TEXT DEFAULT 'Normal'·StartDate TEXT·EndDate TEXT·LinkedTestId TEXT DEFAULT ''). `CREATE TABLE Todos`(신규 DB용)에도 동일 컬럼 추가. (b) `SqliteProjectRepository`: `InsertTodos`(신규 5필드 파라미터 write, Priority=enum.ToString(), 날짜=null이면 DBNull) + `GetTodos`·`ReadTodosForProject`(신규 5필드 read, `HasColumn` 하위호환 가드 — 없으면 기본값).
- **Design**: 해당 없음 — 기존 마이그레이션·직렬화 경로(AddColumnIfNotExists/InsertTodos/Read*)에 필드 추가만, 신규 공개 심볼 없음.
- **Files**: `Infrastructure/Persistence/DatabaseContext.cs`, `Infrastructure/Persistence/SqliteProjectRepository.cs`
- **Edge**: 구버전 DB(신규 컬럼 없음) → `AddColumnIfNotExists`가 추가, read는 `HasColumn` false 시 기본값. StartDate/EndDate 빈 문자열·null 파싱은 기존 `ParseDateTime`/CompletedAt 패턴 준수. Priority 파싱 실패 시 Normal fallback(`Enum.TryParse`).
- **Halt Forecast**: 없음(NFR-3 방식 마이그레이션, 비파괴). 화이트리스트 미등록 컬럼이 있으면 ArgumentException — T2 내에서 등록으로 예방.
- **Acceptance**: 빌드 0. Todos에 5신규 컬럼 마이그레이션. 신규 항목 저장→로드 왕복 시 Category/Priority/StartDate/EndDate/LinkedTestId 보존(코드 대조: Insert가 5필드 write, Get/Read가 5필드 read). 구버전 DB 로드 시 예외 없음(기본값). depends T1.

### T3 — 페이지 네비게이션 컨테이너 (FR-C3) (Type D)
- **내용**: (a) `MainWindow.xaml`: 그룹 탭 `<Border Grid.Row="0">`(234)에 `x:Name="GroupTabsBar"` 부여. (b) `MainWindow.xaml.cs`: `ShowPage(UserControl page)`(DashboardContent.Content=page, GroupTabsBar.Visibility=Collapsed) / `ShowDashboard()`(보관해둔 DashboardView 재표시, GroupTabsBar.Visibility=Visible) 메서드 + DashboardView 인스턴스 필드 보관(재생성 회피). 최초 주입(:118)·언어 재주입(:500)을 이 경로로 정리. `App.MainWindow`를 통해 페이지에서 네비게이션 호출 가능(정적 접근 기존재).
- **Design**: 배치 = MainWindow(셸 소유). 신규 심볼 = `ShowPage`/`ShowDashboard`(콘텐츠 스왑 진입/복귀), DashboardView 보관 필드. 의존 = DashboardContent/GroupTabsBar 참조. 참조하는 곳 = T8(카드→페이지 네비), T5(페이지 뒤로가기 버튼). 비추상화 = **Frame·NavigationService 미도입** — 1단계(대시보드↔페이지) 스왑이라 기존 `.Content =` idiom 유지(과한 네비 인프라 회피). 페이지 스택도 두지 않음(단일 깊이).
- **Files**: `MainWindow.xaml`, `MainWindow.xaml.cs`
- **Edge**: 언어 변경 시 페이지 활성 상태면 대시보드로 복귀 후 재주입(단순화 — 페이지 상태 보존 불요, 재진입 가능). ShowDashboard 시 그룹 탭 복원.
- **Halt Forecast**: 없음(셸 로컬 변경, 기존 주입 2경로만 조정).
- **Acceptance**: 빌드 0. ShowPage 호출 시 DashboardContent가 페이지로 교체되고 그룹 탭 숨김, ShowDashboard 시 대시보드 복귀·그룹 탭 표시. **시각·동작은 T5/T8 완성 후 사용자 확인.**

### T4 — TaskPageViewModel (Type D)
- **내용**: 신규 `Presentation/ViewModels/TaskPageViewModel.cs`(프로젝트 1개 대상). 구성:
  - 생성: `TaskPageViewModel(ProjectItem project, IProjectRepository repo, AppSettings settings, Action refreshCardState)`.
  - 칸반: 상태별 `ObservableCollection<TodoItem>` 4개(예정/진행/완료/보류) + 상태별 카운트(기존 `UpdateTabCounts` 이전). 목록: 카테고리 그룹핑(기존 날짜 그룹화 로직 참고).
  - 뷰 전환: `bool IsKanbanView`(칸반/목록). 카테고리 필터: `SelectedCategory`(전체+각 카테고리) → 컬렉션 재구성.
  - 카테고리 소스: `AppSettingsDialogViewModel.DefaultTaskCategories`.Concat(`settings.TaskCategories`)(cross-VM 정적 패턴 재사용).
  - 변경 커맨드: 추가/편집/삭제/상태변경(`MoveToStatus(todo,status)`) — 각 변경 시 **증분 저장**(`repo.SaveTodos(project.Id, 전체 Todos)`, 기존 `OnTodoChanged`처럼 `Task.Run` 백그라운드 실행 — delete+reinsert의 UI 스레드 블로킹 회피) + `refreshCardState()`(HasActiveTodo 갱신).
  - **완료→작업기록 훅(현행 보존 — M1)**: 완료 전환 시 `ShowWorkLogPopupOnTodoComplete`면 페이지가 `HistoryDialog`를 표시(콜백/이벤트로 View에 위임)하고, **팝업이 반환한 신규 `HistoryEntry`(`HistoryDialogViewModel.NewEntries`)를 `project.Histories`에 병합 후 `repo.SaveHistories(project.Id, project.Histories)`로 영속화**한다(현행 `OnTodoDialogClosed`→`HistoryChanged`→`SaveHistories` 경로가 T8에서 제거되므로 이 저장을 페이지 경로에서 대체). `HistoryDialogViewModel`은 자가 저장하지 않으므로 표시만으로는 유실됨 — 저장 명시 필수.
  - 테스트 배지용: `repo.GetTestCategories(project.Id)`로 로드 → `LinkedTestId`→TestItem.Status 매핑 헬퍼 제공(T9에서 소비).
- **Design**: 배치 = Presentation/ViewModels. 신규 심볼 = `TaskPageViewModel`(작업 페이지 상태·필터·저장 오케스트레이션), 상태별 컬렉션·`MoveToStatus`·`IsKanbanView`·카테고리 필터. 의존 = ProjectItem·IProjectRepository·AppSettings·AppSettingsDialogViewModel(정적 기본값)·TagColor 매핑(View). 참조하는 곳 = T5 View·T6 드래그·T7 다이얼로그·T8 팩토리·T9 배지. 비추상화 = 기존 `TodoDialogViewModel` 필터/카운트 로직을 **이전**(공통 베이스 추상화 없이 페이지 맥락으로 재작성) — 구 VM은 T8에서 제거되므로 중복 아님. 상태별 4컬렉션은 명시 필드(제네릭 딕셔너리보다 지역성·바인딩 명확).
- **Files**: `Presentation/ViewModels/TaskPageViewModel.cs`(신규), `Presentation/ViewModels/ProjectCardViewModel.cs`(`CreateTaskPageViewModel()` 팩토리 + HasActiveTodo 갱신 콜백)
- **Edge**: 빈 프로젝트(작업 0) → 각 열 빈 상태. 카테고리 필터 "전체" → 전부. 삭제된 카테고리를 참조하는 기존 작업 → 필터에 "미분류" 취급(카테고리 목록에 없어도 카드엔 원 이름 표시). 완료 전환 후 되돌리기 시 CompletedAt 처리(기존 OnStatusChanged 동기화).
- **Halt Forecast**: 없음(기존 로직 이전 + repository 재사용).
- **Acceptance**: 빌드 0. VM에 상태별 4컬렉션·카운트·카테고리 필터·뷰 전환·MoveToStatus·증분 저장·배지 매핑 존재. 상태 변경 시 SaveTodos 호출(코드 대조). **완료 팝업 신규 기록이 `SaveHistories`로 영속화됨**(코드 대조: 완료 경로가 project.Histories 병합 + SaveHistories 호출). depends T1, T2.

### T5 — 작업 칸반 페이지 View (Type D)
- **내용**: 신규 `Presentation/Views/TaskPage.xaml`(+`.cs`) UserControl:
  - 헤더: 제목("작업") + "← 대시보드" 뒤로가기(`((MainWindow)App.MainWindow!).ShowDashboard()` — `App.MainWindow`는 `Window?`(App.xaml.cs:15)라 MainWindow 캐스트 필요) + 뷰 전환 토글(칸반/목록) + 카테고리 필터 + 상태별 개수.
  - 칸반: 4열(각 열 = 헤더[라벨+개수] + 카드 목록 `ItemsControl`/`ItemsRepeater`). 목록: 카테고리 그룹 + 항목.
  - 카드 `DataTemplate`: 제목, 카테고리 dot(TagColorConverter)+이름, 우선순위, 시작/종료일, 테스트 배지(T9 자리), 수정/삭제 버튼. "새 작업" 버튼(→ T7 다이얼로그).
  - 팔레트·폰트 Phase 0 리소스 사용. 로컬라이즈(x:Uid/.resw ko·en) — 신규 문자열.
- **Design**: 배치 = Presentation/Views. 신규 심볼 = `TaskPage` UserControl + 작업 카드 DataTemplate. 의존 = TaskPageViewModel(T4)·TagColorConverter·App.MainWindow 네비(T3). 참조하는 곳 = T6 드래그·T8 진입점. 비추상화 = 카드 템플릿을 별도 UserControl로 추출하지 않고 인라인(기존 카드 템플릿 관례) — 칸반/목록에서 공유 필요 시 DataTemplate 리소스로만.
- **Files**: `Presentation/Views/TaskPage.xaml`, `Presentation/Views/TaskPage.xaml.cs`, `Strings/ko-KR/Resources.resw`, `Strings/en-US/Resources.resw`
- **Edge**: 긴 제목/많은 카드 → 열 내부 스크롤. 빈 열 → 빈 상태 표시. 창 폭 축소 → 4열 가로 스크롤 또는 최소폭(WinUI idiom, 사용자 확인).
- **Halt Forecast**: 없음.
- **Acceptance**: 빌드 0. 작업 버튼으로 진입 시 칸반 4열/목록 토글·필터·개수·카드(제목/카테고리/우선순위/날짜/액션) 표시, 뒤로가기 동작. **시각 렌더는 사용자 확인 필요.** depends T4.

### T6 — 칸반 드래그앤드롭 상태 변경 (Type C)
- **내용**: `TaskPage.xaml`/`.xaml.cs`: 칸반 카드 컨테이너(Grid — Button 아님, 런처 주의사항)에 `CanDrag=True`·`AllowDrop=True`. 코드비하인드 `Card_DragStarting`(SetText(todo.Id), Move)·열 `DragOver`(AcceptedOperation=Move)·`Column_Drop`(대상 상태로 `Vm.MoveToStatus(todoId, targetStatus)`). DashboardView 카드 드래그(xaml.cs:296-409) 패턴 참고.
- **Design**: 배치 = TaskPage(View 이벤트) → TaskPageViewModel.MoveToStatus(T4). 신규 심볼 = 드래그 핸들러 3개(code-behind 위임). 의존 = TaskPageViewModel. 비추상화 = 삽입 위치 정렬(카드 순서)까지 구현하지 않고 **상태 열 이동만**(PRD FR-T3 "상태 변경"에 한정 — 열 내 재정렬은 범위 밖).
- **Files**: `Presentation/Views/TaskPage.xaml`, `Presentation/Views/TaskPage.xaml.cs`, `Presentation/ViewModels/TaskPageViewModel.cs`
- **Edge**: 같은 열로 드롭 → 무변경. 완료 열로 드롭 시 완료 처리(+작업기록 팝업 훅). 드래그 취소/영역 밖 드롭 → 무변경.
- **Halt Forecast**: 없음.
- **Acceptance**: 빌드 0. 카드를 다른 상태 열로 드래그 시 상태 변경·저장. 완료 열로 드롭 시 완료 처리 + 작업기록 팝업/저장 훅 동작(T4 경로). **드래그 UX는 사용자 확인 필요.** depends T5.

### T7 — 새 작업/편집 다이얼로그 + 삭제 확인 (Type C)
- **내용**: 신규 `Presentation/Views/Dialogs/TaskEditDialog.xaml`(+`.cs`) ContentDialog + `Presentation/ViewModels/TaskEditDialogViewModel.cs`. 필드: 제목(Text)·설명(Description 재활용)·카테고리(ComboBox: 기본+사용자)·우선순위(ComboBox 높음/보통/낮음)·시작일/종료일(DatePicker, 선택적)·**"테스트 추가" 토글**(FR-T6, T9에서 처리). 새 작업·편집 겸용(생성자에 편집 대상 유무). 저장 시 검증(제목 필수). 삭제 확인은 `DialogService.ShowConfirmAsync` 재사용(별도 다이얼로그 신규 불요).
- **Design**: 배치 = Presentation(Dialogs/ViewModels). 신규 심볼 = `TaskEditDialog`(ContentDialog)·`TaskEditDialogViewModel`(입력·검증·결과). 의존 = TodoItem·TaskPriority(T1)·AppSettings 카테고리·TaskPageViewModel(호출부). 참조하는 곳 = T5 "새 작업"/카드 수정·T9 토글 처리. 비추상화 = 기존 ContentDialog 서브클래스 패턴(Save는 PrimaryButtonClick 검증) 준수, 신규 베이스 없음.
- **Files**: `Presentation/Views/Dialogs/TaskEditDialog.xaml`(+`.cs`), `Presentation/ViewModels/TaskEditDialogViewModel.cs`(신규), `Presentation/ViewModels/TaskPageViewModel.cs`(호출), `Strings/ko-KR/Resources.resw`, `Strings/en-US/Resources.resw`
- **Edge**: 빈 제목 → 저장 차단(args.Cancel). 종료일<시작일 → 경고 또는 허용(사용자 확인 — 기본 허용, 저장만). 카테고리 미선택 → 미분류(빈). 편집 취소 → 무변경.
- **Halt Forecast**: 없음.
- **Acceptance**: 빌드 0. 새 작업/편집 다이얼로그가 6필드+테스트 토글 표시, 저장 시 TodoItem에 반영. 삭제 시 확인 후 제거. **시각은 사용자 확인 필요.** depends T4, T1.

### T8 — 진입점 재배선 + 구 다이얼로그·고아 정리 (Type D, cross-file)
- **내용**: (a) `DashboardView.xaml.cs OnOpenTodoRequested`(178-193): 다이얼로그 대신 `((MainWindow)App.MainWindow!).ShowPage(new TaskPage{DataContext=card.CreateTaskPageViewModel()})` 호출(카드→작업 페이지 네비 — App.MainWindow는 Window?라 캐스트). (b) 구 자산 제거(D-6): `TodoDetailDialogViewModel.cs`(사용처 0), `TodoDialog.xaml`/`.cs`·`TodoDialogViewModel.cs`(페이지로 대체, 필터/카운트 로직은 T4로 이전 완료). `ProjectCardViewModel`의 `CreateTodoDialogViewModel`/`OnTodoDialogClosed`/`TodoChanged` 등 구 경로 정리(신 팩토리 `CreateTaskPageViewModel`으로 대체, HasActiveTodo 갱신은 콜백). `MainViewModel.OnTodoChanged` 경로 정리(증분 저장으로 대체 시 잔재 제거). 제거 후 참조 grep 0 확인.
- **Design**: 배치 = DashboardView/MainWindow/ProjectCardViewModel/MainViewModel. 신규 심볼 없음(재배선+제거 전용). 의존 = T3 네비·T4 팩토리·T5 페이지. 비추상화 = 해당 없음(제거 위주). 제거로 인한 고아까지 정리해 잔재 0.
- **Files**: `Presentation/Views/DashboardView.xaml.cs`, `MainWindow.xaml.cs`, `Presentation/ViewModels/ProjectCardViewModel.cs`, `Presentation/ViewModels/MainViewModel.cs`, (삭제) `Presentation/Views/Dialogs/TodoDialog.xaml`·`.xaml.cs`·`Presentation/ViewModels/TodoDialogViewModel.cs`·`Presentation/ViewModels/TodoDetailDialogViewModel.cs`
- **Edge**: 구 TodoDialog가 참조하던 resw/스타일 중 다른 사용처 없는 것만 제거(grep 확인). ShowWorkLogPopupOnTodoComplete 완료 훅은 T4/T6에서 페이지 경로로 보존(제거 아님).
- **Halt Forecast**: 파일 삭제 4건 → **사전 승인 항목**에 등재(일괄 승인). 예상 밖 참조가 grep에서 발견되면 같은 task에서 정리(범위 내).
- **Acceptance**: 빌드 0·신규 경고 0. 작업 버튼이 페이지를 열고(다이얼로그 아님), `TodoDialog`/`TodoDialogViewModel`/`TodoDetailDialogViewModel` 참조 grep 0. depends T3, T4, T5.

### T9 — 작업↔테스트 연결 + 카드 테스트 배지 (FR-T6/T8) (Type C)
- **내용**: (a) 연결(FR-T6): `TaskEditDialogViewModel`/`TaskPageViewModel` 저장 경로에서 "테스트 추가" 토글 on이면 — 프로젝트 테스트 카테고리 중 이름 "작업"을 찾거나 없으면 자동 생성 → 그 카테고리에 `TestItem`(Text=작업 제목, Status=Testing) 추가 → `TodoItem.LinkedTestId=testItem.Id` → `repo.SaveTestCategories(project.Id, 카테고리들)` 저장. (b) 배지(FR-T8): `TaskPageViewModel`이 로드한 테스트에서 `LinkedTestId`→`TestItem.Status` 해석, 작업 카드에 배지 표시(미실행=Testing·통과=Done·실패=Fix 매핑 — 현재 모델 라벨/색). `TaskPage.xaml` 카드 템플릿에 배지 요소 + 상태→표시 변환(컨버터 또는 VM 노출 프로퍼티).
- **Design**: 배치 = TaskPageViewModel/TaskEditDialogViewModel(연결 로직) + TaskPage(배지 UI). 신규 심볼 = 테스트 자동 생성/연결 헬퍼 + 배지 상태 매핑(VM 프로퍼티 또는 소형 컨버터). 의존 = TestCategory/TestItem·IProjectRepository(GetTestCategories/SaveTestCategories)·LinkedTestId(T1/T2). 비추상화 = 현재 테스트 상태 모델(Testing/Fix/Done) 그대로 소비, Phase 3 통과/실패/미실행 전환 시 배지 매핑만 갱신(이번엔 별도 상태 enum 도입 안 함).
- **Files**: `Presentation/ViewModels/TaskPageViewModel.cs`, `Presentation/ViewModels/TaskEditDialogViewModel.cs`, `Presentation/Views/TaskPage.xaml`, (배지 컨버터 필요 시) `Presentation/Converters/*.cs`+`Resources/Converters.xaml`, `Strings/*`(배지 라벨)
- **Edge**: 토글 off → 테스트 미생성·LinkedTestId 빈. 연결 테스트가 이후 삭제됨 → 배지 미표시(매핑 실패 시 숨김). "작업" 카테고리 이미 존재 → 재사용(중복 생성 안 함). **테스트 추가 토글은 새 작업 전용(FR-T6, T7에서 `ShowTestToggle => !IsEditMode`) — 편집 화면엔 노출되지 않음**. 방어적으로 T9는 `AddTestRequested && LinkedTestId 빈` 조건에서만 테스트를 생성해 중복을 막는다(새 작업은 LinkedTestId가 항상 빈 값이라 자연히 1회 생성).
- **Halt Forecast**: 없음(현재 테스트 모델 소비).
- **Acceptance**: 빌드 0. 토글 on 새 작업 → "작업" 카테고리에 테스트 생성·LinkedTestId 설정·저장. 작업 카드에 연결 테스트 상태 배지 표시. depends T7, T5, T4, T2.

## PRD Coverage
| PRD ID | 우선순위 | 대응 task | 상태 |
|--------|---------|----------|------|
| FR-C3 (페이지 네비 컨테이너) | Must | T3, T5, T8 | ✅ 커버 |
| FR-T1 (상태4·카테고리·우선순위·시작/종료일+영속화) | Must | T1, T2 | ✅ 커버 |
| FR-T2 (전체 페이지·칸반/목록 전환) | Must | T4, T5 | ✅ 커버 |
| FR-T3 (드래그 상태 변경) | Must | T6 | ✅ 커버 |
| FR-T4 (카테고리 필터·그룹핑·상태별 개수) | Must | T4, T5 | ✅ 커버 |
| FR-T5 (새 작업/편집·삭제 확인) | Must | T7 | ✅ 커버 |
| FR-T6 (테스트 추가 토글) | Should | T7, T9 | ✅ 커버(사용자 결정 포함) |
| FR-T7 (담당자 필드) | Could | — | ⏭️ Deferred(사용자 제외 결정) |
| FR-T8 (작업 카드 테스트 배지) | Should | T9 | ✅ 커버(사용자 결정 포함) |
| FR-C1/C2/C4·S* | — | (Phase 0/1 완료) | ✅ 이전 Phase 기구현 |
| FR-E*·H*·N* | — | (Phase 3~5) | ⏭️ 다음 Phase |

## 4-D 재사용 확인
| 신규 심볼 | 유사 기존 구현 검색 결과 | 재사용/신규 사유 |
|---|---|---|
| TaskPageViewModel 필터/카운트/그룹 | TodoDialogViewModel(RefreshFilter/UpdateTabCounts/GroupByDate) | **로직 이전**(구 VM은 T8에서 제거 — 중복 아님) |
| 카테고리 dot 색 | TagColorConverter(DevToolConverters.cs:8-69) | **재사용**(문자열→결정론적 색), 필요 시 L만 파라미터화 |
| 칸반 드래그 | DashboardView 카드 핀 정렬 드래그(xaml.cs:296-409) | **패턴 재사용**(Grid 컨테이너·SetText·Drop→VM) |
| 기본 작업 카테고리 | AppSettingsDialogViewModel.DefaultTaskCategories(:218) | **재사용**(cross-VM 정적 참조 — ProjectSettingsDialogViewModel 선례) |
| 삭제 확인 | DialogService.ShowConfirmAsync | **재사용**(신규 다이얼로그 불요) |
| TaskEditDialog | 기존 ContentDialog 서브클래스(Save=PrimaryButtonClick) | **패턴 재사용**, 다이얼로그 자체는 신규(작업 전용 필드) |
| 페이지 네비 | (없음 — Frame/Nav 전무) | **신규**(경량 콘텐츠 스왑, 기존 `.Content =` idiom 위) |

## Decisions
- **D1 네비게이션 방식**: Frame/NavigationService 미도입, `DashboardContent.Content` 스왑 + 그룹탭 Visibility 토글(1단계 깊이). Source: E1(Frame/DI 전무, `.Content =` idiom) + YAGNI.
- **D2 작업 페이지 스코프**: 프로젝트별만(카드→해당 프로젝트 칸반). 교차 집계(PRD D-2)는 Deferred. Source: 사용자 결정(2026-07-18).
- **D3 TodoStatus 확장**: 열거자명(Waiting/Active/Completed) 유지 + `Hold` 추가, 표시 라벨만 예정/진행중/완료/보류. Source: DB가 열거자명 문자열 저장 → 기존 데이터 무변경 매핑 + 보류만 신규.
- **D4 우선순위·카테고리·연결 표현**: Priority=enum(High/Normal/Low), Category=string, LinkedTestId=string ID. 값 객체·별도 엔티티 없음. Source: 기존 Category/CategoryId 문자열 관례.
- **D5 기본 카테고리 공유**: `AppSettingsDialogViewModel.DefaultTaskCategories` 정적 참조(신규 공유 상수 미도입). Source: ProjectSettingsDialogViewModel의 DefaultTechStackTags 정적 참조 선례.
- **D6 저장 시점**: 페이지는 변경마다 증분 `SaveTodos`(다이얼로그 close-batch 대체) + HasActiveTodo 콜백 갱신. Source: 페이지는 모달 close 시점이 없음.
- **D7 테스트 연결**: 토글 on 시 프로젝트 "작업" TestCategory 자동 생성/재사용 + TestItem 추가, TodoItem.LinkedTestId 링크. 배지는 현재 Testing/Fix/Done 모델 표시(Phase 3 매핑 갱신). Source: 사용자 결정(2026-07-18) + 현재 테스트 모델.
- **D8 고아 정리(D-6)**: TodoDetailDialogViewModel 제거, TodoItem.Description은 새 다이얼로그 "설명"으로 연결(고아 해소), 구 TodoDialog/VM 제거. Source: PRD D-6 + explorer(사용처 0).
- **D9 드래그 범위**: 상태 열 이동만(FR-T3), 열 내 재정렬은 범위 밖. Source: PRD FR-T3 "상태 변경".
- **D10 담당자(FR-T7) 제외**: Could·독립적이나 이번 제외. Source: 사용자 결정.

## Open Questions
- [x] 작업 페이지 스코프(프로젝트별 vs +교차 집계) → **프로젝트별만**(교차 집계 Deferred). (D2)
- [x] 선택 기능(FR-T6/T7/T8) → **T6·T8 포함, T7(담당자) 제외**. (D7, D10)
- [x] 테스트 연결 카테고리 → **"작업" 카테고리 자동 생성/재사용**. (D7)
- [x] T6/T8 Phase 3 결합 감안 포함 여부 → **이번 포함 유지**(배지 매핑은 Phase 3 갱신). (D7)
- [x] 시각 시안 부재 → **PRD §3 추출값 + WinUI idiom으로 구현, 빌드 후 사용자 확인**. (요구 이해)

## 사전 승인 항목 (일괄 승인 대상)
- **파일 삭제 4건**(T8, grep 사용처 0/이전 완료 확인 후): `TodoDialog.xaml`·`TodoDialog.xaml.cs`·`TodoDialogViewModel.cs`·`TodoDetailDialogViewModel.cs`.
- **DB 스키마 비파괴 확장**(T2): Todos에 5컬럼 추가(NFR-3 AddColumnIfNotExists+화이트리스트) 및 InsertTodos/GetTodos/ReadTodosForProject 매핑 확장.
- **도메인 확장**(T1): TodoStatus.Hold·TaskPriority enum·TodoItem 5필드 추가(비파괴).
- **셸 구조 조정**(T3): MainWindow에 페이지 네비 메서드 + 그룹탭 x:Name/Visibility 토글, 콘텐츠 주입 2경로 정리.
- .resw 신규 문자열 추가(ko/en) — 작업 페이지·다이얼로그·배지.

## 불가피한 Halt (위임 불가)
- 없음(Phase 2는 비파괴·로컬 변경). commit/push는 구현·검증 후 별도 승인.

## Deferred / Follow-up
- [Follow-up] **Todo* resw 고아 정리**(T8 V-7 역방향, 대량이라 이연): 삭제된 TodoDialog/TodoDialogViewModel 전용 resw 키(`TodoDialog.*`·`TodoTab*`·`TodoStatus_*`·`TodoLabel_*`·`TodoEditTitle`·`TodoUncomplete*`·`TodoGroupBy*`·`TodoStatusChange*` ko/en ~40항목)가 소스 미사용 상태로 잔존. `TodoButton`·`TodoPopupCheck`도 grep상 미사용(별도 확인 필요). 빌드·런타임 무해 — 별도 세션에서 audit 후 제거.
- [SUGGEST] `TaskPageViewModel`의 `ShowKanban`/`ShowList` RelayCommand 미사용(T5가 RadioButton `IsKanbanView` TwoWay 직접 바인딩) — 제거 검토(T5 quality S1).
- [MINOR] `TaskEditDialog.xaml`의 `x:Name="TitleBox"` 미사용 — 제거 또는 다이얼로그 오픈 시 포커스 활용(T7 quality 리뷰 m1).
- 교차(cross-project) 작업 집계 페이지(PRD D-2, 전체/프로젝트 스코프 필터) — 별도 진행.
- 담당자(who) 필드(FR-T7) — Could, 추후.
- 칸반 열 내 카드 재정렬(정렬 순서 영속화) — 이번 상태 이동만.
- 테스트 배지 상태 라벨/색을 Phase 3 통과/실패/미실행 모델로 매핑 갱신.

## Out of Scope (이 Phase)
- 테스트 페이지 전환·테스트 상태 모델 전환(Phase 3).
- 작업 기록·알림(Phase 4·5).
- git 커밋 ↔ 작업 기록 연동(PRD §7 영구 제외).

## Progress Log
- T9 완료 (전 task 완료): 작업↔테스트 연결(CreateLinkedTest — "작업" TestCategory 자동생성/재사용·TestItem 추가·LinkedTestId·SaveTestCategories+SaveTodos, 중복방지) + 카드 테스트 배지(TodoItem transient LinkedTestBadge[영속화 안 함]·MapTestBadge Testing/Fix/Done→미실행/통과/실패·Rebuild 설정·카드 x:Bind). 빌드 OK. spec/quality OK.
  - 결정: 배지는 카드 DataTemplate(x:DataType=TodoItem)에서 x:Bind 가능해야 해 TodoItem에 transient 표시 프로퍼티로 노출(프로젝트의 ObservableObject 엔티티 관례와 일관, repository 미매핑=영속화 안 함).
- T6·T8 완료: T6 칸반 드래그(카드 CanDrag·4열 드롭 타깃·MoveToStatus). T8 진입점 재배선(작업 버튼→TaskPage 네비, 다이얼로그 제거) + 구 TodoDialog/VM **4파일 삭제** + TodoChanged 이벤트 체인(선언·발생·구독·해제·핸들러) 완전 제거. HistoryChanged·완료훅 보존. 빌드 OK(신규 경고 0). 리뷰 spec OK/quality MINOR(예외 패턴 형제 핸들러와 통일로 해소). Todo* resw 고아는 대량이라 follow-up.
- T5 완료 (커밋 T5 완료커밋): 작업 칸반 페이지 View — 헤더(뒤로가기·제목·뷰전환·카테고리 필터·새 작업) + 칸반 4열(헤더 라벨+개수, ItemsControl) + 목록(카테고리 그룹) + 카드 템플릿(카테고리 dot/우선순위/시작·종료일/상태콤보/수정·삭제). 완료→작업기록 팝업(WorkLogRequested→HistoryDialog→CommitWorkLog). resw ko/en. 빌드 OK.
  - 수정(빌드 실패→해결): x:Bind **정적 함수 바인딩은 Converter 미적용** → 날짜 Visibility를 `DateVisibility(DateTime?)` 헬퍼(Visibility 직접 반환)로 교체. 카드 dot 등 속성+컨버터 바인딩은 정상.
  - V-9 시각 정합: 목업 부재로 요소 존재 확인 + 픽셀 렌더 HUMAN-VERIFY(사용자 결정). 테스트 배지는 T9.
- **순서 조정**: T5(작업 페이지 View)가 "새 작업"·"수정" 버튼으로 TaskEditDialog를 호출하므로, 스텁 없이 완결하려 **T7을 T5보다 먼저** 구현. 의존성(T7←T4) 위배 없음. 남은 순서: T5 → T6 → T8 → T9.
- T7 완료 (커밋 831c... T7 완료커밋): TaskEditDialog(ContentDialog) + TaskEditDialogViewModel — 6필드(제목/설명/카테고리/우선순위/시작·종료일)+테스트 토글(새 작업 전용), BuildResult 매핑(미분류→빈문자열·DateTimeOffset?↔DateTime?), OnSave 제목 검증. resw ko/en. 빌드 OK(XAML 컴파일 통과).
  - 리뷰: spec MINOR(편집 시 토글 숨김 — FR-T6 '새 작업' 정합, T9 Edge 명확화로 해소), quality MINOR(미사용 x:Name — Deferred 등록). 코드 변경 없이 진행.
- T3-T4 완료 (커밋 6431c47, T4 완료커밋): T3 페이지 네비(MainWindow ShowPage/ShowDashboard·GroupTabsBar 토글·Frame 미도입) + T4 TaskPageViewModel(4상태 컬렉션·필터·카운트·뷰전환·MoveToStatus·완료→작업기록 훅·배지 매핑) + ProjectCardViewModel 팩토리. 빌드 OK.
  - 결정(T4 M1 수정): 증분 저장 fire-and-forget → **FIFO 저장 체인**(`_saveChain.ContinueWith` + 호출시점 스냅샷 + try/catch 로깅). 연속 드래그 시 스냅샷 역전 유실·예외 무시 방지. 전제: 저장 트리거는 UI 스레드 호출.
  - Files 확장: T4가 resw `TaskCategory_None`(ko/en) 추가(RebuildCategoryGroups 참조 — 참조-정의 지역성). T5의 나머지 resw와 별개.
- T1-T2 완료 (커밋 8169b33, T2 amend): 도메인 확장(TodoStatus.Hold·TaskPriority·TodoItem 5필드) + Todos 영속화 마이그레이션(DatabaseContext 화이트리스트·5컬럼·CREATE TABLE, Repository InsertTodos·GetTodos·ReadTodosForProject·ReadNullableDateColumn 헬퍼). 빌드 MSBuild x64 OK, 신규 경고 0. spec/quality 리뷰 OK.
  - 결정: LSP가 CommunityToolkit/Microsoft.Data.Sqlite/WinUI를 미해석해 CS0246/CS9248 오류 다수 표출 — 전부 false positive, MSBuild 빌드로 판정(진실은 MSBuild). 빌드 명령: `MSBuild.exe DevDashboard_WinUI/DevDashboard.csproj -t:Build -p:Configuration=Debug -p:Platform=x64`.
  - 결정: plan task에 `[ ]` 체크박스가 없어 require-task-checkbox 훅이 동작 안 함 → `### 진행 체크리스트` 섹션(T1~T9 `- [ ]`) 추가로 해소.

## Phase Ledger
- 전 task(T1~T9) 완료.
- Phase F 통과 (HEAD 3bc8774) — 전체 빌드 OK·신규 경고 0, 통합 정합·완결성·회귀 검증 통과(plan-completion-reviewer OK).
- Phase G 통과 (Must 100%) — PRD Must 8/8(FR-C3·T1·T2·T3·T4·T5)·Should 2/2(FR-T6·T8) 충족, FR-T7 Deferred(사용자 제외). 미충족 0.

## Next Steps
- 권장 다음 액션: **시각·동작 사용자 확인**(칸반 렌더·드래그 UX·테스트 배지·영속 왕복[값 저장→재열기]) → 이상 없으면 커밋을 master로 병합/push는 별도 승인. 이후 Phase 3(테스트) plan-feature.
- Suggested skills: pjc:plan-feature(Phase 3), 공식 /code-review(선택 — T4/T9 로직).
- follow-up: Todo* resw 고아 audit·미사용 RelayCommand/x:Name 정리·README/스크린샷 갱신(시각 확인 후).

## 통과 체크리스트
- [x] 요구 이해 작성(원문 인용 + 이해 5줄)
- [x] Impact Analysis 4-A(심볼: TodoStatus/TodoItem/Todos read·write 3경로·카드 진입 릴레이 전수)·4-B(직렬화: 마이그레이션 task 분리)·4-C(테스트 없음 — 테스트 프로젝트 부재)·4-D(재사용표 작성)
- [x] 각 task acceptance 검증 가능(빌드 + 시각/동작 구분), 상호 모순 0
- [x] Type 분류(T1 C·T2 D·T3 D·T4 D·T5 D·T6 C·T7 C·T8 D·T9 C), Design 필드(Type D 전부 + 신규 심볼 Type C)
- [x] Edge/Halt Forecast 각 task 명시
- [x] Open Questions 전부 해결, 결정 분기 0
- [x] 분할 권고 대상(9 task) — 승인 시 단일 진행/분할 확인
