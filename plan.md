# plan.md — Phase 3: 테스트 — 전체 페이지 전환 + 상태 모델 전환(통과/실패/미실행) + 스위트/통계/배지 restyle

**PRD**: docs/prd.md (Phase 3 = FR-E1, FR-E2, FR-E3, FR-E4, FR-E5 + 결정 D-1/D-2/D-3)
**전체 목표**: DevDashboard 5개 영역 리디자인(§PRD). 이 plan은 **Phase 3(테스트)** 만 다룬다.
**이전 plan**: Phase 2(작업) — 완료·커밋(`Phase G 통과`). notes 보존.
**다음 plan**: Phase 4(작업 기록) — 별도 승인·실행.

## 요구 이해

> 원문(PRD §4 테스트 Phase 3 발췌): "FR-E1 테스트를 전체 페이지로 전환하고, 상태별 통계 카드 + 상태 필터 탭을 제공한다. FR-E2 테스트 상태 모델을 디자인 기준(통과/실패/미실행)으로 정렬한다(현재 Testing/Fix/Done에서 전환, 마이그레이션 포함). FR-E3 테스트 등록/편집 다이얼로그(이름/스위트/방법), 메모 다이얼로그, 삭제 확인 다이얼로그. FR-E4 테스트↔작업 연결(연결 배지·링크), 작업 카드 배지 배선(FR-T8). FR-E5 테스트 목록 restyle(스위트 그룹, 통과율 바, 상태 아이콘/배지, 에러/메모 표시)." + 결정: D-1(상태 전환 Done→통과·Fix→실패·Testing→미실행), D-2(현재 프로젝트 종속 + 교차 집계는 별도), D-3(per-project TestCategory=스위트 유지). + 사용자 스코핑(2026-07-19): "방법"=TestItem 신규 필드 추가, 교차 집계=Deferred(프로젝트별만), FR-E4=작업 카드 배지 매핑만 갱신(테스트→작업 역방향 Deferred).

이해한 요구(Phase 3):
- 테스트를 현재 다이얼로그(`TestListDialog`)에서 **프로젝트별 전체 페이지**로 전환하고, **상태별 통계 카드 + 상태 필터 탭**(통과/실패/미실행/전체)을 제공한다.
- 테스트 상태 모델을 **Testing/Fix/Done → 통과(Pass)/실패(Fail)/미실행(Untested)** 로 전환하고, 기존 데이터를 DB UPDATE로 하위호환 마이그레이션한다(Done→Pass, Fix→Fail, Testing→Untested).
- `TestItem`에 **방법(Method)** 필드를 추가하고, **등록/편집 다이얼로그(이름/스위트/방법)·메모 다이얼로그·삭제 확인**을 제공한다.
- 목록을 **스위트(카테고리) 그룹 + 통과율 바 + 상태 아이콘/색/배지 + 메모** 로 restyle한다.
- 작업 카드의 연결 테스트 **배지 매핑을 새 상태 모델로 갱신**한다(Phase 2 `MapTestBadge` — 코드에 갱신 예정 주석 존재). 테스트→작업 역방향 링크는 이번 제외.

## Goal
Phase 2에서 구축한 페이지 네비게이션(`MainWindow.ShowPage`/`ShowDashboard`, 콘텐츠 스왑 idiom)을 재사용해 프로젝트 카드의 "테스트" 버튼이 다이얼로그 대신 **테스트 전체 페이지**를 연다. `TestItem`의 상태 상수 3값을 통과/실패/미실행으로 전환하고(값 자체 교체 + SQLite UPDATE 마이그레이션), `Method` 필드를 추가한다. `TestListDialogViewModel`의 필터/CRUD 로직을 신규 `TestPageViewModel`(스위트 그룹·상태 통계·통과율·증분 저장)로 이전하고, 구 `TestListDialog`/VM을 정리한다. 작업 카드 배지(`MapTestBadge`)와 연결 테스트 생성(`CreateLinkedTest`) 기본 상태를 새 모델에 맞춘다.

## Investigation Log (근거)

### 위키·시안
- 위키 참조: vault 미설정 — 코드 1차 출처로 진행.
- 리디자인 시안: 목업 HTML(claude.ai/design)이 레포에 없음(Phase 0~2와 동일). 시각 명세는 PRD §3 추출값(테스트 상태 3색·아이콘) + WinUI idiom 기준, 빌드 후 사용자 확인.
- Deferred 대장(docs/plans/deferred.md) 확인: Phase 2에서 이관된 "테스트 배지 Phase 3 모델 매핑"을 T7로 재수용. 그 외 항목(FR-S5·교차 집계·담당자 등)은 이번 무관.

### 도메인·상태 모델 (직접 Read)
- **TestItem**(Domain/Entities/TestItem.cs): Id/CategoryId/Text/`ProgressNote`(메모)/`Status`/CompletedAt/CreatedAt. 상태 상수 `StatusTesting="Testing"`·`StatusFix="Fix"`·`StatusDone="Done"`(11-18), 기본값 `StatusTesting`(36), `IsCompleted => Status == StatusDone`(39). **"방법"·연결 필드 없음**.
- **TestCategory**(TestCategory.cs): Id/Name(스위트명)/Items(List<TestItem>)/CreatedAt. per-project(D-3 유지).
- **ProjectItem**(ProjectItem.cs): `TestCategories`(70), `HasActiveTest`(73, 미완료 테스트 존재 = 카드 진행중 표시).

### 상태 리터럴 전수 조사 (grep — T1/T2 영향 범위)
- **상수 정의**: TestItem.cs:11-18(3상수).
- **하드코딩 SQL(값 직접 참조 — 전환 시 갱신 필수)**:
  - SqliteProjectRepository.cs:64 `SELECT DISTINCT ProjectId FROM TestItems WHERE Status != 'Done'`(ReadActiveTestProjectIds → HasActiveTest 산출). **`'Done'`→`'Pass'`로 갱신**.
  - DatabaseContext.cs:118 `ALTER ... DEFAULT 'Testing'`(구 컬럼 추가), :135 `UPDATE ... SET Status='Done' WHERE IsCompleted=1 AND Status='Testing'`(구 IsCompleted→Status), :281 `CREATE TABLE ... Status ... DEFAULT 'Testing'`.
- **상수 참조(C#)**: SqliteProjectRepository.cs:640(Insert 파라미터 초기값 `StatusTesting`), :936(구버전 fallback `isCompleted ? StatusDone : StatusTesting`); TaskPageViewModel.cs:99-101(MapTestBadge — T7); **구 자산**(T6 제거) TestListDialogViewModel.cs:53-55/160/162, TestListDialog.xaml.cs:86-136/255-258.
- **resw 라벨**: Resources.resw ko/en:374-376(`TestStatusTesting/Fix/Done.Content` = 테스트/수정/완료·Testing/Fix/Done), TestListDialog.xaml:23-25/68-70(탭·콤보 x:Uid — T6 제거).

### 영속성 (직접 Read)
- **DatabaseContext**: `TestItems`(Id/CategoryId/ProjectId/Text/ProgressNote/IsCompleted/Status/CompletedAt/CreatedAt, 274-285)·`TestCategories`(266-272) 테이블. 마이그레이션 = `MigrateSchema`(114-129) `AddColumnIfNotExists` + `AllowedIdentifiers` 화이트리스트(148-158) 검증. **값 마이그레이션 선례 존재**: `MigrateIsCompletedToStatus`(132-137, SQL UPDATE) — Phase 3 값 전환도 동일 패턴. **`Method`는 화이트리스트 미등록 → T2에서 등록 필수**.
- **SqliteProjectRepository**: 테스트 read `ReadTestCategoriesForProject`(~918-, Status/IsCompleted 하위호환 936), write `SaveTestCategories`(226-, InsertTestItems 628- delete+reinsert). 지연 로딩(GetTestCategories 74-).
- **저장 플로우(현행)**: 다이얼로그 close → `TestListDialogViewModel.SaveToModel` → `ProjectCardViewModel.OnTestListDialogClosed`(TestChanged) → `MainViewModel.OnTestChanged`(352-, `SaveTestCategories` 백그라운드). **페이지 전환 시 증분 저장(변경마다 저장)으로 재설계** — Phase 2 `TaskPageViewModel` FIFO 저장 체인(QueueSave/`_saveChain.ContinueWith`) 패턴 재사용.

### UI·진입점·페이지 패턴 (직접 Read)
- **진입점(유일)**: 카드 `OpenTestList` → `OpenTestListRequested` 이벤트 → `DashboardView.xaml.cs OnOpenTestListRequested`(211-226) `new TestListDialog(...).ShowAsync()` 후 `OnTestListDialogClosed`. Phase 2 작업 버튼은 이미 `(App.MainWindow as MainWindow)?.ShowPage(new TaskPage(...))`로 전환됨(178-192) — **동일 패턴 적용**.
- **네비게이션(재사용)**: `MainWindow.ShowPage(UIElement page)`(509-513, DashboardContent.Content=page + GroupTabsBar 숨김)·`ShowDashboard()`(516-521). 언어 변경 시 대시보드 복귀(503-505). **파라미터가 UIElement라 TestPage 그대로 수용**.
- **현재 UI**: `TestListDialog`(ContentDialog) — 스위트 카드 + 탭(Testing/Fix/Done/All) RadioButton + 상태 ComboBox + 중첩 다이얼로그(편집/삭제/메모, `ShowNestedDialogAsync`). VM `TestListDialogViewModel`(필터 `RefreshFilter` 45-71·CRUD·`ChangeTestStatus`).
- **Phase 2 페이지 패턴(모방 대상)**: `TaskPageViewModel`(FIFO 저장 체인 22-25·상태별 ObservableCollection·필터 `Rebuild`·증분 저장), `TaskPage.xaml`(헤더+뷰전환+필터+카드 DataTemplate, `((MainWindow)App.MainWindow!).ShowDashboard()` 뒤로가기), 진입점 `ProjectCardViewModel.CreateTaskPageViewModel()` 팩토리.

### 작업↔테스트 연결 (직접 Read — T7)
- **작업 카드 배지(Phase 2 기구현)**: `TaskPageViewModel.MapTestBadge`(97-103, `StatusDone→통과·StatusFix→실패·StatusTesting→미실행`, resw `TaskTestBadge_Pass/Fail/Untested`). **주석에 "Phase 3에서 통과/실패/미실행 전환 시 매핑 갱신 예정" 명시**. `BuildTestStatusLookup`(80-86)·`GetLinkedTestStatus`.
- **연결 테스트 생성**: `CreateLinkedTest`(183-207) — "작업" TestCategory 자동생성/재사용 + `new TestItem`(기본 Status=`StatusTesting`) + LinkedTestId. **기본 상태를 새 모델 "미실행"으로 자동 승계**(TestItem 기본값 전환 시 자동 반영, 명시 확인).

## 상태 전환 설계 (D-1 확정 — 자체 결정)
- **방식**: TestItem.Status 문자열 **값 자체 교체**(Phase 2 TodoStatus는 enum 이름 유지였으나, TestItem.Status는 이미 string 상수라 값 교체가 자연 — DB에 문자열 저장). 새 상수: `StatusPass="Pass"`·`StatusFail="Fail"`·`StatusUntested="Untested"`. 기본값 = `StatusUntested`(신규 테스트는 미실행).
- **매핑(D-1)**: Done→Pass, Fix→Fail, Testing→Untested.
- **마이그레이션 순서(T2, 멱등)**: 기존 `AddColumnIfNotExists(...DEFAULT 'Testing')`·`MigrateIsCompletedToStatus`(구 IsCompleted→Testing/Done 정규화)를 **그대로 둔 뒤**, 신규 `MigrateTestStatusToNewModel`(UPDATE: Testing→Untested·Fix→Fail·Done→Pass)를 마지막에 추가. 이미 전환된 DB엔 구 값이 없어 UPDATE 무효과(멱등). `CREATE TABLE ... DEFAULT 'Untested'`(신규 DB는 처음부터 새 값)·`ReadActiveTestProjectIds`의 `!= 'Done'`→`!= 'Pass'` 갱신.
- Source: PRD FR-E2("마이그레이션 포함")·D-1(매핑 명시) + DatabaseContext.cs:132-137 값 마이그레이션 선례.

## 시각 요소 분해 (테스트 전체 페이지 — 출처 PRD §3, 목업 부재로 픽셀값 사용자 확인)

| 요소 | 속성 | 디자인 값 | 확인 방법 |
|------|------|----------|-----------|
| 상태 필터 탭 | 개수·라벨 | 4탭: 통과 / 실패 / 미실행 / 전체 | PRD §3 "테스트 상태(3)" + 전체 |
| 통계 카드 | 구성 | 상태별 개수 카드(통과/실패/미실행) | PRD §4 FR-E1 "상태별 통계 카드" |
| 상태 아이콘·색 | 통과 | #5aa3e8 ✓ | PRD §3 |
| 상태 아이콘·색 | 실패 | #e8b45a ✕ | PRD §3 |
| 상태 아이콘·색 | 미실행 | #8a8890 ○ | PRD §3 |
| 스위트 그룹 | 레이아웃 | 스위트(카테고리)별 그룹 헤더 + 항목 | PRD §4 FR-E5 "스위트 그룹" + D-3 |
| 통과율 바 | 표시 | 스위트별 통과율 진행 바 | PRD §4 FR-E5 "통과율 바" |
| 테스트 항목 | 구성 | 이름·상태 배지/아이콘·메모(에러) | PRD §4 FR-E5 |
| 항목 액션 | 컨트롤 | 상태 변경·수정·삭제·메모 | PRD §4 FR-E3 |
| 새 테스트 | 컨트롤 | 등록 다이얼로그(이름/스위트/방법) | PRD §4 FR-E3 |
| 뒤로가기 | 컨트롤 | "← 대시보드" 복귀 | PRD §4 FR-C3(Phase 2 네비 재사용) |
| 팔레트·폰트 | — | 배경 #131316·카드 #1c1c20·강조 코랄 #f0716a·본문 #e8e6e3, Malgun Gothic | PRD §3(Phase 0 전역 적용 완료) |
> 팔레트·폰트·페이지 네비·pill 스타일은 Phase 0~2로 확정. 세부 레이아웃(간격·정렬)은 목업 부재 → WinUI idiom 구현 후 사용자 확인.

## Tasks

### 진행 체크리스트
> 실행 순서 = 이 목록 순서(위상정렬 반영). **T7이 구 상수를 새 상수로 이전한 뒤 T6이 구 상수·구 VM을 삭제**해야 T6 빌드가 성립(B1 반영 — T7을 T6 앞에 배치).
- [x] T1 — 테스트 상태 모델 전환 + Method 필드(도메인)
- [x] T2 — 테스트 영속화(값 마이그레이션 + Method 컬럼)
- [x] T3 — TestPageViewModel
- [x] T5 — 테스트 등록/편집·메모 다이얼로그 (T4보다 먼저 구현)
- [x] T4 — 테스트 전체 페이지 View(스위트/통계/통과율 restyle)
- [x] T7 — 작업 카드 배지 매핑 갱신 + 연결 테스트 기본상태 (T6보다 먼저 — 구 상수 참조 이전)
- [x] T6 — 진입점 재배선 + 구 다이얼로그·구 상수 정리

### T1 — 테스트 상태 모델 전환 + Method 필드 (Type C)
- **내용**: `Domain/Entities/TestItem.cs`(비파괴 추가 — Phase 2 `TodoStatus.Hold` 방식): (a) 새 상수 3개 **추가** — `StatusPass="Pass"`·`StatusFail="Fail"`·`StatusUntested="Untested"`. 구 상수 `StatusTesting/StatusFix/StatusDone`는 **유지**(T6에서 구 VM·Dialog 제거와 함께 삭제 — 그래야 T1 직후 빌드 유지). (b) `Status` 기본값 = `StatusUntested`. (c) `IsCompleted => Status == StatusPass`. (d) 신규 `[ObservableProperty] string Method`(빈=미지정, 테스트 방법). XML 주석 갱신(상태 라벨·설명).
- **Design**: 배치 = Domain(TestItem.cs 단일). 신규 심볼 = 상수 3개(Pass/Fail/Untested — 상태값)·`Method` 프로퍼티(테스트 방법 기재). 의존 = 없음(엔티티 자체). 참조하는 곳 = T2 영속화·T3 VM·T5 다이얼로그·T7 배지. 비추상화 = 상태를 enum으로 승격하지 않고 **기존 string 상수 관례 유지**(TodoStatus enum과 달리 TestItem은 처음부터 string — 일관성·마이그레이션 단순), Method는 값 객체 아닌 단순 string. 구 상수 즉시 삭제 대신 T6까지 병존(비파괴 — 참조부가 T2/T6/T7에 분산돼 T1 단독 삭제 시 빌드 깨짐).
- **Files**: `Domain/Entities/TestItem.cs`
- **Edge**: 빈 Method 기본값. 구 상수 병존 기간(T1~T5): 구 상수를 쓰는 코드(Repository fallback·MapTestBadge·구 VM)는 구 값 반환하나 **빌드는 통과**(참조 유효) — 각 task가 새 상수로 이전하고 T6에서 구 상수+구 VM 삭제해 최종 정합(중간 논리 불일치는 앱 미실행 빌드 검증엔 무영향). 값만 바꾸고 이름 유지(StatusDone="Pass") 대안은 이름-의미 불일치 → 새 상수 추가 채택.
- **Halt Forecast**: 없음(비파괴 상수·필드 추가).
- **Acceptance**: 빌드 0. TestItem에 Pass/Fail/Untested 상수 3개 추가·구 상수 유지·`Method` 프로퍼티 존재, 기본값 Untested, IsCompleted=Pass 기준.

### T2 — 테스트 영속화: 값 마이그레이션 + Method 컬럼 (Type D)
- **내용**: (a) `DatabaseContext`: `AllowedIdentifiers`에 `Method` 추가. `MigrateSchema`에 `AddColumnIfNotExists(conn,"TestItems","Method","TEXT NOT NULL DEFAULT ''")` + 신규 `MigrateTestStatusToNewModel(conn)`(UPDATE TestItems SET Status = CASE Status WHEN 'Done' THEN 'Pass' WHEN 'Fix' THEN 'Fail' WHEN 'Testing' THEN 'Untested' ELSE Status END — 구 값만 전환, 멱등). `CREATE TABLE TestItems`의 Status `DEFAULT 'Testing'`→`DEFAULT 'Untested'` + `Method TEXT NOT NULL DEFAULT ''` 컬럼 추가. 구 `AddColumnIfNotExists(...Status...DEFAULT 'Testing')`·`MigrateIsCompletedToStatus`는 유지(구 DB 정규화 후 새 UPDATE가 전환). (b) `SqliteProjectRepository`: `InsertTestItems`에 Method write + 파라미터 초기값 `StatusTesting`→`StatusUntested`(640) 갱신, `ReadTestCategoriesForProject`에 Method read(`HasColumn` 가드) + 구버전 fallback(936) `isCompleted ? StatusPass : StatusUntested` 갱신, `ReadActiveTestProjectIds`(64) `!= 'Done'`→`!= 'Pass'`.
- **Design**: 해당 없음 — 기존 마이그레이션·직렬화 경로(AddColumnIfNotExists/MigrateSchema/Insert/Read)에 컬럼·UPDATE·값 갱신만, 신규 공개 심볼 없음(`MigrateTestStatusToNewModel`은 private static, 기존 Migrate* 형제 패턴).
- **Files**: `Infrastructure/Persistence/DatabaseContext.cs`, `Infrastructure/Persistence/SqliteProjectRepository.cs`
- **Edge**: 구버전 DB(구 값 Testing/Fix/Done) → UPDATE로 전환. 이미 전환된 DB → 구 값 없어 무효과(멱등). Method 없는 구 DB → `AddColumnIfNotExists` 추가·read `HasColumn` false 시 빈값. Status 컬럼 자체 없는 초구버전 → 기존 IsCompletedToStatus로 Testing/Done 부여 후 새 UPDATE가 전환. 화이트리스트 미등록 Method → T2 내 등록으로 ArgumentException 예방.
- **Halt Forecast**: 없음(NFR-3 비파괴 마이그레이션). 파일 삭제·구조 파괴 없음.
- **Acceptance**: 빌드 0. TestItems에 Method 컬럼·상태 값 전환 마이그레이션 존재(코드 대조: CASE UPDATE + DEFAULT 'Untested' + ReadActive `!= 'Pass'`). 신규 항목 저장→로드 왕복 시 Status(Pass/Fail/Untested)·Method 보존. 구버전 DB 로드 시 예외 없음. depends T1.

### T3 — TestPageViewModel (Type D)
- **내용**: 신규 `Presentation/ViewModels/TestPageViewModel.cs`(프로젝트 1개 대상). 구성:
  - 생성: `TestPageViewModel(ProjectItem project, IProjectRepository repo, Action refreshCardState)`.
  - 스위트 그룹: `ObservableCollection<TestCategory>` 표시용(현재 필터 반영) — 기존 `RefreshFilter`(카테고리별 필터 후 표시용 복제) 로직 이전. 스위트별 **통과율**(통과 수 / 전체 수) 계산 노출(신규 표시용 그룹 모델 또는 프로퍼티).
  - 상태 필터: `SelectedStatus`(Pass/Fail/Untested/전체=null) → 표시 재구성. 상태별 카운트(`PassCount`/`FailCount`/`UntestedCount`/`TotalCount`) — 통계 카드용.
  - 변경 커맨드: 스위트 추가/삭제/이름수정, 테스트 추가/편집/삭제/상태변경/메모수정 — 각 변경 시 **증분 저장**(`repo.SaveTestCategories(project.Id, 전체 categories)`, Phase 2 `TaskPageViewModel`의 FIFO 저장 체인 `QueueSave`/`_saveChain.ContinueWith`+스냅샷+try/catch 패턴 재사용) + `refreshCardState()`(HasActiveTest 갱신).
  - 상태 변경: `ChangeTestStatus(test,newStatus)` — Pass 전환 시 CompletedAt=now, Pass 해제 시 null(기존 로직 이전, StatusDone→StatusPass 기준).
- **Design**: 배치 = Presentation/ViewModels. 신규 심볼 = `TestPageViewModel`(테스트 페이지 상태·필터·통계·저장 오케스트레이션) + 통과율/통계 노출. 의존 = ProjectItem·IProjectRepository. 참조하는 곳 = T4 View·T5 다이얼로그(호출)·T6 팩토리. 비추상화 = 기존 `TestListDialogViewModel` 필터/CRUD를 **이전**(공통 베이스 없이 페이지 맥락 재작성 — 구 VM은 T6 제거로 중복 아님), 스위트 표시는 기존 "필터 결과 복제 TestCategory" 관례 유지(신규 그룹 엔티티 최소화 — 통과율만 필요 시 소형 그룹 모델).
- **Files**: `Presentation/ViewModels/TestPageViewModel.cs`(신규), `Presentation/ViewModels/ProjectCardViewModel.cs`(`CreateTestPageViewModel()` 팩토리 + HasActiveTest 갱신 콜백)
- **Edge**: 빈 프로젝트(스위트 0) → 빈 상태. 통과율 분모 0(빈 스위트) → 0% 또는 N/A(0 나눗셈 가드). 상태 필터 "전체" → 전부. 미실행 탭에서만 스위트 추가/항목 추가 가능(기존 관례) 또는 항상 가능(재검토 — 페이지는 모달 아니므로 항상 허용이 단순, T3에서 항상 허용 채택).
- **Halt Forecast**: 없음(기존 로직 이전 + repository 재사용).
- **Acceptance**: 빌드 0. VM에 스위트 그룹·상태 필터·상태별 카운트·통과율·CRUD·증분 저장·`ChangeTestStatus`(Pass 기준) 존재. 상태 변경 시 SaveTestCategories 호출(코드 대조). depends T1, T2.

### T5 — 테스트 등록/편집 다이얼로그 (Type C) — T4보다 먼저 구현
- **내용**: 신규 `Presentation/Views/Dialogs/TestEditDialog.xaml`(+`.cs`) ContentDialog + `Presentation/ViewModels/TestEditDialogViewModel.cs`. 필드: 이름(Text)·스위트(ComboBox: 기존 카테고리 선택 또는 신규 입력)·방법(Method, TextBox). 새 테스트·편집 겸용(생성자 편집 대상 유무). 저장 검증(이름·스위트 필수). 결과는 `ResultTest`+`ResultSuiteName`, `TestPageViewModel.AddTestToSuite`가 스위트명으로 기존 매칭/생성. **범위 경계**: 등록/편집 다이얼로그만 T5 책임이다. **메모 편집 다이얼로그(ProgressNote)·삭제 확인 다이얼로그는 T4 페이지 코드비하인드**에서 소형 ContentDialog(기존 `ShowNoteEditorAsync` 패턴)·`DialogService.ShowConfirmAsync`로 구현하며, `TestPageViewModel.EditProgressNote`/`DeleteTest`(T3 기구현)를 호출한다.
- **Design**: 배치 = Presentation(Dialogs/ViewModels). 신규 심볼 = `TestEditDialog`(ContentDialog)·`TestEditDialogViewModel`(입력·검증·결과 — 이름/스위트/방법). 의존 = TestItem·TestCategory(스위트 목록)·TestPageViewModel(호출부). 참조하는 곳 = T4 "새 테스트"/항목 수정. 비추상화 = 기존 ContentDialog 서브클래스 패턴(Save=PrimaryButtonClick 검증) 준수, 메모/삭제 다이얼로그는 별도 무거운 다이얼로그 신설 없이 T4 페이지에서 기존 소형 다이얼로그 관례 재사용.
- **Files**: `Presentation/Views/Dialogs/TestEditDialog.xaml`(+`.cs`), `Presentation/ViewModels/TestEditDialogViewModel.cs`(신규), `Presentation/ViewModels/TestPageViewModel.cs`(`AddTestToSuite` 연결), `Strings/ko-KR/Resources.resw`, `Strings/en-US/Resources.resw`
- **Edge**: 빈 이름 → 저장 차단(args.Cancel). 스위트 미선택/신규 입력 빈 → 저장 차단(스위트 필수). 편집 취소 → 무변경. 방법 빈 허용(선택적). 편집 모드 → 스위트 이동 미지원(CanEditSuite=false).
- **Halt Forecast**: 없음.
- **Acceptance**: 빌드 0. 새 테스트/편집 다이얼로그가 이름/스위트/방법 표시·저장 시 TestItem 반영(이름·스위트 빈 값 저장 차단). resw 신규 키 ko/en 정합. (메모 편집·삭제 확인 UI는 T4에서 구현·검증.) depends T3, T1.

### T4 — 테스트 전체 페이지 View (스위트/통계/통과율 restyle) (Type D)
- **내용**: 신규 `Presentation/Views/TestPage.xaml`(+`.cs`) UserControl:
  - 헤더: 제목("테스트") + "← 대시보드" 뒤로가기(`((MainWindow)App.MainWindow!).ShowDashboard()`) + 상태 필터 탭(통과/실패/미실행/전체) + "새 테스트" 버튼.
  - 통계 카드: 상태별 개수(통과/실패/미실행) 카드 행(FR-E1).
  - 스위트 그룹 목록: 스위트 헤더(이름 + 통과율 바 + 스위트 액션[이름수정/삭제]) + 항목 목록. 항목 카드 `DataTemplate`: 이름, 상태 아이콘/색(통과 #5aa3e8 ✓·실패 #e8b45a ✕·미실행 #8a8890 ○), 상태 변경 컨트롤, 메모(있으면 표시), 수정/삭제/메모 버튼.
  - **다이얼로그 배선(T5 범위 경계 반영)**: "새 테스트"·항목 "수정" 버튼 → `TestEditDialog`(T5) 호출 후 `AddTestToSuite`(신규) 또는 `UpdateTest`(편집 — 중복 추가 방지 위해 add/edit 경로 분리, S1) 호출. **메모 편집**은 소형 ContentDialog(기존 `ShowNoteEditorAsync` 패턴)로 ProgressNote 편집 → `EditProgressNote` 호출. **삭제 확인**은 `DialogService.ShowConfirmAsync`(또는 소형 ContentDialog) 후 `DeleteTest`/`DeleteSuite` 호출. 스위트 이름수정도 소형 입력 다이얼로그 → `RenameSuite`.
  - 팔레트·폰트 Phase 0 리소스. 로컬라이즈(x:Uid/.resw ko·en) 신규 문자열. x:Bind 정적 함수 바인딩은 **Converter 미적용**(Phase 2 T5 교훈) → Visibility/색은 헬퍼가 직접 타입 반환 또는 컨버터 속성 바인딩.
- **Design**: 배치 = Presentation/Views. 신규 심볼 = `TestPage` UserControl + 테스트 항목 DataTemplate + 상태→아이콘/색 매핑(정적 헬퍼 또는 소형 컨버터). 의존 = TestPageViewModel(T3)·App.MainWindow 네비(Phase 2)·TestEditDialog(T5). 참조하는 곳 = T6 진입점. 비추상화 = 항목 템플릿 인라인(별도 UserControl 미추출), 통과율 바는 표준 ProgressBar 재사용(커스텀 컨트롤 미신설).
- **Files**: `Presentation/Views/TestPage.xaml`, `Presentation/Views/TestPage.xaml.cs`, `Strings/ko-KR/Resources.resw`, `Strings/en-US/Resources.resw`, (상태 색/아이콘 컨버터 필요 시) `Presentation/Converters/*.cs`+`Resources/Converters.xaml`
- **Edge**: 긴 목록 → 스크롤. 빈 스위트/빈 페이지 → 빈 상태 표시. 통과율 0/0 → 0%. 상태 필터로 항목 0인 스위트 → 헤더 표시 여부(기존 관례 참고, 미실행/전체에서 빈 스위트 노출).
- **Halt Forecast**: 없음.
- **Acceptance**: 빌드 0. 테스트 버튼 진입 시 통계 카드·상태 탭·스위트 그룹(통과율 바)·항목(상태 아이콘/색·메모·액션) 표시, 뒤로가기 동작. **메모 편집·삭제 확인 다이얼로그 동작**(EditProgressNote/DeleteTest/DeleteSuite/RenameSuite 호출, add/edit 경로 분리). **시각 렌더는 사용자 확인 필요.** depends T3, T5.

### T6 — 진입점 재배선 + 구 다이얼로그 정리 (Type D, cross-file)
- **내용**: (a) `DashboardView.xaml.cs OnOpenTestListRequested`(211-226): 다이얼로그 대신 `(App.MainWindow as MainWindow)?.ShowPage(new TestPage(card.CreateTestPageViewModel()))` 호출(Phase 2 작업 버튼과 동형). (b) 구 자산 제거: `TestListDialog.xaml`/`.xaml.cs`·`TestListDialogViewModel.cs`(페이지로 대체, 로직 T3 이전 완료). `ProjectCardViewModel`의 `CreateTestListDialogViewModel`/`OnTestListDialogClosed` 구 경로 정리(신 팩토리 `CreateTestPageViewModel`으로 대체, HasActiveTest 갱신은 콜백). `MainViewModel.OnTestChanged`/`TestChanged` 이벤트 체인 정리(증분 저장으로 대체 시 잔재 제거 — 구독/해제/핸들러). (c) **구 상태 상수 제거**: `TestItem`의 `StatusTesting/StatusFix/StatusDone`(T1 병존분) 삭제 — 이 시점엔 참조부(구 VM/Dialog)가 함께 제거되고 Repository/MapTestBadge는 T2/T7에서 새 상수로 이전 완료. 제거 후 구 상수·구 다이얼로그 참조 grep 0 확인.
- **Design**: 배치 = DashboardView/ProjectCardViewModel/MainViewModel. 신규 심볼 없음(재배선+제거). 의존 = Phase 2 네비·T3 팩토리·T4 페이지. 비추상화 = 해당 없음(제거 위주). 제거 고아까지 정리해 잔재 0.
- **Files**: `Presentation/Views/DashboardView.xaml.cs`, `Presentation/ViewModels/ProjectCardViewModel.cs`, `Presentation/ViewModels/MainViewModel.cs`, `Domain/Entities/TestItem.cs`(구 상수 제거), (삭제) `Presentation/Views/Dialogs/TestListDialog.xaml`·`.xaml.cs`·`Presentation/ViewModels/TestListDialogViewModel.cs`
- **Edge**: 구 TestListDialog가 참조하던 resw/스타일 중 다른 사용처 없는 것만 제거(grep 확인, 대량이면 follow-up). `TestChanged` 이벤트가 다른 구독처 없는지 확인 후 제거. `TestDateGroup`(Presentation/Models) 사용처 확인 — 구 다이얼로그 전용이면 정리, 아니면 유지. 구 상수 제거 전 Repository(T2)·MapTestBadge(T7)가 새 상수로 이전됐는지 확인(depends).
- **Halt Forecast**: 파일 삭제 3건 + TestItem 구 상수 제거 → **사전 승인 항목** 등재(일괄 승인). 예상 밖 참조가 grep에서 발견되면 같은 task에서 정리(범위 내).
- **Acceptance**: 빌드 0·신규 경고 0. 테스트 버튼이 페이지를 열고(다이얼로그 아님), `TestListDialog`/`TestListDialogViewModel`·구 상태 상수(`StatusTesting/Fix/Done`) 참조 grep 0. depends T2, T3, T4, T7.

### T7 — 작업 카드 배지 매핑 갱신 + 연결 테스트 기본상태 (Type C)
- **내용**: `TaskPageViewModel`: (a) `MapTestBadge`(97-103)의 switch를 새 상수로 갱신 — `StatusPass→통과·StatusFail→실패·StatusUntested→미실행`(resw `TaskTestBadge_*` 재사용, 라벨 동일). 주석의 "Phase 3 갱신 예정" 제거/갱신. (b) `CreateLinkedTest`(183-207): `new TestItem`이 명시 Status 지정 없이 기본값(=Untested)을 승계하는지 확인 — 명시 지정이 있으면 새 기본과 일치시킴(미실행). (c) `BuildTestStatusLookup`/`GetLinkedTestStatus`는 값 무관(문자열 그대로) — 매핑만 갱신.
- **Design**: 배치 = TaskPageViewModel 단일. 신규 심볼 없음(기존 헬퍼 값 갱신). 의존 = TestItem 상수(T1). 참조하는 곳 = TaskPage 카드 배지(기존). 비추상화 = 배지 텍스트 매핑 유지(현재 resw 라벨 통과/실패/미실행이 이미 새 모델과 일치 — Phase 2에서 선반영), 별도 상태 enum·컨버터 신설 없음.
- **Files**: `Presentation/ViewModels/TaskPageViewModel.cs`
- **Edge**: 연결 테스트 삭제됨 → 배지 미표시(기존 `_ => string.Empty` 유지). 구 값(Testing 등) 잔존 데이터 → T2 마이그레이션으로 이미 전환됨(런타임엔 새 값만). 미실행 기본 승계 확인.
- **Halt Forecast**: 없음.
- **Acceptance**: 빌드 0. `MapTestBadge`가 Pass/Fail/Untested 기준으로 배지 반환. 새 작업 "테스트 추가" 시 생성 테스트 기본 상태 미실행. depends T1, T2.

## PRD Coverage
| PRD ID | 우선순위 | 대응 task | 상태 |
|--------|---------|----------|------|
| FR-E1 (전체 페이지·통계 카드·상태 필터 탭) | Must | T3, T4 | ✅ 커버 |
| FR-E2 (상태 모델 전환·마이그레이션) | Must | T1, T2 | ✅ 커버 |
| FR-E3 (등록/편집·메모·삭제 확인 다이얼로그) | Must | T5 | ✅ 커버(방법=신규 필드) |
| FR-E4 (테스트↔작업 연결·작업 카드 배지 배선) | Should | T7 | ✅ 커버(배지 매핑만·역방향 Deferred) |
| FR-E5 (목록 restyle·스위트 그룹·통과율 바·상태 아이콘/배지·메모) | Should | T3, T4 | ✅ 커버 |
| FR-C1/C2/C3/C4·S*·T* | — | (Phase 0/1/2 완료) | ✅ 이전 Phase 기구현 |
| FR-H*·N* | — | (Phase 4~5) | ⏭️ 다음 Phase |
| D-2 교차 집계 | — | — | ⏭️ Deferred(사용자 결정) |

## 4-D 재사용 확인
| 신규 심볼 | 유사 기존 구현 검색 결과 | 재사용/신규 사유 |
|---|---|---|
| TestPageViewModel 필터/CRUD/저장 | TestListDialogViewModel(RefreshFilter/CRUD) + TaskPageViewModel(FIFO 저장 체인·필터) | **로직 이전 + 저장 패턴 재사용**(구 VM은 T6 제거 — 중복 아님) |
| 페이지 네비 | MainWindow.ShowPage/ShowDashboard(Phase 2) | **재사용**(UIElement 수용, 신규 인프라 불요) |
| 진입점 재배선 | Phase 2 OnOpenTodoRequested→ShowPage(TaskPage) | **패턴 재사용**(동형 릴레이) |
| TestEditDialog | 기존 ContentDialog 서브클래스(Save=PrimaryButtonClick) + Phase 2 TaskEditDialog | **패턴 재사용**, 다이얼로그 자체 신규(테스트 필드) |
| 상태 값 마이그레이션 | DatabaseContext.MigrateIsCompletedToStatus(SQL UPDATE) | **패턴 재사용**(형제 Migrate* 메서드) |
| 통과율 바 | (없음) | **신규**(표준 ProgressBar 재사용, 커스텀 컨트롤 미신설) |
| 배지 매핑 | TaskPageViewModel.MapTestBadge(Phase 2, 갱신 예정 주석) | **값 갱신**(resw 라벨은 이미 새 모델 선반영) |
| 삭제 확인 | DialogService.ShowConfirmAsync | **재사용** |

## Decisions
- **D1 상태 전환 방식**: TestItem.Status 문자열 값 교체(Pass/Fail/Untested), 기본값 Untested. **비파괴 진행 — 새 상수 3개 추가(T1) 후 구 상수는 참조부 이전 완료 시점(T6)에 삭제**(Phase 2 TodoStatus.Hold 비파괴 추가 방식). Source: TestItem.Status가 이미 string 상수(enum 아님) + PRD FR-E2 마이그레이션 + task별 빌드 검증 안전.
- **D2 상태 매핑(D-1 PRD)**: Done→Pass, Fix→Fail, Testing→Untested. Source: PRD D-1.
- **D3 마이그레이션 멱등**: 구 IsCompleted→Status 정규화 유지 + 신규 CASE UPDATE 마지막 추가(구 값만 전환). Source: DatabaseContext.cs:132-137 선례 + 멱등 요구.
- **D4 "방법" 필드**: TestItem에 Method string 신규 + DB 컬럼 마이그레이션. Source: 사용자 결정(2026-07-19).
- **D5 교차 집계(D-2)**: Deferred(프로젝트별만). Source: 사용자 결정(2026-07-19) — Phase 2 작업과 대칭.
- **D6 FR-E4 범위**: 작업 카드 배지 매핑만 갱신, 테스트→작업 역방향 링크 Deferred. Source: 사용자 결정(2026-07-19).
- **D7 저장 시점**: 페이지는 변경마다 증분 SaveTestCategories(FIFO 체인) + HasActiveTest 콜백. Source: Phase 2 TaskPageViewModel 선례(페이지는 모달 close 없음).
- **D8 스위트 유지(D-3)**: per-project TestCategory=스위트 유지, 라벨만 스위트. Source: PRD D-3.
- **D9 상태 색/아이콘**: 통과 #5aa3e8 ✓·실패 #e8b45a ✕·미실행 #8a8890 ○. Source: PRD §3.

## Open Questions
- [x] "방법" 필드 처리 → **TestItem 신규 필드 추가**. (D4)
- [x] 교차 집계 페이지(D-2) → **Deferred(프로젝트별만)**. (D5)
- [x] FR-E4 범위 → **작업 카드 배지 매핑만 갱신**(역방향 Deferred). (D6)
- [x] 상태 전환 저장 방식 → **값 교체 + CASE UPDATE 마이그레이션**(PRD 명시, 자체 확정). (D1/D3)
- [x] 시각 시안 부재 → **PRD §3 추출값(3색/아이콘) + WinUI idiom, 빌드 후 사용자 확인**. (Phase 0~2 선례)

## 사전 승인 항목 (일괄 승인 대상)
- **파일 삭제 3건**(T6, grep 사용처 0 확인 후): `TestListDialog.xaml`·`TestListDialog.xaml.cs`·`TestListDialogViewModel.cs`.
- **DB 스키마 비파괴 확장·값 마이그레이션**(T2): TestItems에 Method 컬럼 추가(AddColumnIfNotExists+화이트리스트) + Status 값 전환 UPDATE(비파괴·멱등) + CREATE TABLE DEFAULT/ReadActive SQL 갱신.
- **도메인 상수 추가·제거**(T1 추가/T6 제거): TestItem 새 상태 상수 3개(Pass/Fail/Untested) 추가 + Method 필드(T1, 비파괴), 구 상수 3개(Testing/Fix/Done)는 참조부 이전 후 제거(T6).
- .resw 신규 문자열 추가(ko/en) — 테스트 페이지·다이얼로그·상태 라벨.

## 불가피한 Halt (위임 불가)
- 없음(Phase 3은 비파괴·로컬 변경). commit/push는 구현·검증 후 별도 승인.

## Deferred / Follow-up
- [SUGGEST] T4 구현 시 테스트 add/edit 경로 분리 — `AddTestToSuite`(신규, 항상 Items.Add)를 편집에 재사용하면 `_existing` 중복 추가. 편집은 `UpdateTest`, 신규만 `AddTestToSuite` 호출로 분기(T5 quality S1). T4 Acceptance에 반영됨.
- [Follow-up] Todo* resw 고아 정리(Phase 2 이월, 대장에도 등재) + 이번 Test* 구 resw 잔존분(T6에서 삭제 못한 대량분: `TestListDialogTitle`·`TestStatusTesting/Fix/Done.Content`·`TestTab*`·`TestGroupBy*`·`NewTestBox`·`NewCategoryBox`·`TestAddNoteLink`·`EmptyTestText`·`TestDeleteCategoryConfirm` 등 TestListDialog 전용) audit — 별도 세션. 빌드·런타임 무해(고아 resw).
- [Follow-up] `Presentation/Models/TestDateGroup.cs` — 소스 미참조 고아(T6 이전부터 존재, 구 테스트 다이얼로그 이전 버전 잔재). T6 범위 밖이라 미제거, 별도 정리.
- 교차(cross-project) 작업/테스트 집계 페이지(PRD D-2) — 대장 등재, 별도 진행.
- 테스트→작업 역방향 링크/배지(FR-E4 확장) — 대장 등재.
- TestItem.Method 필터/통계 활용 — 대장 등재.
- README/스크린샷 갱신(시각 확인 후).

## Out of Scope (이 Phase)
- 작업 기록·알림(Phase 4·5).
- 실제 테스트 러너 연동(자동 실행·성능 측정) — 상태 수동 관리(PRD §7 영구 제외).
- git 커밋 ↔ 작업 기록 연동(PRD §7 영구 제외).

## Progress Log
- T1-T2 완료 (커밋 7349b29, 4aa5167): T1 TestItem 새 상태 상수(Pass/Fail/Untested) 비파괴 추가·구 상수 T6까지 병존·Method 필드·기본값 Untested·IsCompleted=Pass. T2 DB 값 마이그레이션(MigrateTestStatusToNewModel CASE UPDATE 멱등)·Method 컬럼·CREATE DEFAULT 'Untested'·ReadActive `!= 'Pass'`·Repository Method read/write. 빌드 OK.
  - 결정: 상태 전환은 값 교체 방식(Phase 2 TodoStatus.Hold 비파괴 추가와 동형) — 구 상수를 T6에서 참조부 이전 후 삭제해 task별 빌드 유지.
  - 리뷰: T2 quality MAJOR(InsertTestItems `@method` 루프 바인딩 누락 → silent data loss) 수정 후 통과. spec은 처음부터 OK.
- T3·T5 완료 (커밋 d7b4259, 1a9a65e): T3 TestPageViewModel(스위트 그룹·상태 통계·통과율·CRUD·FIFO 저장)·ProjectCardViewModel 팩토리. T5 TestEditDialog(이름/스위트/방법, ComboBox IsEditable)·AddTestToSuite·resw 9키. 빌드 OK.
  - 결정(T5 B1): plan T5 acceptance의 "메모 편집·삭제 확인 동작"은 실제로 T4 페이지 코드비하인드 책임 → plan T5/T4 절 정정으로 책임 명시 이전(코드 무변경). T5=등록/편집 다이얼로그, T4=메모/삭제/이름수정 소형 다이얼로그 + add/edit 경로 분리(S1).
- T4·T7 완료 (커밋 3e97038, 83f5aac): T4 TestPage(헤더·상태 탭·통계 카드·스위트 통과율·항목 카드·인라인 메모/삭제/이름수정 다이얼로그·정적 색/글리프 헬퍼)·resw 8키. T7 MapTestBadge 새 상수(Pass/Fail/Untested)·CreateLinkedTest 기본 Untested 승계. 빌드 OK.
  - T4 리뷰: quality MAJOR(StatusTab_Checked 주석이 코드와 반대) 수정 후 통과. V-9 요소 존재 확인·픽셀 렌더 HUMAN-VERIFY(목업 부재).
  - T7 후 TaskPageViewModel 구 상수 참조 0건 → T6 구 상수 삭제 선행조건 충족.

## Phase Ledger
- 전 task(T1~T7) 완료.
- Phase F 통과 (HEAD 930a260) — 전체 빌드 OK(코드 무변경으로 재빌드 skip, T6 빌드가 최종)·신규 경고 0, 통합 정합·구 자산 잔재 0·마이그레이션 멱등/하위호환 검증 통과(plan-completion-reviewer OK, BLOCKER/MAJOR/MINOR 0).
- Phase G 통과 (Must 100%) — PRD Must 3/3(FR-E1·E2·E3)·Should 2/2(FR-E4·E5) 충족, NFR-1~4 충족. 역방향 링크(FR-E4 확장)·교차 집계(D-2) Deferred(사용자 결정). 미충족 0. 픽셀 렌더·영속 왕복은 HUMAN-VERIFY.

## Next Steps
- 권장 다음 액션: **시각·동작 사용자 확인**(통계 카드·상태 탭·스위트 통과율·상태 아이콘 렌더·영속 왕복[상태 변경→재열기]·구 데이터 마이그레이션) → 이상 없으면 커밋을 master로 병합/push는 별도 승인. 이후 Phase 4(작업 기록) plan-feature.
- 원안 참고(구현 전): 승인 시 implement-task로 전 task 자율 실행(순서 = 진행 체크리스트: T1→T2→T3→T5→T4→T7→T6). 완료 후 **시각·동작 사용자 확인**(통계 카드·상태 탭·스위트 통과율·상태 아이콘·영속 왕복[상태 변경→재열기]·구 데이터 마이그레이션) → 이상 없으면 master 병합/push는 별도 승인. 이후 Phase 4(작업 기록) plan-feature.
- Suggested skills: pjc:implement-task(승인 후), pjc:plan-feature(Phase 4).

## 통과 체크리스트
- [x] 요구 이해 작성(원문 인용 + 이해 5줄)
- [x] Impact Analysis 4-A(상태 리터럴 전수 grep — SQL 하드코딩 3곳·상수 참조·resw 식별)·4-B(직렬화: 값 마이그레이션 task 분리)·4-C(테스트 없음 — 테스트 프로젝트 부재)·4-D(재사용표 작성)
- [x] 각 task acceptance 검증 가능(빌드 + 시각/동작 구분), 상호 모순 0
- [x] Type 분류(T1 C·T2 D·T3 D·T4 D·T5 C·T6 D·T7 C), Design 필드(Type D 전부 + 신규 심볼 Type C)
- [x] Edge/Halt Forecast 각 task 명시
- [x] Open Questions 전부 해결, 결정 분기 0
- [x] 분할 권고(7 task ≤ 8) — 미해당
