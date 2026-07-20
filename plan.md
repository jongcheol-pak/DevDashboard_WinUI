# plan.md — 새 작업 다이얼로그 후속 수정 + 대시보드 복귀 네비게이션 버그

**PRD**: docs/prd.md (FR-T5 다이얼로그 후속 정정 + **FR-C3 회귀 수정**)
**이전 plan**: TaskEditDialog 시안 정합 재구성 — Phase G 통과(Must 100%). F-8 시각 확인 7항목 중 **6항목은 사용자 확인 완료(이상 없음), 1항목(필수 입력 테두리)이 이번 T1로 이어짐** → 이전 plan 종결(사용자 결정 2026-07-20).
**다음 plan**: 없음.

## 요구 이해

> 원문(사용자, 2026-07-20): "[이미지] - 새 작업 화면에서 필수 입력 ui에만 이미지처럼 빠간 라인을 항상 표시, 카테고리 '미분류' 삭제, 기존 카테고리가 없는 항목은 목록에서만 '미분류'로 표시 / - 작업 화면에서 대시보드 버튼을 클릭해서 대시보드로 이동후 다시 작업버튼을 클릭하면 작업 화면으로 이동하지 않는 문제가 있음"

이해한 요구:
- **① 필수 입력 표시 방식 변경**: 지금은 제목이 비어 있을 때만 입력칸 **테두리 전체**가 빨개진다. 시안은 **입력칸 하단에만 빨간 라인**이고, 이를 **값이 채워져도 항상** 유지한다. 대상은 **필수 입력칸(제목)뿐** — 설명·카테고리·날짜 등 선택 입력에는 붙이지 않는다.
- **② 카테고리 선택지에서 "미분류" 제거**: 새 작업/편집 다이얼로그의 카테고리 콤보에서 "미분류" 항목 자체를 없앤다.
- **③ "미분류"는 표시 전용으로 남긴다**: 카테고리가 비어 있는 **기존** 작업은 칸반·목록의 그룹 헤더에서 계속 "미분류"로 보여야 한다. 즉 "미분류"는 **고를 수 있는 값이 아니라 빈 값의 표시명**이 된다.
- **④ 네비게이션 버그**: 작업 페이지 → "대시보드" 복귀 → 다시 "작업" 클릭 시 아무 반응이 없다. 증상은 작업 버튼이지만 **원인은 카드 이벤트 구독이 복귀 시 되살아나지 않는 것**이라, 실제로는 카드의 다른 버튼(테스트·Git 상태·작업 기록·명령 슬롯)도 함께 죽는다.

## Goal

① `TaskEditDialog` 제목 입력칸의 필수 표시를 **조건부 전체 테두리 → 상시 하단 빨간 라인**으로 바꾸고, ② 카테고리 콤보에서 "미분류" 선택지를 제거하되 **빈 카테고리 데이터는 보존**하고, ③ 칸반·목록의 "미분류" 표시는 그대로 유지하며, ④ `DashboardView`가 대시보드로 복귀할 때 카드 이벤트를 **재구독**하도록 고쳐 페이지 진입 버튼이 다시 동작하게 한다.

## Investigation Log (근거)

### 위키·문서
- 위키 참조: vault 미설정 — 코드 1차 출처로 진행.
- AGENTS.md 존재. 신선도 점검: 이번 계획이 참조하는 경로(`Presentation/Views/DashboardView.xaml.cs`·`Views/Dialogs/TaskEditDialog.*`·`ViewModels/TaskEditDialogViewModel.cs`·`ViewModels/TaskPageViewModel.cs`·`Resources/Palette.xaml`) 전부 실재 확인. **함정 5(x:Bind 함수 바인딩은 ThemeResource 불가)·함정 11(공용 DataTemplate)** 이번 작업과 직결 — 아래 반영. 어긋남 0건.
- PRD 경량 확인: 이번 변경이 **FR-T5**(편집 다이얼로그, `prd.md:62`)와 **FR-C3**(페이지 네비게이션 컨테이너 — "대시보드 ↔ 페이지 전환", `prd.md:47`)에 닿는다 → PRD 연결(Phase G 활성).

### Deferred 대장 확인 (docs/plans/deferred.md)
- **이번과 인접하나 재수용하지 않음**: `[칸반/목록 "미분류" 그룹 정렬 불일치]`(`:27`) — 칸반은 raw key(빈 문자열)로, 목록은 표시명("미분류")으로 정렬해 미분류 그룹의 위치가 서로 다르다. **이번 요청은 "표시" 유지이지 정렬 통일이 아니므로 범위 밖**이며, T2·T3 어느 diff도 이 코드에 닿지 않는다. 대장에 그대로 유지.
- **이번에도 유지(무관)**: `[FR-S5 restyle]`·`[Todo*/Test* resw 고아]`·`[BOM 통일]`·`[NU1903]`·`[README/스크린샷]`·`[시작일 지울 수단 부재]`·`[다른 다이얼로그 라벨 스타일 불일치]`·`[빈 제목 오류 문구 제거에 따른 접근성 약화]`·`[SUGGEST status 선택적 파라미터]` 등.
- **신규 등재 예정**: 아래 Deferred 절 참조(카테고리 필터에서 미분류 선택 불가 / 편집 시 카테고리 미선택 표현).

### ① 필수 입력 표시 — 현행 구현 (직접 Read)
- `TaskEditDialog.xaml:44`가 `BorderBrush="{x:Bind local:TaskEditDialog.TitleBorderBrush(Vm.Title), Mode=OneWay}"`로 **입력칸 테두리 4면 전체**를 danger로 칠한다. 시안의 "하단 라인"과 형태가 다르고, 값이 채워지면 사라져 "항상 표시"와도 다르다.
- `TaskEditDialog.xaml.cs:17-26`의 `_titleRequiredBrush`·`_titleNormalBrush`·`TitleBorderBrush()`는 **이 바인딩 1곳에서만 소비**된다. `TitleBorderBrush` **문자열 참조는 전수 3건**이다 — 정의(`.xaml.cs:25`), XAML 바인딩(`.xaml:44`), 그리고 **`OnSave` 위 주석(`.xaml.cs:57`)**. 주석은 "빈 값일 때 danger 테두리가 이미 표시돼 있어 오류 문구를 두지 않는다"는 **이번에 사라질 동작**을 설명하므로 심볼 제거와 함께 갱신 대상이다(plan-reviewer M1 — 갱신하지 않으면 "잔존 0건" acceptance가 실패하거나 틀린 주석이 남는다).
- 이 브러시 제거로 `using Microsoft.UI;`(`ColorHelper`)와 `using Microsoft.UI.Xaml.Media;`(`SolidColorBrush`/`Brush`)도 고아가 된다 — 파일 내 다른 소비처 0건 확인(Read 전문).
- `ControlCornerRadius` 오버라이드는 `Resources/`에 **없다**(grep) → WinUI 기본 4px. 하단 라인의 아래 모서리를 입력칸과 맞추려면 `CornerRadius="0,0,4,4"`가 필요하다.
- `TextControlBorderBrush`(`Palette.xaml:149`)=`AppBorderStrongColor`, `PointerOver`(`:150`)=`AppBorderStrongerColor`로 오버라이드돼 있다. **라인을 TextBox 위에 겹쳐 그리면 hover가 라인을 덮지 못하므로**, 이전 plan의 D3-a(hover 시 빨간 테두리가 회색으로 바뀌던 문제)가 **부수적으로 해소**된다.
- `AppDangerColor`(`Palette.xaml:31`) == `AppAccentColor`(`:39`) == `#FFF0716A` — 포커스 시 TextBox 자체 하단 accent 강조선과 **동색**이라 겹쳐도 색이 어긋나지 않는다.
- 필수 입력은 **제목 하나뿐**이다: `OnSave`(`TaskEditDialog.xaml.cs:54-62`)가 검증하는 필드는 `Vm.Title`뿐이고, 라벨에 `*`가 붙은 것도 제목뿐(`:34-39`).

### ②③ 카테고리 "미분류" — 현행 구현 (직접 Read)
- `TaskEditDialogViewModel.cs:59-63`: `CategoryOptions`가 `_noneCategoryLabel`("미분류")을 **첫 항목으로 붙여** 만들어진다. 이 항목이 요청 ②의 제거 대상.
- 저장 시 역변환: `BuildResult()`(`:105`)가 `SelectedCategoryOption == _noneCategoryLabel ? string.Empty : ...` — 즉 **"미분류" 선택 = 빈 문자열 저장**. 선택지를 없애면 이 매핑도 함께 정리해야 한다.
- 편집 로드: `:76`이 기존 항목의 빈 카테고리를 `_noneCategoryLabel`로 **되돌려 표시**한다. 선택지가 사라지면 이 대입은 목록에 없는 값을 넣는 꼴이라 콤보가 빈 상태가 된다 → 명시적으로 처리해야 한다(D3).
- **요청 ③은 이미 충족돼 있다**(신규 구현 불필요, 회귀만 막으면 된다):
  - 칸반: `TaskPageViewModel.BuildColumnGroups:140` — `string.IsNullOrEmpty(g.Key) ? Get("TaskCategory_None") : g.Key`
  - 목록: `TaskPageViewModel.RebuildCategoryGroups:173` — 동일 매핑
  - 두 곳 모두 **빈 카테고리를 표시 시점에만** "미분류"로 바꾸며 저장값은 빈 문자열 그대로다. 이번 diff는 이 파일에 닿지 않는다.
- `TaskCategory_None` resw 키(ko `미분류` / en `Uncategorized`, `Resources.resw:410`)는 **위 표시 2곳이 계속 소비**하므로 **제거하지 않는다**. (VM에서의 소비만 사라진다.)
- ⚠️ **`Category`에 null이 새면 DB가 깨진다**(plan-reviewer B1 — 직접 확인): 스키마가 `Todos.Category TEXT NOT NULL DEFAULT ''`(`DatabaseContext.cs:254`, 마이그레이션 `:127`도 동일)이고, 쓰기는 `cmd.Parameters["@category"].Value = t.Category`(`SqliteProjectRepository.cs:571`)로 **값을 그대로 바인딩**하며, 읽기는 `reader.GetString(...)`(`:114`, `:860`)이다. 즉 null이 저장 경로에 들어가면 **NOT NULL 제약 위반**, 어떻게든 저장되면 **읽기에서 캐스트 예외**가 난다.
  - 관련 위험: `SelectedCategoryOption`은 **비-nullable `string`**(`TaskEditDialogViewModel.cs:18`)인데 XAML 바인딩은 `SelectedItem="{x:Bind ... Mode=TwoWay}"`(`TaskEditDialog.xaml:70`)이고 `SelectedItem`은 `object`다. **ItemsSource에 없는 값을 넣으면 ComboBox가 선택을 거부하고 null을 TwoWay로 되써넣을 수 있다** — 이 프레임워크 동작은 이번 계획에서 **확인하지 않았다.**
  - 현행 코드는 항상 목록 내 값(`_noneCategoryLabel` 포함)이 선택돼 이 경로가 잠재돼 있었고, **T2의 D6(미선택 표현)이 이 경로를 처음 활성화**한다. 따라서 D6은 ComboBox 동작에 대한 추측 위에 세우지 않고 **VM 쪽 null 방어로 성립을 보장**한다(D6 참조).
- `AppSettingsDialogViewModel.DefaultTaskCategories`(`:218-221`) = `["UI·UX", "프론트엔드", "백엔드"]` — 항상 3개 이상이라 "미분류" 제거 후에도 `CategoryOptions`가 비지 않는다(D2의 성립 근거).
- `settings.TaskCategories`(사용자 정의)는 앞의 기본 3개 뒤에 붙는다 → 제거 후 **첫 항목은 항상 "UI·UX"**.

### ④ 네비게이션 버그 — 근본 원인 확정 (직접 Read)
`DashboardView`는 **재사용되는 단일 인스턴스**인데(`MainWindow.xaml.cs:24` 필드, `:126` 생성, `:524` 복귀 시 같은 인스턴스 재부착), 그 인스턴스가 시각 트리에서 떨어질 때 **모든 구독을 끊고 되살릴 경로를 스스로 없앤다**:

```
DashboardView.OnUnloaded (DashboardView.xaml.cs:38-52)
  :40  Unloaded -= OnUnloaded;                    ← 자기 자신을 해제
  :41  DataContextChanged -= OnDataContextChanged; ← 재구독 트리거를 해제  ★
  :46  DisplayCards.CollectionChanged -= ...
  :47-48 foreach(card) UnsubscribeCardEvents(card) ← 카드 이벤트 전부 해제  ★
  :50  _subscribedVm = null;
```

재현 경로(코드로 확정):
1. 대시보드 최초 표시 → `OnDataContextChanged`(`:74-98`)가 카드별 `OpenTodoRequested` 등을 구독 → 작업 버튼 동작.
2. 작업 버튼 클릭 → `MainWindow.ShowPage`(`:515-519`)가 `DashboardContent.Content`를 `TaskPage`로 교체 → **DashboardView가 트리에서 분리돼 `Unloaded` 발생** → 위 해제 실행.
3. "대시보드" 클릭 → `TaskPage.Back_Click`(`:151-152`) → `MainWindow.ShowDashboard`(`:522-527`)가 **같은 `_dashboardView` 인스턴스**를 다시 붙인다. **`DataContext`는 처음부터 끝까지 동일한 `MainViewModel`이라 `DataContextChanged`가 발생하지 않는다.**
4. → 재구독이 일어나지 않고, 3단계에서 `DataContextChanged` 핸들러마저 이미 떨어져 있어 **영구적으로 죽는다.** 카드의 작업 버튼을 눌러도 `OpenTodoRequested` 구독자가 0이라 아무 일도 일어나지 않는다. **증상과 정확히 일치.**

영향 범위(같은 원인, 전수):
- `SubscribeCardEvents`(`:136-144`)가 묶는 **6개 이벤트 전부**가 함께 죽는다 — `ShowGitStatusRequested`·`OpenTodoRequested`·`OpenHistoryRequested`·`OpenTestListRequested`·`ConfigureCommandSlotRequested`·`ChangeCommandIconRequested`.
- `DisplayCards.CollectionChanged` 구독도 끊겨, 복귀 후 **프로젝트 추가/삭제 시 카드 이벤트 구독이 갱신되지 않는다**(신규 카드의 버튼도 무반응).
- 진입 경로 무관하게 동일: `TaskPage`·`TestPage`·`NotificationPage`의 `Back_Click`이 모두 `ShowDashboard()`를 호출한다(grep 3건: `TaskPage.xaml.cs:152`·`TestPage.xaml.cs:86`·`NotificationPage.xaml.cs:66`).
- 반대로 `TaskPage`/`TestPage`/`NotificationPage`는 **진입할 때마다 새로 생성**되므로(`DashboardView.xaml.cs:184,217`·`MainWindow.xaml.cs:590,598`) 같은 문제가 없다 — 수정 대상은 `DashboardView` 한 곳뿐이다.
- `ReloadLanguageUI`(`MainWindow.xaml.cs:510`)는 `DashboardView`를 **새로 만들어** 붙이므로 새 인스턴스의 `DataContextChanged`가 정상 발동한다 — 이 경로는 버그 없음(수정 후에도 유지돼야 함).

### 4-D. 재사용 확인

| 신규 심볼 | 유사 기존 구현 검색 결과 | 재사용/신규 사유 |
|---|---|---|
| 제목 입력칸 하단 빨간 라인 | `Border`/`Rectangle`로 라인을 그리는 공용 스타일 없음(grep: `Height="2"`·`VerticalAlignment="Bottom"` 0건). `DashedAddButtonStyle`은 점선 테두리용이라 용도 불일치 | **신규(인라인, 스타일 미신설)** — 소비처 1곳뿐이라 `Styles.xaml`에 스타일을 만들면 소비처 0인 자산이 또 생긴다(`InputLabelStyle`이 그렇게 방치됐던 전례). 색은 기존 `AppDangerBrush` ThemeResource 재사용 |
| `DashboardView.OnLoaded` 핸들러 | 같은 파일의 `OnUnloaded`(`:38`)·`OnDataContextChanged`(`:74`)와 대칭. `MainWindow.OnRootGridLoaded`(`:85`)가 `Loaded`로 초기화하는 선례 | **신규(패턴 재사용)** — 기존 `Loaded` 핸들러가 이 클래스에 없다 |
| 구독/해제 본문 | 기존 `SubscribeCardEvents`/`UnsubscribeCardEvents`(`:136-154`) 및 `OnDataContextChanged`의 구독 루프 | **재사용 + 추출** — 구독 루프가 `OnDataContextChanged`·`OnDisplayCardsChanged`·(신규)`OnLoaded` **3곳**에서 반복되므로 공통화 기준(2회 이상)을 넘는다 → **카드 수준 2개 + VM 수준 2개, 총 4개 메서드로 추출**(D10 — 한 쌍으로 합치면 Reset 분기 동작이 바뀐다) |
| 카테고리 표시명 매핑 | `TaskPageViewModel:140,173`에 이미 존재 | **재사용(무변경)** — 요청 ③은 기존 코드가 이미 충족. 신규 작성 0 |

## 시각 요소 분해

> 출처: 사용자 제공 시안 이미지(새 작업 다이얼로그). 목업 HTML이 없어 **px 값은 확정 불가** — 구조·상대 관계를 명세하고 수치는 기존 관례(WinUI 기본 `CornerRadius` 4, 포커스 강조선 2px)를 따른다. 최종 판정은 빌드 후 사용자 육안 대조(⏳ HUMAN-VERIFY).
>
> 이번 범위는 **제목 입력칸 1요소**다. 나머지 요소(헤더 pill·라벨·카드·버튼·여백)는 이전 plan에서 정합을 마쳤고 **사용자가 이상 없음으로 확인**했으므로 이 표에 다시 넣지 않는다.

| 요소 | 속성 | 디자인 값 | 확인 방법 |
|---|---|---|---|
| 제목 입력칸 | 필수 강조 형태 | **하단 가로 라인만** (4면 테두리 아님) | 시안 — 좌·우·상단은 다른 입력칸과 같은 저강도 테두리 |
| 제목 입력칸 | 라인 색 | danger 살몬 `AppDangerBrush`(#F0716A) | 시안 + `Palette.xaml:31` |
| 제목 입력칸 | 라인 두께 | 2px (입력칸 기본 테두리 1px보다 굵게) | 시안에서 하단만 뚜렷 |
| 제목 입력칸 | 라인 표시 조건 | **항상** — 값이 채워져도, 포커스·hover 여부와 무관하게 유지 | 사용자 요청 "항상 표시" |
| 제목 입력칸 | 라인 폭·모서리 | 입력칸 가로폭과 일치. 아래 모서리는 입력칸 라운드와 **육안상 이질감이 없는 수준**(정확한 4px 재현은 불가 — `Height="2"` 요소의 `CornerRadius`는 높이 절반인 1px로 클램프된다, plan-reviewer m1). 삐져나오면 좌우 `Margin` 1px로 보정 | 시안 + ⏳ HUMAN-VERIFY |
| 설명·카테고리·우선순위·날짜 | 필수 강조 | **없음** (선택 입력) | 사용자 요청 "필수 입력 ui에만" |
| 제목 라벨 빨간 `*` | 존재 | 유지(현행) | 시안 |

## Decisions

| # | 항목 | 카테고리 | 결정 | Source |
|---|---|---|---|---|
| D1 | 하단 라인 구현 방식 | 기술 | `TextBox`를 `Grid`로 감싸고 **하단 정렬 `Border`(Height 2, `AppDangerBrush`, `CornerRadius 0,0,4,4`, `IsHitTestVisible="False"`)를 겹쳐 그린다.** **기각한 대안**: ⓐ `BorderThickness="1,1,1,2"` + `BorderBrush` — 4면이 한 브러시라 상·좌·우까지 빨개져 시안과 다르다. ⓑ `TextBox` 템플릿 복사 — 유지비가 크고 이 화면 1곳에만 필요하다. ⓒ `Styles.xaml`에 전용 스타일 신설 — 소비처 1곳(4-D) | 시안 + `ControlCornerRadius` 미오버라이드 grep |
| D2 | 라인이 hover/포커스에 가려지지 않음 | 기술 | 라인은 `TextBox` **바깥에 겹친 형제 요소**라 `TextControlBorderBrushPointerOver`(회색) 오버라이드의 영향을 받지 않는다 → 이전 plan의 **D3-a(hover 시 회색 전환) 문제가 부수적으로 해소**된다. 포커스 시 TextBox 자체 하단 강조선은 accent(#F0716A)로 danger와 **동색**이라 겹쳐도 어긋나지 않는다 | `Palette.xaml:31,39,150` |
| D3 | `TitleBorderBrush` 처리 | 위치 | **제거**한다 — 유일한 소비처(XAML `BorderBrush` 바인딩)가 사라져 고아가 된다. 정적 브러시 2개(`_titleRequiredBrush`·`_titleNormalBrush`)와 고아가 되는 `using Microsoft.UI;`·`using Microsoft.UI.Xaml.Media;`도 함께 정리하고, **이 이름을 지목하는 `OnSave` 위 주석(`:57`)도 갱신**한다 | grep 전수 **3건**(정의 `:25` · XAML `:44` · 주석 `:57`) |
| D4 | 빈 제목 저장 차단 | 로직 | **유지**한다(`OnSave`의 `args.Cancel = true`). 라인이 상시 표시로 바뀌면 "비어 있음" 신호 기능은 오히려 약해지므로, 저장 차단이 유일한 실제 방어선이 된다 — 제거하면 빈 제목 작업이 생성되는 회귀 | `TaskEditDialog.xaml.cs:54-62` |
| D5 | 카테고리 콤보 기본값 | UX | **첫 카테고리("UI·UX")를 자동 선택**한다 | 사용자 결정 2026-07-20 |
| D6 | 편집 모드의 빈 카테고리 | 로직 | **콤보를 미선택 상태로 열고, 사용자가 고르지 않으면 빈 카테고리를 그대로 저장**한다(D5의 자동 선택은 **새 작업에만** 적용). 근거: 요청 ③이 "기존 카테고리 없는 항목은 목록에서 미분류로 표시"를 요구하므로 그 항목의 빈 값이 **보존돼야** 규칙이 성립한다. 편집창을 여는 것만으로 "UI·UX"가 붙으면 그 항목이 미분류 그룹에서 조용히 사라진다(무성 데이터 변경) | 요청 ③ + `TaskPageViewModel:140,173` |
| D6-a | 미선택의 null 방어 | 기술 | **`SelectedCategoryOption`을 `string?`으로 바꾸고 `BuildResult()`에서 `?? string.Empty`로 흡수**한다. 근거: ComboBox가 목록에 없는 값을 받았을 때 null을 TwoWay로 되써넣는지는 **확인되지 않은 프레임워크 동작**이고, null이 새면 `Todos.Category`(`TEXT NOT NULL`)에서 저장 실패 또는 읽기 예외가 난다. 방어 한 줄로 **ComboBox가 어느 쪽으로 동작하든 D6이 성립**하게 만든다(추측 대신 무해화). **기각한 대안**: 실제 동작을 실험해 한쪽으로 확정 — 실행 확인이 필요해 계획 단계에서 끝나지 않고, 확정해도 방어보다 안전하지 않다 | plan-reviewer B1 + `DatabaseContext.cs:254` + `SqliteProjectRepository.cs:571,114,860` |
| D7 | `TaskCategory_None` resw 키 | 위치 | **존치**한다 — VM 소비는 사라지지만 칸반·목록 표시 2곳이 계속 쓴다. 지우면 표시명이 raw 키로 렌더된다(`LocalizationService.Get`은 미존재 키를 키 문자열로 반환) | `TaskPageViewModel:140,173` + `LocalizationService.cs:56,60` |
| D8 | 목록·칸반 "미분류" 표시 | 범위 | **무변경**(요청 ③은 이미 충족). 이번 diff는 `TaskPageViewModel`에 닿지 않으며, 회귀 방지 확인만 acceptance에 둔다 | Investigation Log ②③ |
| D9 | 네비게이션 버그 수정 방식 | 기술 | **`Loaded`에서 재구독**한다. `Unloaded`에서는 구독만 해제하고 **`Unloaded`·`DataContextChanged` 핸들러 자체는 떼지 않는다**(그것이 재구독 경로를 없앤 원인). **기각한 대안**: ⓐ `Unloaded`에서 아예 해제하지 않기 — 페이지 표시 중에도 카드 이벤트가 살아 있어 백그라운드 다이얼로그가 뜰 수 있다. ⓑ `MainWindow.ShowDashboard()`에서 재구독 호출 — 뷰 내부 사정을 창이 알아야 해 의존이 역전되고, 다른 복귀 경로가 생기면 또 빠뜨린다 | `DashboardView.xaml.cs:38-52` + `MainWindow.xaml.cs:126,524` |
| D10 | 구독 로직 중복 | 구조 | 구독/해제 루프를 private 메서드로 추출하되 **카드 수준(`SubscribeCards`/`UnsubscribeCards`)과 VM 수준(`SubscribeAll`/`UnsubscribeAll`) 2단으로 나눈다.** 한 쌍으로 묶으면 4개 호출처의 요구가 충돌한다 — `OnLoaded`/`OnUnloaded`는 `CollectionChanged`·`_subscribedVm`까지 포함해야 하지만, `OnDisplayCardsChanged`의 Reset 분기는 **카드만** 재구축하는 것이 현행 동작이라 같은 쌍을 쓰면 자기 이벤트를 해제·재등록하는 다른 동작이 된다. 호출처별 매핑은 T3 구성의 표에 못박는다 | plan-reviewer M2 + `DashboardView.xaml.cs:74-134` |
| D11 | 수정 범위(버튼 6종) | 범위 | **한 곳 수정으로 6개 이벤트가 함께 되살아난다** — 작업 버튼만 선별 복구하는 것은 불가능하고 무의미하다. 요청 범위 확대가 아니라 **같은 결함의 전체 영향** | `SubscribeCardEvents:136-144` |

## PRD Coverage

| PRD ID | 우선순위 | 대응 task | 상태 |
|---|---|---|---|
| FR-T5 (편집 다이얼로그 + 삭제 확인) | Must | T1, T2 | ✅ 커버 (필수 표시 형태 + 카테고리 선택지 정정) |
| FR-C3 (페이지 네비 컨테이너 — 대시보드 ↔ 페이지 전환) | Must | T3 | ✅ 커버 (**회귀 수정** — 복귀 후 재진입 불가 상태 해소) |
| FR-T2 (작업 전체 페이지 + 칸반/목록) | Must | — | 이번 범위 외 (기구현). 단 T3이 **진입 경로**를 복구하므로 간접 관련 |
| FR-T1 (작업 데이터 모델) | Must | — | 이번 범위 외 (`TodoItem` 무변경, 스키마·직렬화 무변경) |
| FR-T6 (테스트 추가 토글) | Should | — | 이번 범위 외 (기구현 — `AddTestRequested` 경로 무변경, T1 acceptance에 회귀 방지) |
| FR-T7 (담당자(who) 필드) | Could | — | **의도적 미구현** — 사용자 제외 결정 유지(`deferred.md:9`) |
| FR-E1 (테스트 페이지) | Must | — | 이번 범위 외 (기구현). T3이 진입 경로를 함께 복구 |
| FR-T3·T4·T8·FR-C1·C2·C4·FR-S1~S5·FR-E2~E5·FR-H*·FR-N* | Must/Should | — | 이번 범위 외 (Phase 0~5에서 기구현, 이번 diff가 닿지 않음). 단 **FR-S5**는 기구현이 아니라 `deferred.md:6` 대기 중 — 이번에도 유지 |
| NFR-1 (빌드 오류 0·신규 경고 0) | — | T1·T2·T3 | 검증 대상 (기존 경고 5건 제외) |
| NFR-2 (계층 위반 0) | — | T2·T3 | 검증 대상 (도메인 무변경, VM→페이지 역참조 없음) |
| NFR-3 (DB 스키마 하위호환) | — | — | ✅ 무영향 (`TodoItem`·스키마·직렬화 무변경) |
| NFR-4 (다국어 ko/en 대칭) | — | T2 | ✅ 충족 예정 (**신규·제거 resw 키 0** — `TaskCategory_None` 존치) |
| NFR-5 (테스트) | — | — | 조건 미발동 (테스트 프로젝트 부재 — AGENTS.md 명시) |

## 작업 단계

### T1 — 제목 필수 표시를 상시 하단 빨간 라인으로 변경 `Type C`
- [x] 구현
- **Files**: `DevDashboard_WinUI/Presentation/Views/Dialogs/TaskEditDialog.xaml`, `DevDashboard_WinUI/Presentation/Views/Dialogs/TaskEditDialog.xaml.cs`
- **Design**: ① 배치 — 마크업은 `TaskEditDialog.xaml`의 제목 필드 블록(`:41-45`)에만. 코드비하인드에는 **아무것도 추가하지 않고 제거만** 한다. ② 신규 심볼 — 없음(인라인 `Grid` + `Border` 1개, 이름 없음). ③ 의존 방향 — XAML이 `AppDangerBrush` ThemeResource를 소비. 역참조 없음. ④ 비추상화 — 필수 표시를 `Style`·`Behavior`·재사용 컨트롤로 빼지 않는다(소비처 1곳, 4-D).
- **구성**:
  - 제목 `TextBox`를 `Grid`로 감싸고, 형제로 `Border`(`Height="2"`, `VerticalAlignment="Bottom"`, `Background="{ThemeResource AppDangerBrush}"`, `CornerRadius="0,0,4,4"`, `IsHitTestVisible="False"`)를 겹친다(D1).
  - `TextBox`의 `BorderBrush="{x:Bind local:TaskEditDialog.TitleBorderBrush(...)}"` **바인딩 제거** → 다른 입력칸과 같은 기본 테두리로 복귀.
  - 코드비하인드에서 `TitleBorderBrush()`·`_titleRequiredBrush`·`_titleNormalBrush`와 고아가 되는 `using Microsoft.UI;`·`using Microsoft.UI.Xaml.Media;` **제거**(D3).
  - `xmlns:local` 선언은 **다른 소비처가 없으면 함께 제거**한다(제거 전 이 파일 내 `local:` 참조 0건을 grep으로 확인 — 남기면 미사용 선언).
  - `OnSave`의 **차단 로직(`args.Cancel = true`)은 손대지 않되**, 그 위 주석(`.xaml.cs:56-57`)은 **갱신**한다(plan-reviewer M1) — 현재 주석이 "빈 값일 때 danger 테두리가 이미 표시돼 있어"라는 **사라질 동작**을 근거로 들고 제거될 `TitleBorderBrush`를 지목한다. 새 문구는 D4의 실제 근거(라인이 상시 표시라 "비어 있음" 신호로 쓸 수 없고, **저장 차단이 유일한 방어선**)를 적는다.
- **Acceptance**: 빌드 성공(신규 경고 0) + 제목 입력칸 아래에 danger 라인이 **값 유무와 무관하게** 렌더 + **설명·카테고리·우선순위·날짜 입력에는 라인이 없음** + `TitleBorderBrush`·`_titleRequiredBrush`·`_titleNormalBrush` 문자열 잔존 **0건**(grep — **주석 포함**, M1) + `OnSave` 위 주석이 새 동작을 설명함(사라진 테두리 동작을 근거로 들지 않음) + `TaskEditDialog.xaml`에 `BorderBrush=` 잔존 0건(grep) + **빈 제목으로 "등록"을 눌러도 다이얼로그가 닫히지 않음**(D4 회귀 방지) + **"테스트 추가" ON으로 만든 작업이 여전히 테스트를 생성**(FR-T6 회귀 방지 — `AddTestRequested` 경로 무변경)
- **Edge Cases**: `IsHitTestVisible="False"`를 빠뜨리면 **라인이 입력칸 하단 클릭을 가로채** 커서가 안 잡힌다. `Grid`로 감싸며 기존 `StackPanel`의 세로 흐름이 깨지지 않게 할 것(라벨-입력칸 간격은 `InputLabelStyle`의 `Margin 0,0,0,4`가 담당 — 이전 plan Progress Log). 다이얼로그 폭이 바뀌어도 라인은 `Grid` 폭을 따라 늘어난다(고정 `Width` 금지). 포커스 시 TextBox 자체 강조선과 2중으로 겹쳐 라인이 더 굵어 보일 수 있음(동색이라 색은 어긋나지 않음 — ⏳ HUMAN-VERIFY). 라인이 입력칸 라운드 밖으로 삐져나오면 `CornerRadius`/좌우 `Margin` 1px 조정(⏳ HUMAN-VERIFY).
- **Halt Forecast**: 없음 — 파일 2개, 파괴적·외부 작업 없음. 심볼 제거는 사전 승인 항목 등재.

### T2 — 카테고리 "미분류" 선택지 제거 + 빈 카테고리 보존 `Type C`
- [x] 구현
- **Files**: `DevDashboard_WinUI/Presentation/ViewModels/TaskEditDialogViewModel.cs`
- **Design**: ① 배치 — 선택지 구성·초기 선택·저장 역변환 모두 `TaskEditDialogViewModel` 안에서 끝난다(다이얼로그·페이지 무변경). ② 신규 심볼 — 없음(기존 `CategoryOptions`·생성자·`BuildResult` 수정 + `SelectedCategoryOption`의 nullable 전환). `_noneCategoryLabel` 필드는 **제거**(소비처가 전부 사라진다). ③ 의존 방향 — VM은 `AppSettingsDialogViewModel.DefaultTaskCategories`와 `settings.TaskCategories`만 참조(현행 유지), `TaskPage`를 참조하지 않는다. ④ 비추상화 — "빈 카테고리 ↔ 표시명" 매핑을 공용 헬퍼로 빼지 않는다(표시 쪽 2곳은 이미 각자 처리 중이고 이번엔 VM 쪽 매핑이 **사라지는** 방향이라 공통화 대상이 아니다).
- **구성**:
  - `CategoryOptions`에서 선두 `_noneCategoryLabel` 제거 → `DefaultTaskCategories` + `settings.TaskCategories`만(D5 성립 근거: 기본 3개가 항상 존재).
  - `_noneCategoryLabel` 필드와 `LocalizationService.Get("TaskCategory_None")` 호출 제거. **resw 키 자체는 존치**(D7).
  - **`SelectedCategoryOption`을 `string?`으로 전환**(`:18`, D6-a) — 미선택을 정식으로 표현할 수 있게 하고, ComboBox가 null을 되써넣어도 타입이 깨지지 않게 한다.
  - 새 작업 분기: `SelectedCategoryOption = CategoryOptions[0]`(D5).
  - 편집 분기: 기존 카테고리가 비어 있으면 `SelectedCategoryOption = null`로 두어 **콤보 미선택**(D6). 값이 있으면 현행대로 그 값.
  - `BuildResult()`: **`todo.Category = SelectedCategoryOption ?? string.Empty;`**(D6-a) — 미선택이든 ComboBox가 null을 되써넣었든 **빈 문자열로 흡수**돼 `Todos.Category`(NOT NULL)가 깨지지 않는다. `_noneCategoryLabel` 비교 제거.
  - 관련 주석(`:13` "미분류 표시 라벨", `:26` "미분류 + 기본 + 사용자 정의") 갱신 — 새 동작과 어긋나는 주석을 남기지 않는다. `?? string.Empty`에는 **왜** 방어하는지(NOT NULL 스키마 + ComboBox 미선택) 한 줄 주석을 단다.
- **Acceptance**: 빌드 성공(신규 경고 0 — **nullable 전환으로 인한 CS8600/CS8618 등 신규 경고 0 포함**) + 카테고리 콤보에 **"미분류" 항목이 없음** + 새 작업 시 콤보가 **"UI·UX"로 선택된 상태**로 열림 + **카테고리가 빈 기존 작업을 편집하면 콤보가 미선택으로 열리고, 그대로 저장하면 카테고리가 여전히 빈 문자열**(D6) + **그 저장이 예외 없이 완료되고, 저장 후 목록을 다시 열어도 항목이 정상 표시**(D6-a — null이 DB에 새면 여기서 드러난다) + 카테고리가 있는 기존 작업 편집 시 그 값이 선택된 채 열림 + **칸반·목록에서 빈 카테고리 항목이 계속 "미분류" 그룹으로 표시**(D8 회귀 방지) + `_noneCategoryLabel` 심볼 잔존 **0건**(grep) + `BuildResult`에 `?? string.Empty` **1건**(grep — D6-a 방어 생존) + `TaskCategory_None` resw 키는 ko/en 양쪽 **존치**(grep — 지우면 표시가 raw 키로 깨진다)
- **Edge Cases**: **`SelectedCategoryOption`이 null**(ComboBox 미선택 또는 목록에 없는 값을 거부하며 되써넣은 경우) → `BuildResult`의 `?? string.Empty`가 흡수(D6-a). **빈 문자열** → 그대로 빈 카테고리로 저장돼 미분류 그룹에 들어간다(동일 결과). `settings.TaskCategories`가 비어도 기본 3개가 있어 `CategoryOptions[0]`은 항상 성립(빈 컬렉션 인덱싱 예외 없음) — 사용자가 앱 설정에서 카테고리를 전부 지워도 `DefaultTaskCategories`는 하드코딩이라 리스트가 빌 수 없다. 편집 대상의 카테고리가 목록에 없는 값일 때(설정에서 그 카테고리를 삭제한 뒤 편집) → 콤보가 미선택으로 열리고, 그대로 저장하면 **원래 카테고리가 빈 문자열로 바뀐다**(기존 코드도 동일한 성질이었으나 이번에 관찰 가능해짐 → Deferred 등재). 새 작업의 기본 선택이 생겨 **"카테고리 없는 작업"을 새로 만들 수단이 사라진다**(사용자 D5 선택의 의도된 귀결 → Deferred 등재).
- **Halt Forecast**: 없음 — 단일 파일, 스키마·resw 변경 0.

### T3 — 대시보드 복귀 시 카드 이벤트 재구독 (네비게이션 버그) `Type C`
- [x] 구현
- **Files**: `DevDashboard_WinUI/Presentation/Views/DashboardView.xaml.cs`
- **Design**: ① 배치 — 수정은 `DashboardView` 코드비하인드 한 곳. `MainWindow`·`TaskPage`·VM은 건드리지 않는다(D9 ⓑ 기각 — 뷰의 구독 수명을 창이 알아야 하는 역의존 회피). ② 신규 심볼 — `OnLoaded(object, RoutedEventArgs)` 핸들러 1개 + **2단으로 분리한** private 메서드 4개(아래 D10 표). ③ 의존 방향 — `DashboardView` → `MainViewModel`/`ProjectCardViewModel`(현행 그대로), 역참조 신설 없음. ④ 비추상화 — 구독 수명을 관리하는 범용 헬퍼·베이스 클래스·`WeakEventListener`를 도입하지 않는다(같은 문제를 가진 뷰가 이 하나뿐 — 다른 페이지는 매번 새로 생성된다).
- **구성**:
  - 생성자에 `Loaded += OnLoaded;` 추가(기존 `DataContextChanged`·`Unloaded` 구독은 유지).
  - **추출 메서드의 책임을 2단으로 나눈다**(plan-reviewer M2 — 한 쌍으로는 4개 호출처의 요구가 서로 충돌한다). 카드 수준과 VM 수준을 섞지 않는 것이 핵심:

    | 메서드 | 책임 |
    |---|---|
    | `SubscribeCards()` | **`_subscribedVm`이 null이면 반환**(현행 `:109` 가드 보존 — 없으면 CS8602 신규 경고). 아니면 `_subscribedVm.DisplayCards`의 카드에 `SubscribeCardEvents` + `_subscribedCards`에 등록 |
    | `UnsubscribeCards()` | `_subscribedCards` 전체에 `UnsubscribeCardEvents` + 집합 비우기 |
    | `SubscribeAll(MainViewModel? vm)` | **VM을 파라미터로 받는다**(아래 사유). null이면 **조용히 반환**. 아니면 `_subscribedVm = vm` + `DisplayCards.CollectionChanged` 구독 + `SubscribeCards()` |
    | `UnsubscribeAll()` | `_subscribedVm`이 null이면 반환. 아니면 `CollectionChanged` 해제 + `UnsubscribeCards()` + `_subscribedVm = null` |

    `SubscribeAll`이 `DataContext`를 직접 읽지 않고 **파라미터로 받는 이유**: 현행 `OnDataContextChanged`는 `args.NewValue`로 구독하는데(`:87`), 이벤트 발생 시점에 `DataContext` 프로퍼티가 이미 갱신돼 있는지는 **확인하지 않은 프레임워크 동작**이다(B1과 같은 유형의 가정). 파라미터화하면 `OnDataContextChanged`는 `args.NewValue`를, `OnLoaded`는 `DataContext`를 넘겨 **가정 없이 양쪽 모두 성립**한다.

    | 호출처 | 사용 | 이유 |
    |---|---|---|
    | `OnLoaded`(신규) | `SubscribeAll(DataContext as MainViewModel)` | 복귀 시 VM 수준까지 통째로 복원해야 `CollectionChanged`도 되살아난다 |
    | `OnUnloaded` | `UnsubscribeAll()` | 트리에서 떨어지는 동안 전부 해제 |
    | `OnDataContextChanged` | `UnsubscribeAll()` → `SubscribeAll(args.NewValue as MainViewModel)` | 기존 동작(이전 VM 해제 후 새 VM 구독)과 동일. `args.NewValue`를 그대로 쓰므로 현행과 의미가 바뀌지 않는다 |
    | `OnDisplayCardsChanged` **Reset 분기** | `UnsubscribeCards()` → `SubscribeCards()` | **카드 전용**을 쓴다. VM 수준을 쓰면 자기가 처리 중인 `CollectionChanged`를 해제·재등록하고 `_subscribedVm`을 null로 만들었다 되살리는 **다른 동작**이 된다 |
    | `OnDisplayCardsChanged` Add/Remove 분기 | 현행 유지(개별 항목 구독/해제) | 전체 재구축이 불필요 |
  - `OnLoaded`: **`_subscribedVm`이 현재 `DataContext`와 다르거나 null이면** `UnsubscribeAll()` 후 `SubscribeAll()`. 같으면 아무것도 하지 않는다. 단순히 `is null`만 보지 않는 이유는 plan-reviewer m3 — `Loaded`/`Unloaded`가 같은 레이아웃 패스에서 역순 처리되면 `is null` 가드가 재구독을 건너뛴 뒤 `Unloaded`가 해제해 **원래 버그로 되돌아갈** 수 있다. "현재 DataContext 기준으로 구독 상태가 맞는가"를 판정하면 순서 가정에 기대지 않는다.
  - `OnUnloaded`: `UnsubscribeAll()`만 호출하고 **`Unloaded -= OnUnloaded`·`DataContextChanged -= OnDataContextChanged` 두 줄을 제거**한다(D9 — 이 두 줄이 재구독 경로를 없앤 직접 원인).
  - 왜 이렇게 두는지 주석으로 남긴다 — "이 뷰는 `MainWindow._dashboardView`로 **재사용**되므로 트리에서 분리됐다 다시 붙는다. `DataContext`가 바뀌지 않아 `DataContextChanged`가 재발동하지 않으니 `Loaded`가 재구독 지점이다."
- **Acceptance**: 빌드 성공(신규 경고 0) + `OnUnloaded`에 `Unloaded -=`·`DataContextChanged -=` 잔존 **0건**(grep — 남으면 버그 그대로) + `Loaded +=` **1건**(grep) + **대시보드 → 작업 → 대시보드 → 작업 재진입이 동작**(수동 확인) + 같은 왕복 후 **테스트·Git 상태·작업 기록·명령 슬롯 버튼도 동작**(D11, 수동 확인) + 복귀 후 **프로젝트를 추가하면 새 카드의 버튼도 동작**(`DisplayCards.CollectionChanged` 재구독 확인) + **작업 페이지를 보는 동안 카드 이벤트가 발동하지 않음**(D9 ⓐ 기각 근거 — 페이지 위에 다이얼로그가 뜨지 않는다) + **왕복을 3회 이상 반복해도 다이얼로그·페이지가 한 번만 열림**(중복 구독 회귀 방지) + 언어 변경 후 대시보드 재생성 경로(`ReloadLanguageUI`)에서도 버튼 정상
- **Edge Cases**: `Loaded`는 창 최초 표시 시에도 발동하므로 `OnDataContextChanged`의 최초 구독과 **겹칠 수 있다** → "`_subscribedVm`이 현재 `DataContext`와 같으면 아무것도 하지 않는" 가드가 중복 구독을 막는다(가드 없으면 버튼 1회 클릭에 다이얼로그 2개). **`Loaded`/`Unloaded` 처리 순서가 뒤바뀌어도** 같은 가드가 상태를 기준으로 판정하므로 재구독이 누락되지 않는다(m3). `Loaded` 시점에 `DataContext`가 아직 `null`일 수 있다(`MainWindow`는 `:126`에서 생성과 동시에 `DataContext`를 넣지만 순서 보장에 기대지 않는다) → `SubscribeAll()`이 null이면 조용히 반환. `Unloaded`는 창을 닫을 때도 발동하며, 그때는 재부착이 없어 해제만 되고 끝난다(누수 없음). `ReloadLanguageUI`(`MainWindow.xaml.cs:510`)는 **새 인스턴스**를 만들어 붙이는데, 새 인스턴스의 `DataContextChanged` 구독이 구 인스턴스의 `Unloaded`보다 **먼저** 일어나 같은 `ProjectCardViewModel`들에 잠시 이중 구독되는 구간이 생긴다 — 다만 핸들러가 **인스턴스별 델리게이트**라 서로의 구독을 해제하지 못해 충돌이 없고, 그 사이 사용자 입력이 불가능해 무해하다(기존 동작과 동일 — plan-reviewer m2).
- **Halt Forecast**: 없음 — 단일 파일, 공개 API·시그니처 변경 없음(추가 심볼 전부 private).

## 사전 승인 항목 (일괄 승인 대상)
- `TaskEditDialog.xaml.cs`에서 `TitleBorderBrush()`·정적 브러시 2개·고아 `using` 2개 **제거** + `OnSave` 위 주석 갱신(T1, D3) — 소비처 전수 확인 완료(**참조 3건** = 정의 `:25` + XAML `:44` + 주석 `:57`).
- `TaskEditDialogViewModel`의 `_noneCategoryLabel` 필드 **제거**, `SelectedCategoryOption`을 **`string?`로 전환**, `CategoryOptions`·`BuildResult` 동작 변경(T2, D5·D6·D6-a) — 호출부는 `TaskEditDialog` 1곳뿐(이전 plan grep 확인, 이번에 재확인). 도메인·스키마 무변경.
- `DashboardView.OnUnloaded`에서 핸들러 해제 2줄 **제거** + 구독 로직 4개 메서드로 추출(T3, D9·D10) — 전부 private, 외부 계약 무변경.
- 로컬 작업 브랜치 `task/taskedit-dialog-design-align`(현행 유지, 사용자 결정)에서 task별 commit.

## 불가피한 Halt (위임 불가)
- master 병합·push·태그·릴리즈·PR — **이번 작업 완료 후 이전 작업분과 함께 한 번에 별도 승인**(사용자 결정 2026-07-20).
- 시안 대조의 **최종 시각 판정** — 빌드는 마크업 존재만 보증하고 "시안과 같아 보이는가"는 사용자만 판정 가능(⏳ HUMAN-VERIFY).
- **T3의 동작 확인** — 네비게이션 왕복은 빌드로 검증 불가하며 앱 실행이 필요하다(⏳ HUMAN-VERIFY).

## Deferred / Follow-up
- **[카테고리 필터에서 "미분류"를 고를 수 없음]** — `TaskPage`의 카테고리 필터 콤보는 `AvailableCategories`(실제 카테고리만)로 구성돼(`TaskPage.xaml.cs:72-75`) **"미분류" 그룹만 골라 보는 필터가 없다**. 이번 변경으로 "미분류"가 표시 전용이 되면서 이 비대칭이 더 두드러진다. 필터에 "미분류" 항목 추가 검토. (T2 조사에서 발견)
- **[삭제된 카테고리를 가진 작업을 편집하면 카테고리가 사라짐]** — 앱 설정에서 카테고리를 지운 뒤 그 카테고리를 쓰던 작업을 편집하면, 콤보에 해당 값이 없어 미선택으로 열리고 그대로 저장 시 **빈 카테고리가 된다**. 기존 코드도 같은 성질이었으나 D6으로 미선택 상태가 정식 표현이 되면서 관찰 가능해졌다. 콤보에 "현재 값"을 임시 항목으로 추가하는 방식 검토. (T2 Edge Case)
- **[카테고리 없는 작업을 새로 만들 수단 부재]** — D5(첫 카테고리 자동 선택)의 귀결로, 새 작업은 항상 카테고리를 갖게 된다. "미분류"로 두고 싶으면 콤보를 비울 방법이 필요하다(선택 해제 항목 또는 지우기 버튼). 기존 빈 카테고리 항목은 그대로 유지되므로 표시 규칙 자체는 계속 유효하다. (D5의 귀결)
- **[이전 plan Deferred 유지]** — `[시작일을 지울 수단 부재]`·`[다른 다이얼로그 라벨 스타일 불일치]`·`[빈 제목 오류 문구 제거에 따른 접근성 약화]`·`[SUGGEST status 선택적 파라미터]`는 이번 작업이 해소하지 않는다 → `docs/plans/deferred.md`에 그대로 유지. 단 **hover 시 테두리 회색 전환(D3-a)** 은 T1의 D2로 **해소**되므로 완료 처리한다.
- 기타 대장(`docs/plans/deferred.md`) 기등재 항목은 이번 작업과 무관 — 그대로 유지.

## Out of Scope
- `TodoItem` 도메인·SQLite 스키마·직렬화 변경.
- **칸반/목록의 "미분류" 그룹 정렬 통일**(`deferred.md:27`) — 이번 요청은 표시 유지이지 정렬 규칙 변경이 아니다.
- 다른 다이얼로그·다른 페이지의 필수 입력 표시 정합 — 이번은 `TaskEditDialog` 제목 1곳.
- `TaskCategory_None` resw 키 제거(D7 — 표시가 깨진다).
- 다른 화면의 이벤트 구독 수명 점검 — `DashboardView` 외에는 매번 새로 생성돼 같은 결함이 없음을 확인했다(Investigation Log ④).
- 픽셀 단위 수치 일치 — 목업 HTML이 없어 대조 불가. 구조·형태 일치까지가 목표.

## Open Questions
- [x] 카테고리 "미분류" 제거 후 새 작업의 기본 선택값 → **첫 카테고리("UI·UX") 자동 선택**(사용자, 2026-07-20)
- [x] 이번 작업을 진행할 브랜치 → **현재 `task/taskedit-dialog-design-align`에서 이어서, 병합은 최종 1회**(사용자, 2026-07-20)
- [x] 이전 plan의 F-8 미확인 7항목 처리 → **필수 테두리 1건만 이번에 수정, 나머지 6건은 이상 없음으로 종결**(사용자, 2026-07-20)
- [x] 편집 모드에서 빈 카테고리 항목의 콤보 표현 → **미선택 유지 + 빈 값 보존**(요청 ③의 성립 조건에서 도출 — D6)

## 검증 방법
- 빌드: `"C:/Program Files/Microsoft Visual Studio/18/Professional/MSBuild/Current/Bin/MSBuild.exe" "DevDashboard_WinUI/DevDashboard.csproj" -t:Build -p:Configuration=Debug -p:Platform=x64` → 오류 0 + **AGENTS.md 기존 경고 5건(NU1903 1 + CS0612 4) 외 신규 경고 0**
- 회귀 방지 grep(⚠️ **판정 대상을 정확히 한정**):
  - `TaskEditDialog.xaml.cs`에 `TitleBorderBrush`·`_titleRequiredBrush`·`_titleNormalBrush` **0건**(T1 — **주석 포함**. `:57` 주석이 이 이름을 지목하므로 갱신하지 않으면 실패한다)
  - `TaskEditDialog.xaml`에 `BorderBrush=` **0건**(T1) / `IsHitTestVisible="False"` **1건**(라인이 클릭을 가로채지 않음)
  - `TaskEditDialog.xaml.cs`에 `args.Cancel = true` **1건 이상**(D4 — 빈 제목 저장 차단 생존)
  - `AddTestRequested` 대입부 **1건 이상**(FR-T6 생존)
  - `TaskEditDialogViewModel.cs`에 `_noneCategoryLabel`·`TaskCategory_None` **0건**(T2)
  - `TaskEditDialogViewModel.cs`의 `BuildResult`에 `?? string.Empty` **1건**(D6-a — null이 `Todos.Category`(NOT NULL)로 새는 것을 막는 방어)
  - `Strings/{ko-KR,en-US}/Resources.resw`에 `TaskCategory_None` **각 1건 존치**(D7 — 지우면 표시가 raw 키로 깨진다)
  - `TaskPageViewModel.cs`의 `TaskCategory_None` 소비 **2건 유지**(D8 — 칸반·목록 표시)
  - `DashboardView.xaml.cs`의 `OnUnloaded`에 `Unloaded -=`·`DataContextChanged -=` **0건**, `Loaded +=` **1건**(T3)
  - `DashboardView.xaml.cs`에 `SubscribeCards`·`UnsubscribeCards`·`SubscribeAll`·`UnsubscribeAll` **4개 모두 정의됨**(M2 2단 분리 — 한 쌍으로 합치면 Reset 분기 동작이 바뀐다)
- 동작 확인(빌드로 검증 불가 → ⏳ HUMAN-VERIFY):
  - **T1**: 제목 입력칸 하단 라인이 값 유무·포커스·hover와 무관하게 유지 / 다른 입력칸엔 라인 없음 / 라인 위를 클릭해도 입력 커서 정상 / 빈 제목으로 "등록" 시 닫히지 않음 / 시안 육안 대조
  - **T2**: 콤보에 "미분류" 없음 / 새 작업이 "UI·UX"로 열림 / 빈 카테고리 기존 작업 편집→그대로 저장 시 미분류 그룹에 남아 있음 / 칸반·목록의 "미분류" 그룹 정상 표시
  - **T3**: 대시보드↔작업 왕복 3회 이상 후에도 재진입 정상 / 테스트·Git 상태·작업 기록·명령 슬롯 버튼 정상 / 다이얼로그가 두 번 뜨지 않음 / 복귀 후 프로젝트 추가 시 새 카드 버튼 정상 / 작업 페이지 표시 중 카드 다이얼로그가 뜨지 않음

## Phase Ledger
- 전 task(T1~T3) 완료.
- Phase F 통과 (HEAD f9ddf7d) — F-1 클린 리빌드(-t:Rebuild) 오류 0·신규 경고 0(기존 5건만). F-7 plan-completion-reviewer BLOCKER/MAJOR/MINOR 0.
- Phase G 통과 (Must 100%) — 커버 대상 Must FR(T5·C3) 충족. FR-C3 실제 왕복 동작은 ⏳ HUMAN-VERIFY(앱 실행). Should(T6) 회귀 방지. 갭 0건이라 재루프 없음.
- **F-8 미통과 — 시각/동작 확인 대기**: T1 하단 라인의 렌더 일치 + T3 네비게이션 왕복 동작이 `⏳ 미확인`이라 **완료 선언 보류**(사용자 육안·실행 확인 필요).

## Progress Log
- T1 완료 (커밋 amend): 제목 필수 표시를 조건부 4면 테두리 → Grid에 겹친 상시 하단 danger 라인(2px, IsHitTestVisible=False)으로 변경. TitleBorderBrush·정적 브러시 2개·고아 using 2개·xmlns:local 제거, OnSave 주석 갱신. 빌드 OK(신규 경고 0), spec·quality 리뷰 지적 0.
  - 결정(D2 부수효과 확정): 라인이 TextBox 형제라 hover 시 회색 전환(이전 plan D3-a) 문제가 해소됨. 렌더 육안(라인 두께·모서리·포커스 중첩)은 F-8 인계.
- T2 완료 (커밋 amend): 카테고리 "미분류" 선택지 제거(_noneCategoryLabel 필드 삭제), 새 작업은 CategoryOptions[0]="UI·UX" 자동 선택. SelectedCategoryOption을 string?로 전환 — 편집 빈 카테고리는 null 미선택, BuildResult에서 ?? string.Empty로 흡수(Todos.Category NOT NULL 방어, plan-reviewer B1). TaskPageViewModel의 미분류 표시·TaskCategory_None resw는 존치(D7/D8). 빌드 OK(신규 경고 0), 리뷰 지적 0.
  - 확인: SelectedItem이 object 타입이라 string→string? 전환에 XAML 바인딩(xaml:78) 무영향.
