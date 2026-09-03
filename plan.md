# plan.md — 알림 화면·알림 팝업 디자인 정합 개편 + 헤더 벨 아이콘 오류 수정

**기준 이미지**: 사용자 제공 2장(2026-09-02)을 레포에 고정했다 — ① `docs/design/uploads/notification-page.png`(알림 전체 페이지) ② `docs/design/uploads/notification-popup.png`(헤더 알림 팝업). **대화에만 있던 이미지는 후속 세션이 대조할 수 없어** 계획 단계에서 파일로 남긴다. `## 시각 요소 분해` 참조.
**PRD**: `docs/prd.md` — 이번 변경이 active Must FR **FR-N2**(알림 전체 페이지)·**FR-N3**(헤더 드롭다운 패널)에 직접 닿아 연결한다(Step 1 경량 확인). `## PRD Coverage` 참조.

## 요구 이해

> 원문(사용자, 2026-09-02): "디자인 문서 확인해서 [1번 이미지] [2번 이미지] 알림 화면(1번 이미지), 알림 팝업(2번 이미지) 처럼 화면 수정 및 기능 개선, 그리고 상단에 표시되는 알림 아이콘이 알림해제 상태 아이콘인데 확인해서 수정"

이해한 요구:
- **① 알림 페이지 재구현**: 현행 `NotificationPage`를 시안 ①처럼 바꾼다 — 헤더에 "읽지 않음 N건" pill 추가, 프로젝트 그룹 헤더에 개수 + 가로 구분선, 항목 행을 **마감상태 색 시계 아이콘 박스 + 제목 + "카테고리 · 종료일 yyyy-MM-dd" + 경과일 배지 + 읽음 토글 버튼** 구성으로 교체.
- **② 알림 팝업 재구현**: 헤더 벨의 Flyout이 지금은 문자열 한 줄 목록인데, 시안 ②처럼 **행 구조**(아이콘 · 제목 · "프로젝트 · 카테고리 · 종료일" · 배지 · 읽음 토글)로 바꾸고 항목 클릭 시 해당 작업으로 이동하게 한다. 표시 건수는 **전체 + 스크롤**(사용자 확정 Q1).
- **③ 헤더 아이콘 오류 수정**: 현행 글리프 `E7ED`는 **RingerSilent**(알림 해제 벨)다. 일반 벨 `EA8F`(**Ringer**)로 교체한다.
- **④ 함께 처리(사용자 선택)**: 안읽음 파란 점 제거(시안 정합, Q2) · 헤더 배지 "9+" 상한 · 배지 실시간 갱신 배선.
- **포함하지 않는 것으로 이해**: README·스크린샷 갱신(사용자가 이번 회차에서 제외 — 대장 유지).

## Goal

알림 전체 페이지와 헤더 알림 팝업을 디자인 시안과 동일한 구조·문구·색으로 재구현하고, 헤더 벨이 "알림 해제" 아이콘으로 보이던 오류를 고친다.

## PRD Coverage

| PRD ID | 우선순위 | 대응 task | 상태 |
|---|---|---|---|
| FR-N1 (마감 감지 로직) | Must | T1 | ✅ 커버 — `Notification`에 카테고리 추가(감지 조건 자체는 불변) |
| FR-N2 (알림 전체 페이지) | Must | T2 | ✅ 커버 — 그룹·읽음/안읽음·모두 읽음·클릭 이동 전부 유지하며 시안 정합 |
| FR-N3 (헤더 드롭다운 패널) | Must | T3 | ✅ 커버 — 요약 목록 + "모든 알림 보기" 유지하며 시안 정합 |
| FR-N4 (읽음 상태 영속화) | Must | T2·T3 | ✅ 커버 — `AppSettings.ReadNotificationIds` 경로 불변, 읽음 해제 경로만 추가 |
| 그 밖의 active Must FR (FR-C*·FR-T*·FR-E*·FR-H*·FR-D*) | Must | — | 이번 범위 외 (기구현) — 알림 영역 밖이라 이 plan이 닿지 않는다 |

## Out of Scope

- 알림 감지 규칙(D-3 임박·오늘·경과, 완료 제외) 변경 — 시안과 현행이 이미 동일하다.
- Windows 토스트 알림 등 앱 밖 알림 — 시안에 없고 요청에도 없다.

## Deferred / Follow-up

- [미판정] **[README·스크린샷 갱신]** — 알림 화면이 바뀌어 스크린샷이 또 낡는다. 사용자가 이번 회차에서 제외(Q3 미선택) → 대장 `:18` 항목에 이번 분을 합산한다.
- [미판정] **[팝업 항목 수가 많을 때의 성능]** — 전체 표시로 바꾸면 알림 수천 건에서 `ItemsControl`이 전부 그린다. 개인 대시보드 규모에선 무시할 비용이나, 느려지면 `ItemsRepeater` 가상화 검토.
- [미판정] **[PRD FR-N3 문구가 구현과 어긋남 — 사용자 승인 대기]** — FR-N3은 헤더 패널을 "요약 목록 + 모든 알림 보기"로 적는데, D4로 팝업이 **전체 목록 + 스크롤**이 됐다("요약"을 축약 표시로 읽으면 어긋나지 않으나 표현이 느슨하다). PRD는 승인 후 고정이라 임의 수정하지 않는다 — 대장의 `[PRD FR-E4 문구가 구현과 어긋남 — 사용자 승인 대기]`(`docs/plans/deferred.md`)와 같은 형태로 사용자 승인 후 정정.

## Investigation Log (근거)

- **위키 참조**: `20_projects/personal/devdashboard-winui/` feature 페이지 5종(`feat-data-settings`·`feat-launcher-sidebar`·`feat-project-cards`·`feat-project-launch-actions`·`feat-todo-test-worklog`) 및 허브를 `알림|Notification`으로 grep — **0건**. 알림 기능은 위키 미반영(허브 `synced_commit: 3d1f19d`, 17커밋 뒤처짐)이라 **코드를 1차 출처로 진행**한다.
- **결정 이력**: 대상 프로젝트 위키에 `decisions.md` 없음 — 과거 기각 이력 확인 불가.
- **기존 plan 처리**: 직전 "Git 상태 2뷰 개편" plan은 `## Phase Ledger`에 **Phase F 통과 + F-8 사용자 육안 확인 통과**를 기록했고 PRD 연결이 "없음"이라 Phase G 비대상 → 완료 판정. 그 `## Deferred / Follow-up` 5건은 Ledger의 "등재 4 / 미등재 0"대로 대장에 반영돼 있음을 직접 확인(`docs/plans/deferred.md:84`·`:86` 등). 이 plan으로 교체한다.
- **작업 공간**: `git status` clean(미커밋 0).
- **Deferred 대장 조회**(`docs/plans/deferred.md` `## 대기` — 착수 시점 **68건**(실측)):
  - **할 일 후보(주제 매칭)**: 3건이 이번 주제와 맞닿는다 — `[알림 배지 상한·요약 개수]`(`:16`) · `[알림 배지 실시간 갱신]`(`:19`) · `[NotificationPage 시안 대조]`(`:21`). 앞 둘은 사용자가 이번 회차에 채택(Q3) → T3·T4로 편입. `[NotificationPage 시안 대조]`는 "시안 미확보로 잔여"였는데 **이번에 시안 2장이 확보돼 이 plan이 그 항목 자체를 해소**한다(T2·T3).
  - **전제 반증(제목 스캔 68건 전건 + 걸린 항목 전문)**: `[BOM/no-BOM 인코딩 통일]`(`:17`)이 걸린다 — resw·기존 `.cs`는 BOM 포함이므로 편집 시 BOM을 보존해야 한다(`### 전제 검증` P5). `[AGENTS.md의 "상시 기존 경고 5건" 기록이 stale]`(`:64`)도 걸린다 — 신규 경고 0 판정의 기준선이 AGENTS.md 기재(`NU1903`+`CS0612` 4건)가 아니라 실측 `CS0618` 1건이다(P6). `[README/스크린샷]`(`:18`)은 이번 Deferred로 합산.
  - **소진 batch 판정**: `## 대기` 68건 — 잔량 임계(100건) 미달 → batch task 없음.
- **스킬 개선 큐**: 대상 아님 — 이 세션 cwd에 `plugins/pjc/.claude-plugin/plugin.json` 없음(하네스 레포가 아니다).
- **디자인 소스 (직접 Read — `docs/design/DevDashboard Redesign.dc.html`)**:
  - `:286~330` 알림 페이지 분기(`viewNotif`), `:431~462` 알림 패널(`notifOpen`), `:40~46` 헤더 벨 버튼, `:1096~1150` `notifVals()` 계산 로직.
  - 벨 SVG(`:41`)는 `M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9` + clapper — **사선 없는 일반 벨**이다.
  - 배지 문구(`:1117`): `d > 0 ? \`${d}일 경과\` : d === 0 ? '오늘 마감' : \`D${d}\`` (d = 오늘 − 종료일, 일 단위).
  - 부제(`:1121`): 페이지는 `${t.cat} · 종료일 ${end}`, 팝업은 `${t.project} · ${t.subCat}`.
  - 톤(`:1108~1110`): 경과 `fg #f0716a / bg rgba(240,113,106,.14)` · 오늘 `fg #e8925a / bg rgba(232,146,90,.16)` · 임박 `fg #7ab5ec / bg rgba(90,163,232,.16)`.
  - 읽음 버튼(`:1127`): 안읽음 `color #8a8890 / border #3a3a41`, 읽음 `color #5db463 / border #2e2e34`. **토글**(`onReadToggle` — 읽음↔안읽음 양방향, 항상 표시).
  - 헤더 배지(`:1143` `notifDotStyle`): `min-width:16px; height:16px; bg #f0716a; color #1d1113; font-size:10px; font-weight:700` — **상한 없음**(사용자가 "9+"를 별도 채택).
  - 안읽음 전용 점(dot) 표시는 **디자인에 없다** — 읽음 여부는 `opacity 0.5`와 읽음 버튼 색으로만 구분한다.
- **현행 구현 (직접 Read)**:
  - `Domain/ValueObjects/Notification.cs`: `record Notification { ProjectId, ProjectName, TodoId, TodoText, EndDate, Status }` — **카테고리 필드 없음**.
  - `Domain/Services/NotificationService.cs`: `Detect(projects, today)`가 D-3/오늘/경과를 집계하고 `BuildKey(n) = "{TodoId}:{Status}"`.
  - `Domain/Entities/TodoItem.cs:34`: `Category`(빈 문자열이면 미분류) 실재.
  - `Presentation/ViewModels/NotificationPageViewModel.cs`: `Groups`(프로젝트별), `HasNotifications`, `MarkRead`/`MarkAllRead`/`OpenTask`. **`OpenTask`는 읽음 처리를 하지 않는다**(`:87~91`) — 디자인은 클릭 시 읽음 처리도 한다.
  - `Presentation/Views/NotificationPage.xaml`: 항목 템플릿에 안읽음 `Ellipse`(`:35~40`) + `FormatDue`(`:47`) + 상태 점·라벨(`:56~65`) + 읽음 버튼(안읽음일 때만 표시, `:66~74`). 그룹 헤더는 프로젝트명뿐(`:124`), 개수·구분선 없음. 본문 `MaxWidth="560"`.
  - `Presentation/Views/NotificationPage.xaml.cs`: 정적 브러시 3종(`#F0716A`/`#E8B45A`/`#5AA3E8`) + `DeadlineBrush`·`DeadlineLabel`·`FormatDue`·`UnreadDotVisibility`·`ReadOpacity`.
  - `MainWindow.xaml:114~147`: 알림 버튼 — `FontIcon Glyph="&#xE7ED;"`(**RingerSilent**), Flyout 안은 `StackPanel Width="300"` + 제목 + **문자열 `ItemsControl`** + 빈 상태 + "모든 알림 보기". 배지는 `Border` + `UnreadNotificationCount` 숫자 직결.
  - `MainWindow.xaml.cs:562~577` `NotificationFlyout_Opening`: `RebuildNotifications()` 후 상위 **5건**을 `"{ProjectName} · {TodoText} ({DeadlineLabel})"` 문자열로 만들어 바인딩.
  - `Presentation/ViewModels/MainViewModel.cs:718~772`: `RebuildNotifications`·`GetCurrentNotifications`·`GetReadKeys`·`MarkNotificationRead`·`MarkAllNotificationsRead`·`CountUnread`. `UnreadNotificationCount`(int)·`HasUnreadNotifications`(bool).
- **소비처 전수 (grep, 레포 전체 `.cs`/`.xaml`, `obj/`·`bin/` 제외)**:
  - `NotificationPage.*` 정적 헬퍼 — **15 hit / 3파일**(실측): `NotificationPage.xaml` 8건, `NotificationPage.xaml.cs` 6건(정의), `MainWindow.xaml.cs:569` 1건(`DeadlineLabel`). 전건 Read 완료 — **알림 기능 밖 소비처 0**.
  - `NotificationItemViewModel`·`NotificationProjectGroup`·`NotificationPageViewModel` — **17 hit / 4파일**(실측): VM 정의, `NotificationPage.xaml(.cs)`, `MainWindow.xaml.cs:585`. 알림 기능 전용.
  - `Notification`(VO) — 소비처는 `NotificationService`·`MainViewModel`·`NotificationPageViewModel`뿐. **record라 필드 추가는 기존 소비처에 영향 없다**(생성은 `Detect` 한 곳).
- **배지 실시간 갱신 배선 경로 (직접 Read)**: `ProjectCardViewModel.cs:557~569` — `CreateTaskPageViewModel()`이 `RefreshTodoCardState`를 콜백으로 넘기고, 작업 변경 시 카드가 그것을 호출한다. `ProjectCardViewModel`은 `DeleteRequested`·`HistoryChanged` 등 **이벤트 9종**(`:222~255`)을 노출하고 `MainViewModel.SubscribeToCard`(`:321~328`)가 구독하는 구조 → **같은 패턴으로 이벤트 1종을 추가**해 `RebuildNotifications`를 배선한다.
- **부재 판정 (신규 작성의 근거)**:
  - **Flyout 커스텀 프레젠터 선례 없음** — 검색어 ①`FlyoutPresenterStyle`(레포 전체 `.xaml`) **0건** ②`FlyoutPresenter`(레포 전체 `.xaml`·`.cs`) **0건**. 팝업 폭·패딩·배경을 시안대로 잡으려면 신규 스타일이 필요하다.
  - **경과일 계산·표시 없음** — 검색어 ①`일 경과|DaysOverdue|경과일`(레포 전체 `.cs`·`.resw`) **0건** ②`Overdue`(레포 전체 `.cs`) hit는 전부 `DeadlineStatus.Overdue` 열거값 참조(6건)로 **일수 계산 구현은 0건**. 재사용할 기존 구현이 없다.
  - **배지 상한("9+") 처리 없음** — 검색어 ①`9+|MaxBadge|BadgeText`(레포 전체 `.cs`) **0건** ②`UnreadNotificationCount|HasUnreadNotifications`(레포 전체 `.cs`·`.xaml`) **8 hit / 2파일**(실측 — `MainViewModel.cs:59`·`:60`·`:63`·`:728`·`:747`·`:765`, `MainWindow.xaml:141`·`:142`)이며 전부 int 직결이라 문자열 변환 지점 없음.
- **글리프 확인 (Microsoft 공식 문서 원문 표 직접 대조)** — `MicrosoftDocs/windows-dev-docs` `hub/apps/design/iconography/segoe-fluent-icons-font.md`를 내려받아 grep:
  - `:277` `e7ed` = **RingerSilent** ← 현행 (사용자 지적이 정확하다)
  - `:768` `ea8f` = **Ringer** ← 교체 대상
  - `:1112` `edac` = RingerBadge12 (배지 내장형 — 별도 `Border` 배지를 쓰는 현행 구조와 맞지 않아 채택하지 않음)
  - 함께 확인: `e917` = **Clock**(시안의 원+시침 시계 아이콘 대응), `e76b` = **ChevronLeft**(시안의 `<` 화살표 — 현행 `e72b`는 **Back**(원형 화살표)), `e73e` = CheckMark(현행 유지).
- **팔레트·스타일 선례**: `Resources/Palette.xaml`은 `Default`(다크) 딕셔너리 하나. `Resources/Styles.xaml`에 `SeparatorStyle`(`:248` — 1px `DividerStrokeColorDefaultBrush`), `TopBarIconButtonStyle`(`:181`) 실재. **시안 색을 팔레트로 되돌리지 않는 것이 이 레포 규약**이다 — `TaskPage.xaml.cs:109~111` 주석이 *"시안이 정본이다. Palette.xaml의 AppWarning/AppInfo와는 의도적으로 다르니 팔레트 값으로 되돌리지 말 것"*이라 명시하고, 저채도 배경은 `alpha 0x28`(시안 `.16` 근사)로 만든다(`:118~120`).
- **미분류 처리 선례**: `TaskPageViewModel.cs:157` — 빈 카테고리는 `LocalizationService.Get("TaskCategory_None")`("미분류"/"Uncategorized", resw `:452`)로 표시한다. 알림 부제도 같은 키를 재사용한다.
- **resw 현황**: ko-KR / en-US 각 **454 항목(실측, 대칭)**. 알림 기존 키 11개(`Notification_Title`·`_Back`·`_MarkAll`·`_Empty`·`_DuePrefix`·`_Deadline_Imminent`·`_Deadline_DueToday`·`_Deadline_Overdue`·`_MarkRead_Tooltip`·`_Header_Tooltip`·`_ViewAll` — ko `:498~508`).
- **테스트**: 테스트 프로젝트 부재(AGENTS.md) — 4-C 해당 없음. 검증 = 빌드 + 사용자 육안.

### 전제 검증

| # | 이 plan이 참으로 삼는 전제 | 확인 근거 (파일:라인·명령) | 결과 |
|---|---|---|---|
| P1 | 현행 헤더 벨 글리프 `E7ED`가 "알림 해제" 아이콘이고, `EA8F`가 일반 벨이다 | `segoe-fluent-icons-font.md:277`(`e7ed` = RingerSilent) · `:768`(`ea8f` = Ringer) — MS 원문 표 직접 grep | ✅ |
| P2 | `Notification` VO에 카테고리가 없어 시안 부제("카테고리 · 종료일")를 지금 구조로는 만들 수 없다 | `Domain/ValueObjects/Notification.cs` 전문 Read — 필드 6개에 카테고리 없음 | ✅ |
| P3 | `Notification`에 필드를 추가해도 기존 소비처가 깨지지 않는다 | `record` 선언 + 생성 지점이 `NotificationService.Detect` 1곳뿐(grep 전수, Investigation Log 「소비처 전수」) | ✅ |
| P4 | `x:Bind` 함수 바인딩은 Converter·ThemeResource를 받지 못한다 → 색을 `ThemeResource`로 넘길 수 없어 `Brush` 자체를 값으로 전달해야 한다 | AGENTS.md 함정 5 + `TestPage.StatusBrush`·`TaskPage.PriorityBrush` 선례(`TaskPage.xaml.cs:114~135`) | ✅ — **어디에 둘지의 결론은 P9·D1이 정한다**(선례는 코드비하인드지만 이번엔 팝업이 재사용해야 해 ViewModel에 둔다) |
| P5 | 편집 대상 기존 파일(resw·`.cs`)은 BOM 포함이라 BOM을 보존해야 한다 | AGENTS.md 함정 1 + 대장 `:17` `[BOM/no-BOM 인코딩 통일]` | ✅ |
| P6 | "신규 경고 0" 판정의 기준선은 AGENTS.md 기재(5건)가 아니라 실측 `CS0618` 1건이다 | 대장 `:64` `[AGENTS.md의 "상시 기존 경고 5건" 기록이 stale]`(2026-07-22 실측) + 직전 회차 Phase F 기록(`baseline CS0618 1건`) | ✅ |
| P7 | `ProjectCardViewModel`에 이벤트를 추가해 `MainViewModel`이 구독하는 것이 이 레포의 카드→메인 통지 패턴이다 | `ProjectCardViewModel.cs:222~255`(이벤트 9종) + `MainViewModel.cs:321~337`(`SubscribeToCard`/`UnsubscribeFromCard` 쌍) 직접 Read | ✅ |
| P8 | WinUI `Flyout`의 기본 패딩·최대 폭을 시안대로 바꾸려면 `FlyoutPresenterStyle`이 필요하다 | 레포에 선례 0건(부재 판정) + `Flyout`이 `FlyoutPresenter`를 통해 렌더된다는 WinUI 구조 | ⚠ 미확인 — **성립을 좌우하지 않는다**(스타일이 불필요하면 그대로 두면 되고, 필요하면 T3에서 추가한다. 어느 쪽이든 T3의 acceptance는 "시안과 같은 폭·여백"이라 수단이 바뀌어도 성립) |
| P9 | `MainWindow.xaml`(Window)의 `DataTemplate` 안에서는 `{x:Bind}` 함수 바인딩을 쓸 수 없다 | AGENTS.md 함정 9(`Window` XAML의 `x:Bind ... Converter`는 CS1503) + 실측: `MainWindow.xaml`의 기존 `DataTemplate`(`:186`·`:330`)과 배지(`:141`·`:142`)가 **전부 `{Binding}`**이고 DataTemplate 내부 `x:Bind` 선례 **0건** | ✅ 확인 — **이 제약이 D1의 배치를 결정한다**: 색·툴팁을 `NotificationPage`의 정적 헬퍼(함수 바인딩 전용)에 두면 팝업이 재사용할 수 없으므로, **`NotificationItemViewModel`이 `Brush`·문구를 프로퍼티로 노출**하고 페이지·팝업 양쪽이 `{Binding}`으로 소비한다(D1 수정) |

### 4-D. 재사용 확인

| 신규 심볼 | 검색어·스코프 | hit | 유사 기존 구현 검색 결과 | 재사용/신규 사유 |
|---|---|---|---|---|
| `Notification.TodoCategory` (필드) | `Category` — `Domain/ValueObjects/*.cs` | 0 | 없음 | **신규** — `TodoItem.Category`(`:34`)를 그대로 옮겨 담는 값. 계산 없음 |
| `NotificationItemViewModel.BadgeText` | `일 경과\|DaysOverdue\|경과일` / `Overdue` — 레포 전체 `.cs`·`.resw` | 0 / 6(전부 enum 참조) | 없음 | **신규** — 시안 `:1117` 공식(`{d}일 경과`/`오늘 마감`/`D{d}`)을 구현. 알림 전용이라 공용 서비스로 빼지 않는다 |
| `NotificationItemViewModel.SubCaption` | `SubCaption\|Subtitle` — 레포 전체 `.cs` | 0 | 없음 | **신규** — 미분류 처리는 기존 `TaskCategory_None` 키 **재사용**(`TaskPageViewModel.cs:157` 선례) |
| `NotificationItemViewModel`의 `Brush` 프로퍼티 4종(`BadgeBackground`·`BadgeForeground`·`ReadButtonForeground`·`ReadButtonBorder`) | `PriorityBadgeBrush\|StatusBrush\|SoftBrush` — 레포 전체 `.cs` | 12 (`TaskPage.xaml.cs` 8 · `TestPage.xaml.cs` 4) | `TaskPage.PriorityBrush`/`PriorityBadgeBrush` 쌍(코드비하인드 정적 반환) | **패턴 참고, 배치는 다름** — 선례는 `x:Bind` 함수 바인딩 전용이라 Window(`MainWindow.xaml`)의 DataTemplate이 재사용할 수 없다(P9). 두 화면이 같은 값을 써야 하므로 **항목 ViewModel이 `Brush`를 노출**하고 양쪽이 `{Binding}`으로 소비한다. 정적 `SolidColorBrush` 인스턴스는 `NotificationItemViewModel`의 `private static` 필드로 두어 항목마다 새로 만들지 않는다 |
| `MainViewModel.UnreadNotificationBadge` (string) | `9+\|MaxBadge\|BadgeText` / `UnreadNotificationCount` — 레포 전체 `.cs` | 0 / 5(전부 int 직결) | 없음 | **신규** — "9+" 상한(사용자 채택)을 위해 표시용 문자열이 필요. 기존 int 프로퍼티는 유지하고 파생 프로퍼티만 추가 |
| `ProjectCardViewModel.TodosChanged` (이벤트) | `TodosChanged\|HistoryChanged` — 레포 전체 `.cs` | 4 (전부 `HistoryChanged` — 정의 1 · 발생 1 · 구독/해제 2) | `HistoryChanged` 이벤트 | **패턴 재사용, 심볼 신규** — 같은 배선 구조를 그대로 따른다(P7) |
| `NotificationFlyoutPresenterStyle` (XAML 스타일) | `FlyoutPresenterStyle` / `FlyoutPresenter` — 레포 전체 `.xaml`·`.cs` | 0 / 0 | 없음 | **신규** — 재사용할 스타일이 없다. `Styles.xaml`에 두고 `MainWindow`만 소비 |

### Verified by

- `grep -rn "NotificationPage\.\(DeadlineLabel\|DeadlineBrush\|UnreadDotVisibility\|ReadOpacity\|FormatDue\|MarkReadTooltip\)"` → 15 hits / 3파일, 모두 아래 Impact 표에 포함
- `grep -rn "NotificationItemViewModel\|NotificationProjectGroup\|NotificationPageViewModel"` → 17 hits / 4파일, 모두 포함
- `grep -rn "UnreadNotificationCount\|HasUnreadNotifications"` → **8 hits / 2파일**(`MainViewModel.cs` 6 · `MainWindow.xaml` 2), 모두 포함
- `grep -n "SubscribeToCard\|UnsubscribeFromCard" MainViewModel.cs` → 9 hits — 정의 2(`:321`·`:330`) · 구독 호출 3(`:317`·`:355`·`:484`) · 해제 호출 4(`:347`·`:481`·`:596`·`:658`). T4는 정의 2곳만 고치므로 호출 4곳이 자동으로 커버된다
- `grep -rn "RebuildNotifications"` → 3 hits(`MainViewModel.cs:248`·`:718`, `MainWindow.xaml.cs:564`), 모두 포함

## Risks & Unknowns

| 위험 | 영향 | 완화책 |
|---|---|---|
| `Flyout` 기본 스타일이 시안 폭(380)·무여백 행을 방해 | 팝업이 시안과 다르게 보임 | `FlyoutPresenterStyle`로 `Padding=0`·`MaxWidth`·`Background`·`CornerRadius` 재정의(T3). 그래도 안 되면 `Popup` 전환은 하지 않고 `MinWidth`/`MaxHeight`로 근사 후 F-8 육안 확인에 올린다 |
| 배지 실시간 갱신이 작업 변경마다 전체 프로젝트를 재스캔 | 작업 편집 시 지연 | `NotificationService.Detect`는 메모리 순회뿐이고 개인 대시보드 규모(프로젝트 수십·작업 수백)에선 무시할 비용. 측정값이 없으므로 체감 지연이 보이면 F-8에서 보고 |
| `x:Bind` 함수 바인딩에 `Mode=OneWay`가 필요한 프로퍼티를 놓침 | 읽음 토글 후 UI가 갱신되지 않음 | 읽음 상태에 딸린 모든 바인딩(`Opacity`·읽음 버튼 색·툴팁)에 `Mode=OneWay` 명시 + `IsRead` 변경 시 파생 프로퍼티 `OnPropertyChanged` 발행(T2 Design) |
| resw ko/en 한쪽만 등록 | 다른 언어에서 빈 문자열 + **빌드는 통과** | AGENTS.md 함정 2 — T2·T3 acceptance에 ko/en 대칭 카운트를 판정 명령으로 박는다 |

## Impact Analysis

### 4-A. 심볼/타입 추적 결과

| 심볼 | 영향 받는 파일 | 영향 종류 |
|---|---|---|
| `Notification` (record) | `Domain/ValueObjects/Notification.cs` | 필드 `TodoCategory` 추가 |
| `NotificationService.Detect` | `Domain/Services/NotificationService.cs` | 생성 시 `TodoCategory = todo.Category` 채움 |
| `NotificationItemViewModel` | `Presentation/ViewModels/NotificationPageViewModel.cs` · `NotificationPage.xaml` | 표시 프로퍼티 추가(`BadgeText`·`SubCaption`·`PopupSubCaption`·`ProjectName`), `IsRead` 파생 통지 |
| `NotificationPageViewModel` | 같은 파일 · `NotificationPage.xaml(.cs)` · `MainWindow.xaml.cs:585` | `UnreadCount`·`HasUnread` 추가, `MarkRead` → 토글로 변경, `OpenTask`가 읽음 처리 동반 |
| `NotificationPage`의 정적 멤버 — 공개 6종 + 그 전용 private 4종 (**목록 정본은 T3 Design ⑤**) | `NotificationPage.xaml(.cs)` · `MainWindow.xaml.cs:569` | **T3에서 일괄 제거** — 표시값이 `NotificationItemViewModel` 프로퍼티로 옮겨가고(T2), 마지막 외부 호출자 `MainWindow.xaml.cs:569`가 T3의 행 템플릿으로 대체되면서 사라진다. T2에서 미리 지우면 그 시점에 CS0117로 빌드가 깨지므로 제거 시점을 T3로 고정한다 |
| `MainViewModel.UnreadNotificationCount` | `Presentation/ViewModels/MainViewModel.cs` · `MainWindow.xaml:142` | 유지하되 파생 `UnreadNotificationBadge`(string) 추가, XAML 바인딩을 후자로 교체 |
| `ProjectCardViewModel.RefreshTodoCardState` | `Presentation/ViewModels/ProjectCardViewModel.cs` | `TodosChanged` 이벤트 발생 추가 |
| `MainViewModel.SubscribeToCard`/`UnsubscribeFromCard` | `Presentation/ViewModels/MainViewModel.cs:321~337` | 구독/해제 쌍에 `TodosChanged` 추가 |
| 헤더 벨 `FontIcon` | `MainWindow.xaml:135` | 글리프 `E7ED` → `EA8F` |

### 4-B. 계약·직렬화 변경

- `Notification`은 **런타임 전용 값 객체**다 — DB·JSON 어디에도 직렬화되지 않는다(`AppSettings.ReadNotificationIds`가 저장하는 것은 `BuildKey`가 만든 문자열 키뿐이며 그 공식은 이번에 바꾸지 않는다). 필드 추가에 마이그레이션 불요.
- `NotificationService.BuildKey` 불변 → 기존에 읽음 처리한 알림이 이번 변경 후에도 읽음으로 유지된다.

### 4-C. 테스트 파일

- 해당 없음 — 테스트 프로젝트 부재(AGENTS.md "Test: 없음"). 검증은 빌드 + 사용자 육안(F-8).

## 동반 변경 판정

| 구분 | 항목 | 근거 | 처리 |
|---|---|---|---|
| 필수 | `MainWindow.xaml.cs:569`의 `NotificationPage.DeadlineLabel` 호출 | T3이 팝업을 행 템플릿으로 바꾸면 이 문자열 조립 코드가 남아 있을 수 없다(호출부가 사라지는 자기 유발 변경) | T3에 편입 |
| 필수 | resw `Notification_DuePrefix` 값("마감"/"Due") | 시안 부제는 "종료일"이라 값을 바꾸지 않으면 화면과 시안이 어긋난다 | T2에 편입(값 교체 — 키는 유지) |
| 필수 | resw `Notification_MarkRead_Tooltip` 값("읽음 처리"/"Mark as read") | 읽음 버튼이 토글이 되면서(D2) 툴팁이 상태별 두 문구가 된다 — 기존 값이 "처리"라 토글 상태 표현과 어긋난다 | T2에 편입 — 값을 `읽음으로 표시`/`Mark as read`로 정정하고 짝이 되는 `Notification_MarkUnread_Tooltip`을 신설 |
| 필수 | resw `Notification_Empty` 값("마감이 임박한 작업이 없습니다."/"No upcoming deadlines.") | 시안 빈 상태 문구는 **2줄**(`마감된 작업이 없습니다.` + `종료일이 지난 미완료 작업이 여기에 표시됩니다.` — 디자인 `:300`·`:441`)이라 현행 1줄 값으로는 시안을 만들 수 없다 | T2에 편입 — 값을 1줄째로 교체하고 2줄째용 키 `Notification_EmptyHint` 신설(ko/en) |
| 필수 | `Notification_Deadline_Imminent`("임박")·`_DueToday`("오늘 마감")·`_Overdue`("경과") 3키와 동적 조립 헬퍼 `DeadlineLabel`, 그리고 그 유일한 외부 호출자 `MainWindow.xaml.cs:569` | 시안 배지는 `48일 경과`·`오늘 마감`·`D-2`를 **포맷 문자열**로 만들어 신규 키 3종을 쓴다 → 구 키 3종과 `DeadlineLabel`이 고아가 된다(자기 유발). **호출자가 사라지는 T3에서 함께 제거해야** 중간 상태에서 빌드가 깨지지 않는다 | **T3에 편입**(T2 아님) — 팝업 재구현으로 `MainWindow.xaml.cs:569`가 사라지는 같은 task에서 헬퍼·구 키 3종을 제거한다 |
| 선택 | 헤더 배지 "9+" 상한(대장 `:16`) | 없어도 성립하나 두 자릿수에서 배지가 넓어진다 | **사용자 채택** → T3에 편입 |
| 선택 | 배지 실시간 갱신(대장 `:19`) | 없어도 성립하나 작업 완료 후 배지가 stale | **사용자 채택** → T4에 편입 |
| 선택 | README·스크린샷 갱신(대장 `:18`) | 없어도 성립하나 스크린샷이 낡음 | **사용자 거절** → Deferred 유지(대장에 이번 분 합산) |
| 선택 | `docs/prd.md` FR-N3 문구("요약 목록 + 모든 알림 보기") | D4로 팝업이 **전체 목록 + 스크롤**이 되어 "요약 목록"이라는 표현이 느슨해진다. 없어도 성립하나(축약 표시라는 뜻으로 읽을 수 있다) 다음 회차 Phase G 대조에서 재판정될 여지가 남는다. PRD는 승인 후 고정이므로 임의 수정하지 않는다 | **Deferred 등재** — 대장의 `[PRD FR-E4 문구가 구현과 어긋남 — 사용자 승인 대기]`와 같은 형태로 사용자 승인 대기 |
| 무관 | `docs/prd.md` FR-N1·N2·N4 문구 | 시안 정합 개편이 FR 서술("마감 감지" / "프로젝트별 그룹, 읽음/안읽음, 모두 읽음, 항목 클릭 시 이동" / "읽음 상태 영속화")을 **전부 그대로 만족**한다 — 고칠 문장이 없다 | 건드리지 않음 |
| 무관 | `Package.appxmanifest` 버전 | 이 레포에 버전 증가·릴리즈 규약이 AGENTS.md에 없고 이번 변경은 기능 restyle이다 | 건드리지 않음 |
| 무관 | `Resources/Palette.xaml` | 시안 색을 팔레트로 되돌리지 않는 것이 이 레포 규약(`TaskPage.xaml.cs:109~111`)이라 새 색을 팔레트에 넣지 않는다 | 건드리지 않음 |

## Decisions

### D1. 배지·아이콘 색을 시안 값 정적 브러시로 두되, **항목 ViewModel이 `Brush`를 노출**한다
- **Options**: A) `NotificationPage.xaml.cs` 정적 헬퍼가 `Brush` 반환(`x:Bind` 함수 바인딩 — `TaskPage`/`TestPage` 선례) / B) `NotificationItemViewModel`이 `Brush` 프로퍼티로 노출(양쪽 `{Binding}`) / C) `Palette.xaml`에 새 색 키 추가 후 `ThemeResource` / D) 기존 `AppAccent*`·`AppInfo*` 재사용
- **Chosen**: B
- **Rationale**: C는 원리상 불가 — `x:Bind` 함수 바인딩이 `ThemeResource`를 받지 못한다(AGENTS.md 함정 5). D는 시안 톤(오늘 `#e8925a`, 임박 fg `#7ab5ec`)과 팔레트 값(`#D9954A`, `#5B93D8`)이 달라 시각 충실도가 깨진다. **A는 페이지에서만 되고 팝업에서는 안 된다** — `MainWindow.xaml`은 `Window`라 DataTemplate 안에서 `x:Bind` 함수 바인딩 선례가 0건이고 함정 9가 이 영역의 알려진 지뢰다(P9). 같은 색을 두 화면이 써야 하므로, 값을 한 곳(항목 ViewModel)에 두고 양쪽이 `{Binding}`으로 소비하는 B가 유일하게 두 화면 모두에서 성립한다. 정적 `SolidColorBrush` 인스턴스는 `private static` 필드로 공유해 항목마다 새로 만들지 않는다.
- **Source**: AGENTS.md 함정 5·9, `TaskPage.xaml.cs:109~111`(*"시안이 정본이다 … 팔레트 값으로 되돌리지 말 것"* — 색값 규약은 그대로 따른다), `MainWindow.xaml:141`·`:142`·`:186`·`:330`(전부 `{Binding}`).

### D2. 읽음 버튼을 "읽음 처리 전용"에서 "읽음↔안읽음 토글"로 바꾼다
- **Options**: A) 시안대로 토글(항상 표시) / B) 현행 유지(안읽음일 때만 표시, 단방향)
- **Chosen**: A
- **Rationale**: 시안 `:1127`·`:1128`이 `onReadToggle`로 양방향이고 `readTitle`도 `읽지 않음으로 표시`/`읽음으로 표시` 두 문구를 갖는다. 시각 충실도 요청의 정본이 시안이므로 A. 읽음 해제 경로는 `AppSettings.ReadNotificationIds`에서 키를 제거하면 되고 영속화 구조를 바꾸지 않는다.
- **Source**: 디자인 소스 `:1127~1129`, `MainViewModel.cs:738~749`(`MarkNotificationRead` 구조).

### D3. 안읽음 파란 점을 제거한다
- **Options**: A) 시안대로 제거(투명도 + 버튼 색으로만 구분) / B) 점 유지 + 시안 요소 추가
- **Chosen**: A (사용자 확정)
- **Rationale**: 시안 어디에도 안읽음 dot이 없다. B는 시안과 다른 화면이 된다.
- **Source**: 디자인 소스 `:312~324`(페이지 행)·`:445~457`(팝업 행) — dot 요소 부재, 사용자 답변(Q2).

### D4. 팝업은 전체 알림을 표시하고 높이 제한으로 스크롤한다
- **Options**: A) 전체 + 스크롤(시안) / B) 상위 5건 유지 / C) 상위 10건
- **Chosen**: A (사용자 확정)
- **Rationale**: 시안 `:444`가 `notifItems` 전체를 렌더하고 `:431`이 `max-height:70vh`로 제한한다. 시안 이미지 ②에도 6건이 모두 보인다.
- **Source**: 디자인 소스 `:431`·`:444`, 사용자 답변(Q1).

### D5. 헤더 배지에 "9+" 상한을 적용한다 (시안과 의도적으로 다름)
- **Options**: A) 상한 없음(시안 원본) / B) "9+" 상한
- **Chosen**: B (사용자 확정 — 대장 `:16` 재수용)
- **Rationale**: 시안 `:1143`에는 상한이 없고 `min-width:16px`로 폭이 늘어나게 돼 있다. 사용자가 대장 항목을 이번 회차에 채택했으므로 **의도적 시안 이탈**이며, 이 사실을 `## 시각 요소 분해` 표에 명시해 V-9가 "불일치"로 오판하지 않게 한다.
- **Source**: 사용자 답변(Q3), `docs/plans/deferred.md:16`.

### D6. 배지 실시간 갱신은 카드 이벤트로 배선한다
- **Options**: A) `ProjectCardViewModel`에 `TodosChanged` 이벤트 추가 → `MainViewModel`이 구독해 `RebuildNotifications` / B) `TaskPageViewModel`이 `MainViewModel`을 직접 참조 / C) `MainWindow`가 페이지 전환 시 갱신
- **Chosen**: A
- **Rationale**: B는 Presentation 내부에 역참조를 만들어 카드→메인 단방향 통지 구조를 깬다. C는 페이지를 벗어나야 갱신돼 "실시간"이 아니다. A는 이 레포에 이미 이벤트 9종이 같은 방식으로 배선된 검증된 패턴이다(P7).
- **Source**: `ProjectCardViewModel.cs:222~255`, `MainViewModel.cs:321~337`, `ProjectCardViewModel.cs:562`(`RefreshTodoCardState` 콜백 지점).

### D7. 배지 문구는 시안 공식을 그대로 쓴다
- **Options**: A) 시안 `{d}일 경과` / `오늘 마감` / `D{d}` / B) 현행 `경과` / `오늘 마감` / `임박`
- **Chosen**: A
- **Rationale**: 시안 이미지 ①·②에 `48일 경과`·`47일 경과` 등 일수가 실제로 보인다. 시각 충실도 요청의 정본이 시안이다. `D{d}`는 d가 음수(`d = 오늘 − 종료일`)라 `D-2` 형태로 렌더된다.
- **Source**: 디자인 소스 `:1117`, 시안 이미지 ① 배지 텍스트.

## 시각 요소 분해

**기준**: 디자인 HTML `docs/design/DevDashboard Redesign.dc.html`(알림 페이지 `:286~330` · 알림 패널 `:431~462` · 헤더 벨 `:40~46` · 계산 `:1096~1150`) + 렌더 이미지 `docs/design/uploads/notification-page.png`·`notification-popup.png`.
**대응 주의**: 시안은 웹(HTML/CSS), 대상은 WinUI 3(XAML)이라 1:1 대응하지 않는다 — 아래 표에 XAML 대응 수단을 함께 적는다.

### 시각 속성

| 요소 | 속성 | 디자인 값 | XAML 대응 수단 | 확인 방법 |
|---|---|---|---|---|
| **헤더 벨 버튼** | 글리프 | 사선 없는 일반 벨 | `FontIcon Glyph="&#xEA8F;"` (Ringer) | 디자인 HTML `:41` SVG path + MS 표 `:768` |
| 헤더 벨 버튼 | 크기 | 36×36, radius 9 | 기존 `TopBarIconButtonStyle`(36/36/CornerRadius 8) 재사용 | 디자인 HTML `:40` |
| **헤더 배지** | 배경/글자 | `#f0716a` / `#1d1113` | `Border Background` + `TextBlock Foreground` 직접 색 | 디자인 HTML `:1143` |
| 헤더 배지 | 크기·폰트 | min-width 16, height 16, padding 0/4, radius 8, 10px/700 | `MinWidth="16" Height="16" Padding="4,0" CornerRadius="8"`, `FontSize="10" FontWeight="Bold"` | 디자인 HTML `:1143` |
| 헤더 배지 | 위치 | top -3 right -3 | `HorizontalAlignment=Right VerticalAlignment=Top Margin="0,-3,-3,0"` | 디자인 HTML `:1143` |
| 헤더 배지 | 문구 | 숫자 그대로(상한 없음) | **의도적 이탈** — `9+` 상한 적용(D5) | 사용자 채택 |
| **페이지: 뒤로 버튼** | 아이콘 | `<` chevron 13px | `FontIcon Glyph="&#xE76B;" FontSize="13"` (ChevronLeft) | 디자인 HTML `:289` SVG + MS 표 `:205` |
| 페이지: 뒤로 버튼 | 박스 | height 34, padding 0/13, radius 9, bg `#1d1d21`, border `#2b2b31` | `Button Height="34" Padding="13,0" CornerRadius="9"` + 색 직접 | 디자인 HTML `:288` |
| 페이지: 뒤로 버튼 | 문구 | `대시보드` | 기존 resw `Notification_Back` | 디자인 HTML `:290` |
| **페이지: 제목** | font | 17px / 700 | `FontSize="17" FontWeight="Bold"` | 디자인 HTML `:292` |
| **페이지: 안읽음 pill** | 문구 | `읽지 않음 {N}건` | 신규 resw `Notification_UnreadFormat` | 디자인 HTML `:293` |
| 페이지: 안읽음 pill | 스타일 | 12px, `#b3b0b8`, bg `#26262c`, radius 7, padding 4/10 | `Border CornerRadius="7" Padding="10,4"` + `TextBlock FontSize="12"` | 디자인 HTML `:293` |
| **페이지: 모두 읽음** | 표시 조건 | 안읽음 > 0일 때만 | `Visibility` ← `HasUnread`(현행은 `HasNotifications` — 변경) | 디자인 HTML `:295` |
| 페이지: 모두 읽음 | 스타일 | height 32, padding 0/14, radius 8, bg `#242429`, border `#2e2e34`, 12.5px | `Button Height="32" Padding="14,0" CornerRadius="8"` | 디자인 HTML `:296` |
| **페이지: 헤더 배치** | 간격·여백 | gap 14, padding 16/24/0 | `Grid ColumnSpacing="14"`, `Padding="24,16,24,0"` | 디자인 HTML `:287` |
| **페이지: 본문** | 여백·폭 | padding 18/24/24, max-width 720, 그룹 간 gap 22 | `Padding="24,18,24,24"`, `MaxWidth="720"`, 그룹 `Margin="0,0,0,22"` | 디자인 HTML `:298`·`:302` |
| **페이지: 그룹 헤더** | 구성 | 프로젝트명(14px/700) + 개수(12px, `#8a8890`, 모노) + 나머지 폭 1px 선(`#26262c`) | `Grid` 3열(`Auto,Auto,*`) + `Rectangle Height="1"`(`SeparatorStyle` 재사용), 개수는 `FontFamily="Consolas"` | 디자인 HTML `:305~308` |
| 페이지: 그룹 헤더 | 간격 | gap 10, 헤더~항목 gap 10 | `ColumnSpacing="10"`, `Margin="0,0,0,10"` | 디자인 HTML `:304`·`:306` |
| **페이지: 항목 행** | 박스 | bg `#1f1f24`, border `#27272d`, radius 12, padding 15/18, gap 14 | `Border CornerRadius="12" Padding="18,15"` + `Grid ColumnSpacing="14"` | 디자인 HTML `:1123` |
| 페이지: 항목 행 | 정렬 | `align-items:center` | `VerticalAlignment="Center"` | 디자인 HTML `:1123` |
| 페이지: 항목 행 | hover | border `#3d3d45` | `PointerEntered`/`PointerExited`로 `BorderBrush` 교체(`HistoryDialog.xaml:130~139` 선례) | 디자인 HTML `:312` |
| 페이지: 항목 행 | 읽음 흐림 | `opacity 0.5` | `Opacity="{Binding RowOpacity}"` — 값은 `NotificationItemViewModel`이 노출한다(D1·P9. 현행 `NotificationPage.ReadOpacity` 정적 헬퍼는 팝업이 못 쓰므로 T2에서 VM으로 옮기고 T3에서 제거) | 디자인 HTML `:1123` |
| **페이지: 아이콘 박스** | 크기·색 | 34×34, radius 9, bg = tone.bg, 시계 16px = tone.fg | `Border Width=34 Height=34 CornerRadius=9` + `FontIcon Glyph="&#xE917;" FontSize="16"` (Clock) | 디자인 HTML `:1130`·`:314` |
| **페이지: 제목** | font | 14px / 700 | `FontSize="14" FontWeight="Bold"` | 디자인 HTML `:317` |
| **페이지: 부제** | 문구·색 | `{카테고리} · 종료일 {yyyy-MM-dd}`, 12px, `#98959d`, 위 3px | `FontSize="12" Margin="0,3,0,0"` + 색 직접 | 디자인 HTML `:318`·`:1121` |
| **페이지: 배지** | 문구 | `{d}일 경과` / `오늘 마감` / `D{d}` | 신규 resw 3키 — `Notification_Badge_Overdue`(`{0}일 경과`) · `_Badge_DueToday`(`오늘 마감`) · `_Badge_Imminent`(`D{0}`). 구 `Notification_Deadline_*` 3키는 T3에서 제거 | 디자인 HTML `:1117` |
| 페이지: 배지 | 스타일 | 10.5px/700, radius 6, padding 3/8, bg = tone.bg, fg = tone.fg | `Border CornerRadius="6" Padding="8,3"` + `TextBlock FontSize="10.5" FontWeight="Bold"` | 디자인 HTML `:1131` |
| **페이지: 읽음 버튼** | 크기·색 | 26×26, radius 7, border `#3a3a41`(안읽음)/`#2e2e34`(읽음), fg `#8a8890`(안읽음)/`#5db463`(읽음), 체크 13px | `Button Width=26 Height=26 CornerRadius=7` + `FontIcon Glyph="&#xE73E;" FontSize="13"`, 색은 `{Binding ReadButtonForeground}`·`{Binding ReadButtonBorder}`(항목 ViewModel이 노출 — D1·P9) | 디자인 HTML `:1127`·`:321~323` |
| 페이지: 읽음 버튼 | 툴팁 | `읽지 않음으로 표시`(읽음일 때) / `읽음으로 표시`(안읽음일 때) | 신규 resw `Notification_MarkUnread_Tooltip`(`읽지 않음으로 표시`) + 기존 `Notification_MarkRead_Tooltip` 값을 `읽음 처리` → `읽음으로 표시`로 정정 | 디자인 HTML `:1129` |
| **페이지: 안읽음 dot** | 존재 | **없음** | 현행 `Ellipse`(`NotificationPage.xaml:35~40`) 제거 | 디자인 HTML `:312~324`(요소 부재) |
| **페이지: 빈 상태** | 문구·스타일 | `마감된 작업이 없습니다.` + `종료일이 지난 미완료 작업이 여기에 표시됩니다.`, 13.5px, `#6f6d75`, line-height 1.7, padding 80/0 | 2줄 `TextBlock` + `LineHeight`. 1줄째는 기존 `Notification_Empty` 값 교체, 2줄째는 신규 `Notification_EmptyHint` | 디자인 HTML `:300` |
| **팝업: 패널** | 박스 | width 380, max-height 70vh, bg `#1c1c20`, border `#2e2e34`, radius 14 | `FlyoutPresenterStyle`(신규)로 `Padding=0`·`MinWidth/MaxWidth=380`·`MaxHeight`·`Background`·`CornerRadius=14` | 디자인 HTML `:431` |
| **팝업: 헤더** | 구성·여백 | `알림`(15px/700) + `읽지 않음 {N}건`(11.5px, `#8a8890`) + 우측 `모두 읽음`, padding 16/18, 하단 1px `#26262c` | `Grid` + `Border BorderThickness="0,0,0,1"` | 디자인 HTML `:432~438` |
| 팝업: 모두 읽음 | 스타일 | height 28, padding 0/10, radius 7, 11.5px | `Button Height="28" Padding="10,0" CornerRadius="7"` | 디자인 HTML `:437` |
| **팝업: 항목 행** | 박스 | padding 13/18, gap 11, 하단 1px `#232328`, `align-items:flex-start` | `Border BorderThickness="0,0,0,1" Padding="18,13"` + `Grid ColumnSpacing="11"` + `VerticalAlignment="Top"` | 디자인 HTML `:1124` |
| 팝업: 항목 행 | hover | bg `#232329` | `PointerEntered`/`PointerExited`로 `Background` 교체 | 디자인 HTML `:445` |
| **팝업: 아이콘 박스** | 크기 | 26×26, radius 8, 시계 12px | `Border Width=26 Height=26 CornerRadius=8` + `FontIcon FontSize="12"` | 디자인 HTML `:1129`(iconStyle)·`:447` |
| **팝업: 제목** | font | 13px / 700 | `FontSize="13" FontWeight="Bold"` | 디자인 HTML `:450` |
| **팝업: 부제** | 문구 | `{프로젝트} · {카테고리} · 종료일 {yyyy-MM-dd}`, 11.5px, `#98959d` | `FontSize="11.5"`, `TextWrapping="Wrap"`(시안 이미지 ②에서 2줄로 접힘) | 디자인 HTML `:451`, 이미지 ② |
| **팝업: 배지** | 스타일 | 페이지와 동일(10.5px/700, radius 6, padding 3/8, tone 색) | 페이지 배지와 같은 값 — 팝업 전용 축소 없음 | 디자인 HTML `:453`이 공용 `badgeStyle`(`:1131`)을 그대로 쓴다 |
| **팝업: 읽음 버튼** | 크기·색 | 26×26, radius 7(페이지와 동일), **체크 아이콘만 12px**(페이지는 13px) | `Button Width=26 Height=26 CornerRadius=7` + `FontIcon Glyph="&#xE73E;" FontSize="12"` | 디자인 HTML `:454~455`(공용 `readBtnStyle` `:1127` + SVG 12px) |
| **팝업: 하단 버튼** | 문구·스타일 | `모든 알림 보기`, height 44, 상단 1px `#26262c`, 12.5px/500, 전체 폭 | `Button Height="44" HorizontalAlignment="Stretch"` + 상단 `Border` | 디자인 HTML `:460` |
| 팝업: 하단 버튼 | 표시 조건 | **알림 0건에도 항상 표시**(`notifEmpty` 분기 밖에 있다) | `Visibility` 조건을 걸지 않는다 — 현행 `MainWindow.xaml.cs:576`의 `hasAny` 토글을 제거 | 디자인 HTML `:460`(`sc-if` 밖) vs `:441`(빈 상태는 `sc-if` 안) |
| **팝업: 빈 상태** | 문구·여백 | 페이지와 같은 2줄, 12.5px, padding 44/18 | `TextBlock` 2줄 | 디자인 HTML `:441` |
| **톤(공통)** | 경과 | fg `#f0716a` / bg `rgba(240,113,106,.14)` | `SolidColorBrush` `#FFF0716A` / `#24F0716A` | 디자인 HTML `:1108` |
| 톤(공통) | 오늘 | fg `#e8925a` / bg `rgba(232,146,90,.16)` | `#FFE8925A` / `#28E8925A` | 디자인 HTML `:1109` |
| 톤(공통) | 임박 | fg `#7ab5ec` / bg `rgba(90,163,232,.16)` | `#FF7AB5EC` / `#285AA3E8` (배경 베이스가 fg와 다름 — `TaskPage.xaml.cs:113` 선례와 같은 구조) | 디자인 HTML `:1110` |

## Tasks

- [x] T1. `Notification`에 작업 카테고리를 담고 감지 로직이 채우게 한다
  - **Type**: C
  - **Design**: ① `Domain/ValueObjects/Notification.cs`에 `TodoCategory` 필드 1개 추가(빈 문자열이면 미분류 — `TodoItem.Category`와 같은 규약). ② 신규 심볼은 필드 하나뿐이며 계산·변환을 도메인에 두지 않는다(표시 문구 조립은 T2의 ViewModel 몫). ③ 의존 방향 불변 — `NotificationService`(Domain)가 `TodoItem`(Domain)에서 읽어 채우고, Presentation이 소비한다. ④ **비추상화 선언**: 카테고리 표시명 해석(빈 값 → "미분류")을 도메인에 넣지 않는다 — 그것은 지역화가 필요한 표현 관심사다.
  - **Acceptance**: Given 종료일이 지난 미완료 작업에 카테고리 `백엔드`가 있고, When `NotificationService.Detect`가 집계하면, Then 반환된 `Notification.TodoCategory == "백엔드"`이고 카테고리가 빈 작업은 `TodoCategory == string.Empty`다. 빌드 오류 0 · 신규 경고 0(기준선 `CS0618` 1건).
  - **Files**:
    - 주: `DevDashboard_WinUI/Domain/ValueObjects/Notification.cs` · `DevDashboard_WinUI/Domain/Services/NotificationService.cs`
  - **Edge Cases**:
    - ~~`todo.Category`가 `null`(구버전 DB) → `?? string.Empty`로 정규화~~ → **실측 결과 불요**: `TodoItem.Category`는 non-nullable(`:36`, 기본값 `string.Empty`)이고 DB 읽기가 구버전을 `hasCategory` 가드로 이미 `string.Empty`로 정규화한다(`SqliteProjectRepository.cs:114`·`:882`). 방어용 `??`를 넣었다가 quality 리뷰 m1 지적으로 제거
    - 카테고리가 공백만인 문자열 → 표시 단계(T2)에서 `IsNullOrWhiteSpace`로 미분류 처리
  - **Halt Forecast**:
    - (i) `record` 필드 추가가 기존 생성부를 깨는가 → 생성 지점이 `Detect` 1곳뿐임을 확인 완료(P3)
  - **Depends on**: -

- [x] T2. 알림 전체 페이지를 시안대로 재구현한다
  - **Type**: D
  - **Design**: ① 표시용 값(문구·색·툴팁)은 전부 `Presentation/ViewModels/NotificationPageViewModel.cs`의 `NotificationItemViewModel`에 둔다 — **`NotificationPage.xaml.cs`의 정적 헬퍼를 늘리지 않는다**(그 방식은 Window인 `MainWindow.xaml`이 재사용할 수 없다 — P9·D1). ② 신규 심볼 — `NotificationItemViewModel`: `BadgeText`(시안 `:1117` 공식), `SubCaption`(`{카테고리} · 종료일 {날짜}`), `PopupSubCaption`(`{프로젝트} · {카테고리} · 종료일 {날짜}` — T3가 소비), `ProjectName`, `BadgeBackground`/`BadgeForeground`/`ReadButtonForeground`/`ReadButtonBorder`(`Brush`, `private static` 필드 공유), `ReadToggleTooltip`, `RowOpacity`(읽음 0.5 / 안읽음 1.0 — 팝업이 `{Binding}`으로 쓸 수 있게 VM으로 옮긴다); `NotificationPageViewModel`: `UnreadCount`(int), `HasUnread`(bool), `UnreadCaption`(string — `읽지 않음 {N}건` 완성 문구. **VM에 두는 이유**: T3의 팝업 헤더가 같은 문구를 `{Binding}`으로 재사용한다 — 정적 프로퍼티로 만들면 개수가 바뀔 때 갱신되지 않고 팝업도 못 쓴다). ③ 의존 방향 — View → ViewModel → `Notification`(Domain). ViewModel은 여전히 주입된 콜백으로 영속화를 위임한다(현행 구조 유지). ④ **비추상화 선언**: 배지 문구 포맷터를 공용 서비스로 빼지 않는다(소비처가 이 화면과 팝업 둘뿐이고 둘 다 같은 ViewModel을 쓴다). 시각 값(색·크기)을 `Palette.xaml`로 올리지 않는다(D1). 빈 상태 2줄째용 정적 프로퍼티 `NotificationPage.EmptyHintText`는 이 task에서 추가한다(페이지 전용이라 `x:Bind` 정적 참조로 충분 — 팝업 몫은 T3이 `MainWindow`에 따로 만든다). **기존 `NotificationPage`의 정적 멤버(`DeadlineLabel`·`DeadlineBrush`·`FormatDue`·`UnreadDotVisibility`·`ReadOpacity`·`MarkReadTooltip`)는 이 task에서 지우지 않는다** — `DeadlineLabel`의 외부 호출자(`MainWindow.xaml.cs:569`)가 T3에서야 사라지므로, T2에서 지우면 그 시점에 빌드가 깨진다. **T2가 하는 것은 `NotificationPage.xaml`에서 그 멤버들에 대한 참조를 전부 걷어내는 것까지**이며(그래야 T3의 코드 제거가 XAML 재작업 없이 통과한다), 멤버 자체의 정리는 T3이 호출자와 함께 한다.
  - **Acceptance**:
    - Given 마감 알림이 3개 프로젝트에 걸쳐 6건 있고 그중 2건이 안읽음, When 알림 페이지를 열면, Then 헤더에 `읽지 않음 2건` pill과 `모두 읽음` 버튼이 보이고, 각 프로젝트 그룹 헤더에 `프로젝트명 + 건수 + 가로 구분선`이, 각 행에 `마감상태 색 시계 아이콘 + 제목 + "카테고리 · 종료일 yyyy-MM-dd" + 경과일 배지 + 읽음 토글 버튼`이 표시된다.
    - Given 안읽음이 0건, When 페이지를 열면, Then `모두 읽음` 버튼이 숨겨지고 pill은 `읽지 않음 0건`을 표시한다.
    - Given 읽음 처리된 항목, When 읽음 버튼을 다시 누르면, Then 안읽음으로 되돌아가고 카드 투명도·버튼 색·툴팁이 즉시 바뀐다(앱 재시작 후에도 유지).
    - Given 임의 항목, When 행을 클릭하면, Then 그 항목이 읽음 처리되고 해당 프로젝트 작업 페이지로 이동한다.
    - 안읽음 점·상태 점이 화면에 없다(둘 다 시안에 없고 배지가 대체한다) — `grep -c "Ellipse" DevDashboard_WinUI/Presentation/Views/NotificationPage.xaml` **작성 시점 2건**(안읽음 점 `:36` · 마감상태 점 `:57`) → 수정 후 **0건**.
    - resw ko/en 대칭 — `grep -c "<data name=" DevDashboard_WinUI/Strings/ko-KR/Resources.resw` 와 en-US가 **작성 시점 각 454** → 수정 후 **양쪽 같은 수**.
    - 배지 문구 신규 키 3종이 ko/en 양쪽에 있다 — `grep -rn "Notification_Badge_" DevDashboard_WinUI --include=*.resw | wc -l` **작성 시점 0건** → 수정 후 **6건**(`_Badge_Overdue`·`_Badge_DueToday`·`_Badge_Imminent` × ko/en).
    - 부제 접두 문구가 시안과 같다 — `grep -n "Notification_DuePrefix" DevDashboard_WinUI/Strings/ko-KR/Resources.resw`의 값이 **작성 시점 `마감`** → 수정 후 **`종료일`**(en은 `Due` → `Due date`. 키는 유지).
    - 빈 상태가 시안과 같은 2줄이다 — `grep -n 'name="Notification_Empty"' DevDashboard_WinUI/Strings/ko-KR/Resources.resw`(정확히 이 키만 매칭하도록 좁힌 형태. 넓게 쓰면 같은 task가 만드는 `_EmptyHint`가 함께 걸려 출력이 2줄이 된다)의 값이 **작성 시점 `마감이 임박한 작업이 없습니다.`** → 수정 후 **`마감된 작업이 없습니다.`**이고, `grep -c "Notification_EmptyHint" DevDashboard_WinUI/Strings/ko-KR/Resources.resw` **작성 시점 0건** → 수정 후 **1건**(en도 동일).
    - 읽음 토글 툴팁이 두 문구다 — `grep -c "Notification_MarkUnread_Tooltip" DevDashboard_WinUI/Strings/ko-KR/Resources.resw` **작성 시점 0건** → 수정 후 **1건**, 그리고 `grep -n 'name="Notification_MarkRead_Tooltip"' DevDashboard_WinUI/Strings/ko-KR/Resources.resw`의 값이 **작성 시점 `읽음 처리`** → 수정 후 **`읽음으로 표시`**(en도 동일).
    - `NotificationPage.xaml`에 구 정적 헬퍼 참조가 남지 않는다 — `grep -o "local:NotificationPage\.[A-Za-z]*" DevDashboard_WinUI/Presentation/Views/NotificationPage.xaml | sort -u | wc -l` **작성 시점 10종**(`BackText`·`DeadlineBrush`·`DeadlineLabel`·`EmptyText`·`FormatDue`·`MarkAllText`·`MarkReadTooltip`·`PageTitle`·`ReadOpacity`·`UnreadDotVisibility` — 총 참조 12회) → 수정 후 **5종**(`PageTitle`·`BackText`·`MarkAllText`·`EmptyText`·`EmptyHintText`만 남는다 — 나머지는 VM `{x:Bind}`로 대체). **행 hover 복귀는 `ClearValue(Border.BorderBrushProperty)`로 처리해** 기본 테두리색 사본을 코드비하인드에 두지 않는다(값은 VM의 `RowBorder` 한 곳).
    - 빌드 오류 0 · 신규 경고 0(기준선 `CS0618` 1건). **구 헬퍼 멤버 자체와 `Notification_Deadline_*` 키는 이 task에서 삭제하지 않는다** — 삭제는 T3(호출자가 사라지는 곳). T2 종료 시점에는 참조 없는 정적 멤버가 남아 있고, 이는 컴파일 오류도 MSBuild 경고도 아니다.
  - **Files**:
    - 주: `DevDashboard_WinUI/Presentation/Views/NotificationPage.xaml` · `NotificationPage.xaml.cs` · `DevDashboard_WinUI/Presentation/ViewModels/NotificationPageViewModel.cs`
    - 동반: `DevDashboard_WinUI/Strings/ko-KR/Resources.resw` · `DevDashboard_WinUI/Strings/en-US/Resources.resw` · `DevDashboard_WinUI/Presentation/ViewModels/MainViewModel.cs`(읽음 해제 경로 `UnmarkNotificationRead` 추가)
  - **Edge Cases**:
    - 카테고리가 빈 작업 → 부제에 `미분류 · 종료일 …`(기존 `TaskCategory_None` 키 재사용)
    - 알림 0건 → 그룹 목록 대신 2줄 빈 상태 문구
    - 배지 경과일이 3자리(수백 일) → 배지 폭이 늘어나되 제목이 밀려나지 않게 제목 열만 `*`로 확장
    - 종료일이 오늘 → `오늘 마감`, 내일~3일 후 → `D-1`~`D-3`
    - 제목이 매우 긴 작업 → `TextTrimming="CharacterEllipsis"`로 1줄 유지(시안이 1줄 표시)
    - 읽음 토글 직후 `IsRead` 파생 바인딩(색·툴팁·투명도)이 갱신되지 않는 경우 → `IsRead` 변경 시 파생 프로퍼티를 함께 통지
    - **기존 코드비하인드 핸들러 처리** — `Item_Tapped`(`NotificationPage.xaml.cs:69`)·`MarkRead_Click`(`:76`)은 **핸들러 방식을 유지**하되 후자를 `ReadToggle_Click`으로 개명하고 `Vm.ToggleReadCommand`를 실행한다(이름이 토글 동작과 어긋나지 않게). 커맨드 직결로 바꾸지 않는 이유는 `Tag`로 항목을 넘기는 현행 패턴(`HistoryDialog.xaml:130~139` 선례)을 그대로 두는 편이 변경 폭이 작기 때문이다. `MarkReadCommand`는 토글 커맨드로 대체되므로 VM에서도 이름을 맞춘다
  - **Halt Forecast**:
    - (i) 배지 문구의 정확한 포맷 → D7에서 확정
    - (i) 색을 어디에 둘지 → D1에서 확정
    - (ii-a) `MainViewModel`에 공개 메서드 `UnmarkNotificationRead` 추가(계획된 공개 API 변경) → `## 사전 승인 항목`에 등록
  - **Depends on**: T1 (부제의 카테고리가 T1의 `TodoCategory`를 소비한다)

- [x] T3. 헤더 알림 팝업을 시안대로 재구현하고 벨 글리프·배지 상한을 고친다
  - **Type**: D
  - **Design**: ① 팝업 행 템플릿은 `MainWindow.xaml`의 `Flyout` 안에 두고(현행 위치 유지), 프레젠터 스타일 `NotificationFlyoutPresenterStyle`은 `Resources/Styles.xaml`에 둔다. 행이 소비하는 표시 프로퍼티는 **T2가 만든 `NotificationItemViewModel`을 그대로 재사용**하며, Window 제약 때문에 전부 `{Binding}`으로 바인딩한다(P9·D1 — `x:Bind` 함수 바인딩을 쓰지 않는다). 팝업 전용 부제는 `PopupSubCaption`. ② 신규 심볼 — `MainViewModel.UnreadNotificationBadge`(int → "9+" 상한 문자열), `NotificationFlyoutPresenterStyle`. ③ 의존 방향 — `MainWindow`(View) → `NotificationItemViewModel`(Presentation/ViewModels) → `Notification`(Domain). ④ **비추상화 선언**: 페이지와 팝업의 행 템플릿을 공용 `DataTemplate`으로 묶지 않는다 — 크기·정렬·부제가 서로 다르고(34 vs 26px 아이콘, center vs top 정렬, 부제에 프로젝트명 유무), 공용화하면 AGENTS.md 함정 11(공용 템플릿의 소비처 충돌)을 자초한다. ⑤ **구 경로 철거를 이 task가 맡는다** — `MainWindow.xaml.cs:569`의 문자열 요약 조립이 행 템플릿으로 대체되면서 `NotificationPage.DeadlineLabel`의 마지막 호출자가 사라지므로, 같은 task에서 `NotificationPage.xaml.cs`의 아래를 **전부** 제거한다(공개 멤버만 지우고 그 전용 private 필드를 남기면 죽은 코드가 된다):
    - 공개 정적 **6종**: `DeadlineLabel`(`:50`) · `DeadlineBrush`(`:42`) · `FormatDue`(`:54`) · `UnreadDotVisibility`(`:57`) · `ReadOpacity`(`:61`) · `MarkReadTooltip`(`:35` — 유일 소비처 `NotificationPage.xaml:71`이 T2에서 VM `ReadToggleTooltip`으로 대체돼 고아)
    - 그 전용 private 필드 **4종**: `_overdueBrush`·`_dueTodayBrush`·`_imminentBrush`(`:26~28` — `DeadlineBrush`에서만 읽힘) · `_duePrefix`(`:37` — `FormatDue`에서만 읽힘)
    - ~~**호출부가 사라진 `using` 2개**: `using Microsoft.UI;` · `using Microsoft.UI.Xaml.Media;`~~ → **실측 결과 죽지 않는다**: 행 hover 브러시 `_rowHoverBorderBrush`(`SolidColorBrush` + `ColorHelper`)가 T2에서 추가돼 두 네임스페이스를 계속 소비한다. 제거 대상 없음
    - **유지**: `PageTitle`·`BackText`·`MarkAllText`·`EmptyText`(헤더·빈 상태가 계속 소비) + T2가 추가한 `EmptyHintText`
    - resw `Notification_Deadline_*` 3키(ko/en 각 3건)도 함께 제거.
  - **Acceptance**:
    - Given 마감 알림 6건 중 2건 안읽음, When 헤더 벨을 클릭하면, Then 폭 380의 패널에 헤더(`알림` + `읽지 않음 2건` + `모두 읽음`) · **6건 전부**의 행(아이콘 · 제목 · `프로젝트 · 카테고리 · 종료일` · 배지 · 읽음 토글) · 하단 `모든 알림 보기` 버튼이 보이고, 목록이 패널 높이를 넘으면 목록만 스크롤되며 헤더·하단 버튼은 고정된다.
    - Given 팝업이 열린 상태, When 항목 행을 클릭하면, Then 팝업이 닫히고 그 항목이 읽음 처리되며 해당 프로젝트 작업 페이지로 이동한다.
    - Given 안읽음 12건, When 헤더를 보면, Then 배지에 `9+`가 표시된다(폭 확장 없음).
    - Given 알림 0건, When 벨을 클릭하면, Then 2줄 빈 상태 문구가 보이고 `모두 읽음`은 숨겨지되 **`모든 알림 보기`는 그대로 보인다**(시안 `:460`이 빈 상태 분기 밖에 있다 — 현행의 `hasAny` 토글을 제거).
    - 헤더 벨이 일반 벨이다 — `grep -c "E7ED" DevDashboard_WinUI/MainWindow.xaml` **작성 시점 1건** → 수정 후 **0건**, `grep -c "EA8F" DevDashboard_WinUI/MainWindow.xaml` **작성 시점 0건** → 수정 후 **1건**.
    - 문자열 요약 조립이 사라졌다 — `grep -c "NotificationPage.DeadlineLabel" DevDashboard_WinUI/MainWindow.xaml.cs` **작성 시점 1건** → 수정 후 **0건**.
    - 마감상태 라벨 구 경로가 전부 사라졌다 — `grep -rn "Notification_Deadline_" DevDashboard_WinUI --include=*.cs --include=*.xaml --include=*.resw | wc -l` **작성 시점 7건**(ko resw 3 `:503~505` · en resw 3 `:503~505` · `NotificationPage.xaml.cs:51`의 `$"Notification_Deadline_{status}"` 동적 조립 1) → **T3 수정 후 0건**(배지 문구는 T2가 만든 `Notification_Badge_*` 3키를 쓴다).
    - resw ko/en 대칭(T2와 같은 명령).
    - 빌드 오류 0 · 신규 경고 0(기준선 `CS0618` 1건).
  - **Files**:
    - 주: `DevDashboard_WinUI/MainWindow.xaml` · `DevDashboard_WinUI/MainWindow.xaml.cs` · `DevDashboard_WinUI/Resources/Styles.xaml`
    - 동반: `DevDashboard_WinUI/Presentation/ViewModels/MainViewModel.cs`(`UnreadNotificationBadge`) · `DevDashboard_WinUI/Presentation/Views/NotificationPage.xaml.cs`(Design ⑤의 공개 6종 + private 4종 제거) · `DevDashboard_WinUI/Strings/ko-KR/Resources.resw` · `en-US/Resources.resw`(`Notification_Deadline_*` 3키 제거)
  - **Edge Cases**:
    - 알림이 수십 건 → 패널 `MaxHeight`로 잘리고 목록만 스크롤(헤더·하단 버튼 고정)
    - 안읽음 0건 → pill은 `읽지 않음 0건`, `모두 읽음` 숨김, 배지 자체 숨김(`HasUnreadNotifications`)
    - 안읽음 9건 → `9`, 10건 이상 → `9+`(경계값)
    - 팝업 안에서 읽음 토글 → 팝업이 닫히지 않아야 한다(행 클릭 이벤트와 버튼 클릭이 겹치지 않게 버튼에서 이벤트 전파 차단)
    - 팝업에서 읽음 토글 후 헤더 배지가 즉시 갱신되어야 한다(`RebuildNotifications` 없이 `CountUnread` 재계산)
    - `Flyout` 기본 최대 폭이 380보다 작아 잘리는 경우 → 프레젠터 스타일에서 `MaxWidth` 명시
    - 알림 0건에서 `모든 알림 보기`를 누름 → 빈 알림 페이지가 열린다(시안이 그 버튼을 빈 상태에서도 노출하므로 의도된 동작)
    - 구 헬퍼 제거 후 `NotificationPage.xaml`에 남은 참조 → `DeadlineBrush`·`FormatDue`·`UnreadDotVisibility`는 T2에서 이미 XAML에서 걷어냈으므로 T3의 코드 제거만으로 컴파일이 통과해야 한다(통과하지 않으면 T2가 덜 걷어낸 것 — 그 참조부터 제거)
  - **Halt Forecast**:
    - (i) 팝업 표시 건수 → D4에서 확정
    - (i) 배지 상한 → D5에서 확정
    - (ii-a) `MainViewModel`에 공개 프로퍼티 `UnreadNotificationBadge` 추가(계획된 공개 API 변경) → `## 사전 승인 항목`에 등록
  - **Depends on**: T2 (`NotificationItemViewModel`의 표시 프로퍼티와 색 헬퍼를 재사용한다 — T2가 만들지 않으면 팝업 행을 구성할 수 없다)

- [x] T4. 작업 변경 시 헤더 안읽음 배지를 즉시 갱신한다
  - **Type**: C
  - **Design**: ① `ProjectCardViewModel`에 이벤트 `TodosChanged`를 두고 `RefreshTodoCardState()`(`:565`) 안에서 발생시킨다. 구독은 `MainViewModel.SubscribeToCard`/`UnsubscribeFromCard` 쌍에 추가한다. ② 신규 심볼 — `ProjectCardViewModel.TodosChanged`(이벤트), `MainViewModel.OnCardTodosChanged`(핸들러 — `RebuildNotifications()` 호출). ③ 의존 방향 — 카드가 이벤트를 올리고 메인이 구독한다(역참조 없음, 기존 이벤트 9종과 동일). ④ **비추상화 선언**: 알림 갱신을 위한 별도 서비스·메신저를 도입하지 않는다 — 이 레포는 DI 컨테이너·메신저 없이 손배선하는 구조다(AGENTS.md Conventions).
  - **Acceptance**: Given 종료일이 지난 미완료 작업이 있고 헤더 배지가 `1`, When 작업 페이지에서 그 작업을 완료로 바꾸고 대시보드로 돌아오면, Then **팝업을 열지 않아도** 헤더 배지가 사라진다. 구독/해제가 짝을 이룬다 — `grep -c "TodosChanged" DevDashboard_WinUI/Presentation/ViewModels/MainViewModel.cs` **작성 시점 0건** → 수정 후 **3건**(구독 1 · 해제 1 · 핸들러 정의 1). 빌드 오류 0 · 신규 경고 0.
  - **Files**:
    - 주: `DevDashboard_WinUI/Presentation/ViewModels/ProjectCardViewModel.cs` · `DevDashboard_WinUI/Presentation/ViewModels/MainViewModel.cs`
  - **Edge Cases**:
    - 구독 해제 누락 → 해제 호출부는 4곳(`MainViewModel.cs:347`·`:481`·`:596`·`:658`)이지만 전부 `UnsubscribeFromCard`(`:330`)를 거치므로 **그 메서드 하나에 `-=`를 추가하면 전부 커버**된다. 구독도 마찬가지로 `SubscribeToCard`(`:321`) 하나(호출 3곳: `:317`·`:355`·`:484`). 둘 중 한쪽만 고치면 메모리 누수·중복 호출이 난다
    - 작업 편집이 연달아 일어남 → `RebuildNotifications`가 멱등이므로 중복 호출이 안전(`_currentNotifications`를 통째로 교체)
    - 초기 로드 중 카드 구독이 `RebuildNotifications`(`:248`)보다 먼저 일어나는 경우 → 순서와 무관하게 결과가 같다(둘 다 전체 재계산)
  - **Halt Forecast**:
    - (i) 배선 방식 → D6에서 확정
    - (ii-a) `ProjectCardViewModel`에 공개 이벤트 추가(계획된 공개 API 변경) → `## 사전 승인 항목`에 등록
  - **Depends on**: - (T1~T3과 독립 — 알림 계산 결과만 소비하며 표시 구조에 의존하지 않는다)

## 검증 방법

- **빌드**(각 task 후): `"C:/Program Files/Microsoft Visual Studio/18/Professional/MSBuild/Current/Bin/MSBuild.exe" "DevDashboard_WinUI/DevDashboard.csproj" -t:Build -p:Configuration=Debug -p:Platform=x64` — 오류 0 · 신규 경고 0(기준선 `CS0618` 1건, 대장 `:64` 참조)
- **클린 리빌드**(Phase F): 위 명령에 `-t:Rebuild`
- **회귀 grep**(Phase F): 각 task acceptance의 판정 명령 전건 재실행 + resw ko/en 대칭 카운트
- **육안 확인**(F-8, 사용자): 시안 이미지 2장과 대조 — 아래 인계 목록

## F-8 인계 목록 (렌더 육안 확인 대기)

1. 헤더 벨이 **사선 없는 일반 벨**로 보이는가
2. 안읽음 12건일 때 배지가 `9+`로 보이는가
3. 알림 페이지가 `notification-page.png`와 같은 구조인가(pill · 그룹 구분선 · 아이콘 박스 · 배지 · 읽음 버튼)
4. 알림 팝업이 `notification-popup.png`와 같은 구조인가(폭 · 헤더 · 행 · 하단 버튼)
5. 읽음 토글이 양방향으로 동작하고 재시작 후에도 유지되는가
6. 작업을 완료로 바꾼 뒤 팝업을 열지 않아도 배지가 갱신되는가
7. 마감상태별 색(경과 코랄 / 오늘 주황 / 임박 하늘)이 시안과 같은가
8. 팝업 알림이 많을 때 **목록만 스크롤**되고 헤더("알림 · 읽지 않음 N건 · 모두 읽음")와 하단 "모든 알림 보기"가 고정되는가
9. 팝업에서 읽음 버튼을 눌렀을 때 **팝업이 닫히거나 작업 페이지로 이동하지 않는가**(버튼 클릭이 행 클릭으로 번지지 않는가)

## 사전 승인 항목 (일괄 승인 대상)

- `MainViewModel`에 공개 메서드 `UnmarkNotificationRead(Notification)` 추가 (T2 — 읽음 해제 경로)
- `MainViewModel`에 공개 프로퍼티 `UnreadNotificationBadge`(string) 추가 (T3 — 배지 "9+" 상한)
- `ProjectCardViewModel`에 공개 이벤트 `TodosChanged` 추가 (T4 — 배지 실시간 갱신 배선)
- `Notification` record에 필드 `TodoCategory` 추가 (T1)
- `Resources/Styles.xaml`에 스타일 `NotificationFlyoutPresenterStyle` 추가 (T3)
- resw 신규 키 추가 (T2 — `Notification_Badge_Overdue`·`_Badge_DueToday`·`_Badge_Imminent`·`_UnreadFormat`·`_EmptyHint`·`_MarkUnread_Tooltip`, ko/en 각 1건씩) 및 값 교체 (`Notification_DuePrefix`·`Notification_Empty`)
- 고아 resw 키 `Notification_Deadline_Imminent`·`_Deadline_DueToday`·`_Deadline_Overdue` 제거 (T3 — ko/en 각 3건)
- `NotificationPage` 공개 정적 멤버 6종(`DeadlineLabel`·`DeadlineBrush`·`FormatDue`·`UnreadDotVisibility`·`ReadOpacity`·`MarkReadTooltip`)과 그 전용 private 필드 4종(`_overdueBrush`·`_dueTodayBrush`·`_imminentBrush`·`_duePrefix`) 제거 (T3 — T3 Design ⑤가 정본)
- `NotificationPage`에 정적 프로퍼티 `EmptyHintText` 추가, `MainWindow`에 `NotificationEmptyHintText`·`NotificationMarkAllText` 추가 (T2·T3 — 빈 상태 2줄째 + 팝업 "모두 읽음" 라벨. **`NotificationMarkAllText`는 계획에 없던 1개로, F-7 m2 지적으로 여기 기록** — 팝업 헤더가 페이지와 같은 `Notification_MarkAll` 문구를 쓰는데 Window에서 `x:Bind`로 읽으려면 이 프로퍼티가 필요했다. 문자열 상수라 동작 영향 없음)

## 불가피한 Halt (위임 불가)

- 작업 브랜치 → `master` 병합
- 원격 `origin` push
- 태그·릴리즈 발행

## Open Questions

- [x] Q1. 알림 팝업에 몇 건까지 표시할까? — A) ★〔근본〕**전체 표시 + 스크롤**(시안 `:431`·`:444` 정본) / B) 상위 5건 유지〔우회 — 시안과 어긋남〕 / C) 상위 10건〔우회 — 시안에 없는 임의 수치〕. **사용자 선택: A**(★와 일치). → D4
- [x] Q2. 안읽음 파란 점을 유지할까? — A) ★〔근본〕**시안대로 제거**(시안에 dot 요소 자체가 없다) / B) 점 유지 + 시안 요소 추가〔우회 — 시안과 다른 화면이 됨〕. **사용자 선택: A**(★와 일치). → D3
- [x] Q3. 맞닿은 보류 항목 중 함께 처리할 것은? (자유 조합 — 범위 선택이라 근본/우회 축 비대상) — 후보: ⓐ 헤더 배지 `9+` 상한 / ⓑ 배지 실시간 갱신 / ⓒ README·스크린샷 갱신. **사용자 선택: ⓐ + ⓑ**(ⓒ 미선택 → 대장 유지). → D5·D6, `## Deferred / Follow-up`

## 리뷰 이력 (plan-reviewer)

| 라운드 | 지적 | 심각도 | 반영 방식 |
|---|---|---|---|
| 1R | B1 — T2가 `DeadlineLabel`을 제거하는데 유일한 외부 호출자 `MainWindow.xaml.cs:569`가 T2 Files에 없어 T2 종료 시점 CS0117 | BLOCKER | **수용** — 리뷰어 제안 ⓑ 채택. 구 헬퍼 4종·`ReadOpacity`·`Notification_Deadline_*` 3키 제거를 **호출자가 사라지는 T3로 이동**. T2는 새 경로만 만들고 구 경로를 건드리지 않는다(T2 Design ①·acceptance 마지막 줄, T3 Design ⑤·Files·acceptance, Impact 표, 동반 변경 판정 필수 3행) |
| 1R | M1 — T2 acceptance의 `Notification_Deadline_` 기대값 2건이 "DueToday 재사용"과 동시 만족 불가 | MAJOR | **수용** — 배지 문구를 **신규 3키(`Notification_Badge_*`)로 통일**하고 구 3키는 전부 제거(T3 기대값 **0건**). T2에는 `Notification_Badge_` 6건 판정을 새로 넣었다. 시각 요소 분해 배지 행도 키 이름을 명시 |
| 1R | M2 — 빈 상태에서 `모든 알림 보기`를 숨기는 것이 시안(`:460`, 빈 상태 분기 밖)과 어긋나는데 이탈 기록이 없음 | MAJOR | **수용** — 숨기지 않는 쪽(시안대로)으로 변경. T3 acceptance 수정 + 시각 요소 분해에 「표시 조건」 행 추가 + Edge Case 추가 |
| 1R | M3 — T3가 재사용한다는 T2 색 헬퍼가 `x:Bind` 함수 바인딩 전용이라 Window(`MainWindow.xaml`)에서 쓸 수 없다(함정 9 영역, 미검증) | MAJOR | **수용** — `MainWindow.xaml`의 DataTemplate이 전부 `{Binding}`이고 `x:Bind` 선례 0건임을 실측 확인해 `### 전제 검증` **P9**로 등재. **D1을 "코드비하인드 정적 헬퍼" → "항목 ViewModel이 `Brush` 노출"로 변경**하고 4-D·T2 Design ①②·T3 Design ①을 함께 정정 |
| 1R | m1 — `UnreadNotificationCount` 실측 수치(5건)가 실제(8 hit)와 다름 | MINOR | **수용** — Investigation Log·Verified by 정정(8 hit / 2파일) |
| 1R | m2 — T4 Edge Case가 인용한 구독 해제 라인(`:483`·`:502`)이 실물과 다름 | MINOR | **수용** — 실측 라인(`:347`·`:481`·`:596`·`:658`)으로 정정하고 "메서드 하나만 고치면 전부 커버"를 명시 |
| 1R | m3 — 사전 승인 항목이 고아 키를 `_Imminent` 하나만 적음 | MINOR | **수용** — 신규 키 6종·제거 키 3종·헬퍼 제거를 항목별로 분리 기재 |
| 1R | m4 — 시각 요소 분해에 팝업 배지·읽음 버튼 행이 없고 헤더 배지의 padding·radius 누락 | MINOR | **수용** — 팝업 배지·읽음 버튼 행 2개 추가, 헤더 배지 행에 `padding 0/4`·`radius 8` 보강 |
| 1R | m5 — 시안 빈 상태 문구가 현행 `Notification_Empty` 값과 다른데 판정 명령이 없음 | MINOR | **수용** — 동반 변경 판정에 **필수** 행으로 추가하고 T2 acceptance에 값 교체·`Notification_EmptyHint` 신설 판정 추가 |
| 1R | m6 — PRD FR-N3 "요약 목록"이 D4(전체 목록)와 느슨하게 어긋남 | MINOR | **수용(범위 밖 처리)** — PRD는 승인 후 고정이라 임의 수정하지 않고, 동반 변경 판정 **선택** 행 + `## Deferred / Follow-up`에 "사용자 승인 대기"로 등재 |
| 2R | M1 — D1 변경이 시각 요소 분해 2행(읽음 버튼 색·읽음 흐림)에 미반영이라 표가 기각된 배치를 지시 | MAJOR | **수용** — 두 행을 `{Binding ReadButtonForeground}`·`{Binding ReadButtonBorder}`·`{Binding RowOpacity}`(항목 ViewModel)로 정정 |
| 2R | M2 — 공개 헬퍼만 철거 목록에 있어 그 전용 private 필드(`_overdueBrush`·`_dueTodayBrush`·`_imminentBrush`·`_duePrefix`)와 `MarkReadTooltip`이 고아로 남음 | MAJOR | **수용** — 실측으로 `MarkReadTooltip` 소비처가 `NotificationPage.xaml:71` 1곳뿐임을 확인. T3 Design ⑤를 **공개 6종 + private 4종 + 유지 대상**으로 전면 재작성하고 Files·사전 승인 항목을 맞춤 |
| 2R | m1 — 철거 목록이 task 본문(4종)과 Impact·사전 승인(5종)으로 갈림 | MINOR | **수용** — Impact 행을 "목록 정본은 T3 Design ⑤"로 바꿔 정본을 한 곳으로 모음 |
| 2R | m2 — 전제 P4가 "색은 코드비하인드가 반환해야 한다"로 남아 D1(B안)·P9와 어긋남 | MINOR | **수용** — P4는 "Converter·ThemeResource 불가"라는 사실만 남기고 배치 결론은 P9·D1로 넘김 |
| 2R | m3 — `Notification_MarkRead_Tooltip` 값 교체가 동반 변경에도 판정 명령에도 없음 | MINOR | **수용** — 동반 변경 **필수** 행 추가 + T2 acceptance에 값 교체·`_MarkUnread_Tooltip` 신설 판정 추가 |
| 2R | m4 — T2의 `grep -n "Notification_Empty"`가 같은 task가 만드는 `_EmptyHint`도 매칭 | MINOR | **수용** — `grep -n 'name="Notification_Empty"'`로 좁히고 그 이유를 병기 |
| 3R | m1 — private 필드 라인 인용이 1행 밀림(`:25~27` → `:26~28`) | MINOR | **수용** — 실물 라인으로 정정 |
| 3R | m2 — T3 철거 후 죽는 `using` 2개(`Microsoft.UI`·`Microsoft.UI.Xaml.Media`)가 목록에 없음 | MINOR | **수용** — T3 Design ⑤에 항목 추가(경고는 안 나지만 호출부 소멸에 딸린 위생 정리) |
| 3R | m3 — 코드비하인드 핸들러(`Item_Tapped`·`MarkRead_Click`)의 처리가 미정 | MINOR | **수용** — T2 Edge Case에 확정: 핸들러 방식 유지 + `MarkRead_Click` → `ReadToggle_Click` 개명, VM 커맨드도 `ToggleRead`로 맞춤 |
| 3R | m4 — 안읽음 pill 문구를 어느 심볼이 노출하는지 미명시(구현자가 정적으로 만들 여지) | MINOR | **수용** — T2 Design ②에 `UnreadCaption`(VM) 명시 + 팝업이 재사용한다는 사유 병기 |

**종결**: 3R에서 BLOCKER 0 · MAJOR 0 — 통과. 위 MINOR 4건은 승인 요청 전에 전건 반영했다.

## Progress Log

- **T1-T2 완료** (커밋 `a855012`, T2 pre-review `ffc57cf` → review-fix `7c92417`): `Notification.TodoCategory` 추가 + 알림 페이지 시안 재구현. 빌드 OK, 신규 경고 0.
  - **결정 D1의 실행 형태 확정**: 색·문구를 `NotificationItemViewModel` 프로퍼티로 노출(팝업이 Window라 `x:Bind` 함수 바인딩 불가 — P9). 페이지는 `{x:Bind}`, 팝업은 `{Binding}`으로 같은 값을 쓴다.
  - **hover 복귀는 `ClearValue`가 아니라 값 재대입**: `{x:Bind}` 기본 OneTime은 로컬 값 대입이라 지우면 바인딩이 아니라 기본값(테두리 없음)이 된다(quality 1R M1). `Tag`의 항목 VM에서 `RowBorder`를 직접 재대입한다 — **T3의 팝업 hover도 같은 함정에 걸리므로 같은 방식을 쓴다.**
  - **시안 값 vs 팔레트**: 뒤로 버튼(#1D1D21/#2B2B31 = `AppInputBrush`/`AppHoverFillBrush`), 행 배경 #1F1F24·테두리 #27272D·부제 #98959D는 팔레트에 같은 값이 없어 VM 정적 브러시로 뒀다. 기본 Button 스타일에 맡기면 시안과 다른 색이 된다(spec 1R M1).
  - **T1 계획 정정**: `todo.Category ?? string.Empty`의 `??`는 불필요했다 — `TodoItem.Category`가 non-nullable이고 DB 읽기가 구버전을 이미 `string.Empty`로 정규화한다(`SqliteProjectRepository.cs:114`·`:882`).
  - **구 정적 헬퍼 6종 + private 필드 4종은 T3에서 제거한다** — `MainWindow.xaml.cs:569`가 아직 `DeadlineLabel`을 부르므로 지금 지우면 빌드가 깨진다. 코드에도 사유 주석을 남겼다.
- **T3-T4 완료** (커밋 `9847a07`, T4 `7a18a65`+): 팝업 시안 재구현 + 벨 글리프 `E7ED`→`EA8F` + 배지 "9+" + 배지 실시간 갱신 배선. 빌드 OK, 신규 경고 0.
  - **시안 색을 팔레트 토큰으로 근사하지 않는다**: 팝업 행 구분선(#232328)·배지 글자(#1D1113)는 팔레트에 대응 값이 없어 XAML 리터럴로 뒀다(spec 1R M1·m1). 대응 토큰이 있는 나머지 8곳은 전수 대조해 값이 정확히 일치함을 확인했다.
  - **파생 프로퍼티에는 `NotifyPropertyChangedFor`를 반드시 건다**: `UnreadNotificationBadge`에 빠뜨려 배지 숫자가 첫 값에 고정됐다(spec BLOCKER·quality MAJOR 동시 지적).
  - **T4가 만든 성능 결함을 같은 루프에서 고쳤다**: `RebuildNotifications`가 작업 편집마다 불리게 되면서 `GetAllProjectItemsWithTodos`의 **프로젝트별 DB 재조회**가 그대로 편집 지연이 됐다(quality 1R M1). 카드가 이미 읽어 둔 메모리 Todos를 쓰도록 바꿔 재조회를 없앴다 — 부수적으로 "저장 큐가 비기 전에 DB를 읽어 낡은 값을 볼 수 있던" 잠재 race도 사라졌다(spec 재리뷰 확인).
  - **`Notification_Deadline_*` 3키·구 정적 멤버 6종·private 필드 4종 제거 완료** — 호출부가 사라지는 T3에서 함께 처리해 중간 상태에서 빌드가 깨지지 않았다.

## Next Steps

- 권장 다음 액션: **F-8 사용자 육안 확인 9항목**(위 `## F-8 인계 목록`). 통과하면 `master` 병합·push 승인 요청.
- Suggested skills: `pjc:llm-wiki`(알림 기능이 위키에 아예 없다 — feature 페이지 신규 작성이 필요한 별도 세션), 공식 `/code-review`(병합 전 diff 검토를 원할 때)

## Phase Ledger

- 전 task(T1~T4) 완료.
- **Phase F 통과 (HEAD `2bc474a` 기준 + F-7 반영분)** — F-1 클린 리빌드(`-t:Rebuild`) 오류 0·신규 경고 0(baseline `CS0618` 1건), F-2 해당 없음(테스트 프로젝트 부재), F-3 회귀 grep 전건 기대값(`Ellipse` 0 · `Notification_Badge_` 6 · `local:NotificationPage.` 5종 · `E7ED` 0/`EA8F` 1 · `DeadlineLabel` 0 · `Notification_Deadline_` 0 · `TodosChanged` 3 · resw ko/en 457 대칭), F-6.5 notes·대장 갱신, F-7 `plan-completion-reviewer` **BLOCKER 0 · MAJOR 2 · MINOR 3 → 전건 반영**(M1 대장 `## 종결` 3건 등재 / M2 F-8 목록에 스크롤 고정·팝업 토글 2항목 추가 / m1 `NotificationReadToggle_Click` 주석 정정 / m2 `NotificationMarkAllText`를 사전 승인 항목에 기록 / m3 판정 유보 — 되돌림 불요).
- **Phase G 통과 (Must 100%)** — F-7이 PRD 교차 검증을 수행해 **FR-N1~N4 전건 충족** 확인(N1 감지 로직 불변 + `TodoCategory` 채움 / N2 그룹·읽음·모두 읽음·클릭 이동 유지 / N3 드롭다운 패널 + "모든 알림 보기" 존치 / N4 `BuildKey` 불변 + 읽음 해제 경로만 추가). `## PRD Coverage`의 "이번 범위 외 (기구현)" 행은 대조 제외. FR-N3 문구의 느슨함은 PRD 수정 승인 대기로 대장 등재.
- **F-8 미완** — 렌더 육안 확인 9항목이 사용자 확인 대기. 완료 선언은 그 뒤.
- **F-6.5 대장 반영: 등재 2 / 종결 3 / 합산 1** — 신규 2건(PRD FR-N3 문구 · 팝업 전체 표시 성능), 종결 3건(알림 배지 상한·요약 개수 / 알림 배지 실시간 갱신 / NotificationPage 시안 대조), 기존 1항목(README·스크린샷)에 이번 분 합산.
