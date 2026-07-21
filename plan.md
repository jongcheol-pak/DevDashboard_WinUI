# plan.md — 테스트 화면(TestPage)·테스트 등록 다이얼로그 시안 정합 재구현

**PRD**: docs/prd.md (FR-E1 통계·필터 / FR-E3 등록 다이얼로그 / FR-E5 목록 restyle / FR-E4·FR-T8 배지 배선)
**이전 plan**: 다이얼로그 후속 수정 + 네비게이션 버그 — Phase G 통과(Must 100%), 종결. Deferred는 `docs/plans/deferred.md`에 이관 완료.
**관련 선행 수정(미커밋)**: `CreateLinkedTest`가 링크 테스트를 작업 카테고리 이름 스위트에 넣도록 수정(TaskPageViewModel.cs) + prd FR-T6 노트 + notes 항목. code-quality 리뷰 통과. 이 plan과 같은 목표(배지 정합)라 유지하며, 이 plan의 첫 commit(baseline)에 함께 포함한다.

## 요구 이해

> 원문(사용자, 2026-07-21): "테스트 화면부터 잘못 구현되어 있어 배지가 표시 안 되는 거 같은데 일단 테스트 화면부터 제대로 구현해 / [이미지1] 1번 이미지처럼 테스트 등록 화면의 스위트 항목은 작업 카테고리 표시하고, [이미지2] 2번 이미지처럼 테스트 화면 구현해"

이해한 요구:
- **① 테스트 등록 다이얼로그 스위트 = 작업 카테고리**: 스위트 필드를 자유 입력이 아니라 **작업 카테고리 드롭다운**(UI·UX·프론트엔드·백엔드 + 사용자 정의)으로 바꾸고, 라벨을 "스위트 (작업 카테고리)"로 한다. 이렇게 하면 수동 등록 테스트도 작업 카테고리 이름 스위트로 들어가 칸반 통과율 배지(FR-T8)에 반영된다(= 배지가 안 뜨던 근본 원인의 UI 측 해소).
- **② 테스트 화면 시안 정합(이미지2)**: 헤더(프로젝트명 배지 + 스위트 필터 드롭다운 + 등록 버튼), 넓은 통계 카드 3개, 개수 포함 상태 필터 탭바, 스위트 그룹(폴더 아이콘·"N/M 통과"·우측 인라인 진행바), 테스트 항목을 **행 형태**(상태 아이콘 원 + 이름 + 우측 상태 pill)로 재구성.
- **③ 조작 방식**: 상태 pill 클릭 = 상태 순환(통과→실패→미실행), 우클릭 = 편집/삭제/메모(작업 칸반 카드와 일관). 이미지처럼 행에 버튼을 노출하지 않는다.

## Goal

`TestPage`를 시안(이미지2)대로 재구성하고 `TestEditDialog`의 스위트 선택을 작업 카테고리 드롭다운으로 바꿔, ① 테스트를 작업 카테고리 스위트로 정렬해 칸반 통과율 배지가 실제로 뜨게 하고 ② 통계 카드·상태 탭·스위트 그룹·행 레이아웃·조작(pill 순환·우클릭 메뉴)을 디자인에 맞춘다.

## Investigation Log (근거)

### 위키·문서
- 위키 참조: vault 미설정 — 코드 1차 출처로 진행.
- AGENTS.md 존재. 신선도 점검: 이번 계획이 참조하는 경로(`Presentation/Views/TestPage.xaml(.cs)`·`Views/Dialogs/TestEditDialog.*`·`ViewModels/TestPageViewModel.cs`·`ViewModels/TestEditDialogViewModel.cs`·`ViewModels/ProjectCardViewModel.cs`·`ViewModels/AppSettingsDialogViewModel.cs`·`Resources/Palette.xaml`·`Strings/{ko-KR,en-US}/Resources.resw`) 전부 실재 확인. **함정 2(다국어 ko/en 양쪽)·3(resw 접미 형식)·5(x:Bind 함수 바인딩은 Converter/ThemeResource 불가 → 코드비하인드 정적 헬퍼가 Brush 직접 반환)·7(RadioButton 커스텀 템플릿 GoToState)** 이번 작업과 직결 — 아래 반영. 어긋남 0건.
- **Deferred 대장 재수용**: `docs/plans/deferred.md:23` **[TestPage·NotificationPage 시안 대조]** — "사용자가 '작업 페이지만 먼저'로 범위 한정, 두 페이지도 목업 부재로 시안과 갈라졌을 가능성. 각 페이지 시안 확보 후 진행." 사용자가 이제 TestPage 시안(이미지2)을 제공 → **이 항목의 TestPage 몫을 이번에 재수용**한다(NotificationPage는 이번 범위 밖 — 대장에 유지).
- **Deferred 인접(유지)**: `:11` [테스트→작업 역방향 링크], `:12` [Method 필드 확장 소비], `:24` [BuildPassRateBadge 조회 테이블화], `:13` [Test* 구 resw 고아 정리] — 이번 범위 밖, 그대로 유지.
- PRD 경량 확인: 이번 변경이 **FR-E1**(통계 카드·상태 필터 탭)·**FR-E3**(등록/편집 다이얼로그 이름/스위트/방법)·**FR-E5**(목록 restyle: 스위트 그룹·통과율 바·상태 아이콘/배지)·**FR-E4/FR-T8**(배지 배선)에 닿는다 → PRD 연결(Phase G 활성).

### ① 테스트 등록 다이얼로그 — 현행 구현 (직접 Read)
- `TestEditDialog.xaml:18-24`: 스위트 필드가 **`IsEditable="True"` ComboBox** + `SuiteOptions`(기존 스위트 이름) 바인딩 → 자유 입력 허용. 라벨 `TestEditLabel_Suite.Text`="스위트"(`resw:394`).
- `TestEditDialogViewModel.cs:17-18,26-32`: `SuiteOptions`가 **호출부가 넘긴 기존 스위트 이름 목록**. 생성자 `(existing, suiteNames, presetSuite)`. `CanEditSuite`(:24)는 새 테스트일 때만 true(편집 시 스위트 이동 미지원 — 유지).
- 호출부 `TestPage.xaml.cs:117-121`(AddTest), `:127-128`(EditTest): `SuiteNames()`(:200-201, `Project.TestCategories.Select(Name)`)를 넘긴다 → **기존 테스트 카테고리 이름**을 스위트 옵션으로 씀. 요구 ①은 이를 **작업 카테고리 목록**으로 바꾸는 것.
- `TestEditDialog.xaml.cs:19-29`: Title은 `TestEdit_TitleAdd`/`TestEdit_TitleEdit`, 버튼은 `Dialog_Save`/`Dialog_Cancel`로 **이미 설정됨**. `OnSave`(:37-54)가 이름·스위트 필수 검증(`args.Cancel`). `ShowAsync` `new` 섀도잉으로 `XamlRoot` 설정(:31-35) — 기존 패턴 유지.
- **작업 카테고리 소스 확정**: `AppSettingsDialogViewModel.DefaultTaskCategories`(:218-221)=`["UI·UX","프론트엔드","백엔드"]` + `settings.TaskCategories`(사용자 정의). `TaskPageViewModel`이 `AvailableCategories`(:68-71)로 이 둘을 `Distinct`해 이미 쓴다 → 동일 패턴 재사용.

### ② TestPage — 현행 구현 (직접 Read)
- `TestPage.xaml.cs:16-21` 생성자 `TestPage(TestPageViewModel vm)` — **settings 미보유**. 호출부는 **단 1곳** `DashboardView.xaml.cs:237` `new TestPage(card.CreateTestPageViewModel())`(grep 전수: `new TestPage(` 1건).
- `ProjectCardViewModel.cs:533-537` `CreateTestPageViewModel()`: `EnsureTestsLoaded(); return new TestPageViewModel(_item, _repository, RefreshTestCardState);`. **`_settings` 미전달**. `new TestPageViewModel` 호출부도 **이 1곳뿐**(grep 전수 1건). `ProjectCardViewModel`은 `_settings`를 이미 보유(TaskPageViewModel 생성에 사용).
- `TestPageViewModel.cs:36-47` 생성자 `(project, repository, refreshCardState)` — 요구 ①의 작업 카테고리 노출을 위해 **`settings` 파라미터 추가 필요**(signature 변경, 호출부 1곳).
- 상태별 개수는 이미 계산: `PassCount`/`FailCount`/`UntestedCount`/`TotalCount`(:28-31, 프로젝트 전체 기준·필터 무관 — 통계 카드·탭 개수 공용). `SelectedStatus`(:34) 상태 필터 존재. **스위트 필터는 없음** → 요구 ② 헤더 드롭다운용 `SelectedSuiteFilter` 신규 필요.
- `TestSuiteGroup`(:217-222) record: `Category, Items, PassCount, TotalCount, PassRate` — "N/M 통과" 표시에 필요한 값 **이미 보유**(신규 필드 불필요, 표시 헬퍼만).
- `TestPage.xaml` 현행: 헤더에 상태 필터 RadioButton(개수 없음)·통계 카드는 작은 인라인 3개·스위트 그룹은 이름+통과율%+ProgressBar·항목은 큰 카드(방법·메모·상태 콤보·수정/삭제 버튼). **프로젝트명 배지·스위트 필터 드롭다운·개수 탭·행 레이아웃·pill 없음.**
- 상태 색 정적 브러시 확정(`TestPage.xaml.cs:25-31`, PRD §3): 통과 `#5AA3E8`(파랑)·실패 `#E8B45A`(호박)·미실행 `#8A8890`(회색). `StatusBrush`/`StatusGlyph`(✓/✕/○) 헬퍼 존재 → 아이콘·pill에 재사용.
- 조작 핸들러 존재: `EditTest_Click`/`DeleteTest_Click`/`EditNote_Click`(:124-167) → **우클릭 메뉴에서 재사용**. `StatusCombo_Loaded`/`StatusCombo_SelectionChanged`(:99-111)는 콤보 전용 → 행 재구성 시 **제거**, pill 클릭 순환(`ChangeTestStatus` 재사용)으로 대체.

### 팔레트·스타일 (직접 Read/grep)
- `Palette.xaml`: `AppSuccessBrush #5DB463`/`AppSuccessSoftBrush #285DB463`, `AppWarningBrush #D9954A`/`AppWarningSoftBrush #28D9954A`, `AppDangerBrush #F0716A`, `AppMutedSoftBrush #286F6D75`, `AppCardBrush`/`AppInputBrush`. 상태 pill 배경은 **상태색의 soft(저투명) 버전**이 필요 — 통과(파랑)·미실행(회색)은 soft 브러시가 없어 코드비하인드에서 status→soft Brush 반환 헬퍼 신규(함정 5: x:Bind 함수 바인딩이 Brush 직접 반환).
- `Styles.xaml:189` `TagBadgeStyle`(Border, pill 형태) — 배지·pill 컨테이너로 재사용. `AccentButtonStyle`(등록 버튼)·`SegmentedToggleStyle`(탭)·`DashedAddButtonStyle` 등 기존 스타일 확인.

### 4-D. 재사용 확인

| 신규 심볼 | 유사 기존 구현 검색 결과 | 재사용/신규 사유 |
|---|---|---|
| 작업 카테고리 목록(다이얼로그 스위트 옵션) | `TaskPageViewModel.AvailableCategories`(:68-71)가 `DefaultTaskCategories.Concat(settings.TaskCategories).Distinct()` | **재사용(패턴 복제)** — TestPageViewModel에 동일 방식 `AvailableCategories` 추가. 공용 헬퍼 추출은 소비 2곳뿐이라 보류(YAGNI) |
| 스위트 필터(`SelectedSuiteFilter`) | `TaskPageViewModel.SelectedCategoryFilter`(:51)+`OnSelectedCategoryFilterChanged`→`Rebuild` 선례 | **신규(패턴 재사용)** — 같은 [ObservableProperty]+partial OnChanged→Rebuild 구조 |
| "N/M 통과" 표시 텍스트 | `TestPage.FormatPassRate`(:81) "N%"만 있음 | **신규(정적 헬퍼)** `FormatPassCount(pass,total)`→"N/M 통과". `TestSuiteGroup`은 값 보유 |
| 상태 pill soft 배경 브러시 | `StatusBrush`(:57) 불투명 색만. soft 버전 없음 | **신규(정적 헬퍼)** `StatusSoftBrush(status)`→저투명 Brush(함정 5) |
| 상태 pill 클릭 순환 | `ChangeTestStatus`(VM:165) 존재, 순환 로직 없음 | **재사용+신규** — 순환 다음상태 계산 헬퍼 + 기존 `ChangeTestStatus` 호출 |
| 프로젝트명 배지 | `ProjectItem.Name` + `TagBadgeStyle`+`AppMutedSoftBrush`(TaskEditDialog 상태 pill 선례) | **재사용** — 신규 심볼 0 |
| 우클릭 편집/삭제/메모 | `EditTest_Click`/`DeleteTest_Click`/`EditNote_Click`(:124-167) 존재 | **재사용(무변경)** — MenuFlyout에서 호출 |

## 시각 요소 분해

> 출처: 사용자 제공 시안 이미지 2장(이미지1 새 테스트 등록 / 이미지2 테스트 화면). 목업 HTML이 없어 **px 값은 확정 불가** — 구조·상대 관계를 명세하고 수치는 기존 관례(WinUI 기본 `CornerRadius` 4~8, 기존 카드 Padding)를 따른다. 최종 판정은 빌드 후 사용자 육안 대조(⏳ HUMAN-VERIFY). CSS 대응은 없음(웹 시안 아님, 렌더 이미지).

### 이미지1 — 테스트 등록 다이얼로그

| 요소 | 속성 | 디자인 값 | 확인 방법 |
|---|---|---|---|
| 다이얼로그 제목 | 텍스트 | "새 테스트 등록" | 이미지1 상단 |
| 테스트 이름 라벨 | 텍스트 | "테스트 이름" + 빨간 `*` | 이미지1 |
| 테스트 이름 입력 | placeholder | "예: 로그인_실패시_잠금처리" | 이미지1 |
| 테스트 이름 입력 | 필수 강조 | 하단 빨간 라인(상시) — TaskEditDialog 제목칸과 동일 처리 | 이미지1(입력칸 하단 살몬 라인) |
| 스위트 라벨 | 텍스트 | "스위트 (작업 카테고리)" | 이미지1(사용자 명시) |
| 스위트 입력 | 형태 | **드롭다운(비편집 ComboBox)**, 항목=작업 카테고리, 예시값 "UI·UX" | 이미지1 + 사용자 요구 ① |
| 테스트 방법 라벨 | 텍스트 | "테스트 방법" | 이미지1 |
| 테스트 방법 입력 | placeholder | "테스트 수행 방법을 입력하세요 (선택)" | 이미지1 |
| 버튼 | 텍스트 | 등록 / 취소 | 이미지1 |

### 이미지2 — 테스트 화면

| 요소 | 속성 | 디자인 값 | 확인 방법 |
|---|---|---|---|
| 헤더 | 구성 | ← 대시보드 · "테스트" 제목 · 프로젝트명 배지 · (우측) 스위트 필터 드롭다운 · "+ 테스트 등록" | 이미지2 |
| 프로젝트명 배지 | 형태 | pill(저강도 배경 `AppMutedSoftBrush`), 텍스트=프로젝트명(예 "Aurora API") | 이미지2 |
| 스위트 필터 | 형태 | 드롭다운, 기본 "전체" | 이미지2(우측 상단) + 결정(스위트 필터) |
| 등록 버튼 | 텍스트/스타일 | "+ 테스트 등록", accent(살몬) | 이미지2 |
| 통계 카드 | 레이아웃 | 3개 가로 균등(넓게), 각 카드: 좌측 색점+라벨, 우측 큰 숫자 | 이미지2 |
| 통계 카드 | 값·색 | 통과 4(파랑점)/실패 1(호박점)/미실행 1(회색점) | 이미지2 + PRD §3 색 |
| 상태 필터 탭 | 형태 | 탭바, 각 탭에 개수 배지: 전체 6·통과 4·실패 1·미실행 1, 활성 탭 강조 | 이미지2 |
| 스위트 그룹 헤더 | 구성 | 폴더 아이콘 + 스위트명(굵게) + "N/M 통과"(저강도) + (우측) 인라인 진행바 | 이미지2 |
| 진행바 | 위치·값 | 헤더 우측 인라인, 값=통과율(0~100) | 이미지2 |
| 테스트 항목 행 | 레이아웃 | 좌: 상태 아이콘(원형 배경) + 이름 / 우: 상태 pill | 이미지2 |
| 상태 아이콘 | 글리프·색 | 통과 ✓(파랑)/실패 ✕(살몬·빨강)/미실행 ○(회색), 원형 soft 배경 | 이미지2 |
| 상태 pill | 텍스트·색 | 통과/실패/미실행, soft 배경 + 상태색 텍스트 | 이미지2 |
| 항목 행 | 조작 노출 | 버튼 없음(우클릭 메뉴·pill 클릭) | 이미지2 + 결정(행 조작) |

> ⚠️ 색 주의(⏳ HUMAN-VERIFY): 이미지2의 "실패"는 붉게 보이나 PRD §3·기존 코드는 실패=호박 `#E8B45A`. 이번엔 **기존 PRD 색을 유지**(신규 색 도입 안 함) — 붉은 실패색을 원하면 별도 값 치환(Deferred 후보).

## Decisions

| # | 항목 | 카테고리 | 결정 | Source |
|---|---|---|---|---|
| D1 | 스위트 필드 = 작업 카테고리 드롭다운 | UX | `IsEditable="False"` ComboBox, 항목=`Vm.AvailableCategories`(작업 카테고리). 자유 스위트명 입력 제거. **기각**: 편집형 콤보 유지(자유 입력 병행) — 배지는 이름 일치가 필요한데 자유 입력을 두면 작업 카테고리와 어긋난 스위트가 계속 생겨 근본 해소가 안 됨 | 사용자 요구 ① + `BuildPassRateBadge`(TaskPageViewModel:157) |
| D2 | TestPageViewModel에 settings 주입 | 구조 | 생성자에 `AppSettings settings` 추가, 호출부 `ProjectCardViewModel:536` 1곳 갱신(`_settings` 전달). `AvailableCategories`를 TaskPageViewModel과 동일식으로 노출. **호출부 전수 1곳**(grep) | `ProjectCardViewModel:536` + `TaskPageViewModel:68-71` |
| D3 | 헤더 드롭다운 = 스위트 필터 | UX | `SelectedSuiteFilter`(null=전체)로 스위트(카테고리) 필터. 상태 탭과 직교. `AvailableCategories` + "전체"로 드롭다운 구성 | 사용자 결정 2026-07-21 |
| D4 | 행 조작 = pill 클릭 순환 + 우클릭 메뉴 | UX | 상태 pill 클릭 = 통과→실패→미실행→통과 순환(`ChangeTestStatus` 재사용), 우클릭 MenuFlyout = 편집/삭제/메모(기존 핸들러 재사용). 인라인 상태 콤보·버튼 제거 | 사용자 결정 2026-07-21 + 작업 칸반 카드 선례 |
| D5 | 상태 색 | UX | 기존 PRD §3 색 유지(통과 #5AA3E8·실패 #E8B45A·미실행 #8A8890). pill/아이콘 배경은 soft(저투명). 신규 색 도입 안 함 | `TestPage.xaml.cs:25-31` + PRD §3 |
| D6 | 통계 카드 개수 vs 탭 개수 | 범위 | 둘 다 프로젝트 전체 기준 동일 값(`PassCount` 등, 필터 무관) — 시안이 둘 다 표시하므로 그대로 둔다. "전체" 탭 개수=`TotalCount` | 이미지2 + `TestPageViewModel:28-31` |
| D7 | "N/M 통과" 표시 | 기술 | 정적 헬퍼 `FormatPassCount(pass,total)`→"N/M 통과". `TestSuiteGroup.PassCount/TotalCount` 소비. 기존 `FormatPassRate`(N%)는 진행바 값용으로 유지 or 제거(사용처만 남기면) | `TestSuiteGroup`(:217) |
| D8 | 기존 비-작업-카테고리 스위트 처리 | 범위 | **표시는 무제한**(스위트명 그대로 그룹 표시) — 등록 시에만 작업 카테고리로 제한. 기존 "작업"·자유명 스위트의 테스트는 이동/마이그레이션 하지 않음(go-forward). 배지는 작업 카테고리 이름 스위트만 반영(설계) | 근본 원인 조사(디버깅 세션) |
| D9 | 이름 필수 하단 라인 | UX | TaskEditDialog 제목칸과 동일하게 `TextBox`를 `Grid`로 감싸 하단 danger 라인(`AppDangerBrush`, Height 2, IsHitTestVisible=False) 상시 표시. `OnSave`의 이름 필수 검증은 유지 | 이미지1 + TaskEditDialog 선례 |
| D10 | 작업 카테고리 결합식 공통화 | 구조 | **계획 수정(T1 구현 중, V-6 quality M1)**: 4-D는 소비 2곳으로 보고 YAGNI 유지를 택했으나, 실제로는 `TaskEditDialogViewModel:63`이 같은 표현식을 이미 쓰고 있어 T1이 추가되면 **3곳**이 된다(프로젝트 공통화 문턱 3회 도달). `AppSettingsDialogViewModel.ResolveTaskCategories(AppSettings)` 정적 헬퍼로 추출하고 3곳 모두 호출로 교체. 부수로 쓰기 전용이던 `TestPageViewModel._settings` 필드 제거(m1) | V-6 리뷰 M1/m1 + grep 3곳 확인 |

## PRD Coverage

| PRD ID | 우선순위 | 대응 task | 상태 |
|---|---|---|---|
| FR-E1 (테스트 전체 페이지 + 통계 카드 + 상태 필터 탭) | Must | T1, T4 | ✅ 커버(통계 카드 restyle + 개수 탭바) |
| FR-E3 (등록/편집 다이얼로그 이름/스위트/방법) | Must | T2 | ✅ 커버(스위트=작업 카테고리 드롭다운 + 문구 정합) |
| FR-E5 (테스트 목록 restyle — 스위트 그룹·통과율 바·상태 아이콘/배지) | Should | T3, T5 | ✅ 커버 |
| FR-E4 / FR-T8 (테스트↔작업 연결·칸반 통과율 배지 배선) | Should | T1·T2(스위트=작업 카테고리로 정렬) + 선행 CreateLinkedTest 수정 | ✅ 커버(배선 완성) |
| FR-E2 (테스트 상태 모델 통과/실패/미실행) | Must | — | 이번 범위 외 (기구현 — 상태 모델 무변경) |
| FR-T1~T8·FR-C*·FR-S*·FR-H*·FR-N* | Must/Should | — | 이번 범위 외 (기구현, 이번 diff 무관) |
| NFR-1 (빌드 오류 0·신규 경고 0) | — | 전 task | 검증 대상(기존 경고 5건 제외) |
| NFR-2 (계층 위반 0) | — | 전 task | 검증 대상(VM→페이지 역참조 없음) |
| NFR-3 (DB 스키마 하위호환) | — | — | ✅ 무영향(TestItem·TestCategory·스키마·직렬화 무변경) |
| NFR-4 (다국어 ko/en 대칭) | — | T2·T3·T4·T5 | 신규·변경 resw 키 ko/en 양쪽(함정 2) |
| NFR-5 (테스트) | — | — | 조건 미발동(테스트 프로젝트 부재 — AGENTS.md) |

## 작업 단계

### T1 — TestPageViewModel: settings 주입 + 작업 카테고리 노출 + 스위트 필터 `Type D`
- [ ] 구현
- **Files**: `DevDashboard_WinUI/Presentation/ViewModels/TestPageViewModel.cs`, `DevDashboard_WinUI/Presentation/ViewModels/ProjectCardViewModel.cs`, `DevDashboard_WinUI/Presentation/ViewModels/AppSettingsDialogViewModel.cs`, `DevDashboard_WinUI/Presentation/ViewModels/TaskPageViewModel.cs`, `DevDashboard_WinUI/Presentation/ViewModels/TaskEditDialogViewModel.cs` *(뒤 3개는 V-6 quality 리뷰 M1 대응으로 추가 — 아래 D10)*
- **Design**: ① 배치 — VM 로직은 `TestPageViewModel`, 배선은 `ProjectCardViewModel.CreateTestPageViewModel`. ② 신규 심볼 — `_settings`(필드), `AvailableCategories`(작업 카테고리, `IReadOnlyList<string>`), `SelectedSuiteFilter`([ObservableProperty] `string?`)+`OnSelectedSuiteFilterChanged`→`Rebuild`. ③ 의존 방향 — `TestPageViewModel`→`AppSettings`/`AppSettingsDialogViewModel.DefaultTaskCategories`(TaskPageViewModel과 동일), 페이지 역참조 없음. ④ 비추상화 — 작업 카테고리 계산을 공용 헬퍼로 추출하지 않는다(소비 2곳, YAGNI).
- **구성**:
  - 생성자 `(project, repository, refreshCardState)` → `(project, repository, settings, refreshCardState)`. `ArgumentNullException.ThrowIfNull(settings)` 추가, `_settings` 보관.
  - `AvailableCategories = AppSettingsDialogViewModel.DefaultTaskCategories.Concat(settings.TaskCategories).Distinct(StringComparer.OrdinalIgnoreCase).ToList();`(TaskPageViewModel:68-71 복제).
  - `SelectedSuiteFilter`([ObservableProperty] `string?`) 추가 + `partial void OnSelectedSuiteFilterChanged(string?) => Rebuild();`.
  - `Rebuild()`: 스위트 필터 적용 — `SelectedSuiteFilter` 비null이면 그 이름의 카테고리만 그룹으로. 통계 카드/탭 개수(`PassCount` 등)는 **전체 기준 유지**(필터 무관 — 시안 통계는 프로젝트 전체).
  - `ProjectCardViewModel.CreateTestPageViewModel()`: `new TestPageViewModel(_item, _repository, _settings, RefreshTestCardState)`로 `_settings` 전달.
- **Acceptance**: 빌드 성공(신규 경고 0) + `new TestPageViewModel(` 호출부가 4-인자로 갱신됨(grep — 잔존 3-인자 0건) + `AvailableCategories` 정의 1건(grep) + `SelectedSuiteFilter` 설정 시 그 스위트만 그룹 표시(스위트 필터 동작) + 통계 카드 개수는 스위트 필터와 무관하게 전체 유지 + 기존 상태 필터(`SelectedStatus`)·통계·통과율 회귀 없음(기존 테스트 표시 정상)
- **Edge Cases**: `settings.TaskCategories` 비어도 `DefaultTaskCategories` 3개로 `AvailableCategories` 비지 않음. `SelectedSuiteFilter`가 실제 스위트에 없는 값이면 그룹 0개(빈 목록) — 드롭다운은 실재 카테고리만 담으므로 정상 경로엔 없음. `_project.TestCategories`가 빈 신규 프로젝트 → 그룹 0·개수 0(예외 없음).
- **Halt Forecast**: 없음 — signature 변경이지만 호출부 1곳(사전 승인 등재), 파괴적·외부 작업 없음.

### T2 — TestEditDialog: 스위트=작업 카테고리 드롭다운 + 문구/이름 필수 라인 `Type C`
- [ ] 구현
- **Files**: `DevDashboard_WinUI/Presentation/Views/Dialogs/TestEditDialog.xaml`, `DevDashboard_WinUI/Presentation/ViewModels/TestEditDialogViewModel.cs`, `DevDashboard_WinUI/Presentation/Views/TestPage.xaml.cs`, `DevDashboard_WinUI/Strings/ko-KR/Resources.resw`, `DevDashboard_WinUI/Strings/en-US/Resources.resw`
- **Design**: ① 배치 — 다이얼로그 XAML/VM + 호출부(TestPage.xaml.cs)에서 옵션 소스 교체. ② 신규 심볼 — 없음(기존 `SuiteOptions` 의미만 "기존 스위트명"→"작업 카테고리"로, 편집형 콤보→드롭다운). ③ 의존 방향 — 호출부가 `Vm.AvailableCategories`(T1)를 다이얼로그에 전달. ④ 비추상화 — 스위트 옵션 소스를 다이얼로그 내부에서 계산하지 않고 주입 유지(현행 구조).
- **구성**:
  - `TestEditDialog.xaml`: 스위트 ComboBox `IsEditable="True"`→**제거(false 기본)**, `Text=` TwoWay 바인딩→`SelectedItem` 또는 `SelectedValue` 방식으로 전환(비편집이므로 항목 선택). 라벨 `TestEditLabel_Suite.Text`="스위트 (작업 카테고리)"로 resw 갱신.
  - 이름 필수 하단 라인(D9): 이름 `TextBox`를 `Grid`로 감싸 하단 danger `Border`(Height 2, `AppDangerBrush`, `CornerRadius 0,0,4,4`, `IsHitTestVisible="False"`) 겹침(TaskEditDialog 선례).
  - placeholder resw: `TestEditBox_Name.PlaceholderText`="예: 로그인_실패시_잠금처리", `TestEditBox_Method.PlaceholderText`="테스트 수행 방법을 입력하세요 (선택)". 이름 라벨 `TestEditLabel_Name.Text`="테스트 이름"(+ XAML에 빨간 `*` 별도 요소). 다이얼로그 제목 `TestEdit_TitleAdd`="새 테스트 등록"(현행 값 확인 후 갱신). 등록 버튼은 공용 `Dialog_Save`("저장")인데 시안은 "등록" — 새 테스트 전용 버튼 문구는 다이얼로그 코드비하인드에서 `TestEdit_TitleAdd` 경로에 맞춰 결정(신규 키 `TestEdit_Submit`="등록"/ 편집 "저장"). **공용 `Dialog_Save` 값은 건드리지 않음**(다이얼로그 8종 공용).
  - `TestPage.xaml.cs` `AddTest_Click`(:117-121)/`EditTest_Click`(:124-132): `SuiteNames()` → `Vm.AvailableCategories`로 옵션 소스 교체. `presetSuite`는 편집 시 기존 카테고리, 새 테스트 시 첫 작업 카테고리.
  - `TestEditDialogViewModel`: `SuiteOptions` 주석·의미 갱신("작업 카테고리 목록"). 비편집이므로 `SelectedSuite`가 목록 밖 값이면 미선택 — 편집 대상 테스트의 스위트가 작업 카테고리에 없을 수 있음(기존 자유명 스위트) → `presetSuite`가 목록에 없으면 첫 항목 fallback 또는 그대로(편집은 스위트 이동 미지원이라 표시만).
  - resw는 ko/en 양쪽(함정 2). x:Uid 소비 키는 접미(`.Text`/`.PlaceholderText`) 유지(함정 3).
- **Acceptance**: 빌드 성공(신규 경고 0) + 다이얼로그 스위트 필드가 **드롭다운(작업 카테고리 목록)**이고 자유 입력 불가 + 라벨 "스위트 (작업 카테고리)" + 새 테스트 시 첫 작업 카테고리 기본 선택 + 이름칸 하단 danger 라인 상시 표시 + placeholder·제목·버튼 문구가 시안과 일치 + 새 테스트 등록 시 선택 작업 카테고리 이름 스위트로 저장(`AddTestToSuite`가 그 이름 스위트 생성/연결) + 이름 빈 채 등록 시 닫히지 않음(기존 검증 유지) + resw ko/en 대칭(신규 키 양쪽)
- **Edge Cases**: 편집 대상 테스트의 기존 스위트가 작업 카테고리에 없는 자유명이면 드롭다운에 없음 → 편집은 스위트 이동 미지원(`CanEditSuite=false`)이라 표시만 되고 저장 시 스위트 무변경(회귀 없음 확인). `AvailableCategories` 첫 항목 항상 존재(빈 목록 아님). `SelectedItem` 전환 시 XAML 바인딩 타입(object) 확인.
- **Halt Forecast**: 없음 — 파일 5개(다이얼로그·VM·호출부·resw 2), 파괴적 없음. 신규 resw 키는 사전 승인 등재.

### T3 — TestPage.xaml 헤더: 프로젝트명 배지 + 스위트 필터 드롭다운 + 등록 버튼 `Type C`
- [ ] 구현
- **Files**: `DevDashboard_WinUI/Presentation/Views/TestPage.xaml`, `DevDashboard_WinUI/Presentation/Views/TestPage.xaml.cs`, `DevDashboard_WinUI/Strings/ko-KR/Resources.resw`, `DevDashboard_WinUI/Strings/en-US/Resources.resw`
- **Design**: ① 배치 — TestPage 헤더 Grid + 코드비하인드 필터 핸들러. ② 신규 심볼 — `SuiteFilter_SelectionChanged`(핸들러). ③ 의존 방향 — XAML→`Vm.Project.Name`·`Vm.AvailableCategories`·`Vm.SelectedSuiteFilter`(T1). ④ 비추상화 — 배지용 신규 스타일 만들지 않고 `TagBadgeStyle`+`AppMutedSoftBrush` 재사용.
- **구성**:
  - 헤더에 프로젝트명 배지(`Border` `TagBadgeStyle`+`AppMutedSoftBrush`, 텍스트 `Vm.Project.Name`) 제목 옆 추가.
  - 우측에 스위트 필터 `ComboBox`(항목=["전체"]+`Vm.AvailableCategories`, 기본 "전체"), `SelectionChanged="SuiteFilter_SelectionChanged"` → `Vm.SelectedSuiteFilter`(전체=null). TaskPage 카테고리 필터 콤보 선례(`CategoryFilter_SelectionChanged`) 참조.
  - 등록 버튼 문구 "+ 테스트 등록"으로(resw `TestAdd_Button` 갱신 or 신규). accent 유지.
  - `SuiteFilter_SelectionChanged` 핸들러 추가. 기존 상태 필터 RadioButton은 T4에서 탭바로 이설.
- **Acceptance**: 빌드 성공(신규 경고 0) + 헤더에 프로젝트명 배지 표시(`Vm.Project.Name`) + 스위트 필터 드롭다운("전체"+작업 카테고리) 선택 시 해당 스위트만 그룹 표시(T1 `SelectedSuiteFilter` 연동) + 등록 버튼 "+ 테스트 등록"(accent) 클릭 시 등록 다이얼로그 표시(회귀 없음) + resw ko/en 대칭
- **Edge Cases**: 스위트 필터 "전체" 선택 시 `SelectedSuiteFilter=null`(전체 표시). 콤보 초기 `SelectionChanged` 발화 시 `Vm` non-null(생성자에서 먼저 설정 — TestPage 선례). 프로젝트명 긴 경우 배지 폭 — `TextTrimming` 또는 자연 확장(육안 확인).
- **Halt Forecast**: 없음 — XAML/resw restyle 위주, 파괴적·외부·의존성 요소 없음.

### T4 — TestPage.xaml 통계 카드(넓게) + 상태 필터 탭바(개수) `Type C`
- [ ] 구현
- **Files**: `DevDashboard_WinUI/Presentation/Views/TestPage.xaml`, `DevDashboard_WinUI/Presentation/Views/TestPage.xaml.cs`, `DevDashboard_WinUI/Strings/ko-KR/Resources.resw`, `DevDashboard_WinUI/Strings/en-US/Resources.resw`
- **Design**: ① 배치 — TestPage 통계·필터 영역 XAML + 상태 탭 핸들러(기존 `StatusTab_Checked` 재사용). ② 신규 심볼 — 없음(레이아웃 재구성, 개수 바인딩은 기존 `PassCount` 등). ③ 의존 방향 — XAML→`Vm.PassCount/FailCount/UntestedCount/TotalCount`. ④ 비추상화 — 카드/탭 전용 스타일 신설 최소화, 기존 카드 Border 패턴 재사용.
- **구성**:
  - 통계 카드 3개를 **가로 균등(넓게)** 재배치(`Grid` 3열 `*` 또는 `Stretch`), 각 카드 좌측 색점+라벨 / 우측 큰 숫자(`FontWeight Bold`, 큰 FontSize). 색점=상태 브러시(PassBrush/FailBrush/UntestedBrush).
  - 상태 필터를 **탭바**로: 전체/통과/실패/미실행 각 탭에 개수 배지(전체=`TotalCount`, 통과=`PassCount`, 실패=`FailCount`, 미실행=`UntestedCount`). 활성 탭 강조. 기존 `StatusTab_Checked`(:90-95) 재사용, 필요 시 `SegmentedToggleStyle` 참조(함정 7: 선택/호버 상태 분리).
  - "전체" 탭 개수 라벨 resw/포맷(개수는 바인딩).
- **Acceptance**: 빌드 성공(신규 경고 0) + 통계 카드 3개가 가로로 넓게 배치·우측 큰 숫자 + 각 상태 탭에 개수 배지 표시(전체 `TotalCount` 등) + 탭 클릭 시 해당 상태만 항목 필터(기존 `SelectedStatus` 동작 유지) + 개수가 실제 데이터와 일치(추가/상태변경 후 갱신)
- **Edge Cases**: 개수 0일 때 배지 "0" 표시. 탭 전환 시 스위트 필터(T3)와 직교 동작(둘 다 적용). 활성 탭 표시가 호버에 가려지지 않음(함정 7).
- **Halt Forecast**: 없음 — XAML/resw restyle 위주, 파괴적·외부·의존성 요소 없음.

### T5 — TestPage.xaml 스위트 그룹 + 테스트 행 재구성 + 조작(pill 순환·우클릭) `Type D`
- [ ] 구현
- **Files**: `DevDashboard_WinUI/Presentation/Views/TestPage.xaml`, `DevDashboard_WinUI/Presentation/Views/TestPage.xaml.cs`, `DevDashboard_WinUI/Strings/ko-KR/Resources.resw`, `DevDashboard_WinUI/Strings/en-US/Resources.resw`
- **Design**: ① 배치 — TestPage 스위트 그룹·항목 DataTemplate + 코드비하인드 헬퍼/핸들러. ② 신규 심볼 — `FormatPassCount(int,int)`(정적, "N/M 통과"), `StatusSoftBrush(string)`(정적, 상태 soft Brush — 함정 5), `StatusPill_Click`(순환 핸들러), 순환 다음상태 계산(인라인 or `NextStatus(string)`). ③ 의존 방향 — XAML→정적 헬퍼·`Vm.ChangeTestStatus`. ④ 비추상화 — 상태별 색을 Converter로 빼지 않고 정적 헬퍼 직접 반환(함정 5), pill/아이콘 전용 UserControl 만들지 않음(인라인 템플릿).
- **구성**:
  - 스위트 그룹 헤더: 폴더 `FontIcon`(예 `&#xE8B7;`) + 스위트명(굵게) + `FormatPassCount(PassCount,TotalCount)`("N/M 통과", 저강도) + 우측 인라인 `ProgressBar`(`Value=PassRate`, 폭 고정 or `*`). 기존 rename/delete 버튼은 우클릭 or 유지(스위트 조작은 이미지에 없음 → 우클릭 MenuFlyout로 이동 검토, 최소 기능 유지).
  - 테스트 항목 행 템플릿(카드→행): 좌측 상태 아이콘(원형 `Border` soft 배경 `StatusSoftBrush` + `StatusGlyph` 글리프 `StatusBrush` 색) + 이름 / 우측 상태 pill(`Border` `TagBadgeStyle` + `StatusSoftBrush` 배경 + 상태 텍스트 `StatusBrush` 색). 방법·메모는 이미지에 없음 → 행에서 제거(편집 다이얼로그·메모 다이얼로그에서 계속 편집 — 데이터 보존).
  - 조작: 상태 pill에 `Tapped="StatusPill_Click"`(Tag=TestItem) → 다음 상태 순환(`ChangeTestStatus`). 행 `Border.ContextFlyout` MenuFlyout: 편집(`EditTest_Click`)·삭제(`DeleteTest_Click`)·메모(`EditNote_Click`) 재사용. 기존 `StatusCombo_Loaded`/`StatusCombo_SelectionChanged` **제거**(콤보 삭제).
  - `FormatPassCount`·`StatusSoftBrush`·`StatusPill_Click` 추가. 순환: 통과→실패→미실행→통과.
- **Acceptance**: 빌드 성공(신규 경고 0) + 스위트 그룹 헤더가 폴더 아이콘+이름+"N/M 통과"+우측 진행바 + 테스트 항목이 **행 형태**(좌 상태 아이콘 원·이름 / 우 상태 pill) + 상태 pill 클릭 시 통과→실패→미실행 순환하며 즉시 저장·개수/진행바 갱신 + 우클릭 메뉴로 편집/삭제/메모 동작(기존 핸들러) + 행에 인라인 콤보·버튼 없음 + `StatusCombo_Loaded`/`StatusCombo_SelectionChanged` 잔존 0건(grep — 제거 확인) + resw ko/en 대칭
- **Edge Cases**: pill 연타(순환) 시 `ChangeTestStatus`가 동일 상태 무시 안 함(항상 다음 상태라 매번 저장 — 정상). 통과→실패 전환 시 `CompletedAt` 초기화(기존 `ChangeTestStatus` 로직). 빈 스위트(항목 0) 표시(진행바 0, "0/0 통과" → 0% — 0 나눗셈 방지: `TestSuiteGroup.PassRate`가 이미 `total==0?0`). 긴 테스트 이름 줄바꿈/말줄임. 우클릭 메뉴가 pill 클릭과 충돌 안 함(Tapped vs ContextFlyout 분리).
- **Halt Forecast**: 없음 — 단일 화면 파일 + resw, 공개 API 변경 없음(추가 심볼 전부 정적/private).

## 사전 승인 항목 (일괄 승인 대상)
- **관련 선행 수정(미커밋) 커밋**: `TaskPageViewModel.cs`(CreateLinkedTest→작업 카테고리 스위트) + `docs/prd.md`(FR-T6 노트) + `notes.md` 항목. 이 plan 구현 첫 commit(baseline)에 포함(디버깅 세션에서 code-quality 리뷰 통과).
- **T1 signature 변경**: `TestPageViewModel` 생성자에 `AppSettings settings` 추가 — 호출부 `ProjectCardViewModel.CreateTestPageViewModel` **1곳**(grep 전수 확인). 내부 배선, 공개 계약 외.
- **신규 resw 키**(T2·T3·T5): `TestEdit_Submit`(등록/저장), 문구 갱신(`TestEditLabel_Suite`·placeholder·`TestAdd_Button`·`TestEdit_TitleAdd`) — ko/en 양쪽. 기존 공용 `Dialog_Save`·`TestCategory_None` 등 공용 키 값 **무변경**.
- **행 재구성에 따른 심볼 제거**: `StatusCombo_Loaded`/`StatusCombo_SelectionChanged`(콤보 전용) 제거(T5) — 소비처는 제거될 콤보 1곳.
- 로컬 작업 브랜치(현행)에서 task별 commit.

## 불가피한 Halt (위임 불가)
- master 병합·push·태그·릴리즈·PR — 이번 작업 완료 후 별도 승인.
- 시안 대조 **최종 시각 판정** — 빌드는 마크업 존재만 보증, "시안과 같아 보이는가"는 사용자만 판정(⏳ HUMAN-VERIFY: 배지·pill 색/형태·통계 카드 레이아웃·행 정렬·진행바·프로젝트명 배지·실패색(D5 주의)).
- **배지 실동작 확인** — 새 테스트를 작업 카테고리 스위트로 등록 후 칸반 그 카테고리 그룹에 통과율 배지가 뜨는지는 앱 실행 필요(⏳ HUMAN-VERIFY).

## Deferred / Follow-up
- **[NotificationPage 시안 대조]** — 대장 `:23`의 NotificationPage 몫은 이번 범위 밖. 시안 확보 후 별도 진행(대장 유지).
- **[실패 색 시안 불일치]** — 이미지2의 실패가 붉게 보이나 PRD·코드는 호박 `#E8B45A`. 붉은 실패색 원하면 순수 값 치환(D5).
- **[기존 자유명/"작업" 스위트 마이그레이션]** — 등록을 작업 카테고리로 제한해도 과거 자유명·"작업" 스위트 테스트는 남는다(배지 미반영). 필요 시 마이그레이션 별도 논의(D8).
- **[스위트 조작(이름수정/삭제) 배치]** — 행/그룹에서 버튼을 뺀 자리(우클릭)로 옮길지 이미지에 근거 부족 — 구현 중 최소 유지, 시각 확인 후 정리.

## Out of Scope
- `TestItem`·`TestCategory` 도메인·SQLite 스키마·직렬화 변경.
- 테스트→작업 역방향 링크/배지(대장 `:11`).
- 기존 스위트 데이터 마이그레이션(D8 — go-forward만).
- NotificationPage 재구성.
- 픽셀 단위 수치 일치(목업 HTML 없음 — 구조·형태까지).

## Open Questions
- [x] 헤더 "전체 ▾" 드롭다운 기능 → **스위트(작업 카테고리) 필터**(사용자, 2026-07-21)
- [x] 테스트 항목 행 조작 방식 → **상태 pill 클릭 순환 + 우클릭 메뉴(편집/삭제/메모)**(사용자, 2026-07-21)
- [x] 스위트 필드 형태 → **작업 카테고리 드롭다운(비편집), 자유 입력 제거**(요구 ① + D1)

## 검증 방법
- 빌드: `"C:/Program Files/Microsoft Visual Studio/18/Professional/MSBuild/Current/Bin/MSBuild.exe" "DevDashboard_WinUI/DevDashboard.csproj" -t:Build -p:Configuration=Debug -p:Platform=x64` → 오류 0 + 기존 경고 5건(NU1903 1 + CS0612 4) 외 신규 0
- 회귀 방지 grep:
  - `new TestPageViewModel(` 4-인자 갱신·3-인자 잔존 0(T1)
  - `TestPageViewModel.cs`에 `AvailableCategories`·`SelectedSuiteFilter` 정의(T1)
  - `TestEditDialog.xaml`에 스위트 ComboBox `IsEditable="True"` 잔존 0(T2)
  - `TestPage.xaml.cs`에 `StatusCombo_Loaded`·`StatusCombo_SelectionChanged` 잔존 0(T5)
  - `FormatPassCount`·`StatusSoftBrush`·`StatusPill_Click`·`SuiteFilter_SelectionChanged` 정의(T3·T5)
  - `Strings/{ko-KR,en-US}/Resources.resw` 신규·변경 키 ko/en 대칭(T2·T3·T4·T5)
- 동작 확인(빌드로 검증 불가 → ⏳ HUMAN-VERIFY):
  - **T2**: 다이얼로그 스위트=작업 카테고리 드롭다운(자유 입력 불가)·라벨·placeholder·이름 하단 라인·제목/버튼 문구
  - **T3**: 프로젝트명 배지·스위트 필터 동작·등록 버튼
  - **T4**: 통계 카드 넓게·개수 탭·탭 필터
  - **T5**: 스위트 그룹(폴더·N/M 통과·진행바)·행 레이아웃·상태 pill 클릭 순환·우클릭 메뉴
  - **통합**: 새 테스트를 작업 카테고리(예 UI·UX) 스위트로 등록 → 칸반 그 카테고리 그룹에 통과율 배지 표시(궁극 목표)

## Phase Ledger
- (미시작)
