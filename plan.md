# plan.md — 작업 화면(TaskPage) 목록 보기 시안 정합 재구현

**PRD**: docs/prd.md (FR-T2 칸반/목록 뷰 전환 / FR-T4 카테고리 그룹핑·개수 / FR-T3 드래그 상태 변경 / FR-T8 카테고리 통과율 배지 / FR-E4 작업↔테스트 연결 배지)
**기준 디자인**: `docs/DevDashboard WinUI/DevDashboard Redesign.dc.html` — `data-screen-label="작업 칸반 페이지"`의 `taskViewList` 분기(143~187줄) + 데이터 소스 `taskListGroups`(1405~1440줄)·`colDefs`(1198줄)·`catColors`(1199줄)·`priStyles`(1200~1204줄)

## 요구 이해

> 원문(사용자, 2026-07-22): "디자인 파일 확인해서 작업 화면의 목록보기 화면 구현해"

이해한 요구:
- 작업 화면(TaskPage)의 **목록 보기(칸반/목록 토글의 "목록" 쪽)** 를 시안 원본 HTML의 `taskViewList` 분기대로 재구현한다. 칸반 뷰·헤더(뒤로가기·제목·카테고리 필터·뷰 토글)는 이번 대상이 아니다(단 목록과 **색을 공유**하는 상태 dot·우선순위 배지는 시안 정정 대상 — 사용자 결정).
- 시안 목록 구조는 **2단 그룹**이다: 바깥이 **상태 그룹 4개**(예정/진행 중/완료/보류 — 상태 dot + 이름 + 개수 + 가로 구분선 + "＋ 새 작업" 버튼), 안쪽이 **카테고리 서브그룹**(카테고리 dot + 이름 + 개수 + 테스트 통과율 배지 — 칸반 그룹 헤더와 동일 구성). 현행은 **카테고리 1단 그룹**이라 구조부터 다르다.
- 작업 항목은 **가로 1줄 행**이다(제목 + 설명 1줄 / 날짜 범위 / 우선순위 배지). 현행은 세로 카드에 상태 콤보·수정·삭제 버튼이 달려 있어 시안과 다르다.
- 항목이 없는 상태 그룹은 **"작업 없음" 점선 박스**를 표시한다(그룹 자체는 항상 4개 보인다).
- 상태 변경 수단은 우클릭 메뉴 + **행을 다른 상태 그룹으로 드래그**(사용자 결정), 행 왼클릭은 편집 다이얼로그(사용자 결정).

## Goal

`TaskPage`의 목록 뷰를 시안(`taskViewList`)과 같은 **상태 그룹 → 카테고리 서브그룹 → 가로 행** 구조로 재구현하고, 상태 콤보·수정/삭제 버튼 대신 칸반과 동일한 조작 경로(클릭 편집·우클릭 메뉴·드래그 상태 이동)를 제공한다. 두 뷰가 공유하는 상태 dot·우선순위 배지 색도 시안 값으로 맞춘다.

## Investigation Log (근거)

### 문서·대장
- **위키 참조**: vault 미설정 — 코드 1차 출처로 진행.
- **AGENTS.md 신선도 점검**: 이번에 참조하는 경로(`Presentation/Views/TaskPage.xaml(.cs)`·`Presentation/ViewModels/TaskPageViewModel.cs`·`Resources/{Styles,Palette}.xaml`·`Strings/{ko-KR,en-US}/Resources.resw`·`docs/prd.md`·`docs/plans/deferred.md`) 전부 실재. 빌드 명령의 MSBuild 경로·`DevDashboard.csproj`도 실재. 어긋남 0건.
  - 단 대장 항목 `[AGENTS.md는 git 미추적 — PC 간 미공유]`는 **stale 확인**: `git check-ignore -v AGENTS.md`가 무매치이고 2026-07-22에 정상 커밋됐다. 대장 정정은 이번 코드 범위 밖 — Deferred로 이관.
  - 관련 함정 직결: **2**(resw ko/en 양쪽)·**3**(resw 키 형식)·**5**(x:Bind 함수 바인딩은 Brush/Visibility 직접 반환)·**6**(Border 점선 미지원 → Rectangle)·**11**(공용 DataTemplate 소비처 전수 확인) — 아래 반영.
  - **함정 5 관련 사실 확인(리뷰 검증)**: `DataTemplate`의 `x:DataType`이 VM 타입이어도 `local:TaskPage.<정적메서드>(...)` 함수 바인딩은 성립한다 — `TaskColumnGroupTemplate`(`TaskPage.xaml:147,159,160`, `x:DataType="vm:TaskColumnGroup"`)에 이미 선례가 있다. `x:Uid`도 `DataTemplate` 안에서 동작한다(`DashboardView.xaml:231,536`의 `ProjectCardTemplate`/`AddCardTemplate` 선례).
- **Deferred 대장 확인**(`docs/plans/deferred.md`):
  - `[목록 뷰 시안 대조]` — **이번 작업이 이 항목의 재수용**이다(시안 원본 확보로 조건 충족). 대장에서 `## 종결`로 이동 완료(2026-07-22).
  - `[칸반/목록 "미분류" 그룹 정렬 불일치]`(F-7 리뷰 m2) — 칸반은 raw key 정렬(미분류=빈 문자열이 최상단), 목록은 표시명 정렬. 이번에 목록이 칸반과 **같은 `BuildColumnGroups`를 재사용**하므로 자동 해소된다 → 완료 시 대장 종결 대상(D2).
  - `[TaskPageViewModel.*Items 개명 검토]`(m4) — 이번에도 이름 유지(XAML 바인딩 경로 안정). 대장 유지.
  - `[완료 열 즉시 생성 시 작업기록 팝업 미발생]` — 목록 그룹 헤더의 "＋ 새 작업"도 같은 `ColumnAdd_Click`을 쓰므로 **같은 성질이 목록에도 확장**된다(신규 결함 아님). 대장 유지.
  - `[Todo* resw 고아 정리]` — T2로 고아가 되는 `TaskLabel_Start`·`TaskLabel_End`를 이 항목에 합류시킨다(정리 자체는 이번 범위 밖 — D13).
  - `[카테고리 필터에서 "미분류"를 고를 수 없음]`·`[BuildPassRateBadge 조회 테이블화]`·`[FR-T7 담당자(who) 필드]` — 무관·유지.
  - 그 외 항목은 이번 diff와 무관.
- **PRD 경량 확인**: 이번 변경은 **FR-T2**(목록 뷰)·**FR-T4**(카테고리 그룹핑·상태별 개수)·**FR-T3**(드래그 상태 변경 — 목록으로 확장)·**FR-T8**(카테고리 그룹 통과율 배지 — 목록 서브그룹에도 표시)에 닿고, **FR-E4**(작업↔테스트 연결 배지)의 UI 표현이 이번 구조 교체로 사라진다(D12) → PRD 연결(Phase G 활성).

### 현행 구현 (직접 Read)

**`TaskPage.xaml`**(453줄)
- `TaskKanbanCardTemplate`(:19-106): 칸반 카드. 우클릭 `MenuFlyout`(편집/상태 변경 4개/삭제, :30-58) + `Tapped="Card_Tapped"`(클릭 편집) + `CanDrag="True"`. **목록 행의 조작 경로가 그대로 참고할 선례.**
- `TaskColumnGroupTemplate`(:109-170): 칸반 열 안의 카테고리 그룹 — dot(`TagColor` 컨버터) + 이름 + 개수 + **테스트 배지 2종**(미실행 테두리형 `IdleTestBadgeVisibility` / 실행 채움형 `RanTestBadgeVisibility`). 안쪽에서 `TaskKanbanCardTemplate`을 **하드코딩 참조**(:168) → 목록은 행 템플릿이 달라 이 템플릿을 그대로 못 쓴다(목록용 카테고리 템플릿 신규 필요).
- `TaskCardTemplate`(:173-273): **목록 뷰 전용 세로 카드** — 카테고리 dot·이름·우선순위 텍스트·`LinkedTestBadge`(:203-215) / 제목 / 시작일·종료일 2줄(:227-236) / **상태 ComboBox + 수정·삭제 버튼**(:239-270). 시안과 구조가 전혀 다름 → **제거 대상**.
- 목록 뷰 본문(:437-450): `ScrollViewer` → `ItemsControl ItemsSource={x:Bind Vm.CategoryGroups}`(:440) → 인라인 `DataTemplate x:DataType="vm:TaskCategoryGroup"`(:442) + `ItemTemplate={StaticResource TaskCardTemplate}`(:445). `MaxWidth="520"` 좌측 정렬. → **전면 교체 대상**. **`x:Bind`는 컴파일 타임 바인딩이라 VM 멤버를 지우면서 이 마크업을 남기면 XamlCompiler 오류가 난다** → VM 멤버 제거와 마크업 교체는 **같은 task**여야 한다(T2, 리뷰 B1).
- 칸반 열(:367-432): 4개 `Border Style=KanbanColumnStyle` + `AllowDrop="True" Tag="Waiting|Active|Completed|Hold" DragOver="Column_DragOver" Drop="Column_Drop"` + 하단 `Button Style=DashedAddButtonStyle Tag=<상태> Click="ColumnAdd_Click"`. **드롭 대상·새 작업 버튼의 배선 방식을 목록이 그대로 재사용**할 수 있다(핸들러가 `Tag: string` → `Enum.TryParse<TodoStatus>` 방식이라 요소 종류에 의존하지 않음 — `TaskPage.xaml.cs:205-206`, `:314-315`).
- **소비처 전수**(grep, `obj/`·`bin/` 제외): `TaskCardTemplate`·`TaskCategoryGroup`·`CategoryGroups`·`StatusCombo_Loaded`·`StatusCombo_SelectionChanged`·`EditTask_Click`·`DeleteTask_Click`·`TaskStatusOption`·`StatusOptions` 모두 **`TaskPage.xaml`·`TaskPage.xaml.cs`·`TaskPageViewModel.cs` 3파일 안에서만** 참조된다(hit 25건 전건 확인). 다른 화면 소비처 0 — 함정 11 리스크 없음.
- **`TaskCardTemplate` 제거로 함께 고아가 되는 심볼**(리뷰 M4 지적으로 재조사, grep 전수):
  - `FormatStart`(:228)·`FormatEnd`(:233)·`DateVisibility`(:231,:236) — **유일 소비처가 이 템플릿** → 함께 제거(목록 행은 `FormatDateRange`/`DateRangeVisibility`를 쓴다).
  - resw `TaskLabel_Start`·`TaskLabel_End`(ko/en :444-445) — 위 두 헬퍼가 유일 소비처 → 고아화(삭제는 대장 합류, D13).
  - `TodoItem.LinkedTestBadge`(:210,:212) — **UI 소비처가 이 템플릿뿐**. 제거하면 도메인 속성(`TodoItem.cs:52-54`, 표시 전용·비영속)과 VM의 `MapTestBadge`/`GetLinkedTestStatus`/`Rebuild`의 대입(`TaskPageViewModel.cs:91-106`)이 **화면에 닿지 않는 계산**이 된다 → 계산 경로는 유지하고 PRD 영향을 명시(D12).

**`TaskPage.xaml.cs`**(338줄)
- 상태 dot 정적 브러시 `_statusDotWaiting`~`_statusDotHold`(:39-42)와 공개 프로퍼티 `StatusDotWaiting`~`StatusDotHold`(:44-47) — 칸반 열 헤더가 소비. **파일 내부 private static이라 다른 화면과 공유되지 않는다**(grep 전수 — TaskPage 전용). 현재 값 예정 `#F0716A`·진행 중 `#5B93D8`·완료 `#5DB463`·보류 `#D9954A`는 **시안(`colDefs` :1198)의 `#8a8890`·`#5aa3e8`·`#5db463`·`#e8925a`와 다르다**(예정은 회색 vs 코랄로 확연히 다름) → T4에서 시안 값으로 정정(사용자 결정).
- 라벨 정적 프로퍼티 `LabelWaiting/Active/Completed/Hold`(:50-53), `ColumnAddText`(:31) 존재 — 목록 그룹 헤더는 그룹마다 다른 라벨이 필요하므로 **그룹 데이터에 라벨을 담는다**(D3).
- 우선순위 브러시 `_priorityHigh/Normal/Low(+Soft)`(:126-131) — 칸반 카드와 목록 행이 공유(TaskPage 전용). 현재 값이 시안 `priStyles`(:1200-1204)와 다름(높음 `#D9954A` vs `#e8b45a`, 보통 `#5B93D8` vs `#7ab5ec`, 낮음 배경 반투명 `#286F6D75` vs 불투명 `#2b2b31`) → T4에서 정정(사용자 결정).
- `PriorityText`(:90)·`FormatDateRange`/`DateRangeVisibility`(:107-118)·`IdleTestBadgeVisibility`/`RanTestBadgeVisibility`/`TestBadgeBackground`/`TestBadgeForeground`(:158-171) — 목록에서 **그대로 재사용**.
- `StatusCombo_Loaded`/`StatusCombo_SelectionChanged`(:186-198), `EditTask_Click`/`DeleteTask_Click`(:238-248), `StatusOptions`(:56-62), `TaskStatusOption` record(:335-338), `EditTooltip`/`DeleteTooltip`(:22-23) — **전부 `TaskCardTemplate` 전용** → 함께 제거(T2).
  - ⚠️ `EditTooltip`/`DeleteTooltip`은 **동명의 별개 심볼이 레포 곳곳에 있다**(`TestPage.xaml.cs:60-61`, `HistoryDialogViewModel.cs:68,71`, `ToolItemViewModel.cs:133` 등) → 회귀 grep은 **`TaskPage` 두 파일로 범위를 한정**한다(리뷰 M3).
- `EditTodoAsync`/`DeleteTodoAsync`(:221-236), `CardMenuEdit_Click`·`CardMenuDelete_Click`·`CardMenuMove*_Click`(:260-282) — 칸반 우클릭 메뉴 공용 경로. **목록 행 메뉴도 같은 핸들러를 그대로 재사용**(Tag에 `TodoItem`을 넣는 계약 동일).
- `Card_DragStarting`/`Column_DragOver`/`Column_Drop`(:289-320) — 드래그 소스는 `DataContext: TodoItem`, 드롭 대상은 `Tag: string` 상태. **목록 행·상태 그룹에 그대로 배선 가능**(요소 종류 무관).

**`TaskPageViewModel.cs`**(304줄)
- `BuildColumnGroups(column, source, status)`(:127-146): 한 상태의 작업을 카테고리로 묶어 `TaskColumnGroup`(이름·배지 텍스트·`HasTestResult`·`IsFullPass`·항목)을 만든다. `OrderBy(g.Key, CurrentCulture)` raw key 정렬(빈 카테고리=미분류가 최상단), 항목은 `CreatedAt` 내림차순. **목록 카테고리 서브그룹이 필요로 하는 데이터와 정확히 일치** → 재사용.
- `Rebuild()`(:102-124): 필터 적용 → 칸반 4열 구성 → 개수 합산 → `RebuildCategoryGroups(filtered)`. 목록 몫만 교체하면 된다.
- `RebuildCategoryGroups`(:175-184) + `CategoryGroups`(:39) + `TaskCategoryGroup` record(:297) — **목록 1단 그룹 전용** → 제거 대상(T2 — XAML 소비처와 동시에).
- `WaitingItems`/`ActiveItems`/`CompletedItems`/`HoldItems`(:33-36)는 실제로는 `TaskColumnGroup` 컬렉션(이름과 내용 불일치 — 대장 m4). 이번엔 이름 유지.
- `MoveToStatus`(:187-199)가 상태 변경·저장·작업기록 훅을 모두 담당 — 목록 드래그도 이 경로를 그대로 탄다(신규 로직 0).

**`Resources/Styles.xaml`**
- `TagBadgeStyle`(:189-193, CornerRadius 4·Padding 8,3) — 우선순위 배지가 이미 사용 중, 목록 행도 동일 사용(시안 radius 6·padding 9,3은 소비 지점에서 덮어쓴다 — 공용 스타일 무변경).
- `KanbanColumnStyle`(:217-224) — 칸반 열 전용(목록 그룹은 카드 배경이 없어 미사용).
- `DashedAddButtonStyle`(:232-283) — **점선 테두리 구현 선례**(함정 6: Border는 점선 불가 → 템플릿 안 `Rectangle` + `StrokeDashArray="3,3"`). "작업 없음" 점선 박스가 이 방식을 따른다(스타일 자체는 미재사용 — 4-D 참조).
- `SeparatorStyle`(:196-199, `DividerStrokeColorDefaultBrush` #26262C, Height 1) — 시안의 그룹 헤더 가로 구분선(#26262c)과 **색·두께가 정확히 일치** → 재사용.

**`Resources/Palette.xaml`**(다크 `Default` 단일 — 함정 4)
- 시안 색 ↔ 팔레트 대응: 행 배경 `#1f1f24` ↔ `AppCardBrush` **#1C1C20**(근사) / 행 테두리 `#27272d` ↔ `AppBorderBrush` **#26262C**(근사) / hover 테두리 `#3d3d45` ↔ `ControlStrokeColorSecondaryBrush` **#35353C**(근사) / 점선 박스 테두리 `#35353c` ↔ **`ControlStrokeColorSecondaryBrush`**(정확 일치) / 빈 상태 글자 `#5f5d66` ↔ `TextFillColorDisabledBrush`(정확) / 구분선 `#26262c` ↔ `DividerStrokeColorDefaultBrush`(정확) / 버튼 배경 `#1d1d21` ↔ `AppInputBrush`(정확) / 버튼 테두리 `#2b2b31` ↔ `ControlStrokeColorDefaultBrush`(정확) / 버튼 hover 배경 `#222226` ↔ `ControlFillColorSecondaryBrush`(정확) / 보조 글자 `#8a8890` ↔ `TextFillColorTertiaryBrush`(정확) / 카테고리명 `#c9c6ce` ↔ `TextFillColorSecondaryBrush` **#B3B0B8**(근사) / 설명 `#98959d` ↔ `TextFillColorTertiaryBrush` **#8A8890**(근사).
- ⚠️ **`#35353C`는 `AppBorderStrongerColor`라는 `Color`로만 존재하고 대응 `SolidColorBrush`가 없다**(리뷰 M1) — 브러시가 필요한 자리(`Rectangle.Stroke`)에는 같은 값의 **`ControlStrokeColorSecondaryBrush`**(:128)를 쓴다. 브러시 키를 새로 만들지 않는다.
- **팔레트 신규 색은 추가하지 않는다**(D5).

**resw 현황**(ko/en 대칭 확인): `TaskStatus_Waiting/Active/Completed/Hold`·`TaskCategory_None`·`TaskColumnAdd`("+ 새 작업")·`TaskPassRateBadge`·`TaskNoTestBadge`·`TaskPriority_*`·`TaskDateRange`·`TaskMenu_*` 존재. **신규 필요: 빈 상태 문구 1개**(`TaskList_Empty.Text`).

### 4-D. 재사용 확인

| 신규 심볼 | 유사 기존 구현 검색 결과 | 재사용/신규 사유 |
|---|---|---|
| `TaskListStatusGroup`(record) | `TaskColumnGroup`(카테고리 단위)·`TaskCategoryGroup`(제거 대상) — **상태 단위 그룹 타입은 없음** | **신규** — 상태 라벨·개수·빈 여부·카테고리 서브그룹 목록을 한 항목으로 묶어야 `ItemsControl` 하나로 4개 그룹을 돌릴 수 있다(칸반은 XAML에 4열을 하드코딩했지만 목록은 세로 반복이라 데이터 주도가 맞다) |
| `TaskPage.StatusDotBrush(TodoStatus)` | `StatusDotWaiting/Active/Completed/Hold` 정적 프로퍼티 4개(`TaskPage.xaml.cs:44-47`) | **재사용 + 얇은 신규** — 기존 브러시 4개를 그대로 반환하는 switch 헬퍼만 추가(색 정의 중복 없음) |
| `TaskPage.StatusTag(TodoStatus)` | 칸반 열의 `Tag="Waiting"` 등 XAML 리터럴 | **신규(얇음)** — 데이터 주도 그룹은 리터럴을 쓸 수 없어 enum → 문자열 변환이 필요. 기존 `ColumnAdd_Click`/`Column_Drop`의 `Tag: string` 계약을 그대로 만족시킨다 |
| `TaskPage.EmptyGroupVisibility(bool)`·`CategoriesVisibility(bool)` | `DateRangeVisibility`·`IdleTestBadgeVisibility` 등 다수 선례 | **신규(선례 재현)** — 함정 5로 `Visibility` 직접 반환 |
| 목록 행 hover(테두리 밝아짐) | `TestPage.Row_PointerEntered/Exited` 선례(배경 토글), `FilterTabStyle`·`DashedAddButtonStyle`의 VSM 방식 | **신규(선례 재현)** — `DataTemplate` 안에서는 VSM `GoToState`가 동작하지 않아 `PointerEntered`/`PointerExited` 핸들러로 처리 |
| "작업 없음" 점선 박스 | `DashedAddButtonStyle`(Button 템플릿 안 `Rectangle` + `StrokeDashArray`) | **패턴 재사용, 스타일 미재사용** — 클릭 대상이 아니라 표시 전용이므로 Button 스타일을 쓰면 불필요한 상호작용(포커스·hover·클릭)이 붙는다. 같은 방식을 인라인으로 그린다 |
| 행 클릭·우클릭·드래그 배선 | `Card_Tapped`·`CardMenu*_Click`·`Card_DragStarting`·`Column_DragOver`·`Column_Drop`(전부 기존) | **전부 재사용(신규 0)** — 핸들러가 `DataContext: TodoItem` / `Tag: TodoItem` / `Tag: string` 계약만 보므로 목록 요소에 그대로 배선된다 |
| 상태 그룹 "＋ 새 작업" | `ColumnAdd_Click`(기존, `Tag: string` → `TodoStatus`) | **재사용(신규 0)** |
| 우선순위·날짜·테스트 배지 표시 | `PriorityText`/`PriorityBrush`/`PriorityBadgeBrush`/`FormatDateRange`/`DateRangeVisibility`/`IdleTestBadgeVisibility`/`RanTestBadgeVisibility`/`TestBadgeBackground`/`TestBadgeForeground`(전부 기존) | **전부 재사용(신규 0)** |

## 시각 요소 분해

> 기준: 시안 원본 HTML(`taskViewList` 143~187줄 + `taskListGroups` 1405~1440줄 + `colDefs`/`priStyles`)의 **인라인 스타일 값**. `sc-if`/`{{ }}` 템플릿 문법이라 브라우저 렌더 캡처는 확보하지 못했다 — **모든 항목이 HTML 소스 판독이며 렌더 미확인**이다. 최종 판정은 빌드 후 사용자 육안 대조(⏳ HUMAN-VERIFY). px는 WinUI 논리 단위로 그대로 옮긴다.

| 요소 | 속성 | 디자인 값 | XAML 대응 수단 | 확인 방법 |
|---|---|---|---|---|
| 목록 컨테이너 | padding | `18px 24px 24px` | `Padding="24,18,24,24"` | HTML 소스 :144 |
| 목록 컨테이너 | 그룹 간격 | `gap:22px` (세로) | `ItemsPanel StackPanel Spacing="22"` | HTML 소스 :144 |
| 상태 그룹 | 내부 간격 | `gap:12px` (세로) | `StackPanel Spacing="12"` | HTML 소스 :146 |
| 그룹 헤더 | 배치·간격 | 가로, `gap:9px`, 세로 중앙 | `Grid ColumnSpacing="9"` + `VerticalAlignment="Center"` | HTML 소스 :147 |
| 그룹 헤더 dot | 크기·모양 | `9×9`, `border-radius:3px` (**라운드 사각형**) | `Border Width=9 Height=9 CornerRadius=3` | HTML 소스 :1419 |
| 그룹 헤더 dot | 색 | 예정 `#8a8890` / 진행 중 `#5aa3e8` / 완료 `#5db463` / 보류 `#e8925a` | **T4에서 기존 정적 브러시 4종을 이 값으로 정정**(현재 `#F0716A`·`#5B93D8`·`#5DB463`·`#D9954A` — D11) | HTML 소스 :1198 |
| 그룹 헤더 이름 | 폰트 | `14.5px`, `weight 700` | `FontSize="14.5" FontWeight="Bold"` | HTML 소스 :149 |
| 그룹 헤더 개수 | 폰트·색 | `12px`, `#8a8890`, 등폭 | `FontSize="12" Foreground=TextFillColorTertiaryBrush FontFamily="Consolas"` | HTML 소스 :150 |
| 그룹 헤더 구분선 | 형태 | 남는 폭을 채우는 `height:1px` `#26262c` | `Rectangle Style=SeparatorStyle` in `Width="*"` 열 | HTML 소스 :151 |
| 그룹 헤더 버튼 | 크기·색 | `height:28px`, `padding:0 11px`, `radius:7px`, 테두리 `#2b2b31`, 배경 `#1d1d21`, 글자 `#8a8890` `12px` | `Button Height=28 Padding="11,0" CornerRadius=7` + `AppInputBrush`/`ControlStrokeColorDefaultBrush`/`TextFillColorTertiaryBrush` | HTML 소스 :152 |
| 그룹 헤더 버튼 | hover | 글자 `#e8e6e3`, 배경 `#222226` | 기본 `Button` 스타일의 `PointerOver`(팔레트 `ButtonBackgroundPointerOver`=`#2B2B31`) — **근사, 별도 템플릿 미작성** | HTML 소스 :152 `style-hover` |
| 그룹 헤더 버튼 | 문구 | `＋ 새 작업` | 기존 resw `TaskColumnAdd`("+ 새 작업") | HTML 소스 :152 |
| 빈 그룹 박스 | 형태 | `1px dashed #35353c`, `radius:10px`, `padding:18px 0`, 가운데 정렬 | `Rectangle StrokeDashArray="3,3" RadiusX/Y=10 Stroke=ControlStrokeColorSecondaryBrush` + 겹친 TextBlock (함정 6) | HTML 소스 :155 |
| 빈 그룹 박스 | 문구·색 | "작업 없음", `12.5px`, `#5f5d66` | 신규 resw `TaskList_Empty.Text` + `TextFillColorDisabledBrush` | HTML 소스 :155 |
| 카테고리 그룹 | 들여쓰기·간격 | `padding-left:4px`, `gap:7px` (세로) | `StackPanel Margin="4,0,0,0" Spacing="7"` | HTML 소스 :158 |
| 카테고리 헤더 | 배치·간격·줄바꿈 | 가로, `gap:7px`, **`flex-wrap:wrap`** | `StackPanel Orientation=Horizontal Spacing=7` (**줄바꿈 미지원 — 시안과 차이, D14**) | HTML 소스 :159 |
| 카테고리 dot | 크기·모양 | `8×8`, `radius:3px` (**라운드 사각형**) | `Border Width=8 Height=8 CornerRadius=3` + `TagColor` 컨버터 | HTML 소스 :1430 |
| 카테고리 이름 | 폰트·색 | `12.5px`, `weight 700`, `#c9c6ce` | `FontSize=12.5 FontWeight=Bold Foreground=TextFillColorSecondaryBrush` | HTML 소스 :161 |
| 카테고리 개수 | 폰트·색 | `11.5px`, `#8a8890`, 등폭 | `FontSize=11.5 Foreground=TextFillColorTertiaryBrush` | HTML 소스 :162 |
| 카테고리 순서 | 정렬 | 카테고리 **선언 순서**(`catOrder`) | **이름순 유지**(칸반과 동일 — 사용자 결정, D2) | HTML 소스 :1406 |
| 테스트 배지(미실행) | 형태 | `11px` `#8a8890`, 테두리 `1px #35353c`, `radius:6px`, `padding:2px 7px`, 보통 굵기 | 칸반 `TaskColumnGroupTemplate`의 테두리형 배지와 **동일 마크업** | HTML 소스 :164, :1434 |
| 테스트 배지(실행) | 형태 | `11px` `weight 700`, `radius:6px`, `padding:2px 7px`, 100%면 초록(`#5db463`/16%) 아니면 호박(`#e8b45a`/16%) | 칸반의 채움형 배지와 **동일 마크업**(기존 헬퍼 재사용) | HTML 소스 :1435 |
| 작업 행 | 배치 | 가로 1줄, `gap:12px`, 세로 중앙 | `Grid ColumnSpacing="12"` (열: `*` / Auto / Auto) | HTML 소스 :171, :1411 |
| 작업 행 | 배경·테두리 | 배경 `#1f1f24`, 테두리 `1px #27272d`, `radius:10px` | `Border Background=AppCardBrush BorderBrush=AppBorderBrush CornerRadius=10` | HTML 소스 :1411 |
| 작업 행 | padding | `11px 14px` | `Padding="14,11"` | HTML 소스 :1411 |
| 작업 행 | 행 간격 | `gap:7px` (카테고리 그룹 내부) | `ItemsPanel StackPanel Spacing="7"` | HTML 소스 :158 |
| 작업 행 | hover | 테두리 `#3d3d45`로 밝아짐 | `PointerEntered/Exited` 핸들러로 `BorderBrush` 토글 | HTML 소스 :171 `style-hover` |
| 작업 행 | 드래그 중 | `opacity:0.45` | **미구현**(D6 — WinUI 기본 드래그 비주얼 사용) | HTML 소스 :1411 |
| 행 제목 | 폰트·말줄임 | `13.5px` `weight 700`, 1줄 말줄임 | `FontSize=13.5 FontWeight=Bold TextTrimming=CharacterEllipsis TextWrapping=NoWrap` | HTML 소스 :173 |
| 행 설명 | 폰트·색·조건 | `12px` `#98959d`, 1줄 말줄임, **설명이 있을 때만** | `Foreground=TextFillColorTertiaryBrush` + `StringNotEmptyToVisibility` | HTML 소스 :174-176 |
| 행 제목/설명 | 간격 | `gap:3px` (세로) | `StackPanel Spacing="3"` | HTML 소스 :172 |
| 행 날짜 | 폰트·색 | `11px` `#8a8890`, 등폭, 축소 안 됨 | 기존 `FormatDateRange`(`MM-dd – MM-dd`) + `FontFamily="Consolas"` | HTML 소스 :178 |
| 행 우선순위 배지 | 형태 | `11px` `weight 700`, `radius:6px`, `padding:3px 9px` | `Border Style=TagBadgeStyle` + `CornerRadius="6"`·`Padding="9,3"` 덮어쓰기 | HTML 소스 :179, :1412 |
| 행 우선순위 배지 | 색 | 높음 글자 `#e8b45a`/배경 16% · 보통 글자 `#7ab5ec`/배경 `rgba(90,163,232,.16)` · 낮음 글자 `#8a8890`/배경 **불투명 `#2b2b31`** | **T4에서 기존 브러시를 이 값으로 정정**(현재 `#D9954A`·`#5B93D8`·반투명 `#286F6D75` — D7) | HTML 소스 :1200-1204 |

## Decisions

- **D1 (목록 데이터 구조 = 상태 그룹의 컬렉션)**: 목록 뷰를 `ObservableCollection<TaskListStatusGroup>` 하나로 표현하고 `ItemsControl` 하나가 4개 상태 그룹을 돌린다. 칸반처럼 XAML에 4개를 하드코딩하지 않는다 — 칸반은 가로 4열 고정이라 열마다 `Grid.Column`·`MinWidth`가 달랐지만, 목록은 **세로로 같은 모양이 4번 반복**되므로 하드코딩하면 그룹 헤더 마크업이 4벌 중복된다. *Source*: `TaskPage.xaml:367-432`, 시안 :145 `sc-for taskListGroups`.
- **D2 (카테고리 서브그룹은 `TaskColumnGroup` 재사용 · 정렬은 이름순 유지)**: 목록의 카테고리 서브그룹 데이터는 칸반과 완전히 동일하므로 `BuildColumnGroups`가 만든 `TaskColumnGroup`을 그대로 담는다(새 record 없음). **정렬은 시안(`catOrder` — 카테고리 선언 순서)과 다른 이름순(`CurrentCulture`)을 유지**한다 — 사용자 결정(2026-07-22): 칸반과 같은 순서로 보이는 것이 우선이며, 대장 `[칸반/목록 "미분류" 그룹 정렬 불일치]`도 이로써 해소된다. *Source*: `TaskPageViewModel.cs:127-146`, 시안 :157·:1406, 사용자 결정.
- **D3 (상태 라벨·dot 색의 전달 경로)**: 그룹 라벨(`예정`…)은 **VM이 그룹 데이터에 담아** 넘기고(`LocalizationService.Get($"TaskStatus_{status}")` — VM은 이미 같은 방식으로 `TaskCategory_None`을 쓴다), dot 색은 **View의 `StatusDotBrush(TodoStatus)` 헬퍼**가 상태 enum으로부터 정한다(함정 5 — VM이 `Brush`를 들면 Presentation 타입이 VM에 샌다). *Source*: `TaskPageViewModel.cs:137`, `TaskPage.xaml.cs:39-47`.
- **D4 (상태 콤보·수정/삭제 버튼 제거 + 조작 경로 통일)**: 목록 행의 상태 ComboBox·수정·삭제 버튼을 제거하고 **칸반과 동일한 3경로**(왼클릭=편집 / 우클릭 메뉴=편집·상태 변경·삭제 / 드래그=상태 이동)로 통일한다. 상태 그룹을 드롭 대상으로 만들어 드래그가 실제로 동작하게 한다. *Source*: 사용자 결정(2026-07-22) — 시안은 행에 `draggable="true"`를 주면서도 드롭 대상을 두지 않아 드래그가 무의미했는데, 대상을 넣으면 외형은 시안 그대로면서 기능이 성립한다.
- **D5 (팔레트 신규 색·브러시 없음)**: 시안 값 `#1f1f24`·`#27272d`·`#3d3d45`·`#c9c6ce`·`#98959d`는 기존 팔레트 브러시로 근사한다. 브러시가 없는 `#35353C`는 같은 값의 `ControlStrokeColorSecondaryBrush`로 대체한다(리뷰 M1 — `AppBorderStrongerColor`는 Color만 존재). 칸반 카드도 시안 `#25252b`를 `AppCardBrush`로 근사한 선례가 있어 화면 간 일관성이 유지된다.
- **D6 (드래그 중 반투명 미구현)**: 시안의 `opacity:0.45`(드래그 중인 행)는 구현하지 않는다. WinUI `CanDrag`는 기본 드래그 비주얼을 제공하며, 원본 요소 투명도를 낮추려면 `DragStarting`/`DropCompleted`에서 해당 행을 찾아 되돌리는 배선이 필요하다(칸반 카드도 미구현이라 두 뷰가 일관된다). Deferred 등재.
- **D7 (우선순위 배지 색 시안 정정)**: 높음 `#D9954A`→`#E8B45A`, 보통 `#5B93D8`→`#7AB5EC`, 낮음 배경 반투명 `#286F6D75`→**불투명 `#2B2B31`**(글자 `#8A8890` 유지). 소프트 배경은 기존 관례대로 알파 `0x28`을 유지한다(시안 `.16`≈`0x29`). **칸반 카드와 공유하는 색이라 칸반도 함께 바뀐다** — 사용자 결정(2026-07-22). *Source*: 시안 :1200-1204, `TaskPage.xaml.cs:126-131`.
- **D8 (빈 상태 그룹 표시)**: 항목이 0건인 상태 그룹도 헤더와 "작업 없음" 점선 박스를 표시한다(그룹 4개 항상 노출). *Source*: 사용자 결정(2026-07-22) + 시안 :154-156.
- **D9 (신규 resw 키)**: `TaskList_Empty.Text`("작업 없음" / "No tasks") ko/en 양쪽(함정 2). **XAML `x:Uid` 소비**이므로 `.Text` 접미를 붙인다(함정 3 — 접미 없이 등록하고 `x:Uid`로 쓰면 빌드 오류 없이 빈 라벨이 된다).
- **D10 (목록 폭)**: 현행 `MaxWidth="520" HorizontalAlignment="Left"` 제한을 제거하고 폭 전체를 쓴다. 시안 목록은 컨테이너 폭을 그대로 채우며(`flex:1`), 가로 1줄 행은 제목·날짜·배지가 양끝으로 벌어져야 읽힌다. *Source*: 시안 :144, `TaskPage.xaml:443`.
- **D11 (상태 dot 색 시안 정정)**: 예정 `#F0716A`→`#8A8890`, 진행 중 `#5B93D8`→`#5AA3E8`, 완료 `#5DB463`(무변경), 보류 `#D9954A`→`#E8925A`. 특히 예정은 코랄→회색으로 확연히 달라진다. **칸반 열 헤더와 공유하는 색이라 칸반도 함께 바뀐다** — 사용자 결정(2026-07-22). 이 브러시들은 `TaskPage.xaml.cs` 내부 private static이라 다른 화면에는 영향이 없다(grep 전수). *Source*: 시안 :1198, `TaskPage.xaml.cs:39-42`.
- **D12 (`LinkedTestBadge` 표시 소멸 — 계산 경로는 유지)**: 시안 목록 행에는 작업별 테스트 연결 배지가 없다(테스트 정보는 카테고리 헤더의 통과율 배지가 담당). `TaskCardTemplate` 제거로 `TodoItem.LinkedTestBadge`의 **UI 소비처가 0**이 되지만, 도메인 속성과 VM 계산(`MapTestBadge`/`GetLinkedTestStatus`/`Rebuild`의 대입)은 **유지**한다 — 도메인 변경은 Out of Scope이고, PRD **FR-E4**(Should, active)의 "연결 배지"가 이로써 화면에서 사라지므로 되돌릴 여지를 남긴다. PRD Coverage에 영향으로 명시하고 Deferred 등재. *Source*: `TodoItem.cs:52-54`(표시 전용·비영속), `TaskPageViewModel.cs:91-106`, `TaskPage.xaml:210-212`, 리뷰 M4.
- **D13 (고아 resw는 대장 합류)**: T2로 고아가 되는 `TaskLabel_Start`·`TaskLabel_End`(ko/en)는 이번에 삭제하지 않고 대장 `[Todo* resw 고아 정리]`에 합류시킨다(고아 resw 일괄 audit이 이미 대기 중이라 개별 삭제는 중복 작업).
- **D15 (dot 모양도 시안으로 통일 — 칸반 포함)**: 시안의 상태 dot(:1419)·카테고리 dot(:1430)은 모두 `border-radius:3px` **라운드 사각형**인데 현행 칸반은 `Ellipse`(원)다. 목록만 시안을 따르면 한 화면 안에서 두 뷰의 dot 모양이 갈리므로, **칸반의 `Ellipse` 5곳도 함께 라운드 사각형으로 바꾼다**(T4). 색 정정(D7·D11)이 이미 칸반에 파급되는 것과 같은 성격의 확장이며, 요소 종류만 바뀌고 레이아웃·동작은 그대로다. *Source*: 시안 :1419·:1430, `TaskPage.xaml:120-124,372,389,406,423`, 리뷰 m2.
- **D14 (카테고리 헤더 줄바꿈 미지원)**: 시안 카테고리 헤더는 `flex-wrap:wrap`이라 항목이 넘치면 줄바꿈되지만, WinUI `StackPanel`은 줄바꿈이 없다. 칸반의 같은 헤더도 `Grid`/`StackPanel`로 구현돼 동일 성질이므로 **두 뷰 일관성을 우선해 줄바꿈을 구현하지 않는다**(넘치면 잘린다). 긴 카테고리명이 실제 문제가 되면 Deferred에서 다룬다. *Source*: 시안 :159, `TaskPage.xaml:111-119`, 리뷰 m1.

## PRD Coverage

| PRD ID | 우선순위 | 대응 task | 상태 |
|---|---|---|---|
| FR-T2 (작업 전체 페이지 + 칸반/목록 뷰 전환) | Must | T1, T2 | ✅ 커버(목록 뷰 재구현 몫 — 전환 토글 자체는 기구현·무변경) |
| FR-T4 (카테고리 필터·카테고리별 그룹핑·상태별 개수) | Must | T1, T2 | ✅ 커버(목록의 상태별 개수·카테고리 그룹핑 몫 — 필터 콤보는 기구현·무변경) |
| FR-T3 (칸반 드래그앤드롭 상태 변경) | Must | T3 | ✅ 커버(목록 뷰로 확장 — 칸반 경로는 무변경) |
| FR-T8 (카테고리 그룹 헤더 테스트 통과율 배지) | Should | T2 | ✅ 커버(목록 서브그룹에도 동일 배지 표시) |
| FR-E4 (테스트↔작업 연결 배지·링크 + FR-T8 배선) | Should | T2 | ⚠️ **부분 커버(축소)** — FR-T8 배선 몫은 유지되나, **작업별 "연결 배지"의 화면 표현이 이번 구조 교체로 사라진다**(시안 목록·칸반 어디에도 없음 — D12). 데이터·계산 경로는 유지해 되돌릴 여지를 남기고 Deferred 등재 |
| FR-T1 (TodoItem 상태·카테고리·우선순위·날짜 필드) | Must | — | 이번 범위 외 (기구현 — 도메인·스키마 무변경) |
| FR-T5 (새 작업/편집·삭제 확인 다이얼로그) | Must | — | 이번 범위 외 (기구현 — 목록에서 같은 다이얼로그를 호출만 함) |
| FR-T6 (테스트 추가 토글) | Should | — | 이번 범위 외 (기구현 — `ColumnAdd_Click` 경로 무변경) |
| FR-T7 (담당자 필드) | Could | — | 이번 범위 외 (사용자 제외 결정 — 대장 등재. 시안 목록 행에도 담당자가 없어 시안과 일치) |
| FR-E*(E4 제외)·FR-C*·FR-S*·FR-H*·FR-N* 등 그 외 active Must | Must/Should | — | 이번 범위 외 (기구현, 이번 diff 무관) |
| NFR-1 (빌드 오류 0·신규 경고 0) | — | 전 task | 검증 대상(기존 경고 5건 제외) |
| NFR-2 (계층 위반 0) | — | T1 | 검증 대상(VM은 `Brush`·`Visibility` 등 View 타입을 참조하지 않는다 — D3) |
| NFR-3 (DB 스키마 하위호환) | — | — | ✅ 무영향(도메인·스키마·직렬화 무변경) |
| NFR-4 (다국어 ko/en 대칭) | — | T2 | 신규 resw 키 ko/en 양쪽(함정 2·3) |
| NFR-5 (테스트) | — | — | 조건 미발동(테스트 프로젝트 부재 — AGENTS.md) |

## 작업 단계

> **T1과 T2의 경계(리뷰 B1 반영)**: `x:Bind`는 컴파일 타임 바인딩이라 VM 멤버를 지우면서 XAML 소비처를 남기면 **그 task에서 반드시 빌드가 깨진다**. 따라서 T1은 **추가만** 하고(기존 `CategoryGroups` 경로는 그대로 살려 둔다), 제거는 XAML 교체와 **같은 task**(T2)에서 한다.

### T1 — VM: 목록 뷰용 상태 그룹 데이터 추가 `Type C`
- [x] 구현
- **Files**: `DevDashboard_WinUI/Presentation/ViewModels/TaskPageViewModel.cs`
- **Design**: ① 배치 — 전부 `TaskPageViewModel.cs`(기존 파일). ② 신규 심볼 — `TaskListStatusGroup(TodoStatus Status, string StatusLabel, int Count, bool IsEmpty, IReadOnlyList<TaskColumnGroup> Categories)` record: 목록 뷰의 한 상태 그룹(헤더 표시값 + 카테고리 서브그룹) / `ListStatusGroups` 컬렉션 프로퍼티 / `BuildListStatusGroups(filtered)` 비공개 메서드. ③ 의존 방향 — `Rebuild()`가 `BuildListStatusGroups`를 호출하고, 그것이 기존 `BuildColumnGroups`의 산출(`TaskColumnGroup`)을 재사용한다. VM은 `Brush`·`Visibility` 등 View 타입을 참조하지 않는다(D3). ④ 비추상화 — 상태 4개를 순회하는 범용 "상태 메타데이터 테이블"이나 칸반/목록 공용 그룹 빌더를 만들지 않는다(칸반은 4열이 XAML 하드코딩이라 공용화 대상이 없다 — 배열 순회 6줄이면 충분).
- **구성**:
  - (D1) `ListStatusGroups`(`ObservableCollection<TaskListStatusGroup>`) 추가.
  - (D2·D3) `BuildListStatusGroups(IEnumerable<TodoItem> filtered)`: 상태 4개(`Waiting`→`Active`→`Completed`→`Hold` 순)를 돌며, 각 상태에 대해 임시 `ObservableCollection<TaskColumnGroup>`에 기존 `BuildColumnGroups`를 채워 서브그룹 목록을 얻고, 라벨(`LocalizationService.Get($"TaskStatus_{status}")`)·항목 합계(`CountItems`)·빈 여부와 함께 `TaskListStatusGroup`을 만들어 담는다.
  - `Rebuild()` 끝에서 `BuildListStatusGroups(filtered)`를 호출한다. **기존 `RebuildCategoryGroups(filtered)` 호출과 `CategoryGroups`·`TaskCategoryGroup`은 이 task에서 건드리지 않는다**(T2에서 XAML과 함께 제거 — 위 경계 주석).
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0(기존 5건 제외).
  2. `TaskListStatusGroup` record가 정의되고 `ListStatusGroups`가 `Rebuild()` 경로에서 채워진다.
  3. 상태 4개가 **항상** 그룹으로 만들어진다(항목 0건이어도 `IsEmpty=true` 그룹 생성 — D8).
  4. 각 상태 그룹의 `Count`가 그 상태의 작업 수(카테고리 서브그룹 항목 합계)와 같고, 같은 상태의 칸반 열 개수(`WaitingCount` 등)와 일치한다.
  5. `TaskPageViewModel.cs`에 `Microsoft.UI.Xaml` 계열 using·타입이 추가되지 않는다(NFR-2).
  6. `CategoryGroups`·`RebuildCategoryGroups`·`TaskCategoryGroup`이 **그대로 남아 있다**(제거는 T2 — 이 task 단독 빌드 성립 조건).
- **Edge Cases**: 카테고리 필터로 모든 상태가 비는 경우 → 그룹 4개 전부 `IsEmpty` / `_project.Todos`가 null(기존 `?? []` 가드 유지) / 카테고리가 빈 작업 → 기존대로 "미분류" 서브그룹 / 같은 이름 카테고리의 대소문자 차이(`BuildColumnGroups`의 `OrdinalIgnoreCase` 그룹핑 유지) / `Rebuild()`는 드래그·편집마다 호출되므로 목록 그룹 재생성이 매번 일어난다(기존 `RebuildCategoryGroups`도 동일 — 성능 성질 무변경, 이 task 동안은 두 경로가 함께 도는 일시적 중복이 있으나 T2에서 해소).
- **Halt Forecast**: 없음 — 추가만 하므로 제거·계약 변경·파괴적 작업이 없다.

### T2 — XAML: 목록 뷰를 시안 구조로 재구성 + 구 카드 경로 일괄 제거 `Type D`
- [x] 구현
- **Files**: `DevDashboard_WinUI/Presentation/Views/TaskPage.xaml`, `DevDashboard_WinUI/Presentation/Views/TaskPage.xaml.cs`, `DevDashboard_WinUI/Presentation/ViewModels/TaskPageViewModel.cs`, `DevDashboard_WinUI/Strings/ko-KR/Resources.resw`, `DevDashboard_WinUI/Strings/en-US/Resources.resw`
- **Design**: ① 배치 — 템플릿 3종은 `TaskPage.xaml`의 `UserControl.Resources`(뷰 국소), 헬퍼는 `TaskPage.xaml.cs` 정적 메서드. ② 신규 심볼 — `TaskListRowTemplate`(가로 1줄 작업 행) / `TaskListCategoryTemplate`(카테고리 서브그룹) / `TaskListStatusGroupTemplate`(상태 그룹 헤더 + 빈 상태 + 서브그룹 목록) / `TaskPage.StatusDotBrush(TodoStatus)`(상태 → dot 브러시) / `TaskPage.StatusTag(TodoStatus)`(상태 → `Tag` 문자열, 기존 `ColumnAdd_Click`·`Column_Drop`의 `Tag: string` 계약 충족) / `TaskPage.EmptyGroupVisibility(bool)`·`TaskPage.CategoriesVisibility(bool)`(빈 그룹 ↔ 목록 배타 표시, 함정 5로 `Visibility` 직접 반환). ③ 의존 방향 — XAML이 코드비하인드 정적 헬퍼를 `x:Bind` 함수 바인딩으로 참조하고, 헬퍼는 기존 정적 브러시만 참조한다. ④ 비추상화 — 칸반과 목록이 공유하는 "카테고리 헤더" 부분을 공용 템플릿/UserControl으로 뽑지 않는다(칸반은 안쪽 카드 템플릿이 하드코딩돼 있어 공용화하려면 템플릿 주입 기구가 필요하고, 그 간접화는 마크업 20줄 중복보다 추적이 어렵다 — 함정 11의 교훈과 정합).
- **구성**:
  - (D10) 목록 `ScrollViewer` 내부를 `ItemsControl ItemsSource={x:Bind Vm.ListStatusGroups, Mode=OneWay}` + `ItemsPanel`(`StackPanel Spacing="22"`)로 교체, `MaxWidth`·`HorizontalAlignment="Left"` 제거, 컨테이너 `Padding="24,18,24,24"`.
  - `TaskListStatusGroupTemplate`(`x:DataType="vm:TaskListStatusGroup"`): 루트 `StackPanel Spacing="12"` — 헤더 `Grid`(열 Auto·Auto·Auto·`*`·Auto, ColumnSpacing 9) = dot(`Border` 9×9 CornerRadius 3, `{x:Bind local:TaskPage.StatusDotBrush(Status)}`) + 라벨(`StatusLabel`, 14.5 Bold) + 개수(`Count`, 12, Tertiary, Consolas) + `Rectangle Style="{StaticResource SeparatorStyle}"` + "＋ 새 작업" `Button`(Height 28, Padding 11,0, CornerRadius 7, `Tag="{x:Bind local:TaskPage.StatusTag(Status)}"`, `Click="ColumnAdd_Click"`, Content `{x:Bind local:TaskPage.ColumnAddText}`) / 빈 상태 `Grid`(`Visibility={x:Bind local:TaskPage.EmptyGroupVisibility(IsEmpty)}`) 안 `Rectangle StrokeDashArray="3,3" RadiusX=10 RadiusY=10 Stroke={ThemeResource ControlStrokeColorSecondaryBrush}` + 가운데 `TextBlock x:Uid="TaskList_Empty"` / 서브그룹 `ItemsControl ItemsSource={x:Bind Categories}`(`ItemsPanel Spacing="12"`, `Visibility={x:Bind local:TaskPage.CategoriesVisibility(IsEmpty)}`).
  - `TaskListCategoryTemplate`(`x:DataType="vm:TaskColumnGroup"`): `StackPanel Margin="4,0,0,0" Spacing="7"` — 헤더 `StackPanel Orientation=Horizontal Spacing=7`(dot 8×8 CornerRadius 3 `TagColor` 컨버터 + 이름 12.5 Bold Secondary + 개수 11.5 Tertiary + **테스트 배지 2종은 `TaskColumnGroupTemplate`(:139-166)의 마크업을 그대로 옮긴다**) + 행 `ItemsControl ItemsSource={x:Bind Items}`(`ItemsPanel Spacing="7"`, `ItemTemplate=TaskListRowTemplate`).
  - `TaskListRowTemplate`(`x:DataType="models:TodoItem"`): `Border`(AppCardBrush / AppBorderBrush 1 / CornerRadius 10 / Padding 14,11) 안 `Grid`(열 `*`·Auto·Auto, ColumnSpacing 12, 세로 중앙) = 좌측 `StackPanel Spacing=3`(제목 13.5 Bold `TextWrapping=NoWrap TextTrimming=CharacterEllipsis` + 설명 12 Tertiary 동일 말줄임 + `StringNotEmptyToVisibility`) / 날짜 `{x:Bind local:TaskPage.FormatDateRange(StartDate, EndDate)}`(11, Tertiary, Consolas, `DateRangeVisibility`) / 우선순위 배지(`Border Style=TagBadgeStyle CornerRadius=6 Padding=9,3 Background={x:Bind local:TaskPage.PriorityBadgeBrush(Priority)}` + `TextBlock` 11 Bold `PriorityText`/`PriorityBrush`).
  - **제거 (동시 수행 — 소비처와 정의를 같은 task에서 지운다)**:
    - XAML: `TaskCardTemplate`(:173-273), 구 목록 뷰 마크업(:440-448의 `CategoryGroups`·`TaskCategoryGroup` 바인딩).
    - 코드비하인드: `StatusCombo_Loaded`·`StatusCombo_SelectionChanged`·`EditTask_Click`·`DeleteTask_Click`·`StatusOptions`·`TaskStatusOption` record·`EditTooltip`·`DeleteTooltip`·**`FormatStart`·`FormatEnd`·`DateVisibility`**(리뷰 M4 — 유일 소비처가 `TaskCardTemplate`).
    - VM: `CategoryGroups`·`RebuildCategoryGroups`·`TaskCategoryGroup` record + `Rebuild()`의 호출 1줄.
    - 파일 상단 주석(:14-18) "목록 뷰는 드롭 대상이 없어…"를 새 동작에 맞게 갱신(D4로 드롭 대상이 생긴다). **함께 stale해지는 주석 2곳도 갱신**: `TaskPageViewModel.cs:300`("목록 뷰의 TaskCategoryGroup과 달리…" — 비교 대상이 사라진다), `TaskPage.xaml.cs:49`("StatusOptions와 동일 리소스 키 재사용" — `StatusOptions`가 사라진다). 두 주석은 acceptance 2의 grep에도 걸리므로 함께 처리해야 검증이 통과한다(리뷰 m4).
    - **제거하지 않는 것**: `TodoItem.LinkedTestBadge`와 VM의 `MapTestBadge`/`GetLinkedTestStatus`/`Rebuild`의 대입(D12), resw `TaskLabel_Start`·`TaskLabel_End`(D13).
  - (D9) resw `TaskList_Empty.Text` ko("작업 없음")/en("No tasks") 양쪽.
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0.
  2. **`TaskPage.xaml`·`TaskPage.xaml.cs`·`TaskPageViewModel.cs` 세 파일 안에서** `TaskCardTemplate`·`StatusCombo_`·`EditTask_Click`·`DeleteTask_Click`·`StatusOptions`·`TaskStatusOption`·`EditTooltip`·`DeleteTooltip`·`FormatStart`·`FormatEnd`·`DateVisibility`·`CategoryGroups`·`RebuildCategoryGroups`·`TaskCategoryGroup` 잔존 0건(**레포 전체 grep 금지** — 동명의 별개 심볼이 `TestPage`·`HistoryDialog*`·`ToolItemViewModel`에 있다, 리뷰 M3).
  3. `TaskListStatusGroupTemplate`·`TaskListCategoryTemplate`·`TaskListRowTemplate`이 정의되고 각각 정확히 1곳에서 소비된다.
  4. `StatusDotBrush`·`StatusTag`·`EmptyGroupVisibility`·`CategoriesVisibility`가 정의되고 전부 XAML에서 소비된다(고아 0).
  5. `TaskList_Empty.Text`가 ko/en **양쪽**에 각 언어 값으로 존재하고 **`.Text` 접미**가 붙어 있다(함정 2·3).
  6. `TaskListRowTemplate` 안에 `ComboBox`가 0건이고 수정·삭제 `Button`이 없다(시안 정합).
  7. 우선순위 배지·날짜·테스트 배지가 **기존 헬퍼를 재사용**한다(`PriorityBadgeBrush`·`FormatDateRange`·`IdleTestBadgeVisibility`·`RanTestBadgeVisibility`가 목록에서도 소비).
  8. `TodoItem.LinkedTestBadge`와 VM의 `MapTestBadge`/`GetLinkedTestStatus`가 **남아 있다**(D12 — 계산 경로 유지).
- **Edge Cases**: 제목이 매우 김 → 1줄 말줄임(행 높이 불변) / 설명 없음 → 설명 줄 미표시로 행이 얇아짐(시안과 동일) / 시작·종료일 모두 없음 → 날짜 열 `Collapsed`(Auto 열이라 폭 0) / 카테고리가 빈 작업 → "미분류" 서브그룹(배지 없음 — `BuildPassRateBadge`가 빈 문자열 반환) / **카테고리명이 길거나 배지가 많으면 헤더가 넘쳐 잘린다**(D14 — 시안은 `flex-wrap`으로 줄바꿈하지만 WinUI `StackPanel`은 미지원, 칸반과 동일 성질) / 창을 좁히면 제목 열(`*`)이 먼저 줄고 날짜·배지는 유지 / 빈 그룹은 서브그룹 `ItemsControl`이 `Collapsed`라 점선 박스만 보인다.
- **Halt Forecast**: (ii-a) 사전 승인 — 공개 정적 멤버 `StatusOptions`·`EditTooltip`·`DeleteTooltip`·`FormatStart`·`FormatEnd`·`DateVisibility`, 공개 record `TaskStatusOption`, VM 공개 멤버 `CategoryGroups`·`TaskCategoryGroup` 제거 + resw 신규 키 1개(ko/en). (i) 사전 해소 — 소비처 전수 grep으로 함정 11 리스크 제거, `x:Bind`/`x:Uid`의 `DataTemplate` 내 성립 여부는 기존 선례로 확인 완료. 파괴적·외부 작업 없음.

### T3 — 목록 행 조작: 클릭 편집 · 우클릭 메뉴 · hover · 드래그 상태 이동 `Type C`
- [x] 구현
- **Files**: `DevDashboard_WinUI/Presentation/Views/TaskPage.xaml`(행·상태 그룹 배선), `DevDashboard_WinUI/Presentation/Views/TaskPage.xaml.cs`(hover 핸들러 2개)
- **Design**: ① 배치 — 배선은 `TaskPage.xaml`의 두 템플릿, hover 핸들러는 `TaskPage.xaml.cs`. ② 신규 심볼 — `ListRow_PointerEntered`/`ListRow_PointerExited`(행 테두리 밝기 토글) **2개뿐**. 편집·삭제·상태 변경·드래그·새 작업은 전부 기존 핸들러 재사용(4-D). ③ 의존 방향 — XAML → 코드비하인드 핸들러 → 기존 `Vm.MoveToStatus`/`EditTodoAsync`/`DeleteTodoAsync`. ④ 비추상화 — hover를 위해 스타일·VisualState 인프라를 새로 만들지 않는다(`DataTemplate` 안에서 VSM `GoToState`가 동작하지 않는다는 것이 `TestPage`에서 확인된 사실 — 핸들러 2개가 최소 해법).
- **구성**:
  - (D4) `TaskListRowTemplate`의 루트 `Border`에 `Tapped="Card_Tapped"`(클릭 편집 — 칸반과 동일 핸들러, `DataContext: TodoItem` 계약 충족) + `CanDrag="True" DragStarting="Card_DragStarting"` + `Border.ContextFlyout`(칸반 카드 :30-58과 **동일 구성**: 편집 / 상태 변경 서브메뉴 4개 / 구분선 / 삭제, 각 `Tag="{x:Bind}"`) 배선.
  - (D4) `TaskListStatusGroupTemplate`의 루트에 `AllowDrop="True" Background="Transparent" Tag="{x:Bind local:TaskPage.StatusTag(Status)}" DragOver="Column_DragOver" Drop="Column_Drop"` 배선(그룹 루트가 드롭을 받아야 **빈 그룹에도 놓을 수 있다** — 칸반 열과 동일 결론. `Background="Transparent"`가 없으면 빈 영역이 hit-test되지 않는다).
  - hover: 행 루트 `Border`에 `PointerEntered`/`PointerExited` → `BorderBrush`를 `ControlStrokeColorSecondaryBrush`(hover) ↔ `AppBorderBrush`(기본)로 토글. 두 브러시는 `UserControl.Resources`에 별칭 키로 두고 코드에서 `Resources[...]`로 가져온다(`Application.Current.Resources` 경유 ThemeDictionaries 조회의 불확실성 회피 — `TestPage` 선례).
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0.
  2. `TaskListRowTemplate`에 `Tapped="Card_Tapped"`·`CanDrag="True"`·`DragStarting="Card_DragStarting"`·`ContextFlyout`이 있고, 메뉴가 칸반 카드와 동일하게 **편집 1 + 상태 변경 서브메뉴 4 + 삭제 1 = 항목 6개**를 건다.
  3. `TaskListStatusGroupTemplate` 루트에 `AllowDrop="True"`·`DragOver="Column_DragOver"`·`Drop="Column_Drop"`·`Tag` 바인딩·`Background="Transparent"`가 있다.
  4. `ListRow_PointerEntered`/`ListRow_PointerExited`가 정의되고 XAML에서 소비된다(고아 0).
  5. **신규 상태 변경 로직 0** — 목록 드래그가 기존 `Column_Drop` → `Vm.MoveToStatus`를 그대로 탄다(`TaskPage.xaml.cs`에 새 드롭 핸들러가 추가되지 않는다).
- **Edge Cases**: 같은 상태 그룹에 드롭 → `MoveToStatus`가 동일 상태를 무시(저장·작업기록 훅 미발생, 기존 동작) / 드래그 성립 시 Tapped 미발생(칸반에서 확인된 성질) / 우클릭 메뉴를 연 뒤 포인터가 벗어나면 `PointerExited`가 오지 않아 **hover 테두리가 잔존할 수 있다**(칸반에는 hover가 없어 처음 나타나는 성질 — ⏳ HUMAN-VERIFY, 잔존하면 Flyout `Closed`에서 복원 필요) / 완료 그룹에 드롭 → 작업기록 팝업(`WorkLogRequested`)이 기존대로 발생 / 드롭 후 `Rebuild()`로 행이 다른 그룹으로 이동해 hover 대상 요소가 사라짐(핸들러가 sender 기준이라 예외 없음) / 행 드래그가 안쪽 `ScrollViewer` 스크롤과 경합할 수 있음(칸반과 동일 성질).
- **Halt Forecast**: 없음 — 신규 파일·의존성·파괴적 작업 없음, 기존 핸들러 재사용이라 계약 변경 0.

### T4 — 상태 dot·우선순위 배지 색 + dot 모양을 시안 값으로 정정 `Type C`
- [x] 구현
- **Files**: `DevDashboard_WinUI/Presentation/Views/TaskPage.xaml.cs`(색), `DevDashboard_WinUI/Presentation/Views/TaskPage.xaml`(칸반 dot 모양)
- **Design**: 해당 없음 — 신규 심볼 0(기존 정적 브러시의 색 리터럴 교체 + 칸반 dot 요소 종류 교체 + 주석 갱신).
- **구성**:
  - (D11) 상태 dot: `_statusDotWaiting` `0xF0,0x71,0x6A`→`0x8A,0x88,0x90` / `_statusDotActive` `0x5B,0x93,0xD8`→`0x5A,0xA3,0xE8` / `_statusDotCompleted` `0x5D,0xB4,0x63`(무변경) / `_statusDotHold` `0xD9,0x95,0x4A`→`0xE8,0x92,0x5A`.
  - (D7) 우선순위: `_priorityHighBrush` `0xD9,0x95,0x4A`→`0xE8,0xB4,0x5A` / `_priorityHighSoftBrush` `0x28,0xD9,0x95,0x4A`→`0x28,0xE8,0xB4,0x5A` / `_priorityNormalBrush` `0x5B,0x93,0xD8`→`0x7A,0xB5,0xEC` / `_priorityNormalSoftBrush` `0x28,0x5B,0x93,0xD8`→`0x28,0x5A,0xA3,0xE8` / `_priorityLowBrush` `0x8A,0x88,0x90`(무변경) / `_priorityLowSoftBrush` `0x28,0x6F,0x6D,0x75`→**불투명 `0xFF,0x2B,0x2B,0x31`**.
  - (D15) **dot 모양 통일**: 칸반의 상태 dot(`TaskPage.xaml:372,389,406,423`)과 카테고리 dot(`:120-124`)이 `Ellipse`(원)인데, 시안(:1419·:1430)은 양쪽 다 `border-radius:3px` **라운드 사각형**이다. 목록만 시안을 따르면 같은 화면에서 두 뷰의 dot 모양이 갈리므로(리뷰 m2), 칸반의 `Ellipse` 5곳도 `Border Width/Height + CornerRadius="3"`으로 바꾼다.
  - 주석 갱신: 이 브러시들이 "Palette.xaml 값과 수동으로 맞춘다"고 적혀 있으나 이제 **시안(`colDefs`/`priStyles`) 값이 정본**임을 명시한다(팔레트 `AppWarning`/`AppInfo`와 의도적으로 갈린다는 사실을 남겨 후속 수정이 되돌리지 않게 한다).
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0.
  2. `TaskPage.xaml.cs`에 `0xF0, 0x71, 0x6A`(구 예정 dot)·`0x28, 0x6F, 0x6D, 0x75`(구 낮음 배경) 잔존 0건.
  3. **변경 대상 8개**(상태 dot 3: Waiting·Active·Hold / 우선순위 5: High·HighSoft·Normal·NormalSoft·LowSoft)가 위 구성 값과 정확히 일치하고, **무변경 2개**(`_statusDotCompleted`·`_priorityLowBrush`)는 그대로다(리뷰 m3).
  4. 주석이 "시안 값이 정본"으로 갱신돼 있다(팔레트 동기화 문구 잔존 0).
  5. `TaskPage.xaml`에 `Ellipse` 잔존 0건이고, 대체된 dot 5곳이 `CornerRadius="3"`을 갖는다(D15).
  6. `Palette.xaml` 등 이 두 파일 밖의 색 정의는 변경되지 않는다.
- **Edge Cases**: `_priorityLowSoftBrush`가 반투명→불투명으로 바뀌어 **행·카드 배경 위에서 더 또렷해진다**(의도된 변화) / 예정 dot이 코랄→회색이라 칸반 "예정" 열 헤더의 인상이 크게 달라진다(사용자 결정) / 완료 dot·낮음 글자색은 이미 시안과 같아 변경 없음 / `Ellipse`→`Border` 교체 시 `Fill`→`Background` 속성명이 바뀐다(빌드가 잡는다) / 목록 템플릿(T2에서 이미 `Border`로 작성)은 이 task의 대상이 아니다.
- **Halt Forecast**: (ii-a) 사전 승인 — 칸반 마크업 변경(dot 요소 종류 5곳, D15). 그 외 순수 값 치환이라 파괴적·외부 작업 없음.

## 사전 승인 항목 (일괄 승인 대상)
- **VM 공개 멤버 제거**: `TaskPageViewModel.CategoryGroups`, `TaskCategoryGroup` record, `RebuildCategoryGroups`(T2) — 소비처는 같은 task에서 교체되는 목록 뷰 마크업 1곳뿐(grep 전수).
- **View 공개 멤버 제거**: `TaskPage.StatusOptions`·`EditTooltip`·`DeleteTooltip`·`FormatStart`·`FormatEnd`·`DateVisibility`, `TaskStatusOption` record, 핸들러 `StatusCombo_Loaded`·`StatusCombo_SelectionChanged`·`EditTask_Click`·`DeleteTask_Click`(T2) — 소비처는 함께 제거되는 `TaskCardTemplate` 뿐.
- **칸반 뷰에 파급되는 변경**(T4): ① 상태 dot·우선순위 배지 **색 8종** — 목록과 공유하는 브러시라 칸반 화면도 함께 바뀐다(사용자 결정 2026-07-22). ② 칸반 dot **모양** `Ellipse`→라운드 사각형 5곳(D15 — 두 뷰의 dot 모양이 갈리지 않게).
- **신규 resw 키**: `TaskList_Empty.Text` ko/en 양쪽(T2). 기존 키 값은 무변경.
- 로컬 작업 브랜치(`task/taskpage-list-view`)에서 task별 commit.

## 불가피한 Halt (위임 불가)
- master 병합·push·태그·릴리즈·PR — 이번 작업 완료 후 별도 승인.
- **시안 대조 최종 시각 판정** — 빌드는 마크업 존재만 보증한다. "시안과 같아 보이는가"(그룹 간격·행 높이·구분선·점선 박스·배지 크기·hover 밝기·정정된 dot/배지 색)는 사용자만 판정(⏳ HUMAN-VERIFY).
- **목록 드래그 실동작 확인** — 행을 다른 상태 그룹으로 끌어 옮기는 동작, 빈 그룹에 드롭, 완료 그룹 드롭 시 작업기록 팝업은 앱 실행 확인 필요(⏳ HUMAN-VERIFY).

## Deferred / Follow-up
- **[FR-E4 작업별 연결 배지 표시 소멸]** — `TaskCardTemplate` 제거로 `TodoItem.LinkedTestBadge`의 화면 표현이 없어진다(시안 목록·칸반 어디에도 없음, D12). 데이터·계산 경로는 유지했으므로, 작업 단위 테스트 상태를 다시 보고 싶다는 요구가 생기면 표시 위치(행 우측 배지 등)부터 논의. 요구가 없으면 `MapTestBadge`/`GetLinkedTestStatus`/`TodoItem.LinkedTestBadge`를 고아 정리 대상으로 묶어 일괄 제거.
- **[드래그 중 행 반투명 미구현]** — 시안은 드래그 중인 행을 `opacity:0.45`로 흐리게 하나 이번엔 구현하지 않는다(D6). 칸반 카드도 동일하게 미구현이라 두 뷰가 일관된다.
- **[카테고리 헤더 줄바꿈 미지원]** — 시안은 `flex-wrap:wrap`으로 카테고리명·배지가 넘치면 줄바꿈되지만 WinUI `StackPanel`은 미지원이라 잘린다(D14, 칸반도 동일). 긴 카테고리명이 실제로 문제가 되면 `WrapPanel` 대체 컨트롤 검토(AGENTS.md 메모: `muxc:WrapLayout`은 빌드 오류).
- **[카테고리 서브그룹 정렬이 시안과 다름]** — 시안은 카테고리 선언 순서(`catOrder`), 구현은 이름순(칸반과 통일 — 사용자 결정 D2). 등록 순서로 보고 싶다는 요구가 생기면 칸반·목록을 함께 바꾼다.
- **[`TaskLabel_Start`·`TaskLabel_End` resw 고아]** — T2로 유일 소비처(`FormatStart`/`FormatEnd`)가 사라진다. 대장 `[Todo* resw 고아 정리]`에 합류시켜 일괄 audit(D13).
- **[AGENTS.md 미추적 항목 stale]** — 대장 `[AGENTS.md는 git 미추적 — PC 간 미공유]`는 더 이상 사실이 아니다(2026-07-22 커밋 완료). 대장 항목 정정 필요 — 이번 코드 범위 밖이라 이연.
- **[칸반/목록 "미분류" 정렬 불일치 — 해소 확인 후 대장 종결]** — T1의 `BuildColumnGroups` 재사용으로 자동 해소된다(D2). Phase F에서 대장 항목을 `## 종결`로 옮긴다.
- **[테스트 화면 F-8 육안 확인 미완]** — 이전 plan의 잔여 확인 항목(대장에 이관 완료). 이번 작업과 별개로 사용자 확인 필요.

## Out of Scope
- 칸반 뷰의 **구조·레이아웃·카드 템플릿·드래그** 변경 — 이번 요청은 목록 보기 한정(단 공유 브러시 색은 T4로 함께 바뀐다).
- 헤더 영역(뒤로가기·제목·프로젝트 스코프 배지·카테고리 필터 콤보·칸반/목록 토글) 변경.
- `TodoItem`·`TodoStatus` 도메인·SQLite 스키마·직렬화 변경(`LinkedTestBadge` 속성 제거 포함 — D12).
- 새 작업/편집 다이얼로그(`TaskEditDialog`)·삭제 확인·작업기록 팝업의 내용 변경.
- `Palette.xaml`에 신규 색·브러시 추가(D5).
- 고아 resw 키 삭제(D13 — 대장의 일괄 audit 몫).
- 픽셀 단위 수치 일치(브라우저 렌더 캡처를 확보하지 못했고 CSS↔XAML 렌더 모델이 달라 구조·형태까지가 기준).

## Open Questions
- [x] 목록 뷰의 상태 변경 수단 → **우클릭 메뉴 + 드래그 이동**(상태 그룹을 드롭 대상으로) — 사용자, 2026-07-22
- [x] 행 왼클릭 동작 → **편집 다이얼로그 열기**(칸반 카드와 동일) — 사용자, 2026-07-22
- [x] 항목 0건 상태 그룹 처리 → **시안대로 "작업 없음" 점선 박스 표시**(그룹 4개 항상 노출) — 사용자, 2026-07-22
- [x] 상태 dot 색이 시안과 다름(예정=회색 vs 코랄) → **시안 값으로 정정**(칸반 열 헤더도 함께 변경) — 사용자, 2026-07-22
- [x] 우선순위 배지 색이 시안과 다름 → **시안 값으로 정정**(칸반 카드도 함께 변경) — 사용자, 2026-07-22
- [x] 카테고리 서브그룹 정렬 → **현재대로 이름순 유지**(칸반과 통일, 시안의 선언 순서와는 차이) — 사용자, 2026-07-22

## 검증 방법
- 빌드: `"C:/Program Files/Microsoft Visual Studio/18/Professional/MSBuild/Current/Bin/MSBuild.exe" "DevDashboard_WinUI/DevDashboard.csproj" -t:Build -p:Configuration=Debug -p:Platform=x64` → 오류 0 + 기존 경고 5건(NU1903 1 + CS0612 4) 외 신규 0
- 회귀 방지 grep:
  - **T1**: `TaskListStatusGroup` 정의 1 + `ListStatusGroups`가 `Rebuild()`에서 채워짐 / `CategoryGroups`는 **아직 남아 있다**(T1 단독 빌드 성립 조건)
  - **T2**(검사 범위 = `TaskPage.xaml`·`TaskPage.xaml.cs`·`TaskPageViewModel.cs` **세 파일 한정** — 동명 별개 심볼 오탐 방지): `TaskCardTemplate`·`StatusCombo_`·`EditTask_Click`·`DeleteTask_Click`·`StatusOptions`·`TaskStatusOption`·`EditTooltip`·`DeleteTooltip`·`FormatStart`·`FormatEnd`·`DateVisibility`·`CategoryGroups`·`RebuildCategoryGroups`·`TaskCategoryGroup` 잔존 0 / `TaskListStatusGroupTemplate`·`TaskListCategoryTemplate`·`TaskListRowTemplate` 각 정의 1 + 소비 1 / `StatusDotBrush`·`StatusTag`·`EmptyGroupVisibility`·`CategoriesVisibility` 정의 + 소비 / `TaskListRowTemplate` 안 `ComboBox` 0 / `MapTestBadge`·`GetLinkedTestStatus`·`LinkedTestBadge`(VM·도메인) **잔존 확인**(D12)
  - **T2 resw**: `TaskList_Empty.Text`가 ko/en 양쪽에 존재
  - **T3**: `ListRow_PointerEntered`·`ListRow_PointerExited` 정의 + XAML 소비 / `TaskListStatusGroupTemplate`에 `AllowDrop`·`Drop`·`DragOver` 존재
  - **T4**: `TaskPage.xaml.cs`에 `0xF0, 0x71, 0x6A`·`0x28, 0x6F, 0x6D, 0x75` 잔존 0 / `TaskPage.xaml`에 `Ellipse` 잔존 0
- 동작 확인(빌드로 검증 불가 → ⏳ HUMAN-VERIFY):
  - **T1·T2**: 목록 전환 시 상태 그룹 4개(예정/진행 중/완료/보류)가 세로로 보이고, 각 그룹 안에 카테고리 서브그룹 + 가로 1줄 행이 표시된다 / 개수·테스트 배지가 칸반과 같은 값 / 빈 그룹에 "작업 없음" 점선 박스 / 그룹 헤더 구분선이 남는 폭을 채움
  - **T3**: 행 클릭 → 편집 다이얼로그 / 행 우클릭 → 편집·상태 변경·삭제 메뉴 / 행 hover 시 테두리가 밝아짐(메뉴를 닫은 뒤 잔존하지 않는지 포함) / 행을 다른 상태 그룹으로 드래그 → 상태 변경·저장 / 빈 그룹에도 드롭 가능 / 완료 그룹에 드롭 → 작업기록 팝업 / 그룹 헤더 "＋ 새 작업" → 그 상태로 작업 생성
  - **T4**: 예정 dot이 회색, 보류 dot이 주황빛으로 보이는지(칸반 열 헤더 포함) / 우선순위 배지 3종의 색이 시안과 같은지 / "낮음" 배지 배경이 불투명 회색으로 또렷한지 / **칸반·목록의 dot이 모두 라운드 사각형으로 같아 보이는지**(D15)

## Phase Ledger
- (구현 시작 전)

## Progress Log
- T1~T2 완료: VM에 상태 그룹 데이터 추가(T1) → XAML 목록 뷰를 시안 구조(상태 그룹 → 카테고리 서브그룹 → 가로 행)로 교체하고 구 세로 카드 경로 14개 심볼 일괄 제거(T2). 빌드 OK, 리뷰 지적 해소 완료.
  - 결정(T2): 리뷰가 "목록 행에 클릭·우클릭 배선이 없는데 주석은 있다고 기술"(MAJOR)을 지적 — 배선은 plan상 T3 범위이므로 코드 대신 **주석을 현재 사실에 맞게 정정**했다(TaskPage.xaml 상단, Card_Tapped 문서주석).
- T3~T4 완료: 목록 행에 클릭 편집·우클릭 메뉴·hover·드래그 상태 이동 배선(T3, 신규 심볼은 hover 핸들러 2개뿐 — 나머지는 칸반 핸들러 재사용) → 상태 dot·우선순위 배지 색 8종과 dot 모양(Ellipse→라운드 사각형 5곳)을 시안 값으로 정정(T4). 빌드 OK, 리뷰 지적 0.
  - 결정(T3): 드롭 대상을 안쪽 목록이 아니라 **상태 그룹 루트**에 두고 `Background="Transparent"`를 줬다 — 그래야 항목이 없는 그룹에도 드롭할 수 있다(칸반 열과 같은 결론).
  - 반증(T2): quality 재리뷰가 `TaskEdit_Tooltip`·`TaskDelete_Tooltip` resw를 고아로 지적(MAJOR)했으나, `TestPage.xaml.cs:60-61` + `TestPage.xaml:31,44`가 실제 소비 중임을 근거로 반증 → 리뷰어가 오탐 인정·철회. T2가 제거한 것은 **동명의 `TaskPage` 프로퍼티**일 뿐이다(plan이 grep 범위를 TaskPage 3파일로 한정한 이유와 동일한 함정).
