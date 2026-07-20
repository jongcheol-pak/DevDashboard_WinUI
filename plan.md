# plan.md — 작업(TaskPage) 칸반 화면 시안 정합 재구성

**PRD**: docs/prd.md (FR-T2·FR-T4·FR-T5·FR-T8 재작업 — 기능 요구는 이미 충족, **시각 구조만 시안에 맞춤**)
**이전 plan**: Phase 5(알림) — Phase G 통과(Must 100%), master 병합 완료(HEAD 2846418). 이 plan은 master 기준 새 브랜치(`task/taskpage-design-align`)에서 진행.
**다음 plan**: 없음. (TestPage·NotificationPage 시안 대조는 사용자가 "작업 페이지만 먼저"로 한정 — 결과 확인 후 별도 판단)

## 요구 이해

> 원문(사용자, 2026-07-20): "구현된 작업 화면이 이미지하고 전혀 다른데 다시 확인해서 수정" + 첨부 시안 이미지 1장(작업 칸반 화면).

이해한 요구:
- 첨부 이미지가 **작업 화면의 디자인 기준**이고, 현재 구현된 `TaskPage`가 그 구조와 어긋나 있으니 **시안과 같은 레이아웃으로 다시 맞춘다**.
- 어긋난 이유는 확인됨 — Phase 2 구현 당시 목업이 레포에 없어 PRD 텍스트만으로 구현했다(이전 `plan.md` Investigation Log에 "시각 명세는 PRD §3 추출값 + WinUI idiom" 기록). **기능 결함이 아니라 시각 구조 불일치**다.
- 따라서 이번 작업은 **기능 추가가 아니라 시각 정합 재구성**이다 — 드래그앤드롭·완료시 작업기록 팝업·카테고리 필터·목록 뷰·테스트 연결은 전부 보존한다.
- 사용자 확정: 담당자 필드는 제외 유지, 카드 조작은 클릭=편집·우클릭=메뉴, 범위는 TaskPage 한 화면.

## Goal

`TaskPage`의 칸반 레이아웃을 시안과 동일한 구조로 재구성한다 — ① 열을 패널로 감싸고 상태 색 dot 부여 ② **열 안에서 카테고리별로 그룹핑**하고 그룹 헤더에 색 dot·개수·테스트 통과율 배지 표시 ③ 카드를 제목+우선순위 pill / 설명 / 날짜 범위 3단 구성으로 압축하고 상태 콤보·아이콘 버튼 제거(클릭=편집, 우클릭=메뉴) ④ `+ 새 작업`을 열 하단 점선 버튼으로 이동 ⑤ 헤더를 카테고리 콤보 + 칸반/목록 세그먼트 토글로 정리. 기능 동작은 무손실.

## Investigation Log (근거)

### 위키·시안
- 위키 참조: vault 미설정 — 코드 1차 출처로 진행.
- 시안: 사용자 제공 이미지 1장(작업 칸반). 목업 HTML은 여전히 레포에 없어 **픽셀 값이 아닌 구조·구성 요소 기준**으로 대조한다(아래 `## 시각 요소 분해`).
- **AGENTS.md 부재** — 5개 Phase 내내 없었다. 빌드 명령은 확인된 사실(x64 MSBuild)이라 추측 모드가 아니며, AGENTS.md 생성은 이번 범위 밖(완료 보고에서 별도 제안).

### Deferred 대장 확인 (docs/plans/deferred.md)
- **재수용**: `[미사용 심볼 정리]` 중 `TaskPageViewModel.ShowKanban`/`ShowList` RelayCommand 미사용 건 — 이번 **T4**(세그먼트 토글)가 정확히 그 지점을 건드리므로 **이번에 해소**한다(토글이 커맨드를 실제 소비하거나, 소비하지 않으면 제거). 같은 항목의 `TaskEditDialog.xaml x:Name="TitleBox"` 미사용 건은 무관 — 대장 유지.
- **이번에도 유지**: `[칸반 열 내 카드 재정렬]`(열 내 정렬 영속화 — 이번은 시각 구조만), `[FR-T7 담당자]`(사용자 제외 결정 재확인), `[Todo*/Test* resw 고아 정리]`, `[교차 집계 페이지]`, `[README/스크린샷]`, `[BOM 통일]`.

### 영향 범위 — 전수 확인 (직접 grep + Read)
- 변경 대상 심볼(`WaitingItems`/`ActiveItems`/`CompletedItems`/`HoldItems`/`*Count`/`CategoryGroups`/`TaskCategoryGroup`/`IsKanbanView`) 전수 grep(obj·bin 제외) 결과 **사용처가 `Presentation/ViewModels/TaskPageViewModel.cs`와 `Presentation/Views/TaskPage.xaml` 2개 파일뿐**. 외부 호출자·구현체·직렬화 대상 **0건**.
- `TaskPageViewModel(ProjectItem, IProjectRepository, AppSettings, Action)` 생성자와 `TaskPage(TaskPageViewModel, AppSettings)` 생성자는 **이번에 변경하지 않는다** → 호출부(`ProjectCardViewModel.CreateTaskPageViewModel():517`, `DashboardView.xaml.cs:184`, 알림 페이지 이동 경로) 무영향.
- 단, 위 전수 조사는 **파일 간** 영향이다. **동일 파일 내부 사용처**(`FillColumn():131`·`WaitingCount = WaitingItems.Count`:122-125)는 별도 처리 대상 — 아래 "리뷰에서 드러난 함정" 참조.
- `TodoItem`(Domain/Entities/TodoItem.cs) **무변경** — 담당자 제외 결정으로 도메인·DB 스키마·직렬화 변경 없음. 마이그레이션 불요.

### 데이터 가용성 (직접 Read)
- `ProjectCardViewModel.CreateTaskPageViewModel():517-523`가 `EnsureTodosLoaded()` + `EnsureHistoriesLoaded()` + **`EnsureTestsLoaded()`**를 모두 호출 → `_project.TestCategories`가 이미 로드된 상태로 VM에 들어온다. **통과율 배지에 신규 데이터 로딩 불필요**.
- `TaskPageViewModel.BuildTestStatusLookup():80-86`가 이미 `_project.TestCategories`를 순회 중 — 같은 소스에서 카테고리별 통과율을 계산할 수 있다.
- `TestPageViewModel.Rebuild():75-82`의 통과율 계산식 확인: `total = cat.Items.Count`, `pass = cat.Items.Count(t => t.Status == TestItem.StatusPass)`, `passRate = total == 0 ? 0 : (double)pass/total*100`. **이 식을 그대로 재사용**한다(사용자 결정: 같은 이름의 테스트 카테고리 기준).
- `TestPage.FormatPassRate(double):81` = `$"{rate:0}%"` — 서식 선례.

### 재사용 자산 (직접 Read)
- `Resources/Styles.xaml`: `TagBadgeStyle`(CornerRadius 4 / Padding 8,3) — **우선순위 pill·통과율 pill의 기반으로 재사용**. `GroupTabRadioButtonStyle`은 대시보드 그룹탭 전용(Height 48, 하단 보더 3px 탭)이라 시안의 소형 세그먼트 토글과 형태가 달라 **재사용 불가** → 신규 스타일 필요. `GroupAddButtonStyle`도 Height 48 고정·점선 없음이라 열 하단 추가 버튼과 불일치 → 신규 필요.
- `Resources/Palette.xaml`: `AppAccentBrush`(#F0716A 살몬)·`AppSuccessBrush`(#5DB463 초록)·`AppCardBrush`·`AppCardAltBrush`·`AppSurfaceBrush`·`AppBorderBrush`·`AppBorderStrongBrush`·`AppText{Primary,Secondary,Tertiary,Muted}Brush` 존재. **파랑·주황 계열은 미정의** → 상태 dot(진행 중=파랑, 보류=주황)과 우선순위 pill(보통=파랑, 높음=주황)에 신규 2색 추가 필요.
- `Presentation/Converters`: `TagColor`(이름→결정론적 색) — 카테고리 그룹 dot에 재사용(기존 카드에서 이미 사용 중).
- `TaskEditDialog(TodoItem? existing, AppSettings settings)` — **시그니처 변경 불필요**. 열별 `+ 새 작업`은 다이얼로그 결과 `TodoItem`의 `Status`를 코드비하인드에서 해당 열 상태로 지정한 뒤 `Vm.AddTodo`에 넘기면 된다.

### 리뷰에서 드러난 함정 (plan-reviewer 1회차, 직접 재확인 완료)
- **`TaskCardTemplate`은 칸반·목록 공용이다**(`TaskPage.xaml:15` 주석 "칸반·목록 공용", `:186/199/212/225` 칸반 + `:239` 목록이 모두 소비). 카드를 그 자리에서 고치면 **목록 뷰가 강제로 함께 바뀐다** → 칸반 전용 템플릿을 **신규**로 만들고 기존 템플릿은 목록 전용으로 존치한다(D11). 목록 뷰에는 `AllowDrop`이 없어 드래그 경로가 아예 없으므로, 목록 카드에서 상태 콤보를 빼면 상태 변경 수단이 사라진다 — 존치가 기능적으로도 옳다.
- **`Vm.CreateLinkedTest`의 유일한 호출부가 `AddTask_Click`**(`TaskPage.xaml.cs:114-115`, 전 레포 grep 확인)이다. 헤더 `+새 작업` 제거 시 **FR-T6("테스트 목록에도 추가" 토글)가 조용히 죽는다** → 열별 추가 버튼이 이 분기를 반드시 이관한다(D12).
- **`Palette.xaml`에는 `Default`(다크) 딕셔너리 하나뿐**이다(`:19`, `:10-11` 주석이 "앱은 항상 다크로 고정 … Light/HighContrast 분기는 두지 않는다(다크 단일 — YAGNI)"로 명시). 신규 색은 **Default에만** 추가한다 — Light 딕셔너리를 새로 만들면 Phase 0의 의도적 설계 결정을 뒤집는다.
- **`WaitingItems.Count`가 `WaitingCount`의 계산식**(`TaskPageViewModel.cs:122-125`)이다. 요소 타입을 그룹으로 바꾸면 `.Count`가 **작업 수가 아니라 카테고리 수**가 된다 — 컴파일 오류 없이 조용히 잘못된 숫자를 표시한다(FR-T4 "상태별 개수" 직결). 개수는 그룹 합계로 재계산한다.
- **`TaskPageViewModel.TotalCount`(`:47`)는 계산·설정만 되고 XAML 어디에도 바인딩되지 않는다**(grep 확인). 미사용 심볼 정리 대상에 포함한다.
- resw 기존 키 확인 — `TaskStatus_{Waiting,Active,Completed,Hold}`, `TaskPriority_{High,Normal,Low}`, `TaskFilter_All`, `TaskView_Kanban/List`, `TaskAdd_Button`, `TaskEdit_Tooltip`, `TaskDelete_Tooltip`, `TaskCategory_None` 전부 존재 → **대부분 재사용**. 신규는 날짜 범위 서식·컨텍스트 메뉴 항목·통과율 배지 서식뿐.

### 기술 제약 (확인 완료)
- **WinUI `Border`는 점선 테두리를 지원하지 않는다**(`StrokeDashArray`는 `Shape` 계열 속성). 시안의 점선 `+ 새 작업` 버튼은 **Button 템플릿 안에 `Rectangle` + `StrokeDashArray="3,3"`** 으로 구성한다. → T1에서 스타일로 1회 정의해 4개 열이 공유.

### 4-D. 재사용 확인

| 신규 심볼 | 유사 기존 구현 검색 결과 | 재사용/신규 사유 |
|---|---|---|
| `TaskColumnGroup` (record) | `TaskCategoryGroup`(TaskPageViewModel.cs:262), `TestSuiteGroup`(TestPageViewModel.cs:218) | **신규** — 기존 두 record 모두 통과율 배지 필드가 없다. `TaskCategoryGroup`은 목록 뷰가 계속 쓰므로 확장 대신 별도 신규(목록 뷰에 불필요한 필드를 얹지 않음). |
| `TaskKanbanCardTemplate` (DataTemplate) | `TaskCardTemplate`(TaskPage.xaml:15, 칸반·목록 공용) | **신규** — 기존 템플릿을 그 자리에서 고치면 목록 뷰가 강제로 함께 바뀐다(D11). 기존은 목록 전용으로 존치. |
| 통과율 계산 | `TestPageViewModel.Rebuild():75-82` | **재사용** — 동일 계산식을 `TaskPageViewModel`에 이식(3회 미만 중복이라 공용 추출 안 함, CLAUDE.md "2회 이상 확인 시 공통화" 기준 미달). |
| `DashedAddButtonStyle` | `GroupAddButtonStyle`(Styles.xaml:140) | **신규** — 기존은 Height 48 고정·점선 없음(대시보드 그룹탭 전용). 형태 불일치. |
| `SegmentedToggleStyle` | `GroupTabRadioButtonStyle`(Styles.xaml:82) | **신규** — 기존은 하단 보더 탭(Height 48). 시안은 소형 라운드 세그먼트. |
| 우선순위/통과율 pill | `TagBadgeStyle`(Styles.xaml:189) | **재사용** — CornerRadius/Padding 그대로 쓰고 Background만 지정. |
| 상태 dot / 카테고리 dot | `Ellipse` + `TagColor` 컨버터(TaskPage.xaml:28-32) | **재사용** — 카테고리 dot은 기존 패턴 그대로. 상태 dot만 고정색 4개 신규 브러시. |

## 시각 요소 분해

> 출처: 사용자 제공 시안 이미지. 목업 HTML이 없어 **px 값은 확정 불가** — 구조·구성 요소·상대 관계를 명세하고, 수치는 기존 페이지 관례(TaskPage/TestPage 현행 값)를 따른다. 확인은 빌드 후 사용자 육안 대조(⏳ HUMAN-VERIFY).

| 요소 | 속성 | 디자인 값 | 확인 방법 |
|---|---|---|---|
| 헤더 뒤로가기 | 구성 | `‹` 글리프 + "대시보드", 저강도(subtle) 버튼 | 시안 좌상단 |
| 헤더 제목 | 스타일 | "작업" 굵게, 뒤로가기 우측 | 시안 |
| 헤더 우측 | 구성·순서 | 카테고리 콤보("전체") → 칸반/목록 세그먼트 토글 | 시안 우상단 |
| 세그먼트 토글 | 형태 | 두 버튼이 맞붙은 라운드 그룹, 선택된 쪽만 밝은 배경, 각 항목에 아이콘 + 라벨 | 시안 |
| 헤더 `+새 작업` | 존재 | **없음**(열 하단으로 이동) | 시안에 부재 |
| 칸반 열 | 컨테이너 | 배경 + 1px 테두리 + 라운드 패널, 열 4개 등폭 | 시안 |
| 열 헤더 | 구성 | 상태 색 dot + 상태명(굵게) + 개수(저강도) | 시안 |
| 열 헤더 dot 색 | 색 | 예정=살몬(AppAccent) / 진행 중=파랑(신규) / 완료=초록(AppSuccess) / 보류=주황(신규) | 시안 dot 색상 판독 |
| 카테고리 그룹 헤더 | 구성 | 카테고리 색 dot + 카테고리명 + 개수 | 시안 (열 안 그룹) |
| 통과율 배지 | 구성·색 | `✓ 통과율 {n}% · {n}건` pill, 초록 텍스트 + 저채도 초록 배경 | 시안 그룹 헤더 우측 |
| 통과율 배지 | 조건 | 같은 이름의 테스트 카테고리가 없으면 **미표시** | 사용자 결정(2026-07-20) |
| 작업 카드 | 컨테이너 | 배경 + 1px 테두리 + 라운드, 열 패널보다 밝은 톤 | 시안 |
| 카드 1행 | 구성 | 제목(굵게, 줄바꿈) 좌 / 우선순위 pill 우 | 시안 |
| 우선순위 pill 색 | 색 | 높음=주황 / 보통=파랑 / 낮음=회색 | 시안 배지 색상 판독 |
| 카드 2행 | 구성 | 설명 텍스트, 저강도·작게, 줄바꿈. 비어 있으면 미표시 | 시안 |
| 카드 3행 | 구성 | `MM-dd – MM-dd` 날짜 범위 **한 줄**, 저강도 | 시안 |
| 카드 조작 | 구성 | 상태 콤보·수정·삭제 버튼 **없음** | 시안 |
| 열 하단 버튼 | 형태 | `+ 새 작업`, **점선 테두리**, 열 폭 전체, 저강도 텍스트 | 시안 각 열 하단 |

## Decisions

| # | 항목 | 카테고리 | 결정 | Source |
|---|---|---|---|---|
| D1 | 담당자 필드 | 범위 | **제외 유지** — TodoItem·DB 무변경, 카드 하단은 날짜만 | 사용자 결정 2026-07-20 (deferred.md:9 FR-T7 대기 유지) |
| D2 | 카드 편집 진입 | UX | **카드 좌클릭 → TaskEditDialog** | 사용자 결정 2026-07-20 |
| D3 | 카드 삭제·상태변경 | UX | **우클릭 컨텍스트 메뉴**(편집·삭제·상태 변경 4종) — 드래그 못 쓰는 상황의 접근성 경로 겸용 | 사용자 결정 2026-07-20 |
| D4 | 통과율 배지 집계 | 로직 | **작업 카테고리명과 같은 이름의 `TestCategory` 기준**. `TestPageViewModel.Rebuild():75-82` 계산식 재사용. 비교자는 **`OrdinalIgnoreCase`**(`CreateLinkedTest():188`·필터 `:115` 선례 계승). 일치 카테고리 없거나 테스트 0건이면 배지 숨김. **"미분류" 그룹은 배지 미표시**(실제 카테고리가 아님) | 사용자 결정 2026-07-20 + 레포 비교자 선례 |
| D5 | 열별 `+새 작업`의 상태 | 로직 | 다이얼로그 결과 `TodoItem.Status`를 **해당 열의 상태로 지정** 후 `AddTodo` | 시안이 열마다 버튼을 두므로 열 상태 계승이 자연스러움. `TaskEditDialog` 시그니처 무변경(TaskEditDialog.xaml.cs:20) |
| D6 | 카드별 테스트 배지(`LinkedTestBadge`) | 범위 | **카드에서 제거**(시안에 없음). 단 `TodoItem.LinkedTestBadge`·`GetLinkedTestStatus`·`MapTestBadge`는 **존치** — FR-T8 요구가 살아 있고 통과율 배지로 대체 표현되므로 로직 삭제는 별건 | 시안 카드에 배지 부재 + PRD FR-T8 존치 |
| D7 | 신규 색 2종 위치 | 위치 | `Resources/Palette.xaml`에 `AppInfo*`(파랑)·`AppWarning*`(주황) 추가 — 기존 `AppSuccess`/`AppDanger` 명명 관례 계승 | Palette.xaml:38-39 관례 |
| D8 | 점선 버튼 구현 | 기술 | `Button` 템플릿 내 `Rectangle StrokeDashArray="3,3"` — WinUI `Border`는 점선 미지원 | 기술 제약(Investigation Log) |
| D9 | 미사용 심볼 | 정리 | `ShowKanban`/`ShowList` RelayCommand는 세그먼트 토글이 소비하거나 제거. `TotalCount`(미바인딩)도 함께 제거 | deferred.md:11 + grep 확인 |
| D10 | 목록 뷰 | 범위 | **이번 변경 없음** — 시안이 칸반만 보여주므로 목록 뷰 시안은 미확보. `TaskCategoryGroup`·기존 카드 템플릿·상태 콤보 전부 유지 | 시안에 목록 뷰 화면 부재 |
| D11 | 카드 템플릿 분리 | 구조 | `TaskCardTemplate`(칸반·목록 공용)을 **분리** — 칸반용 `TaskKanbanCardTemplate` 신규, 기존 템플릿은 이름 그대로 **목록 전용으로 존치**(내용 무변경). 목록 뷰는 `AllowDrop`이 없어 상태 콤보가 유일한 상태 변경 수단이므로 존치가 기능상 필수 | plan-reviewer B1 + TaskPage.xaml:15/239 확인 |
| D12 | FR-T6 이관 | 로직 | 열별 `+새 작업` 핸들러가 `dialog.AddTestRequested` 시 `Vm.CreateLinkedTest(todo)`를 **반드시 호출**한다 — 헤더 버튼 제거로 유일 호출부가 사라지므로 | plan-reviewer B2 + TaskPage.xaml.cs:114-115 확인 |
| D13 | 상태 dot·pill 색 | 위치 | 신규 2색은 `Palette.xaml`의 **`Default` 딕셔너리에만** 추가(Light 딕셔너리 신설 금지 — 다크 단일 정책) | Palette.xaml:10-11 주석 |
| D14 | PRD FR-T8 문구 | 요구 | 카드 배지 → 그룹 통과율 배지로 **요구 자체가 바뀌므로 `docs/prd.md`의 FR-T8(및 FR-E4의 "작업 카드 배지 배선") 문구를 먼저 갱신**한다. ⚠️ **사용자 승인 필요** | plan-reviewer M2 + docs/prd.md:65,71 |

## PRD Coverage

| PRD ID | 우선순위 | 대응 task | 상태 |
|---|---|---|---|
| FR-T1 (작업 데이터 모델: 상태·카테고리·우선순위·날짜) | Must | — | 이번 범위 외 (Phase 2 기구현, `TodoItem` 무변경 — D1) |
| FR-T2 (전체 페이지 + 칸반/목록 전환) | Must | T4 | ✅ 커버(시각 재구성, 기능 기구현) |
| FR-T3 (드래그앤드롭 상태 변경) | Must | T2, T3 | ✅ 커버(기구현 — **회귀 방지**가 acceptance에 포함) |
| FR-T4 (카테고리 필터·그룹핑·상태별 개수) | Must | T3, T4 | ✅ 커버(칸반 내 그룹핑 신규 충족 — T3 그룹핑·개수, T4 헤더 필터 콤보 재배치) |
| FR-T5 (편집 다이얼로그 + 삭제 확인) | Must | T2 | ✅ 커버(진입 경로 변경, 다이얼로그 기구현) |
| FR-T6 (테스트 추가 토글) | Should | T3 | ✅ 커버(**회귀 방지** — 호출부 이관, D12) |
| FR-T8 (연결 테스트 상태 배지) | Should | T1(PRD 갱신), T3 | ✅ 커버(카드 배지 → 그룹 통과율 배지로 **요구 갱신**, D6·D14) |
| FR-E4 (테스트↔작업 연결 배지 배선) | Should | T1(PRD 갱신) | 문구만 갱신 — 구현은 이번 범위 외(기구현 상태 유지) |
| FR-C3·FR-S1~S3·FR-E1~E3·FR-H*·FR-N* | Must/Should | — | 이번 범위 외 (Phase 0~5에서 기구현) |

## 작업 단계

### T1 — 시각 자산 선행 정비 + PRD FR-T8 문구 갱신 `Type C`
- [x] 구현
- **Files**: `DevDashboard_WinUI/Resources/Palette.xaml`, `DevDashboard_WinUI/Resources/Styles.xaml`, `DevDashboard_WinUI/Strings/ko-KR/Resources.resw`, `DevDashboard_WinUI/Strings/en-US/Resources.resw`, `docs/prd.md`
- **Design**: ① 배치 — 색은 `Palette.xaml`의 **`Default` 딕셔너리에만**(D13, Light 신설 금지), 스타일은 `Styles.xaml`, 문자열은 resw ko/en 양쪽. ② 신규 심볼 — `AppInfoColor/Brush`·`AppWarningColor/Brush`(상태 dot·우선순위 pill 공용 색), `DashedAddButtonStyle`(점선 추가 버튼), `SegmentedToggleStyle`(칸반/목록 토글), `KanbanColumnStyle`(열 패널 Border). ③ 의존 방향 — `TaskPage.xaml`이 참조, 역참조 없음. ④ 비추상화 — 우선순위→색, 상태→색 매핑을 **Converter로 추상화하지 않는다**(x:Bind 정적 헬퍼로 직접 반환, `TestPage.StatusBrush` 선례와 동일). 색 토큰도 "미래 테마용" 일반화 없이 필요한 2색만.
- **신규 resw 키(ko/en 양쪽) — ⚠️ 소비 방식별로 name 형식이 다르다**: 이 레포는 두 관례가 공존한다 — `x:Uid` 방식은 **속성 접미 필수**(`TaskAdd_Button.Content`처럼, `Resources.resw:432`), `LocalizationService.Get()` 방식은 **베어네임**(`TaskPage.xaml.cs:23-26`). 접미 없이 등록하고 `x:Uid`로 쓰면 **빌드 오류 없이 빈 라벨**이 된다.
  - 베어네임(코드에서 `LocalizationService.Get()`으로 소비): `TaskDateRange`(`{0} – {1}`), `TaskPassRateBadge`(`✓ 통과율 {0}% · {1}건`) — 둘 다 `string.Format` 대상이라 코드 소비 확정
  - `MenuFlyoutItem`·`Button` 라벨(`x:Uid`로 소비 시 접미 필수): `TaskMenu_Edit.Text`·`TaskMenu_Delete.Text`·`TaskMenu_MoveTo.Text`, `TaskColumnAdd.Content`. **코드에서 소비하기로 하면 베어네임으로 등록** — 어느 쪽이든 T2/T3의 실제 소비 방식과 일치시킨다.
- **PRD 갱신(D14 — 사용자 승인 후)**: `docs/prd.md` FR-T8을 "작업 카드에 연결 테스트 상태 배지" → "**칸반 카테고리 그룹 헤더에 해당 카테고리의 테스트 통과율 배지**"로, FR-E4의 "작업 카드 배지 배선(FR-T8)" 문구를 이에 맞춰 수정. 변경 이유 1줄 기록.
- **Acceptance**: 빌드 성공(경고 0) + 신규 키 6개가 ko/en 양쪽에 동일 name으로 존재(grep 대조) + `Palette.xaml`에 `Default` 외 딕셔너리가 추가되지 않음 + `docs/prd.md` FR-T8·FR-E4 문구가 갱신됨 + 기존 파일 BOM 유무 보존
- **Edge Cases**: resw 한쪽만 추가 시 다른 언어에서 빈 문자열 → 양쪽 대조. `Rectangle StrokeDashArray`는 `Stroke`가 지정돼야 렌더되므로 스타일에 `Stroke` 필수(D8).
- **Halt Forecast**: PRD 문구 변경은 **plan 승인에 포함**(승인 프롬프트에 명시) → 자율 루프 중단 없음.

### T2 — 칸반 전용 카드 템플릿 신규 + 조작 경로(클릭/우클릭) `Type C`
- [x] 구현
- **Files**: `DevDashboard_WinUI/Presentation/Views/TaskPage.xaml`(`UserControl.Resources`), `DevDashboard_WinUI/Presentation/Views/TaskPage.xaml.cs`
- **Design**: ① 배치 — `TaskKanbanCardTemplate`을 `UserControl.Resources`에 **신규 추가**. 기존 `TaskCardTemplate`은 **`DataTemplate` 본문을 고치지 않고 존치**(목록 뷰 전용이 됨, D11) — 따라서 `StatusCombo_Loaded`/`StatusCombo_SelectionChanged`/`EditTask_Click`/`DeleteTask_Click` 핸들러도 **전부 존치**한다. **단 `TaskPage.xaml:14`의 주석 `<!-- 작업 카드 템플릿 (칸반·목록 공용) -->`은 거짓이 되므로 "목록 뷰 전용 카드 템플릿"으로 갱신한다**(CLAUDE.md "코드를 수정하면 딸린 주석을 일치시킨다" — 주석 1줄만 예외, 본문은 무변경). ② 신규 심볼 — `FormatDateRange(DateTime?, DateTime?)`·`DateRangeVisibility(DateTime?, DateTime?)`·`PriorityBrush(TaskPriority)` x:Bind 정적 헬퍼, `Card_Tapped`·`CardMenuEdit_Click`·`CardMenuDelete_Click`·`CardMenuMove_Click` 핸들러(기존 `EditTask_Click`/`DeleteTask_Click` 본문 재사용 — 위임). ③ 의존 방향 — T1 스타일·색 참조. ④ 비추상화 — 컨텍스트 메뉴를 재사용 컴포넌트로 빼지 않고 `MenuFlyout`을 템플릿에 직접 둔다(사용처 1곳).
- **칸반 카드 구성**: 1행 제목(굵게, 줄바꿈, Grid `*`) + 우선순위 pill(Grid `Auto`) / 2행 설명(비면 Collapsed) / 3행 날짜 범위 1줄(양쪽 다 없으면 Collapsed). 상태 콤보·수정/삭제 버튼·카드별 `LinkedTestBadge` 없음(D6). `CanDrag="True"` + `DragStarting="Card_DragStarting"` **유지**(FR-T3).
- **Acceptance**: 빌드 성공 + `TaskCardTemplate`의 `DataTemplate` 본문이 diff에서 무변경(주석 1줄 갱신은 예외) + 신규 템플릿이 정의됨(이 시점엔 아직 미소비 — T3에서 배선) + 우클릭 `MenuFlyout`에 편집·삭제·상태 변경 4종 정의 + 삭제 경로가 기존 확인 다이얼로그(`TaskDelete_Confirm`)를 거침 + **신규 템플릿이 참조하는 `StaticResource` 키명이 T1에서 정의한 키명과 문자열 일치**(grep 대조 — WinUI는 `DataTemplate` 내 `StaticResource`를 인스턴스화 시점에 해석하므로 미소비 상태의 "빌드 성공"이 오타를 잡지 못한다)
- **Edge Cases**: 카드 클릭과 드래그 시작이 충돌 → `Tapped`는 드래그가 성립하면 발생하지 않으므로 우선 `Tapped` 사용, 오작동 시 `PointerReleased`+이동거리 판정으로 조정(⏳ HUMAN-VERIFY). 설명·날짜 모두 없는 카드 → 제목+pill만 있는 1행 카드. **시작일만 또는 종료일만 있는 경우 → `MM-dd – ` 꼴 금지, 있는 쪽만 표시**. 제목이 매우 길 때 → `TextWrapping="Wrap"` + Grid 2열로 pill 우측 고정.
- **Halt Forecast**: 없음(신규 템플릿 추가 + 핸들러 추가만, 기존 경로 무변경 → 이 시점 앱은 현행 그대로 동작).

### T3 — VM 열별 그룹핑·통과율 + 칸반 열 재구성 (VM·XAML 동시) `Type C`
- [ ] 구현
- **Files**: `DevDashboard_WinUI/Presentation/ViewModels/TaskPageViewModel.cs`, `DevDashboard_WinUI/Presentation/Views/TaskPage.xaml`(칸반 본문), `DevDashboard_WinUI/Presentation/Views/TaskPage.xaml.cs`
- **왜 한 task인가**: VM의 컬렉션 요소 타입을 그룹으로 바꾸는 순간 기존 칸반 XAML(`x:DataType="models:TodoItem"`)이 그 컬렉션과 맞지 않아 **빌드는 통과하지만 카드가 렌더되지 않는다**. VM과 칸반 마크업은 분리 불가 — 함께 바꿔야 중간 커밋이 동작 가능 상태로 남는다.
- **Design**: ① 배치 — VM 파일 내 + 칸반 4열 마크업 교체. ② 신규 심볼 — `TaskColumnGroup(string CategoryName, string PassRateBadge, IReadOnlyList<TodoItem> Items)` record(`TaskCategoryGroup` 옆), `BuildPassRateBadge(string category)` private 메서드, `BuildColumnGroups(...)`(기존 `FillColumn` 대체), `TaskColumnGroupTemplate` DataTemplate, `ColumnAdd_Click` 핸들러, `StatusDotBrush(TodoStatus)` x:Bind 정적 헬퍼. 4개 컬렉션은 프로퍼티 이름 유지(`WaitingItems` 등) + 요소 타입만 `TaskColumnGroup`으로 교체. ③ 의존 방향 — `_project.TestCategories`(기로드) 참조, T1 스타일·T2 카드 템플릿 소비. ④ 비추상화 — 통과율 계산을 `TestPageViewModel`과 공용 서비스로 추출하지 **않는다**(사용처 2곳, CLAUDE.md 공통화 기준 미달 + 두 화면의 필터 의미가 달라 조기 결합 위험). 4개 열도 `ItemsControl`로 데이터 구동화하지 않는다(각 열의 `Tag`·드롭 타깃·정적 라벨이 달라 현행처럼 명시 마크업이 추적하기 쉽다).
- **⚠️ `*Count` 재계산 필수**: `WaitingCount = WaitingItems.Count`(현행 `:122-125`)를 그대로 두면 **작업 수가 아니라 카테고리 수**가 표시된다(컴파일 오류 없는 무성 오작동, FR-T4 직결). 개수는 **그룹의 `Items` 합계** 또는 filtered 기준으로 재계산한다.
- **⚠️ FR-T6 이관 필수(D12)**: `ColumnAdd_Click`은 `Vm.AddTodo` 후 **`if (dialog.AddTestRequested) Vm.CreateLinkedTest(todo);`를 반드시 포함**한다 — T4에서 헤더 버튼이 사라지면 이것이 유일한 호출부가 된다.
- **열 구성**: `Border`(패널, `KanbanColumnStyle`) > `StackPanel` > [열 헤더(상태 dot + 상태명 + 개수)] + [`ItemsControl` 카테고리 그룹 → `TaskColumnGroupTemplate` → 내부 `ItemsControl` → `TaskKanbanCardTemplate`] + [점선 `+ 새 작업`]. 드롭 타깃(`AllowDrop`/`Tag`/`DragOver`/`Drop`)은 **패널 Border로 이동**해 빈 영역에도 드롭 가능하게 한다.
- **Acceptance**: 빌드 성공 + 4개 열이 패널로 렌더되고 카드가 표시됨 + 열 헤더에 상태별로 다른 색 dot + 열 안 카테고리 그룹 헤더(dot+이름+개수, 해당 시 통과율 배지) + **열 헤더 개수 = 그 열의 실제 작업 수**(카테고리 수 아님) + 일치 `TestCategory` 없는 카테고리는 배지 미표시 + 점선 버튼이 **그 열 상태로** 작업 생성 + **"테스트 목록에도 추가" 토글이 여전히 테스트를 생성**(FR-T6 회귀 방지) + **드래그앤드롭 열 이동 정상**(FR-T3 회귀 방지)
- **Edge Cases**: 카테고리 빈 문자열 → `TaskCategory_None`("미분류") 그룹(`RebuildCategoryGroups():143` 관례 계승), **미분류 그룹은 통과율 배지 미표시**(D4). 열에 작업 0개 → 그룹 0개(헤더+추가버튼만) + **빈 영역 드롭이 여전히 가능해야 함**(패널 Border 드롭 타깃 + MinHeight). `TestCategories` null → `?? []` 방어(`BuildTestStatusLookup():83` 선례). 테스트 0건 카테고리 → `total==0`이면 배지 숨김(0% 표시 금지). 통과율 배지 + 긴 카테고리명 → 그룹 헤더 Grid(`Auto`/`*`/`Auto`)로 배지 우측 고정. 열 내용 세로 넘침 → 기존 바깥 `ScrollViewer` 유지. **카테고리 필터가 걸린 상태에서 열별 `+새 작업`으로 다른 카테고리 작업을 만들면 즉시 목록에서 사라진다**(`Rebuild()`가 필터 적용, `:113-115`) — 헤더 버튼 시절과 동일한 **기존 동작이므로 유지**(버튼이 열마다 생겨 노출만 커짐).
- **Halt Forecast**: 없음 — 결정이 D4·D12·`*Count` 블록에 전부 사전 확정됐고, 파괴적 작업(파일 삭제·외부 호출·비가역 데이터 변경)이 없다. VM 요소 타입 변경은 사전 승인 항목에 등재됨.

### T4 — 헤더 정리 (세그먼트 토글 + `+새 작업` 제거) + 미사용 심볼 정리 `Type C`
- [ ] 구현
- **Files**: `DevDashboard_WinUI/Presentation/Views/TaskPage.xaml`(헤더), `DevDashboard_WinUI/Presentation/Views/TaskPage.xaml.cs`, `DevDashboard_WinUI/Presentation/ViewModels/TaskPageViewModel.cs`, `DevDashboard_WinUI/Strings/ko-KR/Resources.resw`, `DevDashboard_WinUI/Strings/en-US/Resources.resw`
- **Design**: ① 배치 — 헤더 Grid 열 구성 축소(6열 → 4열: 뒤로가기·제목·spacer·[콤보+토글]) + VM 미사용 심볼 제거. ② 신규 심볼 — 없음(T1의 `SegmentedToggleStyle` 적용). ③ 의존 방향 — T1 스타일 소비. ④ 비추상화 — 커스텀 `SegmentedControl`을 만들지 않고 `RadioButton` 2개 + 스타일로 처리(사용처 1곳).
- **선행 조건**: T3에서 `ColumnAdd_Click`의 FR-T6 이관(D12)이 완료된 뒤에만 헤더 `AddTask_Click`을 제거한다 — 순서가 뒤집히면 FR-T6가 죽는 구간이 생긴다.
- **D9 처리**: 세그먼트 토글이 `ShowKanbanCommand`/`ShowListCommand`를 실제 바인딩해 소비하거나, `IsChecked` TwoWay가 더 단순하면 두 커맨드를 **제거**한다(둘 중 하나 — 미사용 잔존 금지). 미바인딩 `TotalCount` 프로퍼티도 함께 제거. 선택 결과를 완료 보고에 명시.
- **Acceptance**: 빌드 성공 + 헤더에 카테고리 콤보와 칸반/목록 세그먼트 토글만 존재(`+새 작업` 부재) + 토글로 칸반↔목록 전환 동작 + `TaskPageViewModel`에 미사용 RelayCommand·미사용 `TotalCount` 0개(grep 대조) + `TaskAdd_Button` 키가 ko/en 양쪽에서 제거되고 잔존 참조 0건(grep 대조) + **목록 뷰가 기존과 동일하게 동작**(카드 상태 콤보·수정/삭제 버튼 그대로, D10/D11)
- **Edge Cases**: `TaskAdd_Button` resw 키가 고아가 됨 → 열 버튼(`TaskColumnAdd`)이 즉시 대체하므로 **키 제거**(고아 대장 등재보다 즉시 정리가 맞다). 사용처는 `TaskPage.xaml:154` 단 1곳이라 안전(grep 실측). 목록 뷰 전환 시 기존 마크업 그대로 동작(D10).
- **Halt Forecast**: 없음 — `TaskAdd_Button` 키 제거·미사용 심볼(`ShowKanban`/`ShowList`/`TotalCount`) 제거는 **사전 승인 항목에 등재**돼 있어 중단 없이 진행한다. 그 외 파괴적·외부 작업 없음.

## 사전 승인 항목 (일괄 승인 대상)
- **`docs/prd.md`의 FR-T8·FR-E4 문구 갱신**(D14) — 카드 배지 → 그룹 통과율 배지로 **요구 자체가 바뀐다**. 이 plan 승인이 PRD 문구 변경 승인을 포함한다.
- `Resources/Palette.xaml`의 `Default` 딕셔너리에 색 2종(`AppInfo*`·`AppWarning*`) 추가 — 기존 키 무변경, 추가만.
- `Resources/Styles.xaml`에 스타일 4종 추가 — 기존 스타일 무변경, 추가만.
- resw(ko/en)에 키 6개 추가 + `TaskAdd_Button` 키 1개 제거(T4 Edge Case 근거).
- `TaskPageViewModel`의 칸반 4개 컬렉션 **요소 타입 변경**(`TodoItem` → `TaskColumnGroup`) — 사용처가 같은 plan 내 `TaskPage.xaml` 1곳뿐(전수 확인 완료).
- `TaskPageViewModel`의 미사용 `ShowKanban`/`ShowList` RelayCommand·`TotalCount` 소비 또는 제거(D9).
- 로컬 작업 브랜치 `task/taskpage-design-align` 생성 및 task별 commit.

## 불가피한 Halt (위임 불가)
- master 병합·push·태그·릴리즈·PR.
- 시안 대조의 **최종 시각 판정** — 빌드는 구조 구현만 보증하고 "시안과 같아 보이는가"는 사용자만 판정 가능(⏳ HUMAN-VERIFY로 보고).

## Deferred / Follow-up
- **목록 뷰 시안 대조** — 시안 이미지가 칸반만 담고 있어 목록 뷰는 이번에 무변경(D10). 목록 뷰 시안 확보 시 별도 진행.
- **TestPage·NotificationPage 시안 대조** — 사용자가 "작업 페이지만 먼저"로 한정. 작업 페이지 결과 확인 후 판단.
- **[칸반 열 내 카드 재정렬]** — 대장 유지(이번은 시각 구조만).
- **[FR-T7 담당자]** — 대장 유지(사용자 제외 결정 재확인, D1).
- **AGENTS.md 부재** — 5개 Phase 내내 없음. 빌드 명령·구조가 메모리·plan에만 있어 PC 간 공유가 안 된다. `pjc:bootstrap-agents-md`로 생성 제안(완료 보고에서).
- 기타 대장(`docs/plans/deferred.md`) 기등재 항목은 이번 작업과 무관 — 그대로 유지.

## Out of Scope
- `TodoItem` 도메인·SQLite 스키마·직렬화 변경(담당자 제외 결정의 귀결, D1).
- **목록 뷰 레이아웃 변경**(D10) — 기존 `TaskCardTemplate`을 목록 전용으로 존치해 실제로 무변경을 보장한다(D11). 목록 카드의 상태 콤보·수정/삭제 버튼도 그대로 유지(목록엔 드래그 경로가 없어 콤보가 유일한 상태 변경 수단).
- 대시보드 하단 상태바·헤더 등 TaskPage 밖 화면.
- 픽셀 단위 수치 일치 — 목업 HTML이 없어 대조 불가. 구조·구성 요소 일치까지가 이번 목표.

## Open Questions
- [x] 담당자 필드 포함 여부 → **제외 유지**(사용자, 2026-07-20)
- [x] 카드 조작 방식 → **클릭=편집 / 우클릭 메뉴=편집·삭제·상태변경**(사용자, 2026-07-20)
- [x] 작업 범위 → **TaskPage만**(사용자, 2026-07-20)
- [x] 통과율 배지 집계 정의 → **같은 이름의 TestCategory 기준, 없으면 숨김**(사용자, 2026-07-20)

## 검증 방법
- 빌드: `MSBuild.exe "DevDashboard_WinUI/DevDashboard.csproj" -t:Build -p:Configuration=Debug -p:Platform=x64` → 오류 0·경고 0
- resw 대조: ko/en 신규 키 name 집합 일치 grep + `TaskAdd_Button` 잔존 참조 0건 grep
- 미사용 심볼: `TaskPageViewModel`의 RelayCommand·`TotalCount` 사용처 grep
- 회귀 방지 grep(⚠️ **판정 대상을 호출부로 한정** — `CreateLinkedTest`만 검색하면 정의부 `TaskPageViewModel.cs:182`가 잡혀 이관이 누락돼도 통과한다):
  - `Vm.CreateLinkedTest(` 를 **`Presentation/Views/TaskPage.xaml.cs` 범위에서** grep → **1건 이상**(FR-T6 생존, D12)
  - `TaskCardTemplate`의 `DataTemplate` 본문이 diff에서 무변경(D11 — 주석 1줄 갱신은 예외)
- 회귀(빌드로 검증 불가 → ⏳ HUMAN-VERIFY): 드래그 열 이동 / 빈 열 드롭 / 카드 클릭 편집 / 우클릭 메뉴 / 열별 추가 버튼의 상태 계승 / "테스트 목록에도 추가" 토글 / 완료 전환 시 작업기록 팝업 / 카테고리 필터 / 열 헤더 개수 정확성 / **목록 뷰가 이전과 동일** / 시안 육안 대조

## Phase Ledger
- (implement-task가 갱신)

## Progress Log
- (implement-task가 갱신)
