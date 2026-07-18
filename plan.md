# plan.md — Phase 4: 작업 기록 — 유형(kind) + 페이지네이션 + 공통 베이스 VM 정리

**PRD**: docs/prd.md (Phase 4 = FR-H1, FR-H2, FR-H3, FR-H4 + 결정 D-5)
**전체 목표**: DevDashboard 5개 영역 리디자인(§PRD). 이 plan은 **Phase 4(작업 기록)** 만 다룬다.
**이전 plan**: Phase 3(테스트) — 완료(`Phase G 통과`), task/phase3-test 브랜치. **Phase 4 구현 전 Phase 3를 master 병합**(사용자 결정) 후 master 기준 새 브랜치.
**다음 plan**: Phase 5(알림) — 별도 승인·실행.

## 요구 이해

> 원문(PRD §4 작업 기록 Phase 4 발췌): "FR-H1 `HistoryEntry`에 유형(kind) 필드 추가, 앱 설정의 작업 기록 유형(+작업 카테고리)에서 선택. 영속화(마이그레이션 포함). FR-H2 작업 기록 다이얼로그에 페이지네이션 적용(페이지당 표시 개수 = FR-S1). FR-H3 작업 기록 다이얼로그를 디자인 레이아웃으로 restyle(검색·유형 배지·날짜 그룹·펼침 상세·내보내기·인라인 새 기록 폼). FR-H4 HistoryDialog·ProjectHistoryDialog 중복 정리(공통 컴포넌트화 또는 통합)." + 결정 D-5(공통 컴포넌트화 기본) + 사용자 스코핑(2026-07-19): FR-H4=공통 베이스 VM 추출, FR-H2=페이지 버튼(이전/다음+번호), 브랜치=Phase 3 master 병합 후.

이해한 요구(Phase 4):
- `HistoryEntry`에 **유형(Kind)** 필드를 추가하고, 새 기록/편집 폼에서 앱 설정 유형(`DefaultHistoryKinds` + `settings.HistoryKinds`)에서 선택하도록 하며 DB 마이그레이션으로 영속화한다.
- 작업 기록 다이얼로그에 **페이지네이션(페이지 버튼: 이전/다음+번호)** 을 적용하고 `AppSettings.PageSize`(기본 100)를 소비한다.
- 목록에 **유형 배지**를 추가한다. 검색·날짜 그룹·펼침 상세·마크다운 내보내기·인라인 새 기록 폼은 **이미 구현**되어 있어 유지한다.
- `HistoryDialogViewModel`·`ProjectHistoryDialogViewModel`의 중복 로직(검색·날짜그룹·CRUD·마크다운·펼침·유형·페이지네이션)을 **공통 추상 베이스 VM으로 추출**해 두 VM이 상속한다(두 다이얼로그는 유지 — 카드별/전체).

## Goal
`HistoryEntry`에 `Kind`(유형) 문자열 필드를 추가하고 SQLite 하위호환 마이그레이션으로 영속화한다. `HistoryDialogViewModel`·`ProjectHistoryDialogViewModel`의 중복 로직을 신규 추상 `HistoryDialogViewModelBase`로 이전하고(검색·날짜그룹·CRUD·마크다운·펼침 + 신규 페이지네이션·유형 필터), 두 VM은 상속으로 각자의 저장/프로젝트 선택만 담당한다. 새 기록/편집 폼과 개별 편집(`HistoryEntryDialogViewModel`)에 유형 선택을 추가하고, 목록에 유형 배지(`TagColorConverter` 재사용)와 페이지 컨트롤(이전/다음+번호)을 배선한다.

## Investigation Log (근거)

### 위키·시안
- 위키 참조: vault 미설정 — 코드 1차 출처로 진행.
- 리디자인 시안: 목업 HTML(claude.ai/design)이 레포에 없음(Phase 0~3과 동일). 시각 명세는 PRD §3 추출값 + WinUI idiom, 빌드 후 사용자 확인.
- Deferred 대장(docs/plans/deferred.md) 확인: `[FR-H2 선행] PageSize 실제 소비 — 작업 기록 페이지네이션, Phase 4에서 구현 예정` 항목을 이번 재수용(T3/T5). 종결 처리 예정.

### 도메인·설정 (직접 Read)
- **HistoryEntry**(Domain/Entities/HistoryEntry.cs): Id/Title/Description/CompletedAt/CreatedAt. **유형(Kind) 없음** → 추가 대상.
- **AppSettings**(AppSettings.cs): `HistoryKinds`(31, List<string> 사용자 정의)·`PageSize`(34, 기본 100) — Phase 1 추가·영속화 완료.
- **AppSettingsDialogViewModel**: `DefaultHistoryKinds`(258-261 = "기능","수정","리팩터링","문서", PRD §3 일치)·`HistoryKinds`(264, 사용자) — 유형 소스로 재사용(cross-VM 정적 참조, Phase 2/3 선례).

### 영속성 (직접 Read)
- **DatabaseContext**: `Histories` 테이블(260-268 = Id/ProjectId/Title/Description/CompletedAt/CreatedAt). **Kind 컬럼 없음**. 마이그레이션 = `MigrateSchema`(`AddColumnIfNotExists` + `AllowedIdentifiers` 화이트리스트 167). `Kind`는 화이트리스트 미등록 → T2 등록 필수.
- **SqliteProjectRepository**: read **2경로** — `GetHistories`(128-150, 메인 지연로딩)·`ReadHistoriesForProject`(869-, import용). write = `InsertHistories`(582-609, 단일 공유 — SaveHistories 215/Add/Save/import 다경로가 호출). 저장은 `SaveHistories`(215, delete+reinsert). **Kind read 2곳·write 1곳(InsertHistories)에 추가**. `@kind` 루프 갱신 누락 주의(Phase 3 T2 quality MAJOR 교훈 — 파라미터는 루프 밖 1회 초기화, 루프 내 각 항목 값 갱신 필수).

### UI·VM (직접 Read — FR-H3/H4 현황)
- **HistoryDialogViewModel**(HistoryDialogViewModel.cs:82-216): SearchText·DateGroups(날짜 그룹)·HasEntries·AddEntry/UpdateEntry/DeleteEntry·`RebuildGroups`(검색 필터+날짜 GroupBy)·`ExportToMarkdown`·NewEntries·SaveToModel. **HistoryEntryViewModel**(30-79: 펼침 IsExpanded·DescriptionVisibility·ExpandChevron)·**HistoryDateGroup**(11-27) 동일 파일 정의(ProjectHistory도 공유).
- **ProjectHistoryDialogViewModel**(ProjectHistoryDialogViewModel.cs:10-198): 위와 **거의 동일**(SearchText·DateGroups·CRUD·RebuildGroups·ExportToMarkdown) + `SelectedProject`/`Projects` ComboBox·`_modifiedProjectIds` 다중 저장(`SaveAll` 191-196). → **중복 로직이 베이스 추출 대상**.
- **HistoryEntryDialogViewModel**(HistoryEntryDialogViewModel.cs): 등록/수정 다이얼로그 — Title/Description/CompletedAt·Validate·ToModel. **유형 없음** → 추가.
- **HistoryDialog.xaml.cs**: 검색(SearchBox_TextChanged)·**인라인 새 기록 폼**(AddPanel/ToggleAddPanel/SaveAdd_Click 160-176)·펼침(EntryBorder_Tapped)·편집(중첩 ContentDialog, EditEntry_Click)·삭제·**마크다운 내보내기**(Export_Click 208). **유형 UI·페이지네이션 없음**. `OpenAddPanel`(83, Todo 완료 훅에서 호출).
- **진입점**: 카드 "작업기록" → `OnOpenHistoryRequested`(DashboardView.xaml.cs:194-209) → `HistoryDialog`. MainWindow 헤더 "전체 작업 기록"(MainWindow.xaml.cs:474/543) → `ProjectHistoryDialog(projects, repo)`.

### 재사용 (직접 Read)
- **TagColorConverter**(Presentation/Converters/DevToolConverters.cs): 이름→결정론적 색(작업 카테고리 dot·테스트 스위트에서 재사용) → **유형 배지 색에 재사용**.
- **페이지네이션**: 기존 구현 없음(신규). 날짜 그룹 유지 위에 페이지 슬라이스 계층 추가.

## 시각 요소 분해 (작업 기록 다이얼로그 — 출처 PRD §3/§4, 목업 부재로 세부는 사용자 확인)

| 요소 | 속성 | 디자인 값 | 확인 방법 |
|------|------|----------|-----------|
| 유형 배지 | 표시 | 항목에 유형 배지(색+텍스트) | PRD §4 FR-H3 "유형 배지" |
| 유형 배지 | 색 | 이름 기반 결정론적 색(TagColorConverter) | 작업 카테고리 dot 관례 재사용 |
| 유형 선택 | 컨트롤 | 새 기록/편집 폼에 유형 ComboBox | PRD §4 FR-H1 "설정 유형에서 선택" |
| 페이지 컨트롤 | 구성 | 이전/다음 버튼 + 페이지 번호 표시 | 사용자 결정(페이지 버튼) |
| 페이지 크기 | 값 | PageSize(기본 100, 설정값) | PRD §4 FR-H2/FR-S1 |
| 검색·날짜그룹·펼침·내보내기·인라인폼 | — | 기존 유지 | HistoryDialog 현행(기구현) |
| 팔레트·폰트 | — | 배경 #131316·카드 #1c1c20·강조 코랄 #f0716a, Malgun Gothic | PRD §3(Phase 0 전역 적용 완료) |
> 팔레트·폰트는 Phase 0 전역 적용. 신규 시각 요소(유형 배지·페이지 컨트롤)만 추가. 세부 레이아웃은 목업 부재 → WinUI idiom 후 사용자 확인.

## Tasks

### 진행 체크리스트
- [x] T1 — HistoryEntry 유형(Kind) 필드(도메인)
- [x] T2 — Histories 영속화(Kind 컬럼 마이그레이션 + Repository)
- [ ] T3 — HistoryDialogViewModelBase 추출 + 페이지네이션·유형 필터
- [ ] T4 — 새 기록/편집 폼 유형 선택
- [ ] T5 — 페이지네이션 UI + 유형 배지 + restyle(두 다이얼로그)

### T1 — HistoryEntry 유형(Kind) 필드 (Type C)
- **내용**: `Domain/Entities/HistoryEntry.cs`에 `string Kind`(빈=미분류, 작업 기록 유형) 추가. 한글 XML 주석. (HistoryEntry는 `ObservableObject` 아닌 POCO — 기존 관례 유지, Kind도 단순 프로퍼티.)
- **Design**: 배치 = Domain(HistoryEntry.cs 단일). 신규 심볼 = `Kind` 프로퍼티(작업 기록 유형 기재). 의존 = 없음. 참조하는 곳 = T2 영속화·T3 VM(필터/배지)·T4 다이얼로그. 비추상화 = 유형을 enum·값 객체가 아닌 단순 string(작업 Category·테스트 Method 관례와 동형 — 사용자 정의 유형이 동적이라 문자열이 자연).
- **Files**: `Domain/Entities/HistoryEntry.cs`
- **Edge**: 기존 기록엔 Kind 없음 → 기본 빈 문자열(미분류). 빈 Kind는 배지 미표시(T5).
- **Halt Forecast**: 없음(비파괴 필드 추가).
- **Acceptance**: 빌드 0. HistoryEntry에 `Kind` 프로퍼티 존재(기본 빈 문자열).

### T2 — Histories 영속화: Kind 컬럼 마이그레이션 + Repository (Type D)
- **내용**: (a) `DatabaseContext`: `AllowedIdentifiers`에 `Kind` 추가. `MigrateSchema`에 `AddColumnIfNotExists(conn,"Histories","Kind","TEXT NOT NULL DEFAULT ''")`. `CREATE TABLE Histories`에 `Kind TEXT NOT NULL DEFAULT ''` 컬럼 추가. (b) `SqliteProjectRepository`: `InsertHistories`(582)에 Kind write(INSERT 컬럼/파라미터 + **루프 내 `@kind` 값 갱신** — 누락 시 항상 빈값 저장, Phase 3 T2 교훈), `GetHistories`(128)·`ReadHistoriesForProject`(869)에 Kind read(`HasColumn` 가드 — 없으면 빈값).
- **Design**: 해당 없음 — 기존 마이그레이션·직렬화 경로(AddColumnIfNotExists/Insert/Read)에 컬럼·필드 추가만, 신규 공개 심볼 없음.
- **Files**: `Infrastructure/Persistence/DatabaseContext.cs`, `Infrastructure/Persistence/SqliteProjectRepository.cs`
- **Edge**: 구버전 DB(Kind 없음) → `AddColumnIfNotExists` 추가·read `HasColumn` false 시 빈값. 화이트리스트 미등록 Kind → T2 등록으로 ArgumentException 예방. `@kind` 루프 갱신 필수(silent data loss 방지).
- **Halt Forecast**: 없음(NFR-3 비파괴 마이그레이션).
- **Acceptance**: 빌드 0. Histories에 Kind 컬럼 마이그레이션. 저장→로드 왕복 시 Kind 보존(코드 대조: Insert 루프 `@kind` write, Get/Read가 Kind read). 구버전 DB 로드 시 예외 없음. depends T1.

### T3 — HistoryDialogViewModelBase 추출 + 페이지네이션·유형 필터 (Type D)
- **내용**: 신규 추상 `Presentation/ViewModels/HistoryDialogViewModelBase.cs`:
  - 공통 이전: `SearchText`·`DateGroups`·`HasEntries`·`RebuildGroups`(검색 필터+날짜 GroupBy)·`AddEntry`/`UpdateEntry`/`DeleteEntry`/`RefreshAfterEdit`·`ExportToMarkdown`. 현재 엔트리 목록은 protected(`_entries`) + 추상 훅(`OnEntriesModified()` — 파생이 저장/수정표시 처리).
  - **페이지네이션(FR-H2)**: 베이스 생성자에 **`int pageSize` 주입**(AppSettings는 전역 정적이 아니라 생성자 전파 방식 — 호출부가 보유한 `settings.PageSize`를 전달)·`CurrentPage`·`TotalPages`(필터 후 항목 수 / PageSize)·페이지 이동 커맨드(`PrevPage`/`NextPage`/`GoToPage`)·`PageInfoText`("N / M"). `RebuildGroups`가 **필터 → CurrentPage 슬라이스(PageSize) → 날짜 그룹핑** 순으로 재구성. 검색어 변경·항목 추가/삭제 시 CurrentPage 보정(범위 클램프).
  - **유형(FR-H1)**: `AvailableKinds`(DefaultHistoryKinds + settings.HistoryKinds, cross-VM 정적 참조). `HistoryEntryViewModel`에 `Kind`·`KindVisibility`(빈이면 Collapsed) 노출(배지용).
  - 파생: `HistoryDialogViewModel(ProjectItem, int pageSize)`(단일 프로젝트 — SaveToModel·NewEntries·OnEntriesModified=플래그 없음), `ProjectHistoryDialogViewModel(List<ProjectItem>, IProjectRepository, int pageSize)`(다중 — SelectedProject/Projects·SaveAll·OnEntriesModified=MarkModified). **두 생성자에 pageSize 파라미터 추가**(그 외 public 멤버 시그니처는 유지 — CRUD/검색/내보내기 호출부 무영향), **생성 호출부 4곳은 각자 보유한 settings.PageSize를 전달하도록 함께 갱신**한다.
- **Design**: 배치 = Presentation/ViewModels(신규 베이스 파일 + 기존 2 VM 리팩터링 + 호출부 4파일). 신규 심볼 = `HistoryDialogViewModelBase`(공통 상태·검색·그룹·페이지네이션·유형·마크다운), 페이지 이동 커맨드·PageInfoText. 의존 = HistoryEntry·AppSettings.PageSize(int로 전달)·HistoryKinds·HistoryEntryViewModel/HistoryDateGroup(공유). 참조하는 곳 = 두 파생 VM·T4 다이얼로그·T5 XAML. 비추상화 = 공통 로직을 **추상 베이스로 이전**(제네릭·인터페이스 계층 없이 상속 1단계 — 두 VM만 존재). PageSize는 AppSettings 전체가 아닌 **int만 주입**(VM이 설정 객체 전체에 의존하지 않게 — 최소 결합). HistoryEntryViewModel/HistoryDateGroup은 이미 공유되므로 이동 없이 재사용, Kind 노출만 추가.
- **Files**: `Presentation/ViewModels/HistoryDialogViewModelBase.cs`(신규), `Presentation/ViewModels/HistoryDialogViewModel.cs`, `Presentation/ViewModels/ProjectHistoryDialogViewModel.cs`, `Presentation/ViewModels/ProjectCardViewModel.cs`(CreateHistoryDialogViewModel:550 → `_settings.PageSize` 전달), `Presentation/Views/TaskPage.xaml.cs`(:179 완료 워크로그 팝업 → `_settings.PageSize`), `Presentation/Views/Dialogs/ProjectHistoryDialog.xaml.cs`(:20 생성자에 pageSize 파라미터 추가+VM 전달), `MainWindow.xaml.cs`(:543 `new ProjectHistoryDialog(projects, repo, _settings.PageSize)`)
- **Edge**: 빈 기록 → 0 페이지(또는 1/1 빈). 검색으로 항목 급감 → CurrentPage 범위 초과 시 마지막 페이지로 클램프. 항목 추가/삭제 후 TotalPages 재계산. PageSize 큰데 항목 적음 → 1페이지. 프로젝트 전환(ProjectHistory) 시 CurrentPage=1 리셋. 유형 목록에 없는 기존 Kind → 배지엔 원 이름 표시(필터/선택 목록엔 없어도 무방).
- **Halt Forecast**: 없음 — 두 VM 생성자 시그니처 변경(pageSize 추가)은 호출부 4곳을 같은 task에서 함께 갱신하므로 계획된 cross-file 범위 내(파괴적·외부 요소 없음).
- **Acceptance**: 빌드 0. 베이스에 검색·날짜그룹·CRUD·마크다운·페이지네이션(CurrentPage/TotalPages/이동)·유형 노출 존재. 두 파생 VM이 상속, 생성자에 pageSize 추가되고 호출부 4곳(ProjectCardVM·TaskPage·ProjectHistoryDialog·MainWindow)이 settings.PageSize 전달하도록 갱신됨(코드 대조). CRUD/검색/내보내기 등 그 외 public 멤버 시그니처 유지. 필터→페이지 슬라이스→날짜그룹 순 재구성(코드 대조). depends T1.

### T4 — 새 기록/편집 폼 유형 선택 (Type C)
- **내용**: (a) `HistoryEntryDialogViewModel`: `SelectedKind`(string)·`AvailableKinds`(DefaultHistoryKinds+settings.HistoryKinds) 추가, `ToModel`이 Kind 반영, 생성자 편집 시 기존 Kind 로드. (b) `HistoryDialog.xaml.cs` 인라인 새 기록 폼(SaveAdd_Click)·편집(EditEntry_Click 중첩 다이얼로그)에 유형 ComboBox 추가 → `AddEntry`/`UpdateEntry`에 Kind 전달. (c) `ProjectHistoryDialog.xaml.cs` 동일. `UpdateEntry` 시그니처에 kind 파라미터 추가(베이스, 양 VM·호출부 함께 갱신).
- **Design**: 배치 = Presentation(ViewModels/Views.Dialogs). 신규 심볼 = `HistoryEntryDialogViewModel.SelectedKind`/`AvailableKinds`. 의존 = AppSettings 유형·HistoryEntry.Kind(T1)·베이스 AddEntry/UpdateEntry(T3). 참조하는 곳 = 두 다이얼로그 코드비하인드. 비추상화 = 유형 선택은 표준 ComboBox(신규 컨트롤 없음), 인라인 폼·중첩 편집 다이얼로그는 기존 패턴에 필드 추가만.
- **Files**: `Presentation/ViewModels/HistoryEntryDialogViewModel.cs`, `Presentation/ViewModels/HistoryDialogViewModelBase.cs`(UpdateEntry kind 파라미터), `Presentation/Views/Dialogs/HistoryDialog.xaml`(+`.cs`), `Presentation/Views/Dialogs/ProjectHistoryDialog.xaml`(+`.cs`), `Strings/ko-KR/Resources.resw`, `Strings/en-US/Resources.resw`
- **Edge**: 유형 미선택 → 빈(미분류) 허용. 편집 시 기존 Kind가 목록에 없으면(삭제된 유형) 그 값 표시 유지 or 미분류. 빈 제목 → 기존 검증 유지.
- **Halt Forecast**: 없음 — 유형 ComboBox·필드 추가와 UpdateEntry kind 파라미터 전파(베이스+두 VM+양 코드비하인드, 같은 task Files)뿐, 파괴적·외부 요소 없음.
- **Acceptance**: 빌드 0. 새 기록/편집 폼에 유형 ComboBox 표시, 저장 시 HistoryEntry.Kind 반영. depends T3, T1.

### T5 — 페이지네이션 UI + 유형 배지 + restyle (Type D)
- **내용**: `HistoryDialog.xaml`·`ProjectHistoryDialog.xaml`: (a) **유형 배지**: 항목 DataTemplate에 Kind 배지(TagColorConverter 색 dot/pill + Kind 텍스트, `KindVisibility`로 빈이면 숨김). (b) **페이지 컨트롤**: 하단에 이전/다음 버튼 + `PageInfoText`("N / M") + (선택)페이지 번호. `PrevPage`/`NextPage` 커맨드 바인딩. (c) restyle: Phase 0 팔레트 정합 확인, 검색·날짜그룹·펼침·내보내기·인라인폼은 기존 유지. 로컬라이즈(x:Uid/.resw ko·en) 신규 문자열(유형 라벨·페이지 컨트롤).
- **Design**: 배치 = Presentation/Views/Dialogs(두 XAML + 필요 시 코드비하인드). 신규 심볼 = 유형 배지 요소·페이지 컨트롤 요소(XAML). 의존 = 베이스 VM 페이지네이션/유형(T3)·TagColorConverter·HistoryEntryViewModel.Kind. 비추상화 = 페이지 컨트롤은 표준 Button+TextBlock(커스텀 페이저 컨트롤 미신설), 배지는 작업 카드 배지 패턴 재사용(신규 컨버터 없음).
- **Files**: `Presentation/Views/Dialogs/HistoryDialog.xaml`, `Presentation/Views/Dialogs/ProjectHistoryDialog.xaml`, `Strings/ko-KR/Resources.resw`, `Strings/en-US/Resources.resw`, (필요 시) 각 `.xaml.cs`
- **Edge**: 1페이지뿐 → 이전/다음 비활성(또는 컨트롤 숨김). 빈 기록 → 페이지 컨트롤 숨김·빈 상태 표시. 긴 유형명 → 배지 트리밍. 유형 없는 기존 항목 → 배지 미표시.
- **Halt Forecast**: 없음 — XAML 배지/페이지 컨트롤 추가·기존 바인딩 확장뿐, 파괴적·외부 요소 없음.
- **Acceptance**: 빌드 0. 두 다이얼로그에 유형 배지·페이지 컨트롤(이전/다음+번호) 표시, 페이지 이동 동작. **시각 렌더는 사용자 확인 필요.** depends T3, T4.

## PRD Coverage
| PRD ID | 우선순위 | 대응 task | 상태 |
|--------|---------|----------|------|
| FR-H1 (유형 필드·설정 선택·영속화) | Must | T1, T2, T4 | ✅ 커버 |
| FR-H2 (페이지네이션·PageSize 소비) | Must | T3(로직), T5(UI) | ✅ 커버 |
| FR-H3 (restyle·검색·유형 배지·날짜 그룹·펼침·내보내기·인라인 폼) | Must | T4(유형 선택), T5(유형 배지·restyle) | ✅ 커버(검색/그룹/펼침/내보내기/인라인폼 기존 유지) |
| FR-H4 (HistoryDialog·ProjectHistoryDialog 중복 정리) | Should | T3(공통 베이스 추출) | ✅ 커버(D-5 공통 컴포넌트화) |
| FR-C*·S*·T*·E* | — | (Phase 0~3 완료) | ✅ 이전 Phase 기구현 |
| FR-N* | — | (Phase 5) | ⏭️ 다음 Phase |

## 4-D 재사용 확인
| 신규 심볼 | 유사 기존 구현 검색 결과 | 재사용/신규 사유 |
|---|---|---|
| HistoryDialogViewModelBase | HistoryDialogViewModel·ProjectHistoryDialogViewModel(거의 동일 로직) | **공통 추출**(D-5, 중복 제거 — 두 VM 상속) |
| 유형 배지 색 | TagColorConverter(작업 카테고리 dot·테스트 스위트) | **재사용**(이름→결정론적 색) |
| 유형 소스 | AppSettingsDialogViewModel.DefaultHistoryKinds + settings.HistoryKinds | **재사용**(cross-VM 정적 참조, Phase 2/3 선례) |
| 페이지네이션 | (없음) | **신규**(날짜 그룹 위 페이지 슬라이스, 표준 Button 페이저) |
| HistoryEntryViewModel/HistoryDateGroup | 기존(HistoryDialogViewModel.cs 정의, 양 VM 공유) | **재사용**(Kind 노출만 추가) |
| 유형 선택 컨트롤 | 기존 ComboBox 패턴(작업/테스트 다이얼로그) | **재사용**(표준 ComboBox) |

## Decisions
- **D1 유형 표현**: HistoryEntry.Kind = string(빈=미분류). enum·값 객체 아님. Source: 작업 Category·테스트 Method 문자열 관례 + 사용자 정의 유형 동적.
- **D2 유형 소스**: `AppSettingsDialogViewModel.DefaultHistoryKinds`(기능/수정/리팩터링/문서) + `settings.HistoryKinds` 정적 참조. Source: Phase 2/3 cross-VM 정적 참조 선례.
- **D3 FR-H4 정리 방식(D-5)**: 공통 추상 `HistoryDialogViewModelBase` 추출, 두 VM 상속(다이얼로그는 유지). Source: 사용자 결정(2026-07-19) + PRD D-5 기본 제안.
- **D4 페이지네이션 UX**: 페이지 버튼(이전/다음+번호), PageSize 단위 슬라이스 후 날짜 그룹핑. Source: 사용자 결정(2026-07-19).
- **D5 유형 배지 색**: TagColorConverter(이름→결정론적 색) 재사용. Source: 작업 카테고리 dot 관례.
- **D6 restyle 범위**: 신규(유형 배지·페이지 컨트롤)만 추가, 검색·날짜그룹·펼침·내보내기·인라인폼은 기존 유지(이미 FR-H3 상당수 구현). Source: HistoryDialog 현행 코드.
- **D7 브랜치**: Phase 3를 master 병합(별도 승인) 후 Phase 4 새 브랜치. Source: 사용자 결정(2026-07-19).

## Open Questions
- [x] FR-H4 중복 정리 방식(D-5) → **공통 베이스 VM 추출**. (D3)
- [x] FR-H2 페이지네이션 UX → **페이지 버튼(이전/다음+번호)**. (D4)
- [x] 브랜치 전략 → **Phase 3 master 병합 후 Phase 4 새 브랜치**. (D7)
- [x] 유형 배지 색·마이그레이션 기본값 → **TagColorConverter 재사용·빈(미분류)**(자체 확정). (D1/D5)

## 사전 승인 항목 (일괄 승인 대상)
- **DB 스키마 비파괴 확장**(T2): Histories에 Kind 컬럼 추가(AddColumnIfNotExists+화이트리스트) + Insert/Get/Read 매핑 확장.
- **도메인 확장**(T1): HistoryEntry에 Kind 필드 추가(비파괴).
- **VM 구조 리팩터링**(T3): `HistoryDialogViewModelBase` 추상 클래스 추출 + 두 VM 상속 전환(기존 public 멤버 시그니처 유지 — 내부 구조 변경). `UpdateEntry`에 kind 파라미터 추가(T4, 양 VM·호출부 함께 갱신).
- .resw 신규 문자열 추가(ko/en) — 유형 라벨·페이지 컨트롤.

## 불가피한 Halt (위임 불가)
- **Phase 3 master 병합**(구현 착수 전제): push/병합은 자율 루프 권한 밖 — 별도 승인·실행. plan 승인과 별개.
- commit/push는 구현·검증 후 별도 승인.

## Deferred / Follow-up
- README/스크린샷 갱신(Phase 4 시각 확인 후).
- Todo*/Test* 구 resw 고아·TestDateGroup 정리(Phase 2/3 이월, 대장 등재) — 별도 세션.

## Out of Scope (이 Phase)
- 알림(Phase 5).
- git 커밋 ↔ 작업 기록 자동 연동(PRD §7 영구 제외).
- 작업 기록의 페이지 전환(PRD §1 — 작업 기록은 다이얼로그 유지, 페이지 아님).

## Progress Log
- T1-T2 완료 (커밋 fe41fa3, d0774ad): T1 HistoryEntry.Kind 필드(POCO). T2 DB Kind 컬럼 마이그레이션(AddColumnIfNotExists+화이트리스트+CREATE)·Repository InsertHistories(@kind 루프 갱신)·GetHistories/ReadHistoriesForProject Kind read(HasColumn 가드). 빌드 OK. Phase 3 T2 @method 루프 누락 회귀 없음 확인.
  - 착수 전 Phase 3를 master `--no-ff` 병합(사용자 승인), task/phase4-history 브랜치(master 기준)에서 진행.

## Next Steps
- 권장 다음 액션: implement-task로 T3부터 계속(T3 베이스 VM 추출·페이지네이션 → T4 유형 선택 → T5 UI). 완료 후 시각·동작 사용자 확인 → 이후 Phase 5(알림) plan-feature.
- Suggested skills: pjc:implement-task(승인 후), pjc:plan-feature(Phase 5).

## 통과 체크리스트
- [x] 요구 이해 작성(원문 인용 + 이해 5줄)
- [x] Impact Analysis 4-A(HistoryEntry/Histories read 2경로·write 1경로·두 VM 중복·진입점 전수)·4-B(직렬화: Kind 마이그레이션 task 분리)·4-C(테스트 없음 — 테스트 프로젝트 부재)·4-D(재사용표 작성)
- [x] 각 task acceptance 검증 가능(빌드 + 시각/동작 구분), 상호 모순 0
- [x] Type 분류(T1 C·T2 D·T3 D·T4 C·T5 D), Design 필드(Type D 전부 + 신규 심볼 Type C)
- [x] Edge/Halt Forecast 각 task 명시
- [x] Open Questions 전부 해결, 결정 분기 0
- [x] 분할 권고(5 task ≤ 8) — 미해당
