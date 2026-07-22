# plan.md — 테스트 화면(TestPage) "연결된 작업" 배지 추가

**PRD**: docs/prd.md (FR-E4 테스트↔작업 연결 배지·링크)
**기준 디자인**: `docs/DevDashboard WinUI/DevDashboard Redesign.dc.html` — 테스트 목록 페이지의 링크 배지 마크업(261~266줄) + 데이터 정의(`hasLink`/`link`, 1078줄) + 링크 값의 출처(1508줄 `link: title`)
**사용자 제공 렌더 이미지**: 배지가 실제로 그려진 화면(파란 테두리 pill + 링크 아이콘 + 텍스트) — 2026-07-22

## 요구 이해

> 원문(사용자, 2026-07-22): "테스트↔작업 연결 배지 시안이 이미지처럼 되어 있는데 다시 확인해서 추가해놔" (+ 테스트 화면 스크린샷 1장)

이해한 요구:
- 직전 작업 보고에서 "FR-E4 연결 배지의 화면 표현이 사라졌다"고 했는데, **시안에는 테스트 화면 쪽에 연결 배지가 실재**하므로 그것을 구현하라는 지시다. 사용자가 제공한 이미지는 **테스트 화면(TestPage)**이며(폴더 아이콘 + "UI·UX" + "0/1 통과" = 스위트 헤더), 테스트 항목 이름 아래에 **링크 아이콘 + 텍스트**의 파란 테두리 pill이 붙어 있다.
- 시안 원본에서 이 배지는 `t.hasLink`일 때만 표시되며 내용은 **연결된 작업의 제목**(`link: title`, 1508줄)이다 — 즉 **테스트 → 작업 방향**의 표시다.
- 현행 코드는 `TodoItem.LinkedTestId`(작업 → 테스트) **단방향**이라 테스트에서 작업 제목을 얻으려면 **역참조 조회**가 필요하다. 이것이 대장의 `[테스트→작업 역방향 링크/배지]` 항목이며, 이번에 재수용한다.
- 이번 범위는 **연결 배지 하나**다. 시안 같은 행에 있는 방법(`t.method`)·에러(`t.error`) 표시는 사용자가 지목하지 않았으므로 이번에 하지 않는다(각각 대장 유지).

## Goal

테스트 항목 행에 **연결된 작업 제목**을 시안(261~266줄)과 같은 파란 테두리 pill로 표시해, 테스트에서 어느 작업과 연결됐는지 화면에서 확인할 수 있게 한다. PRD FR-E4의 "연결 배지" 몫을 테스트 쪽에서 충족한다.

## Investigation Log (근거)

### 문서·대장
- **위키 참조**: vault 미설정 — 코드 1차 출처로 진행.
- **AGENTS.md 신선도 점검**: 이번에 참조하는 경로(`Domain/Entities/{TestItem,TodoItem}.cs`·`Presentation/ViewModels/TestPageViewModel.cs`·`Presentation/Views/TestPage.xaml`·`Resources/Converters.xaml`) 전부 실재. 빌드 명령·`DevDashboard.csproj`도 실재. 어긋남 0건.
  - 관련 함정 직결: **5**(x:Bind **함수 바인딩**은 Converter를 못 받음 — 단 **일반 x:Bind는 Converter 사용 가능**하며 `TaskPage.xaml`의 `{x:Bind Description, Converter={StaticResource StringNotEmptyToVisibility}}`가 그 선례다)·**11**(공용 DataTemplate 소비처 전수 확인).
  - **AGENTS.md 경고 baseline stale**(대장 등재분): 실측 상시 경고는 `CS0618` 1건이며 문서의 "NU1903 1 + CS0612 4"와 다르다 — 이번 plan의 "신규 경고 0" 판정은 **실측 baseline(CS0618 1건)** 기준으로 한다.
- **Deferred 대장 확인**(`docs/plans/deferred.md`):
  - **`[테스트→작업 역방향 링크/배지]`(FR-E4 확장) — 이번 작업이 이 항목의 재수용이다.** 원문: "테스트 목록에서 연결된 작업 배지/링크 표시. 현재 TodoItem.LinkedTestId 단방향만. TestItem→Todo 역참조 조회 필요. 사용자 결정으로 Phase 3 제외." 요구·기술 판단이 이번 요청과 정확히 일치한다 → 완료 시 `## 종결`로 이동.
  - `[FR-E4 작업별 연결 배지 표시 소멸]`(2026-07-22 등재) — 그건 **작업 화면(TaskPage) 쪽** 몫(`TodoItem.LinkedTestBadge`)이라 이번 작업과 **별개**다. 이번 구현은 반대 방향(테스트→작업)이므로 그 항목은 대장에 그대로 유지한다.
  - `[PRD FR-E4 문구가 구현과 어긋남 — 사용자 승인 대기]` — 이번 구현으로 FR-E4의 "연결 배지"가 **테스트 쪽에서 충족**되므로 문구 정정의 필요성이 줄어든다(작업 쪽 배지만 미표시로 남음). 항목은 유지하되 이번 결과를 반영해 재판단 대상으로 남긴다.
  - `[FR-T6/E4 "방법" 필드 확장 소비]`·`[FR-E5 에러 표시 미착수]` — 시안 같은 행의 방법·에러 표시는 이번 범위 밖이라 **그대로 유지**.
  - `[Test* 구 resw 고아 정리]`·`[BOM/no-BOM 인코딩 통일]` 등 나머지는 무관.
- **PRD 경량 확인**: 이번 변경은 **FR-E4**(Should — "테스트↔작업 연결(연결 배지·링크), 칸반 카테고리 그룹 통과율 배지 배선") 중 **연결 배지** 몫에 직접 닿는다 → PRD 연결(Phase G 활성).

### 현행 구현 (직접 Read)

**`Domain/Entities/TestItem.cs`**(51줄)
- `ObservableObject` 상속, `[ObservableProperty] partial` 프로퍼티 방식(`Text`·`Method`·`ProgressNote`·`Status`·`CompletedAt`). `Id`·`CategoryId`·`CreatedAt`은 일반 프로퍼티.
- **연결 정보 필드 없음** — 테스트에서 작업을 가리키는 값이 도메인에 존재하지 않는다.

**`Domain/Entities/TodoItem.cs`**
- `LinkedTestId`(:50, 일반 string, **영속**) — 작업 → 테스트 단방향 링크.
- `LinkedTestBadge`(:52-54, `[ObservableProperty]`, 주석에 "**표시 전용 — 영속화하지 않으며 TaskPageViewModel이 설정**") — **이번에 만들 속성의 정확한 선례**다(도메인에 표시 전용 비영속 속성을 두고 VM이 채우는 패턴).

**`Presentation/ViewModels/TaskPageViewModel.cs`**(대칭 선례)
- `BuildTestStatusLookup`(:75-81)이 `testId → status` 딕셔너리를 만들고, `Rebuild()`(:105-106)가 `t.LinkedTestBadge = MapTestBadge(GetLinkedTestStatus(t))`로 **매 재구성마다 표시 값을 채운다**. 이번 구현은 이 패턴의 **반대 방향** 대칭이다.

**⚠️ `ProjectItem.Todos`는 지연 로딩이다 (plan-reviewer B1으로 발견 — 이 plan의 성립 조건)**
- `SqliteProjectRepository.cs:35` 주석대로 `Todos`·`TestCategories`·`Histories`는 **진입 시점에 필요한 것만** DB에서 채운다. 채우는 주체는 `ProjectCardViewModel`의 `EnsureTodosLoaded()`(:295-300)·`EnsureTestsLoaded()`(:311-316)이며, 각각 `_todosLoaded`/`_testsLoaded` 플래그로 1회만 로드한다.
- **작업 화면 진입** `CreateTaskPageViewModel()`(:517-523)은 `EnsureTodosLoaded()` + `EnsureHistoriesLoaded()` + `EnsureTestsLoaded()`를 **모두** 호출한다.
- **테스트 화면 진입** `CreateTestPageViewModel()`(:533-537)은 **`EnsureTestsLoaded()`만** 호출한다 — 즉 테스트 화면을 열면 `_item.Todos`는 로드되지 않은 초기 상태다.
- **결과**: 이 사실을 모르고 구현하면 `BuildLinkedTaskTitles()`의 딕셔너리가 항상 비어 **모든 배지가 `Collapsed`가 된다**. 빌드·grep은 전부 통과하므로 정적 검증으로는 잡히지 않는다(리뷰어 지적 그대로). → **T1에서 `CreateTestPageViewModel()`에 `EnsureTodosLoaded()`를 추가**해 해소한다(작업 화면이 이미 `EnsureTestsLoaded()`를 부르는 것과 대칭).
- 유일 진입 경로 확인: `DashboardView.xaml.cs:237`이 `CreateTestPageViewModel()`을 호출하는 단일 지점(grep 전수).

**`Presentation/ViewModels/TestPageViewModel.cs`**(현재 확인 범위 1~105줄)
- `_project`(`ProjectItem`) 보유 — `_project.Todos` 필드 자체는 접근 가능하나, **로드 여부는 위 지연 로딩 규약에 달려 있다**(T1이 해소).
- `Rebuild()`(:66-101): 통계 → 스위트 필터 → `SuiteGroups` 재구성. **표시 값을 채울 지점이 이 메서드 하나로 모여 있다**(`OnSelectedStatusChanged`·`OnSelectedSuiteFilterChanged`·생성자가 전부 여기로 수렴).
- 항목 표시는 `TestSuiteGroup`에 `ObservableCollection<TestItem>`을 담는 방식이라, **`TestItem` 자체에 표시 값이 있으면 XAML이 그대로 바인딩**할 수 있다(별도 wrapper 불필요).

**`Presentation/Views/TestPage.xaml`**
- `TestItemRowTemplate`(:22-115): `Border`(구분선·hover·`ContextFlyout`) 안 `Grid` — **열 3개**(아이콘 `Auto` / 본문 `*` / pill `Auto`), **행 2개**(0=이름, 1=메모). 아이콘·pill은 `Grid.RowSpan="2"`로 세로 중앙.
  - 이름 `TextBlock`(:76-83): 13px, 최대 2줄 말줄임.
  - 메모 `Border`(:86-105): `Grid.Row="1"`, `NoteVisibility(ProgressNote)` 조건, 좌측 warning 바 + 깃발 아이콘.
  - **배지를 이름과 메모 사이에 넣으려면 행이 하나 더 필요**하다(시안 순서: 이름 → 링크 배지 → 메모).
- **색 리터럴 직접 사용 선례**: 미실행 테스트 배지가 상태에 따라 변하지 않는 색을 XAML에 `#8A8890`/`#35353C` 리터럴로 직접 적었다(notes 2026-07-21 기록). 링크 배지도 상태 무관 고정색이라 **같은 방식이 적절**하다 — 정적 브러시 헬퍼 불필요.
- **소비처 전수**(grep): `TestItemRowTemplate`은 `TestPage.xaml` 안에서만 참조(정의 1 + 소비 1) — 함정 11 리스크 없음.

**`Resources/Converters.xaml`**
- `StringNotEmptyToVisibility`(:16) 등록됨 — `TaskPage.xaml`이 `{x:Bind Description, Converter={StaticResource StringNotEmptyToVisibility}}`로 사용 중. **일반 x:Bind + Converter 조합이 이 레포에서 동작함이 확인된 선례**이므로 신규 Visibility 헬퍼를 만들 필요가 없다.

**아이콘**: 시안은 체인 2개가 겹친 SVG(`:263`). Segoe Fluent Icons의 대응 글리프는 **`E71B`(Link)**. 이 레포의 기존 `FontIcon` 사용 글리프를 전수 확인한 결과 `E71B`는 **미사용**이라 충돌 없음.

### 4-D. 재사용 확인

| 신규 심볼 | 유사 기존 구현 검색 결과 | 재사용/신규 사유 |
|---|---|---|
| `TestItem.LinkedTaskTitle` | `TodoItem.LinkedTestBadge`(`TodoItem.cs:52-54`) — **표시 전용·비영속·VM이 채움**의 동일 패턴 | **신규(선례 대칭 재현)** — 반대 방향(테스트→작업)이라 기존 속성을 재사용할 수 없다. 같은 주석 규약("표시 전용 — 영속화하지 않으며 TestPageViewModel이 설정")을 따른다 |
| `TestPageViewModel`의 역참조 채우기 | `TaskPageViewModel.BuildTestStatusLookup`/`MapTestBadge`/`GetLinkedTestStatus`(:75-97) — 반대 방향의 같은 구조 | **신규(선례 대칭 재현)** — 방향이 반대라 그 메서드들을 호출할 수 없다. 단 **딕셔너리 없이** 단순 조회로 둔다(아래 D3) |
| 배지 표시 조건 | `StringNotEmptyToVisibility` 컨버터(`Converters.xaml:16`), `TestPage.NoteVisibility`(정적 헬퍼) | **컨버터 재사용(신규 0)** — 일반 x:Bind는 Converter를 받으므로 헬퍼를 새로 만들지 않는다 |
| 배지 색 | `AppInfoBrush`(#5B93D8 — 시안 #7AB5EC와 다름), TestPage의 색 리터럴 직접 사용 선례 | **XAML 리터럴(신규 심볼 0)** — 상태와 무관한 고정색이라 헬퍼·팔레트 추가가 불필요 |
| 배지 컨테이너 스타일 | `TagBadgeStyle`(`Styles.xaml:189`, CornerRadius **4**) | **미사용** — 시안은 `border-radius:999px` 완전 캡슐이라 값이 다르고, 재사용해도 CornerRadius를 덮어써야 해 이득이 없다. 인라인 `Border`로 그린다 |

## 시각 요소 분해

> 기준: 시안 원본 HTML `:261-266`의 인라인 스타일 + 사용자 제공 렌더 이미지(2026-07-22). px는 WinUI 논리 단위로 그대로 옮긴다. 최종 판정은 빌드 후 사용자 육안 대조(⏳ HUMAN-VERIFY).

| 요소 | 속성 | 디자인 값 | XAML 대응 수단 | 확인 방법 |
|---|---|---|---|---|
| 링크 배지 | 표시 조건 | `hasLink` = 연결된 작업이 있을 때만 | `Visibility` + `StringNotEmptyToVisibility` 컨버터 | HTML 소스 :261, :1078 |
| 링크 배지 | 위치 | 테스트 이름 **아래**, 메모 **위** | `Grid.Row="1"`(메모를 행 2로 밀어냄) | HTML 소스 :259-275 (순서: 이름 → 링크 → 방법 → 메모) |
| 링크 배지 | 가로 정렬 | `align-self:flex-start` (내용 폭만큼만, 좌측) | `HorizontalAlignment="Left"` | HTML 소스 :262 |
| 링크 배지 | 배치·간격 | `inline-flex`, `align-items:center`, `gap:5px` | `StackPanel Orientation="Horizontal" Spacing="5"` + 세로 중앙 | HTML 소스 :262 |
| 링크 배지 | 모서리 | `border-radius:999px` (**완전 캡슐**) | `CornerRadius="999"` — ⚠️ **레포 내 선례 0(미검증)**. 캡슐로 렌더되지 않으면 배지 높이(글자 11 + padding 4 ≈ 19)의 절반인 `CornerRadius="10"`으로 대체한다(자율 루프가 이 대안으로 진행, 최종 판정은 육안) | HTML 소스 :262 |
| 링크 배지 | 테두리 | `1px solid rgba(90,163,232,.45)` | `BorderThickness="1" BorderBrush="#735AA3E8"`(0.45×255≈0x73) | HTML 소스 :262 |
| 링크 배지 | 배경 | 없음(투명 — 테두리형) | `Background` 미지정 | HTML 소스 :262 |
| 링크 배지 | padding | `2px 9px` | `Padding="9,2"` | HTML 소스 :262 |
| 링크 배지 | 글자 | `11px`, `#7ab5ec` | `FontSize="11" Foreground="#7AB5EC"` | HTML 소스 :262 |
| 링크 배지 | 폭 | **내용 폭에 딱 맞음**(`inline-flex` + `align-self:flex-start`), 넘치면 말줄임(`max-width:100%`) | 배지 내부를 **`Grid`(Auto/**Auto**)** 로 담는다 + `TextTrimming="CharacterEllipsis" TextWrapping="NoWrap"`. ⚠️ 두 실패 경로를 모두 피해야 한다: `StackPanel`은 자식에게 **무한 폭**을 줘 트리밍 미발동(F-7 M1), `*` 열은 **남는 폭을 다 먹어 배지가 가로로 늘어난다**(사용자 육안 지적 2026-07-22) | HTML 소스 :262, :264 + 사용자 제공 시안/현재 비교 이미지 |
| 링크 아이콘 | 모양 | 체인 2개가 겹친 링크 아이콘 | `FontIcon Glyph="&#xE71B;"`(Segoe Fluent "Link") | HTML 소스 :263 |
| 링크 아이콘 | 크기·색 | `10×10`, `stroke:currentColor`(= 글자색 `#7ab5ec`) | `FontSize="10" Foreground="#7AB5EC"` | HTML 소스 :263 |
| 링크 아이콘 | 축소 | `flex-shrink:0` (텍스트가 길어도 아이콘은 안 줄어듦) | `Grid`가 아니라 `StackPanel` + 텍스트만 말줄임 → 자동 충족 | HTML 소스 :263 |
| 배지 텍스트 | 내용 | 연결된 **작업 제목**(`link: title`) | `{x:Bind LinkedTaskTitle}` | HTML 소스 :264, :1508 |
| 행 간격 | 이름↔배지 | `gap:3px` (본문 세로 스택) | 배지 `Margin="0,3,0,0"` | HTML 소스 :259 |

## Decisions

- **D1 (표시 값을 도메인 표시 전용 속성으로)**: `TestItem`에 `LinkedTaskTitle`(`[ObservableProperty]`, **비영속**)을 추가하고 `TestPageViewModel`이 채운다. *Source*: `TodoItem.LinkedTestBadge`(`TodoItem.cs:52-54`)가 정확히 같은 규약("표시 전용 — 영속화하지 않으며 TaskPageViewModel이 설정")으로 이미 존재하며, 그 값을 `TaskPage.xaml`이 직접 바인딩해 왔다. 같은 패턴을 반대 방향으로 재현하면 `TestSuiteGroup` wrapper를 바꾸지 않고 XAML이 `TestItem`을 그대로 바인딩할 수 있다.
  - **대안 기각**: VM에 `testId → 작업제목` 딕셔너리를 두고 XAML이 함수 바인딩으로 조회 — 함정 5로 함수 바인딩은 Converter를 못 받고, `TestItem`이 자기 표시 값을 모르면 행 템플릿이 VM 참조를 별도로 들어야 해 복잡해진다.
- **D2 (영속화하지 않음)**: `LinkedTaskTitle`은 매 `Rebuild()`마다 재계산하며 DB에 저장하지 않는다. *Source*: 값의 출처가 `TodoItem.Text`(이미 영속)라 **중복 저장은 동기화 실패 지점**만 만든다 — 작업 제목이 바뀌면 저장된 사본이 낡는다. `SaveTestCategories`의 INSERT 컬럼을 건드리지 않으므로 NFR-3(스키마 하위호환)에도 무영향(AGENTS.md의 "INSERT 컬럼 추가 시 파라미터 갱신" 함정도 비해당).
- **D3 (딕셔너리 없이 단순 조회)**: 역참조는 `_project.Todos`에서 `LinkedTestId == test.Id`인 항목을 찾는 방식으로 하되, **매 테스트마다 전체 Todos를 훑지 않도록** `Rebuild()` 시작에서 `LinkedTestId → 작업제목` 딕셔너리를 **한 번** 만들어 쓴다. *Source*: `TaskPageViewModel.BuildTestStatusLookup`(:75-81)이 같은 이유로 `_testStatusById` 딕셔너리를 쓰는 선례. 개인 대시보드 규모라 성능 자체는 문제가 아니지만, 이중 루프보다 **의도가 명확**하고 선례와 일관된다.
- **D4 (한 테스트에 여러 작업이 연결된 경우 첫 항목만)**: `LinkedTestId`는 작업당 1개지만 **여러 작업이 같은 테스트 Id를 가질 수 있다**(도메인이 막지 않음 — `CreateLinkedTest`는 항상 새 테스트를 만들어 1:1이지만 데이터상 보장은 없다). 딕셔너리 구성 시 **먼저 만난 항목을 유지**한다(`TryAdd`). 시안도 링크가 문자열 하나라 복수 표현이 없다.
- **D5 (배지는 표시 전용 — 클릭 동작 없음)**: 시안 마크업(`:262-265`)에 `onClick`이 없다. 작업으로 이동하는 링크 동작은 넣지 않는다(넣으려면 페이지 전환·스크롤·하이라이트 설계가 필요해 별도 논의 대상).
- **D6 (색은 XAML 리터럴)**: 글자 `#7AB5EC`·테두리 `#735AA3E8`을 XAML에 직접 적는다. *Source*: 상태에 따라 변하지 않는 고정색이며, TestPage의 미실행 배지가 같은 이유로 `#8A8890`/`#35353C`를 리터럴로 쓴 선례가 있다(notes 2026-07-21). 팔레트 신규 색은 추가하지 않는다(함정 4 — 다크 단일 딕셔너리 정책 유지).
- **D8 (테스트 화면 진입 시 `Todos`도 로드한다)**: `ProjectCardViewModel.CreateTestPageViewModel()`에 `EnsureTodosLoaded()`를 추가한다. *Source*: `SqliteProjectRepository.cs:35`(지연 로딩 규약) + `ProjectCardViewModel.cs:517-523`(작업 화면은 `EnsureTestsLoaded()`까지 부른다) vs `:533-537`(테스트 화면은 `EnsureTestsLoaded()`만) — 이 비대칭 때문에 역참조 대상인 `Todos`가 비어 **배지가 전멸**한다(plan-reviewer B1). 로드는 `_todosLoaded` 플래그로 1회만 일어나므로 비용은 최초 진입 1회의 DB 조회뿐이고, 작업 화면이 이미 반대 방향(테스트 로드)을 하고 있어 **대칭을 맞추는 것이 자연스럽다**.
  - **대안 기각**: `TestPageViewModel` 생성자에서 리포지토리로 직접 `GetTodos`를 부르는 방식 — 로드 책임이 `ProjectCardViewModel`에 모여 있는 기존 구조(플래그 기반 1회 로드)를 깨고 중복 조회를 만든다.
- **D7 (작업이 삭제되면 배지도 자동으로 사라진다)**: 역참조를 **매번 계산**하므로 작업이 삭제되면 딕셔너리에 없어져 배지가 자동 소멸한다 — 별도 정리 로직이 필요 없다. 반대로 **테스트가 삭제되면** `TodoItem.LinkedTestId`가 고아 값으로 남지만(기존 동작), 그것은 작업 쪽 배지 경로의 문제라 이번 범위 밖이다(기존과 동일하게 `GetLinkedTestStatus`가 null을 반환해 배지 미표시).

## PRD Coverage

| PRD ID | 우선순위 | 대응 task | 상태 |
|---|---|---|---|
| FR-E4 (테스트↔작업 연결 배지·링크 + FR-T8 배선) | Should | T1, T2 | ⚠️ **부분 커버(확대)** — "연결 **배지**"의 **테스트 쪽 표현**을 이번에 구현(시안에 실재하는 유일한 연결 배지). FR-T8 배선 몫은 기구현·무변경. **미충족 잔여 2건**: ① **"링크"(배지 클릭 → 연결된 작업으로 이동)** 미구현 — 시안에 `onClick`이 없어 표시 전용으로 뒀다(D5, Deferred 등재) ② **작업 쪽 배지**(`TodoItem.LinkedTestBadge`)는 시안에 없어 여전히 미표시(대장 `[FR-E4 작업별 연결 배지 표시 소멸]` 유지) |
| FR-E1·E2·E3·E5 (테스트 페이지·상태 모델·다이얼로그·목록 restyle) | Must/Should | — | 이번 범위 외 (기구현 — 행 템플릿에 요소 1개를 추가할 뿐 기존 구성 무변경) |
| FR-T1~T8 (작업 도메인·칸반·목록·다이얼로그) | Must/Should | — | 이번 범위 외 (기구현 — `TodoItem`은 **읽기만** 하고 수정하지 않는다) |
| FR-C*·FR-S*·FR-H*·FR-N* 등 그 외 active Must | Must/Should | — | 이번 범위 외 (기구현, 이번 diff 무관) |
| NFR-1 (빌드 오류 0·신규 경고 0) | — | 전 task | 검증 대상(실측 baseline `CS0618` 1건 제외) |
| NFR-2 (계층 위반 0) | — | T1 | 검증 대상(VM은 View 타입을 참조하지 않는다 — 표시 값은 `string`) |
| NFR-3 (DB 스키마 하위호환) | — | — | ✅ 무영향(D2 — 비영속 표시 전용 속성, INSERT 컬럼 무변경) |
| NFR-4 (다국어 ko/en 대칭) | — | — | 해당 없음(신규 문구 0 — 배지 내용은 사용자 데이터인 작업 제목) |
| NFR-5 (테스트) | — | — | 조건 미발동(테스트 프로젝트 부재 — AGENTS.md) |

## 작업 단계

### T1 — 데이터: `TestItem`에 연결 작업 제목 + VM 역참조 채우기 `Type C`
- [x] 구현
- **Files**: `DevDashboard_WinUI/Domain/Entities/TestItem.cs`, `DevDashboard_WinUI/Presentation/ViewModels/TestPageViewModel.cs`, `DevDashboard_WinUI/Presentation/ViewModels/ProjectCardViewModel.cs`(지연 로딩 — D8)
- **Design**: ① 배치 — 표시 값 속성은 `Domain/Entities/TestItem.cs`(선례 `TodoItem.LinkedTestBadge`와 같은 자리), 채우는 로직은 `TestPageViewModel`, 데이터 로드 보장은 `ProjectCardViewModel.CreateTestPageViewModel()`. ② 신규 심볼 — `TestItem.LinkedTaskTitle`(표시 전용 비영속 string, 연결된 작업 제목 / 빈 문자열이면 미연결) / `TestPageViewModel.BuildLinkedTaskTitles()`(`Rebuild()` 앞부분에서 `LinkedTestId → 작업 제목` 딕셔너리를 만들어 각 `TestItem`에 대입). ③ 의존 방향 — VM이 `_project.Todos`(도메인)를 읽어 `TestItem`(도메인)에 쓴다. View 타입 참조 없음(값이 `string`). ④ 비추상화 — 양방향 링크를 관리하는 서비스/인덱스 클래스를 만들지 않는다(조회 지점이 이 한 곳뿐 — 3회 문턱 미달). `TodoItem`·`TestItem`에 상호 참조 필드를 추가하지도 않는다(D2 — 영속 중복 금지).
- **구성**:
  - (D1·D2) `TestItem`에 `[ObservableProperty] public partial string LinkedTaskTitle { get; set; } = string.Empty;` 추가 + 주석에 **"표시 전용 — 영속화하지 않으며 TestPageViewModel이 설정"** 명시(`TodoItem.LinkedTestBadge` 규약 그대로).
  - (D3·D4) `BuildLinkedTaskTitles()`: `_project.Todos`를 훑어 `LinkedTestId`가 비어 있지 않은 항목으로 `Dictionary<string, string>`(테스트 Id → 작업 제목)을 만들되 **`TryAdd`로 첫 항목 유지**. 그 뒤 `_project.TestCategories`의 전 항목을 돌며 `t.LinkedTaskTitle = map.GetValueOrDefault(t.Id, string.Empty)`로 대입(연결이 없으면 빈 문자열 — 이전 값이 남지 않게 **항상 대입**).
  - `Rebuild()` 시작(통계 계산 전)에서 `BuildLinkedTaskTitles()` 호출 — 필터 변경·재구성 때마다 최신 값이 반영된다.
  - **(D8) `ProjectCardViewModel.CreateTestPageViewModel()`(:533-537)에 `EnsureTodosLoaded();`를 추가**한다(`EnsureTestsLoaded()` 앞·뒤 무관, 작업 화면의 `CreateTaskPageViewModel()`과 대칭). **이 한 줄이 없으면 배지가 하나도 뜨지 않으며 빌드·grep으로는 검출되지 않는다.** 같은 메서드의 XML 주석(`:532` "TestCategories를 로드해 전달합니다")도 사실과 어긋나게 되므로 **"TestCategories와 Todos(연결 배지 역참조용)를 로드해 전달합니다"로 함께 갱신**한다.
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0(실측 baseline `CS0618` 1건 제외).
  2. `TestItem.LinkedTaskTitle`이 `[ObservableProperty] partial` 방식으로 정의되고, 주석에 "표시 전용"·"영속화하지 않"음이 명시된다.
  3. `TestPageViewModel.Rebuild()`가 `BuildLinkedTaskTitles()`를 호출하고, 그 메서드가 `_project.Todos`의 `LinkedTestId`로 딕셔너리를 만든다.
  4. 연결이 없는 테스트에는 **빈 문자열이 대입**된다(이전 값 잔존 없음 — 작업 삭제·링크 해제 시 배지가 사라져야 함, D7).
  5. `TestItem`이 **DB 저장 경로에 추가되지 않는다** — `SqliteProjectRepository`의 테스트 INSERT/SELECT 컬럼 목록에 `LinkedTaskTitle`이 없다(NFR-3, AGENTS.md의 "INSERT 컬럼 추가 시 파라미터 갱신" 함정 비해당 확인).
  6. `TestPageViewModel.cs`에 `Microsoft.UI.Xaml` 계열 using·타입이 추가되지 않는다(NFR-2).
  7. **`CreateTestPageViewModel()`이 `EnsureTodosLoaded()`를 호출한다**(D8) — 이 acceptance가 없으면 기능이 무동작인 채로 전 검증을 통과한다.
- **Edge Cases**: **`Todos`가 로드되지 않은 상태 → 배지 전멸(정상 아님, D8이 해소)** — `?? []` 가드는 예외만 막을 뿐 이 실패를 고치지 못한다 / `_project.Todos`가 null(`?? []` 가드) / `LinkedTestId`가 빈 문자열인 작업 → 딕셔너리 제외 / `LinkedTestId`가 이미 삭제된 테스트를 가리킴 → 그 키는 어떤 테스트와도 매칭되지 않아 무해 / 같은 테스트에 여러 작업이 연결 → 첫 항목만(D4) / 작업 제목이 빈 문자열 → 배지가 `StringNotEmptyToVisibility`로 미표시(빈 pill이 그려지지 않음) / 작업 제목이 매우 김 → T2에서 말줄임 / `TestCategories`가 null(`?? []` 가드 유지) / `EnsureTodosLoaded()`는 `_todosLoaded` 플래그로 1회만 로드하므로 작업 화면을 먼저 열었다 와도 **중복 DB 조회가 없다**.
- **Halt Forecast**: (ii-a) 사전 승인 — **도메인 엔티티(`TestItem`)에 공개 속성 1개 추가**(비영속·표시 전용, 기존 선례와 동일 규약) + `CreateTestPageViewModel()`에 로드 호출 1줄 추가(기존 공개 메서드 `EnsureTodosLoaded` 재사용, 시그니처 변경 0). 파괴적·스키마 변경·외부 작업 없음.

### T2 — XAML: 테스트 항목 행에 링크 배지 추가 `Type C`
- [x] 구현
- **Files**: `DevDashboard_WinUI/Presentation/Views/TestPage.xaml`
- **Design**: ① 배치 — `TestItemRowTemplate`(`TestPage.xaml`) 안, 이름 행과 메모 행 사이. ② 신규 심볼 — **없음**(마크업만 추가. 표시 조건은 기존 `StringNotEmptyToVisibility` 컨버터, 색은 리터럴 — 4-D). ③ 의존 방향 — XAML이 `TestItem.LinkedTaskTitle`(T1)을 바인딩. 코드비하인드 변경 없음. ④ 비추상화 — 배지용 공용 스타일(`LinkBadgeStyle` 등)을 `Styles.xaml`에 만들지 않는다(소비처 1곳 — 3회 문턱 미달, 인라인이 추적에 낫다).
- **구성**:
  - `TestItemRowTemplate`의 `Grid.RowDefinitions`를 2행 → **3행**으로 늘리고, 기존 **메모 `Border`를 `Grid.Row="1"` → `Grid.Row="2"`로 이동**. 상태 아이콘·상태 pill의 `Grid.RowSpan="2"`를 **`"3"`으로** 갱신(세로 중앙 유지).
  - 링크 배지를 `Grid.Column="1" Grid.Row="1"`에 추가 — `Border`(`CornerRadius="999"`, `BorderThickness="1"`, `BorderBrush="#735AA3E8"`, `Padding="9,2"`, `HorizontalAlignment="Left"`, `Margin="0,3,0,0"`, `Visibility="{x:Bind LinkedTaskTitle, Converter={StaticResource StringNotEmptyToVisibility}, Mode=OneWay}"`) 안에 `StackPanel Orientation="Horizontal" Spacing="5"`(세로 중앙) = `FontIcon Glyph="&#xE71B;" FontSize="10" Foreground="#7AB5EC"` + `TextBlock Text="{x:Bind LinkedTaskTitle, Mode=OneWay}" FontSize="11" Foreground="#7AB5EC" TextWrapping="NoWrap" TextTrimming="CharacterEllipsis"`.
  - 배지 위에 시안 근거 주석 1줄(어느 시안 요소인지 — 후속 수정자가 색 리터럴을 팔레트로 바꾸지 않도록 D6 근거 포함).
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0.
  2. `TestItemRowTemplate`의 `Grid`가 **3행**이고, 메모 `Border`가 `Grid.Row="2"`, 링크 배지가 `Grid.Row="1"`이다.
  3. 상태 아이콘·상태 pill의 `Grid.RowSpan`이 **`"3"`**이다(2 잔존 0 — 갱신 누락 시 세로 중앙이 깨진다).
  4. 배지가 `LinkedTaskTitle`에 `Mode=OneWay`로 바인딩되고, `Visibility`가 `StringNotEmptyToVisibility` 컨버터를 쓴다(신규 Visibility 헬퍼 추가 0).
  5. 배지 마크업이 시각 요소 분해 표의 값과 일치한다 — `CornerRadius="999"`·`BorderBrush="#735AA3E8"`·`Padding="9,2"`·`Spacing="5"`·`HorizontalAlignment="Left"`·`FontSize` 11(글자)/10(아이콘)·`Foreground="#7AB5EC"`·`Glyph="&#xE71B;"`.
  6. `TestPage.xaml.cs`·`Styles.xaml`·`Palette.xaml`이 **변경되지 않는다**(신규 심볼·공용 스타일·팔레트 색 0).
- **Edge Cases**: 연결 없음 → 배지 `Collapsed`(행 높이 0, 이름과 메모가 붙는다) / 작업 제목이 매우 김 → 본문 열(`*`) 폭에서 말줄임(아이콘은 축소되지 않음) / 이름이 2줄 + 배지 + 메모 동시 → 행이 세로로 길어지고 아이콘·pill은 `RowSpan=3`으로 세로 중앙 / 메모는 없고 배지만 있음 → 배지가 마지막 요소 / 배지 pill 위에서 우클릭 → 행의 `ContextFlyout`이 그대로 뜬다(배지가 입력을 가로채지 않음 — `Border`에 핸들러 없음).
- **Halt Forecast**: (i) 사전 해소 — `TestItemRowTemplate` 소비처가 `TestPage.xaml` 1곳임을 grep 전수 확인(함정 11). 파괴적·외부·신규 의존성 없음.

## 사전 승인 항목 (일괄 승인 대상)
- **도메인 엔티티 공개 속성 추가**: `TestItem.LinkedTaskTitle`(T1) — 비영속·표시 전용, `TodoItem.LinkedTestBadge`와 동일 규약. DB 스키마·직렬화 무영향.
- 로컬 작업 브랜치에서 task별 commit(현재 브랜치 `task/taskpage-list-view`에 이어서 진행 — 같은 세션의 연속 작업이라 새 브랜치를 파지 않는다).

## 불가피한 Halt (위임 불가)
- master 병합·push·태그·릴리즈·PR — 별도 승인.
- **시안 대조 최종 시각 판정** — 배지의 캡슐 모양·테두리 농도·아이콘 모양(`E71B`가 시안의 체인 아이콘과 비슷해 보이는지)·이름/메모와의 간격은 사용자만 판정(⏳ HUMAN-VERIFY).
- **연결 데이터 실동작 확인** — "테스트 추가" 토글로 만든 작업↔테스트 쌍에서 실제로 배지에 작업 제목이 뜨는지는 앱 실행 확인 필요(⏳ HUMAN-VERIFY).
- **표시 중복이 의도한 표현인지 판정** — `CreateLinkedTest`는 테스트 제목을 **작업 제목 그대로 복사**하므로(`TaskPageViewModel.cs:239` `Text = todo.Text`), "테스트 추가"로 만든 연결에서는 **행 이름과 배지 텍스트가 같은 문자열로 두 줄 겹쳐 보인다**(작업 제목을 나중에 바꿔야 갈라진다). 시안 예시는 둘이 다른 데이터라 이 중복이 드러나지 않는다. 그대로 둘지, 배지를 다르게(예: 접두어·아이콘만) 표현할지는 사용자 판정 대상(⏳ HUMAN-VERIFY, plan-reviewer m3).

## Deferred / Follow-up
- **[테스트 행에 방법(Method)·에러 미표시]** — 시안 같은 행에 `t.method`(`:267-269`)·`t.error`(`:277-279`)도 있으나 이번 요청은 연결 배지 한정. 대장의 `[FR-T6/E4 "방법" 필드 확장 소비]`·`[FR-E5 에러 표시 미착수]` 그대로 유지.
- **[배지 클릭으로 작업 이동 미구현]** — 시안에 클릭 동작이 없어 표시 전용으로 뒀다(D5). 연결된 작업으로 이동하고 싶다는 요구가 생기면 페이지 전환·하이라이트 설계부터 논의.
- **[작업 쪽 연결 배지는 여전히 미표시]** — 대장 `[FR-E4 작업별 연결 배지 표시 소멸]`은 이번 작업(테스트 쪽)과 별개로 유지된다. 다만 이번 구현으로 FR-E4의 "연결 배지"가 한쪽에서 충족되므로, 대장 `[PRD FR-E4 문구가 구현과 어긋남]`의 정정 필요성은 재판단 대상이 된다.
- **[SUGGEST] 링크 배지 색을 팔레트 근접색으로 통일 검토** — 시안 값 `#7AB5EC`/테두리 `rgba(90,163,232,.45)`가 팔레트에 없어 XAML 리터럴로 적었다(D6). 팔레트의 `AppInfoBrush`(#5B93D8)가 "정보/링크성" 의미에 부합하는 근접색이라 재사용하면 임의색 도입을 피할 수 있으나, 시안과 색이 달라져 이번엔 시안 정합을 우선했다. 색 리터럴이 더 늘면 팔레트 이관을 함께 검토. (T2 quality 리뷰 S1, 2026-07-22)
- **[테스트 삭제 시 `TodoItem.LinkedTestId` 고아]** — 테스트를 지워도 작업의 `LinkedTestId`가 남는다(기존 동작, 이번 변경과 무관). 표시상으로는 배지가 안 떠 무해하나 데이터 위생 관점의 후속.

## Out of Scope
- `TodoItem`·`LinkedTestId` 변경 — 이번 작업은 **읽기 전용**으로만 참조한다.
- DB 스키마·직렬화·마이그레이션 변경(D2 — 비영속).
- 시안 같은 행의 방법·에러 표시(위 Deferred).
- 배지 클릭 시 작업으로 이동하는 네비게이션(D5).
- `Palette.xaml` 신규 색·`Styles.xaml` 신규 공용 스타일(D6·Design ④).
- 작업 화면(TaskPage) 쪽 연결 배지 복원 — 시안에 없으며 별개 항목.

## Open Questions
- 없음 — 시안 원본(`:261-266`·`:1078`·`:1508`)과 사용자 제공 렌더 이미지로 형태·데이터 출처가 모두 확정됐고, 구현 방식은 기존 선례(`TodoItem.LinkedTestBadge` 대칭)로 결정 가능하다.

## 검증 방법
- 빌드: `"C:/Program Files/Microsoft Visual Studio/18/Professional/MSBuild/Current/Bin/MSBuild.exe" "DevDashboard_WinUI/DevDashboard.csproj" -t:Build -p:Configuration=Debug -p:Platform=x64` → 오류 0 + 실측 baseline(`CS0618` 1건) 외 신규 0
- 회귀 방지 grep(소스 `*.cs`/`*.xaml`, `obj/`·`bin/` 제외):
  - **T1**: `TestItem.cs`에 `LinkedTaskTitle` 정의 1 + "표시 전용"·"영속화하지 않" 주석 / `TestPageViewModel.cs`에 `BuildLinkedTaskTitles` 정의 1 + `Rebuild()` 호출 1 / `SqliteProjectRepository.cs`에 `LinkedTaskTitle` 잔존 **0**(비영속 확인) / `TestPageViewModel.cs`에 `Microsoft.UI.Xaml` 0 / **`ProjectCardViewModel.cs` 파일 전체 `EnsureTodosLoaded` 3건**(정의 :295 + `CreateTaskPageViewModel` 1 + **`CreateTestPageViewModel` 신규 1**) — 판정 기준은 그중 **`CreateTestPageViewModel` 본문 1건의 존재**다(D8 — 이 grep이 B1 재발을 막는 유일한 정적 방어) / 같은 메서드 XML 주석에 `Todos` 언급 1건
  - **T2**: `TestItemRowTemplate` 안 `RowDefinition` 3개 / `Grid.RowSpan="2"` 잔존 0·`Grid.RowSpan="3"` 2건 / `Glyph="&#xE71B;"` 1건 / `#7AB5EC` 2건(아이콘·텍스트)·`#735AA3E8` 1건 / `StringNotEmptyToVisibility`가 `LinkedTaskTitle`에 적용 / `TestPage.xaml.cs`·`Styles.xaml`·`Palette.xaml` diff 없음
- 동작 확인(빌드로 검증 불가 → ⏳ HUMAN-VERIFY):
  - 작업 화면에서 "테스트 추가"를 켜고 새 작업을 만든 뒤 → 테스트 화면의 그 테스트 행에 **파란 캡슐 배지 + 링크 아이콘 + 작업 제목**이 뜨는지
  - 배지가 이름 아래·메모 위에 좌측 정렬로 붙는지, 제목이 길면 말줄임되는지
  - 연결이 없는 테스트에는 배지가 없는지 / **연결된 작업을 삭제하면 배지가 사라지는지**(D7)
  - 상태 아이콘·상태 pill이 배지가 생겨도 세로 중앙에 유지되는지

## F-8 인계 목록 (렌더 육안 확인 필요 — 완료 선언 보류 사유)

1. 배지가 **파란 캡슐**로 보이는지 — `CornerRadius="999"`의 캡슐 렌더는 레포 내 선례가 없다(미검증). 모서리가 덜 둥글면 `CornerRadius="10"`으로 대체.
2. 이름 아래·메모 위에 좌측 정렬로 붙는지, **배지 폭이 내용에 딱 맞는지**(2026-07-22 육안 지적 후 `*` → `Auto` 정정), **작업 제목이 길면 말줄임(…)** 되는지
3. **"테스트 추가"로 만든 작업↔테스트 쌍에서 실제로 배지에 작업 제목이 뜨는지**(D8 지연 로딩 해소의 실동작 확인)
4. **연결된 작업을 삭제하면 배지가 사라지는지**(D7 — 역참조를 매번 재계산)
5. 상태 아이콘·상태 pill이 배지가 생겨도 세로 중앙에 유지되는지(RowSpan 3)
6. `E71B` 글리프가 시안의 체인 아이콘과 비슷해 보이는지 / 테두리 알파(0x73) 농도
7. ⚠️ **행 이름과 배지 텍스트가 같은 문자열로 겹쳐 보이는 것**이 의도한 표현인지 — `CreateLinkedTest`가 테스트 제목을 작업 제목 그대로 복사하기 때문(plan-reviewer m3)

## Phase Ledger
- 전 task(T1~T2) 완료.
- **Phase F 통과** — F-2 클린 리빌드(`-t:Rebuild`) 오류 0·신규 경고 0(자동 생성 `CS0618` 1건만), F-3 회귀 grep 10종 기대값, F-6.5 notes 기록(22,636자 — 아카이브 기준 미만)·Deferred 대장 반영(`[테스트→작업 역방향 링크/배지]` 종결 이동), F-7 `plan-completion-reviewer`: BLOCKER 0 / **MAJOR 1**(M1 배지 말줄임 미동작 — `StackPanel`→`Grid` 교체로 **수정 완료**) / MINOR 3(m1 PRD Coverage에 "링크" 미구현 명시 → 반영, m2 Phase Ledger → 이 기록, m3 notes 문구 → M1 수정으로 사실과 일치).
- **Phase G 통과 (Must 100%)** — 이번 plan이 커버 대상으로 선언한 FR은 **FR-E4(Should)** 하나이며 배지 몫 충족·링크 몫 Deferred로 정직하게 기록. active **Must** FR은 전부 `이번 범위 외 (기구현)`이고 F-7이 코드 4파일 외 무변경으로 회귀 표면 0을 확인 → 미충족 Must 0건, 재루프 0회.
- **F-8 미통과 — 시각/실동작 확인 대기**: 위 `## F-8 인계 목록` 7건이 남아 **완료 선언 보류**.

## Progress Log
- T1~T2 완료: `TestItem.LinkedTaskTitle`(표시 전용·비영속) + VM 역참조 채우기 + `EnsureTodosLoaded()` 추가(T1) → `TestItemRowTemplate`에 링크 배지 마크업 추가, 행 2→3단으로 확장하고 메모를 행 2로 이동(T2). 빌드 OK, 리뷰 지적 0.
  - 결정(T1): plan-reviewer가 BLOCKER로 잡은 **지연 로딩**이 핵심이었다 — `CreateTestPageViewModel()`이 `EnsureTestsLoaded()`만 부르고 `Todos`를 로드하지 않아, 그대로 뒀으면 빌드·grep을 전부 통과하면서 **배지가 조용히 전멸**했을 것이다. 한 줄 추가로 해소하고 acceptance·grep으로 재발을 막았다.
  - 결정(T2): 배지 색(`#7AB5EC`·`#735AA3E8`)은 시안 값이 팔레트에 없어 XAML 리터럴로 적었다(같은 파일 미실행 배지의 선례). quality 리뷰가 근접색 `AppInfoBrush`(#5B93D8) 재사용을 SUGGEST했으나 **시안 정합 우선**으로 유지하고 Deferred에 등재.
