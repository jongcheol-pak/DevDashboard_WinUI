# plan.md — Phase 5: 알림 — 마감 감지 + 전체 페이지 + 헤더 드롭다운 + 읽음 상태 영속화

**PRD**: docs/prd.md (Phase 5 = FR-N1, FR-N2, FR-N3, FR-N4)
**전체 목표**: DevDashboard 5개 영역 리디자인(§PRD). 이 plan은 **Phase 5(알림)** 만 다룬다 — 리디자인 로드맵의 마지막 Phase.
**이전 plan**: Phase 4(작업 기록) — 완료(`Phase G 통과 Must 100%`), master 병합 완료(HEAD 04beef8). **Phase 5는 master 기준 새 브랜치**(`task/phase5-notifications`)에서 진행.
**다음 plan**: 없음(리디자인 5개 영역 완료). 이후는 Deferred 대장·시각 확인 후속.

## 요구 이해

> 원문(PRD §4 알림 Phase 5 + §3 확정 데이터 발췌): "FR-N1 마감 알림 감지 로직 — 미완료 작업 중 종료일 D-3 임박·오늘 마감·경과 항목 집계(완료 제외). FR-N2 알림 전체 페이지(프로젝트별 그룹, 읽음/안읽음, 모두 읽음, 항목 클릭 시 해당 작업으로 이동). FR-N3 헤더 알림 드롭다운 패널(요약 목록 + '모든 알림 보기'). FR-N4 읽음 상태 영속화." + §3 "알림 대상: 종료일 D-3 임박 + 오늘 마감 + 경과, 완료 상태 제외, 읽음/안읽음 추적" + 사용자 스코핑(2026-07-19): 읽음 키=작업Id+마감상태(재환기), 클릭 이동=프로젝트 작업 페이지 열기, 벨 배지=안읽음 개수 표시, 단위 테스트=미추가(현행 유지).

이해한 요구(Phase 5):
- 모든 프로젝트의 미완료 작업(`TodoItem`) 중 **종료일(EndDate)이 D-3 임박·오늘 마감·경과**인 항목을 순수 계산으로 집계하는 **마감 감지 로직**을 만든다(완료 제외).
- 집계 결과를 **알림 전체 페이지**(프로젝트별 그룹·읽음/안읽음 표시·모두 읽음·항목 클릭 시 해당 프로젝트 작업 페이지로 이동)로 표시한다(TaskPage·TestPage처럼 `ShowPage` 네비 재사용).
- **헤더에 알림 벨 버튼 + 드롭다운 Flyout 패널**(요약 목록 + "모든 알림 보기")을 추가하고, 벨에 **안읽음 개수 배지**를 표시한다.
- **읽음 상태를 영속화**한다 — 키는 `작업Id + 마감상태`로, 마감이 임박→오늘→경과로 악화되면 안읽음으로 재환기한다.

## Goal
신규 순수 도메인 로직 `NotificationService`(+ `Notification` 값 객체·`DeadlineStatus` enum)로 전체 프로젝트의 마감 임박/오늘/경과 미완료 작업을 집계한다. `MainViewModel`에 전체 프로젝트 Todo 로드(`GetAllProjectItemsWithTodos`, 기존 `...WithHistories` 미러)·알림 재계산·읽음 처리·안읽음 개수(`UnreadNotificationCount`)·프로젝트별 작업 페이지 이동 진입점을 추가한다. `AppSettings.ReadNotificationIds`(List\<string\>)로 읽음 키를 영속화한다. `NotificationPage`(전체 페이지)와 헤더 벨 버튼 + Flyout 드롭다운 패널을 신규 배선하고, 항목 클릭 시 해당 프로젝트 `TaskPage`로 이동한다.

## Investigation Log (근거)

### 위키·시안
- 위키 참조: vault 미설정 — 코드 1차 출처로 진행.
- 리디자인 시안: 목업 HTML(claude.ai/design)이 레포에 없음(Phase 0~4와 동일). 시각 명세는 PRD §3 추출값 + WinUI idiom, 빌드 후 사용자 확인.
- Deferred 대장(docs/plans/deferred.md) 확인: **[교차 작업/테스트 집계 페이지](PRD D-2)** 항목이 유관하나 별개다 — 알림 페이지는 "마감 임박 작업의 프로젝트별 그룹"으로 본질상 교차 프로젝트지만, D-2의 범용 교차 집계 페이지(전체/프로젝트 스코프 필터)를 구축하지 않는다(FR-N2 전용). 이번에도 Deferred 유지. 그 외 대기 항목은 Phase 5와 무관.

### 도메인·데이터 (직접 Read)
- **TodoItem**(Domain/Entities/TodoItem.cs:24-58): `Status`(`TodoStatus` Waiting/Active/Completed/Hold)·`IsCompleted`(Status==Completed 동기화, 61-65)·`EndDate`(48, `DateTime?` 종료/마감일)·`Text`(16 본문)·`Id`(12). **마감 감지 입력 = EndDate!=null && !IsCompleted**.
- **TodoStatus**(Domain/Entities/TodoStatus.cs): Waiting/Active/Completed/Hold. "완료 제외" = Status!=Completed(=Hold·Waiting·Active 포함).
- **ProjectItem**(Domain/Entities/ProjectItem.cs:64): `List<TodoItem> Todos`(지연 로딩 — 다이얼로그/페이지 열 때 Repository에서 로드). `Id`·이름 보유.

### 데이터 집계·영속화 (직접 Read)
- **MainViewModel**(Presentation/ViewModels/MainViewModel.cs): `_allCards`(28, `List<ProjectCardViewModel>` 메모리 전체 프로젝트)·`GetAllProjectItemsWithHistories`(620-626, ToModel + `_projectRepository.GetHistories` per-project 로드)·`GetSettings`(661)·`GetProjectRepository`(663)·`SaveSettings`(236-242 → `_storageService.Save(_settings)`)·`SaveAppSettings`(665). **알림 집계 = `GetAllProjectItemsWithTodos`(신규, WithHistories 미러 — `_projectRepository.GetTodos(item.Id)`)**.
- **IProjectRepository**(Domain/Repositories/IProjectRepository.cs:13): `List<TodoItem> GetTodos(string projectId)` — per-project Todo 조회(이미 존재, 신규 쿼리 불필요).
- **AppSettings**(Domain/Entities/AppSettings.cs:7-50): Aggregate Root. `List<string>` 필드 선례 다수(TechStackTags·TaskCategories·HistoryKinds). **읽음 키 저장 = `List<string> ReadNotificationIds` 추가**(Phase 1 PageSize/HistoryKinds 선례 동형).
- **JsonStorageService**(Infrastructure/Services/JsonStorageService.cs:10-44): `Load()`/`Save(AppSettings)` → `ApplicationData.Current.LocalSettings` JSON. **AppJsonContext**(Infrastructure/Services/AppJsonContext.cs): `[JsonSerializable(typeof(AppSettings))]` — AppSettings 전체 직렬화이므로 **필드 추가만으로 자동 커버**(Context 무수정).
- **데이터 손실 검증**: `AppSettingsDialogViewModel.ApplyTo`(:379-406)는 전달받은 `settings` 객체를 **제자리 변경**(`settings.X = ...`, `new AppSettings()` 아님)하므로 설정 다이얼로그 저장 경로에서도 `ReadNotificationIds` 보존됨(매핑 불필요). LoadFrom/ApplyTo 수정 불필요.

### UI·네비 (직접 Read — explorer 확인 후 정독)
- **페이지 네비**: `MainWindow.ShowPage(UIElement)`/`ShowDashboard()`(MainWindow.xaml.cs:508-521 — `DashboardContent.Content` 스왑 + `GroupTabsBar` Visibility 토글). 카드→페이지는 `DashboardView.xaml.cs:178-192`(`OnOpenTodoRequested` → `new TaskPage(card.CreateTaskPageViewModel(), Vm.GetSettings())` → `ShowPage`). **알림 페이지도 동일 패턴**.
- **헤더**: `MainWindow.xaml:31-132` — `HeaderButtonsPanel`(StackPanel Orientation=Horizontal Spacing=6). 버튼 5개(SortButton·AllHistoryButton·ExportImportButton·AppSettingsButton·AddProjectButton), 각 36×36 Padding=0 + FontIcon + x:Uid. **벨 버튼 = ExportImportButton↔AppSettingsButton 사이 삽입**(Segoe MDL2 벨 글리프). Flyout은 기존 SortButton/ExportImportButton Flyout 패턴.
- **"전체 작업 기록" 핸들러**(MainWindow.xaml.cs:536-550): `_viewModel.GetAllProjectItemsWithHistories()` + `new ProjectHistoryDialog(...)`. 헤더 버튼 핸들러 배선 선례.
- **TaskPage 생성**: `TaskPageViewModel(ProjectItem, IProjectRepository, AppSettings, Action refreshCardState)`(TaskPageViewModel.cs:58-77), `TaskPage(TaskPageViewModel vm, AppSettings settings)`(TaskPage.xaml.cs:37-52), `ProjectCardViewModel.CreateTaskPageViewModel()`(:517-523, EnsureTodosLoaded 후 생성). **특정 작업 카드 포커스 기능 없음** — 사용자 결정(프로젝트 작업 페이지 열기)으로 신규 스크롤 인프라 불필요.
- **ProjectCardViewModel 팩토리**(:517-551): Create{Task,Test}PageViewModel/CreateHistoryDialogViewModel — Ensure*Loaded 후 `_item`·`_repository`·`_settings`·콜백 주입.

### 재사용 (직접 Read)
- **TagColorConverter**(Presentation/Converters/DevToolConverters.cs): 이름→결정론적 색(작업 카테고리·유형 배지 재사용) → 프로젝트 그룹 헤더 색 dot 등에 재사용 가능(선택).
- **페이지 헤더·뒤로가기·빈 상태 패턴**: TaskPage/TestPage XAML(헤더 ← 대시보드·제목·액션) → NotificationPage 레이아웃 재사용.

## 시각 요소 분해 (알림 UI — 출처 PRD §3, 목업 부재로 세부는 사용자 확인)

| 요소 | 속성 | 디자인 값 | 확인 방법 |
|------|------|----------|-----------|
| 헤더 벨 버튼 | 크기·글리프 | 36×36 Padding=0 + Segoe MDL2 벨 글리프 | HeaderButtonsPanel 기존 버튼 관례 |
| 안읽음 배지 | 표시 | 벨 우상단 안읽음 개수(색 점+숫자) | 사용자 결정(배지 표시) + PRD §3 읽음 추적 |
| 드롭다운 패널 | 구성 | 요약 목록(상위 N) + "모든 알림 보기" 버튼 | PRD §4 FR-N3 |
| 알림 페이지 헤더 | 구성 | ← 대시보드 · 제목 · "모두 읽음" | PRD §4 FR-N2 + TaskPage 헤더 관례 |
| 프로젝트 그룹 | 구성 | 프로젝트명 헤더 + 소속 알림 항목 | PRD §4 FR-N2 "프로젝트별 그룹" |
| 알림 항목 | 구성 | 작업 텍스트 · 마감상태 배지 · 종료일 · 읽음 표시 | PRD §3/§4 |
| 마감상태 배지 | 색·라벨 | 임박/오늘/경과 3구분(경과 강조 코랄 #f0716a 계열) | PRD §3 팔레트 + WinUI idiom |
| 읽음/안읽음 | 표시 | 안읽음 강조(점·굵기), 읽음 흐림 | PRD §3 읽음 추적 |
| 빈 상태 | 표시 | "마감 임박 작업 없음" 안내 | TaskPage/TestPage 빈 상태 관례 |
| 팔레트·폰트 | — | 배경 #131316·카드 #1c1c20·강조 코랄 #f0716a, Malgun Gothic | PRD §3(Phase 0 전역 적용 완료) |
> 팔레트·폰트는 Phase 0 전역 적용. 신규 시각 요소(벨 배지·드롭다운·알림 페이지·마감상태 배지)만 추가. 세부 레이아웃·마감상태 배지 색은 목업 부재 → WinUI idiom 후 사용자 확인.

## Tasks

### 진행 체크리스트
- [x] T1 — 알림 도메인 모델 + 마감 감지 서비스(순수)
- [x] T2 — 읽음 상태 영속화(AppSettings.ReadNotificationIds)
- [x] T3 — MainViewModel 집계·읽음 처리 + NotificationPageViewModel
- [x] T4 — NotificationPage 전체 페이지 + resw
- [x] T5 — 헤더 벨 버튼 + Flyout 드롭다운 + 배지 + MainWindow 배선

### T1 — 알림 도메인 모델 + 마감 감지 서비스 (Type C)
- **내용**: (a) `Domain/Enums/DeadlineStatus.cs` — `Imminent`(D-3 임박)·`DueToday`(오늘 마감)·`Overdue`(경과). (b) `Domain/ValueObjects/Notification.cs` — 불변 record(`ProjectId`·`ProjectName`·`TodoId`·`TodoText`·`DateTime EndDate`·`DeadlineStatus Status`). (c) `Domain/Services/NotificationService.cs`(신규 폴더) — 순수 정적 `Detect(IEnumerable<ProjectItem> projects, DateTime today)` → `List<Notification>`(각 프로젝트 Todos에서 `EndDate!=null && Status!=Completed` 필터 → 경계 판정으로 DeadlineStatus 부여 → EndDate 오름차순). `BuildKey(Notification)` → `$"{TodoId}:{Status}"`(읽음 키). 한글 XML 주석.
- **Design**: 배치 = Domain(Enums·ValueObjects·신규 Services 폴더). 신규 심볼 = `DeadlineStatus`(enum)·`Notification`(값 객체 — 마감 알림 1건의 불변 사실)·`NotificationService`(마감 감지 순수 로직·읽음 키 생성). 의존 = TodoItem·ProjectItem·TodoStatus만 참조(순수, Repository·UI·전역 상태 무의존 — DateTime today를 인자로 주입해 테스트/재현 가능). 참조하는 곳 = T3 MainViewModel/NotificationPageViewModel. 비추상화 = 감지를 인터페이스·DI 없이 **정적 순수 함수**로(도메인 서비스 첫 도입이나 상태·의존이 없어 클래스 상태·주입 불필요 — NFR-2 도메인 로직 배치와 정합, YAGNI). IsRead는 값 객체에 넣지 않음(읽음은 영속 상태 기반 표현 관심사 — T3 표현 계층에서 부여).
- **Files**: `Domain/Enums/DeadlineStatus.cs`(신규), `Domain/ValueObjects/Notification.cs`(신규), `Domain/Services/NotificationService.cs`(신규)
- **Edge**: EndDate null·완료(Status==Completed) → 제외. Todos 빈/전부 완료 → 빈 목록. 경계값: 남은일수 4+ → 제외, 3/2/1 → 임박, 0 → 오늘, 음수 → 경과(시각 무시, `EndDate.Date`와 `today.Date` 비교). 같은 날 여러 작업 → 각각 알림.
- **Halt Forecast**: 없음(신규 순수 파일, 파괴적·외부 요소 없음).
- **Acceptance**: 빌드 0. `NotificationService.Detect`가 EndDate 있는 미완료 작업만 D-3 임박/오늘/경과로 분류(코드 대조: 경계식·완료 제외·EndDate!=null). `BuildKey`=`작업Id:상태`. 신규 도메인 심볼 3종 존재.

### T2 — 읽음 상태 영속화 (Type C)
- **내용**: `Domain/Entities/AppSettings.cs`에 `List<string> ReadNotificationIds { get; set; } = []`(읽은 알림 키 = 작업Id:마감상태) 추가. 한글 XML 주석. **AppJsonContext 무수정**(AppSettings 전체 직렬화 자동 커버 — 근거 Investigation). **AppSettingsDialogViewModel.LoadFrom/ApplyTo 무수정**(ApplyTo 제자리 변경으로 보존 — 근거 Investigation).
- **Design**: 해당 없음 — 기존 AppSettings Aggregate에 `List<string>` 필드 1개 추가만, 신규 공개 심볼(타입/메서드) 없음(Phase 1 HistoryKinds 선례 동형).
- **Files**: `Domain/Entities/AppSettings.cs`
- **Edge**: 구버전 저장 데이터(필드 없음) → JSON 역직렬화 시 기본 빈 리스트(초기자 `= []`). 읽음 키 누적 방지는 T3 재계산 시 프루닝(현재 알림 키와 교집합)으로 처리.
- **Halt Forecast**: 없음(비파괴 영속화 필드 추가 — NFR-3 하위호환).
- **Acceptance**: 빌드 0. AppSettings에 `ReadNotificationIds`(기본 빈 리스트) 존재. 직렬화/역직렬화 왕복 시 보존(AppJsonContext 자동 — 코드 대조로 Context·다이얼로그 매핑 무수정 확인).

### T3 — MainViewModel 집계·읽음 처리 + NotificationPageViewModel (Type D)
- **내용**:
  - `MainViewModel`: (a) `GetAllProjectItemsWithTodos()`(WithHistories 미러 — ToModel + `_projectRepository.GetTodos(item.Id)` per-project). (b) `RebuildNotifications()` — WithTodos + `NotificationService.Detect(projects, DateTime.Now)` → 최신 알림 목록 보관 + `UnreadNotificationCount`([ObservableProperty], 안읽음=키 미포함) 갱신 + **읽음 키 프루닝**(`ReadNotificationIds` ∩ 현재 알림 키, 변경 시 SaveSettings). (c) `MarkNotificationRead(Notification)`/`MarkAllNotificationsRead(IEnumerable<Notification>)` — `ReadNotificationIds`에 키 추가 → SaveSettings → UnreadNotificationCount 갱신. (d) `IReadOnlyList<Notification> GetCurrentNotifications()`·`IReadOnlySet<string> GetReadKeys()`(페이지/패널 소비). (e) `TaskPageViewModel? CreateTaskPageViewModelForProject(string projectId)`(`_allCards`에서 id 매칭 카드 → CreateTaskPageViewModel; 없으면 null). 초기 로드 완료 시점에 `RebuildNotifications` 1회 호출(배지 초기값).
  - `Presentation/ViewModels/NotificationPageViewModel.cs`(신규): 알림 목록 + 읽음 키를 받아 **프로젝트별 그룹**(`NotificationProjectGroup` record: ProjectName + `List<NotificationItemViewModel>`) 구성. `NotificationItemViewModel`(ObservableObject): Notification 래핑 + `IsRead`([ObservableProperty]) + 마감상태 배지 표시용 노출(라벨·`DeadlineStatus`). 커맨드: `MarkRead`(항목)·`MarkAllRead`·`OpenTask`(항목 → 프로젝트 이동 이벤트). 읽음 처리는 MainViewModel 콜백(생성자 주입 `Action<Notification> markRead`·`Action markAll`·`Action<string> navigateToProject` — TaskPageViewModel의 `Action refreshCardState` 주입 선례). `HasNotifications`/빈 상태.
- **Design**: 배치 = Presentation/ViewModels(신규 NotificationPageViewModel + MainViewModel 확장) + Domain 소비. 신규 심볼 = `NotificationPageViewModel`(그룹·읽음·이동)·`NotificationItemViewModel`(항목 읽음 상태)·`NotificationProjectGroup`(그룹 record)·MainViewModel 공개 메서드(WithTodos·RebuildNotifications·MarkNotificationRead/All·GetCurrentNotifications·GetReadKeys·CreateTaskPageViewModelForProject)·`UnreadNotificationCount`. 의존 = NotificationService·Notification(T1)·AppSettings.ReadNotificationIds(T2)·GetTodos·TaskPageViewModel. 참조하는 곳 = T4 NotificationPage·T5 헤더/MainWindow. 비추상화 = 그룹/항목은 record + ObservableObject(신규 컬렉션 프레임워크·제네릭 계층 없이 HistoryDateGroup·TestSuiteGroup 선례 동형). 읽음 저장은 MainViewModel이 단일 소유(`_settings.ReadNotificationIds` + SaveSettings) — VM은 콜백으로 위임(영속 소유 분산 방지). 이동은 특정 카드 포커스 없이 **프로젝트 작업 페이지 열기**(사용자 결정 — ScrollToItem 신규 인프라 회피).
- **Files**: `Presentation/ViewModels/MainViewModel.cs`, `Presentation/ViewModels/NotificationPageViewModel.cs`(신규)
- **Edge**: 알림 0건 → 빈 그룹·UnreadCount 0. 읽은 뒤 마감 악화(임박→오늘) → 새 키라 안읽음 재등장(재환기, 사용자 결정). 작업 삭제/완료 → 다음 RebuildNotifications에서 소멸 + 프루닝으로 읽음 키 정리. 프로젝트 다수·Todos 다수 → per-project GetTodos 반복(초기 1회 + 열 때 재계산, 성능은 사용자 확인). 이동 대상 카드 없음(삭제됨) → null 가드(무동작). "모두 읽음"은 현재 표시 알림 전체 대상.
- **Halt Forecast**: 없음 — MainViewModel 공개 메서드 추가·신규 VM은 계획된 Files 내 cross-file(파괴적·외부 요소 없음). 기존 public 시그니처 변경 없음(추가만).
- **Acceptance**: 빌드 0. RebuildNotifications가 전체 프로젝트 Todo를 로드해 알림 집계·UnreadNotificationCount 설정·읽음 키 프루닝(코드 대조). MarkNotificationRead/All이 ReadNotificationIds 갱신 + SaveSettings 호출. NotificationPageViewModel이 프로젝트별 그룹·항목 IsRead·이동/읽음 커맨드 노출. CreateTaskPageViewModelForProject가 id로 카드 조회. depends T1, T2.

### T4 — NotificationPage 전체 페이지 + resw (Type D)
- **내용**: `Presentation/Views/NotificationPage.xaml`(+`.cs`) 신규 — 헤더(← 대시보드[Back_Click → ShowDashboard]·제목·"모두 읽음" 버튼[MarkAllRead])·프로젝트별 그룹 목록(그룹 헤더 프로젝트명 + 항목 카드)·항목 카드(작업 텍스트·마감상태 배지[색+라벨, 임박/오늘/경과]·종료일·안읽음 강조·클릭 시 OpenTask 이동)·빈 상태("마감 임박 작업 없음"). 생성자 `NotificationPage(NotificationPageViewModel vm)`. 로컬라이즈(x:Uid/.resw ko·en) 신규 문자열(제목·모두 읽음·마감상태 라벨 3종·빈 상태). 마감상태 배지 색은 정적 헬퍼/컨버터(TestPage 상태 색 헬퍼 패턴 — x:Bind 함수 Brush 직접 반환).
- **Design**: 배치 = Presentation/Views(신규 페이지 XAML+코드비하인드) + Strings. 신규 심볼 = `NotificationPage`(전체 페이지 View)·마감상태→색/라벨 표시 헬퍼. 의존 = NotificationPageViewModel(T3)·마감상태 배지 색·TaskPage 헤더 관례. 참조하는 곳 = T5(헤더 "모든 알림 보기"·MainWindow ShowPage). 비추상화 = 페이지는 TaskPage/TestPage 레이아웃 관례 재사용(신규 네비 프레임워크 없음), 배지 색은 정적 헬퍼(신규 컨버터 최소화 — TestPage 선례).
- **Files**: `Presentation/Views/NotificationPage.xaml`(신규), `Presentation/Views/NotificationPage.xaml.cs`(신규), `Strings/ko-KR/Resources.resw`, `Strings/en-US/Resources.resw`, `DevDashboard_WinUI.csproj`(신규 Page 등록 — 자동 include 여부 확인 후 필요 시)
- **Edge**: 알림 0 → 빈 상태 표시(그룹·"모두 읽음" 숨김 또는 비활성). 긴 작업 텍스트 → 트리밍. 안읽음/읽음 시각 구분. 경과 항목 강조(코랄 계열). 항목 클릭 → 이동 후 페이지 전환.
- **Halt Forecast**: 없음 — XAML 페이지·resw 추가, 파괴적·외부 요소 없음. csproj Page include는 기존 관례(Phase 2/3 TaskPage/TestPage 추가 선례).
- **Acceptance**: 빌드 0. NotificationPage가 프로젝트별 그룹·마감상태 배지·읽음 표시·"모두 읽음"·빈 상태 렌더, 항목 클릭 시 이동 커맨드 호출. resw ko/en 신규 키 등록. **시각 렌더는 사용자 확인 필요.** depends T3.

### T5 — 헤더 벨 버튼 + Flyout 드롭다운 + 배지 + MainWindow 배선 (Type D)
- **내용**: (a) `MainWindow.xaml`: `HeaderButtonsPanel`의 ExportImportButton↔AppSettingsButton 사이에 `NotificationButton`(36×36 + 벨 FontIcon + x:Uid) 추가, 우상단 **안읽음 개수 배지**(UnreadNotificationCount>0일 때 표시, 색 점+숫자), 버튼에 `Flyout` 드롭다운 패널(요약 목록 상위 N + "모든 알림 보기" 버튼·빈 상태). 배지·요약은 `_viewModel`(UnreadNotificationCount·GetCurrentNotifications) 바인딩. (b) `MainWindow.xaml.cs`: Flyout Opening 시 `_viewModel.RebuildNotifications()`(최신화) + 요약 갱신, "모든 알림 보기" → `new NotificationPage(new NotificationPageViewModel(...))` 구성 후 `ShowPage`, 항목 이동 이벤트 → `_viewModel.CreateTaskPageViewModelForProject(id)` → `new TaskPage(vm, settings)` → `ShowPage`(카드→페이지 선례). 요약 항목/페이지가 공유하는 NotificationPageViewModel 구성 헬퍼. (c) resw(ko/en) 신규 문자열(벨 툴팁·"모든 알림 보기"·빈 상태). 아이콘 버튼 접근성 `ToolTipService.ToolTip`(Phase 4 T5 교훈).
- **Design**: 배치 = Presentation(MainWindow XAML/코드비하인드) + Strings. 신규 심볼 = `NotificationButton`·드롭다운 Flyout 패널·배지 요소(XAML)·MainWindow 핸들러(Flyout 열기·모든 알림 보기·항목 이동)·NotificationPageViewModel 구성 헬퍼. 의존 = MainViewModel(T3 UnreadNotificationCount·RebuildNotifications·GetCurrentNotifications·CreateTaskPageViewModelForProject)·NotificationPage(T4)·ShowPage. 비추상화 = 벨/Flyout은 기존 헤더 버튼·SortButton Flyout 패턴 재사용(신규 컨트롤 없음), 배지는 표준 Border+TextBlock(커스텀 배지 컨트롤 미신설). 이동은 카드→페이지 선례 재사용.
- **Files**: `MainWindow.xaml`(프로젝트 루트), `MainWindow.xaml.cs`(프로젝트 루트), `Presentation/ViewModels/MainViewModel.cs`(배지 표시용 `HasUnreadNotifications` 파생 프로퍼티 추가 — 구현 중 편입), `Strings/ko-KR/Resources.resw`, `Strings/en-US/Resources.resw`
- **Edge**: 안읽음 0 → 배지 숨김. 프로젝트 없음(HasAnyProjects false) → 벨 무알림·빈 상태. 드롭다운 요약 다수 → 상위 N만(예: 5) + "모든 알림 보기". Flyout 열 때마다 재계산(최신 반영). 이동 대상 카드 없음 → null 가드. 배지 개수 큰 값 → 표시 상한(예: "9+").
- **Halt Forecast**: 없음 — 헤더 버튼/Flyout/배지 추가·MainWindow 핸들러 배선, 파괴적·외부 요소 없음.
- **Acceptance**: 빌드 0. 헤더에 벨 버튼·안읽음 배지(개수)·드롭다운(요약 + "모든 알림 보기") 표시, 열 때 재계산. "모든 알림 보기" → NotificationPage 전환, 항목 클릭 → 해당 프로젝트 TaskPage 이동. resw ko/en 등록. **시각 렌더는 사용자 확인 필요.** depends T3, T4.

## PRD Coverage
| PRD ID | 우선순위 | 대응 task | 상태 |
|--------|---------|----------|------|
| FR-N1 (마감 감지 — D-3/오늘/경과, 완료 제외) | Must | T1, T3(집계) | ✅ 커버 |
| FR-N2 (알림 전체 페이지 — 그룹·읽음·모두읽음·이동) | Must | T3(VM), T4(페이지) | ✅ 커버 |
| FR-N3 (헤더 드롭다운 패널 — 요약 + 모든 알림 보기) | Must | T5 | ✅ 커버 |
| FR-N4 (읽음 상태 영속화) | Must | T2(저장), T3(처리) | ✅ 커버 |
| FR-C*·S*·T*·E*·H* | — | (Phase 0~4 완료) | ✅ 이전 Phase 기구현 |

## 4-D 재사용 확인
| 신규 심볼 | 유사 기존 구현 검색 결과 | 재사용/신규 사유 |
|---|---|---|
| NotificationService | (없음 — Domain/Services 폴더 부재) | **신규**(마감 감지 순수 로직, 첫 도메인 서비스 — 정적 순수) |
| Notification / DeadlineStatus | (없음) | **신규**(알림 값 객체·마감상태 enum) |
| GetAllProjectItemsWithTodos | MainViewModel.GetAllProjectItemsWithHistories(동형 로더) | **미러 신규**(Histories→Todos, GetTodos 재사용) |
| NotificationPageViewModel/Item/Group | HistoryDateGroup·TestSuiteGroup·TaskPageViewModel(그룹+항목 VM 패턴) | **신규**(패턴 재사용, 알림 도메인 전용) |
| NotificationPage | TaskPage·TestPage(전체 페이지 + ShowPage 네비) | **패턴 재사용 신규**(헤더·빈상태·네비 관례) |
| 헤더 벨 버튼·Flyout·배지 | HeaderButtonsPanel 버튼·SortButton Flyout | **패턴 재사용**(신규 컨트롤 없음) |
| 마감상태 배지 색 | TestPage 상태 색 정적 헬퍼(통과/실패/미실행) | **패턴 재사용**(정적 Brush 헬퍼) |
| CreateTaskPageViewModelForProject | ProjectCardViewModel.CreateTaskPageViewModel | **재사용**(id로 카드 조회 후 위임) |

## Decisions
- **D1 마감 감지 경계**: `EndDate.Date` vs `today.Date` 남은일수 — 3/2/1=임박, 0=오늘, 음수=경과, 4+ 제외. Status==Completed·EndDate==null 제외. Source: PRD §3 "D-3 임박 + 오늘 마감 + 경과, 완료 제외"(자체 확정).
- **D2 읽음 키 = 작업Id + 마감상태(재환기)**: 키=`{TodoId}:{DeadlineStatus}`. 마감 악화(임박→오늘→경과) 시 새 키라 안읽음 재등장. Source: 사용자 결정(2026-07-19).
- **D3 읽음 키 누적 방지**: RebuildNotifications 시 ReadNotificationIds를 현재 알림 키와 교집합으로 프루닝(변경 시 저장). Source: D2 재환기로 키 누적 → 경계 유지 위해 자체 확정(현재 알림에 없는 키는 해당 (작업,상태) 소멸이라 안전).
- **D4 클릭 이동 = 프로젝트 작업 페이지 열기**: 특정 작업 카드 스크롤/포커스 없이 해당 프로젝트 TaskPage 전환(ShowPage 재사용). Source: 사용자 결정(2026-07-19).
- **D5 벨 배지 = 안읽음 개수 표시**: 벨 우상단 개수 배지(0이면 숨김). Source: 사용자 결정(2026-07-19).
- **D6 감지 로직 위치 = Domain/Services 정적 순수**: DI·인터페이스 없이 정적 함수(상태·의존 없음). Source: NFR-2(도메인 로직 Domain 배치) + 기존 무 DI-서비스 관례 + YAGNI(자체 확정).
- **D7 읽음 상태 저장 = AppSettings.ReadNotificationIds(List\<string\>)**: 신규 테이블 없이 AppSettings JSON 영속(AppJsonContext 자동). Source: Phase 1 HistoryKinds/PageSize 선례(자체 확정).
- **D8 알림 갱신 시점 = 초기 로드 1회 + 드롭다운/페이지 열 때 + 읽음/작업 변경 후 재계산**: 파생 데이터라 온디맨드 재계산, 배지 초기값은 로드 후 1회. Source: 파생 특성 + 배지 표시 요구(자체 확정).
- **D9 단위 테스트 미추가**: 테스트 프로젝트 부재 — 현행 유지(빌드+사용자 확인 검증). Source: 사용자 결정(2026-07-19) + Phase 0~4 일관.
- **D10 브랜치**: master 기준 새 브랜치 `task/phase5-notifications`. Source: Phase 3/4 선례(Phase별 새 브랜치).

## Open Questions
- [x] 읽음/안읽음 추적 키 → **작업Id + 마감상태(재환기)**. (D2)
- [x] 알림 클릭 이동 범위 → **프로젝트 작업 페이지 열기**. (D4)
- [x] 헤더 벨 안읽음 개수 배지 → **표시**. (D5)
- [x] NotificationService 단위 테스트 → **미추가(현행 유지)**. (D9)
- [x] 감지 경계·로직 위치·저장 위치·갱신 시점 → **자체 확정**(D1/D3/D6/D7/D8).

## 사전 승인 항목 (일괄 승인 대상)
- **도메인 확장**(T1): 신규 `Domain/Services/` 폴더 + `NotificationService`·`Notification`·`DeadlineStatus`(비파괴 신규 심볼).
- **영속화 필드 추가**(T2): AppSettings에 `ReadNotificationIds`(List\<string\>) 추가(비파괴, AppJsonContext 자동).
- **VM 공개 API 확장**(T3): MainViewModel 신규 공개 메서드·`UnreadNotificationCount` 추가(기존 시그니처 무변경 — 추가만). 신규 NotificationPageViewModel.
- **신규 페이지·헤더 배선**(T4/T5): NotificationPage 신규 등록, 헤더 벨 버튼/Flyout/배지 추가.
- .resw 신규 문자열 추가(ko/en) — 알림 제목·모두 읽음·마감상태 라벨·벨 툴팁·빈 상태.

## 불가피한 Halt (위임 불가)
- commit/push는 구현·검증 후 별도 승인. master 병합·태그·릴리즈·PR은 이 위임에 미포함.
- 새 브랜치 `task/phase5-notifications` 생성 + 로컬 작업 commit(체크포인트·task 완료)은 implement-task 위임 범위(plan 승인 포함). push·병합만 별도.

## Deferred / Follow-up
- [교차 작업/테스트 집계 페이지](PRD D-2) — 유관하나 별개(알림은 FR-N2 전용). 대장 유지.
- README/스크린샷 갱신(Phase 5 시각 확인 후) — 리디자인 5개 영역 완료 시점에 통합 갱신 검토.
- Todo*/Test* 구 resw 고아·`TestDateGroup`·`HistoryEntryDialogViewModel`·`InitKindCombo`/`KindFromCombo` 중복 정리(Phase 2/3/4 이월, 대장 등재) — 별도 세션.
- 알림 요약 상위 N 개수·배지 상한("9+") 등 세부 수치는 시각 확인 시 조정 가능(순수 값, 후속).
- [인코딩] `MainViewModel.cs`·`MainWindow.xaml`·`Resources.resw`(ko/en) 등 레거시 BOM 파일 — Edit 시 기존 BOM 보존(CLAUDE.md "기존 인코딩 유지" 준수, Phase 4에서 BOM 손실이 MAJOR였음). check-utf8-and-lines hook은 no-BOM을 권고하나 전체 파일 인코딩 변환은 이번 범위 밖이라 미수행(신규 파일은 no-BOM). 프로젝트 BOM/no-BOM 통일은 별도 세션.

## Out of Scope (이 Phase)
- git 커밋 ↔ 작업/알림 자동 연동(PRD §7 영구 제외).
- 실제 OS 토스트/윈도우 알림 센터 연동(PRD 범위 밖 — 인앱 알림만).
- 특정 작업 카드 스크롤/하이라이트 포커스(D4로 프로젝트 페이지 열기 채택 — 신규 인프라 미도입).
- 범용 교차 프로젝트 집계 페이지(D-2, Deferred).
- 구버전 설정 export를 import해 AppSettings가 통째 교체되는 경우 `ReadNotificationIds`가 기본값(빈)으로 리셋될 수 있음 — **양성(읽음 상태 재설정) 허용**, 별도 마이그레이션 미도입(plan-reviewer m2).

## Progress Log
- T1-T2 완료 (커밋 cdd861a, 다음): T1 Domain 신규 3파일(DeadlineStatus·Notification·NotificationService 정적 순수 Detect/BuildKey, 경계 D-3/오늘/경과·완료 제외·EndDate!=null). T2 AppSettings.ReadNotificationIds(List<string>) 추가(직렬화 왕복 보존 코드 대조 — AppJsonContext 통짜·ApplyTo 제자리 mutate). 빌드 x64 OK·신규 경고 0. 두 task 모두 spec/quality OK.
  - 결정: GlobalUsings에 Domain.Services 등록은 소비처 생기는 T3로 이연(T1 단독 등록 시 CS8019 불필요 using 경고 회피). csproj 실제명 `DevDashboard.csproj`(메모리의 DevDashboard_WinUI.csproj는 부정확).
- T3-T4 완료 (커밋 f48d4d3, 38ee59c→): T3 MainViewModel 알림 집계·읽음 처리·이동 진입점 + NotificationPageViewModel(그룹·읽음·커맨드). T4 NotificationPage(UserControl, 정적 헬퍼 색/라벨/포맷, 프로젝트별 그룹·배지·빈상태) + resw ko/en 9키. 빌드 x64 OK·신규 경고 0. 두 task 모두 spec/quality OK.
  - 결정: csproj Page 자동 include(`<Page Remove="App.xaml"/>`만 존재)라 NotificationPage 등록 편집 불필요. 마감상태 배지 색 = 경과 코랄#f0716a/오늘 앰버#e8b45a/임박 블루#5aa3e8(PRD §3 팔레트, TestPage 헬퍼 패턴). 항목 클릭=Border Tapped→OpenTaskCommand, 읽음 버튼=Click→MarkReadCommand(TaskPage Tag 패턴). 시각 렌더는 ⏳ HUMAN-VERIFY.
- T5 완료 (커밋 ca6c58a→): 헤더 벨 버튼(E7ED Ringer)+안읽음 배지({Binding HasUnreadNotifications/UnreadNotificationCount})+Flyout 드롭다운(요약 상위5 + 모든 알림 보기). MainWindow 핸들러 NotificationFlyout_Opening(재계산)·ViewAllNotifications_Click(→NotificationPage)·NavigateToProjectTasks(→TaskPage). MainViewModel에 HasUnreadNotifications 파생 프로퍼티+NotifyPropertyChangedFor(배지 표시용, Files 편입). resw ko/en 2키(Header_Tooltip·ViewAll). 빌드 x64 OK·신규 경고 0. spec MINOR(Files 목록 누락→plan 정정)·quality OK+SUGGEST(배지 상한 — Deferred 기등재). 전체 알림 심볼 end-to-end 배선 완결.
  - 결정: 배지 visibility는 Window XAML x:Bind+Converter CS1503 회피 위해 {Binding ..., Converter=BoolToVisibility}(DataContext=RootGrid._viewModel 상속). 요약 항목은 List<string> 프리포맷(Window XAML 바인딩 단순화). 시각 렌더 ⏳ HUMAN-VERIFY.

## Phase Ledger
- (착수 전 — 전 task 완료 후 Phase F/G 진행)

## Next Steps
- 권장 다음 액션: master 기준 새 브랜치에서 implement-task로 T1부터 순차(T1 도메인·감지 → T2 영속화 → T3 집계·VM → T4 페이지 → T5 헤더). 완료 후 시각·동작 사용자 확인 → Phase F/G.
- Suggested skills: pjc:implement-task(승인 후).

## 통과 체크리스트
- [x] 요구 이해 작성(원문 인용 + 이해 4줄)
- [x] Impact Analysis 4-A(TodoItem/EndDate·MainViewModel 집계·헤더/네비 진입점·AppSettings 영속 전수)·4-B(직렬화: AppJsonContext 자동·ApplyTo 제자리 변경으로 손실 없음 검증)·4-C(테스트 없음 — 테스트 프로젝트 부재, 사용자 결정 미추가)·4-D(재사용표 작성)
- [x] 각 task acceptance 검증 가능(빌드 + 시각/동작 구분), 상호 모순 0
- [x] Type 분류(T1 C·T2 C·T3 D·T4 D·T5 D), Design 필드(Type D 전부 + 신규 심볼 Type C — T1)
- [x] Edge/Halt Forecast 각 task 명시
- [x] Open Questions 전부 해결, 결정 분기 0
- [x] 분할 권고(5 task ≤ 8) — 미해당
