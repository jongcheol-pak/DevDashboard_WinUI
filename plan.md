# plan.md — 메인 카드 목록(대시보드) 화면 시안 정합

**기준 디자인**: `docs/design/DevDashboard Redesign.dc.html` — 대시보드 분기 `:332~404`(그룹 탭·카드 그리드·카드 마크업), 카드 메뉴 `:450~487`, 카드 데이터 `:1055~1064`, 파생 스타일 `:1279~1296`, 색 팔레트 `:1450`, 프로젝트 추가 다이얼로그의 "카드 헤더 색상" `:674`
**사용자 제공 렌더 이미지**: 시안 대시보드 화면 1장(2026-07-22) — 4열 카드 그리드, 색상 헤더 밴드, 좌상단 "새 프로젝트 추가" 카드, 하단 상태바
**PRD**: `docs/prd.md` — **정정 승인을 전제로 연결**한다(아래 `## PRD 충돌` 절). 현행 PRD에는 대시보드 카드 FR이 없고 §7이 이 작업을 영구 제외로 규정하므로, 승인 시 FR-D1~D4를 신설한 뒤 이 줄이 유효해진다(Phase G 활성 — plan-reviewer M7).

## 요구 이해

> 원문(사용자, 2026-07-22): "`docs/design/DevDashboard Redesign.dc.html` 디자인 화면을 확인해서 메인 카드목록 화면 수정" (+ 시안 대시보드 렌더 이미지 1장)

이해한 요구:
- 대시보드(프로젝트 카드 목록) 화면을 **시안 그대로** 다시 만든다. 현행 카드는 흰 배경·아이콘 이미지·버튼 8개 구조라 시안(색상 헤더 밴드 + 이니셜 아바타 + 큰 실행 버튼 + `…` 메뉴 + 스크립트 슬롯)과 **구조부터 다르다** — 값 조정이 아니라 카드 템플릿 재구성이다.
- 시안 카드에는 **프로젝트별 색상**(`iconBg`)이 있으나 현행 도메인에는 그 필드가 없다. 사용자 결정(2026-07-22)으로 **도메인 필드 + DB 마이그레이션 + 설정 다이얼로그 색 팔레트**까지 추가한다.
- 액션은 시안대로 **축약**한다 — 큰 IDE 실행 버튼 + 작업/테스트 아이콘만 남기고 폴더·터미널·Git·작업 기록·설정은 `…` 메뉴로 옮긴다. **신규 기능은 만들지 않는다**(시안 메뉴의 GitHub·메모는 현행에 기능이 없어 제외).
- 주변 요소 중 **스크립트 슬롯 시안화·그리드 폭 가변화·그룹 탭 개수 배지**를 함께 한다. 태그는 현행 마키 컨트롤을 **유지**한다(사용자 미선택).

## PRD 충돌 — 선행 승인 필요

`docs/prd.md:112`(§7 Out of Scope, 영구 제외)이 **"대시보드 카드 자체의 대규모 기능 변경(카드 액션 재배치 등 시안의 대시보드 화면은 참고만, 이번 5개 영역 외)"** 을 명시한다. 이번 요청은 정확히 그 항목(카드 액션 재배치 + 카드 재구성)이다.

PRD `:5`가 "요구 변경은 PRD → plan → 코드 순서로만"을 규약으로 두므로, **코드를 고치기 전에 PRD §7의 해당 줄을 정정해야** PRD와 구현이 모순되지 않는다. 제안:

- **정정안**: §7의 해당 줄을 삭제하고 §4에 `### 대시보드 카드 (Phase 6)` 절을 신설해 이번 범위를 FR로 등재(FR-D1 카드 시안 재구성 / FR-D2 프로젝트 카드 색상 / FR-D3 액션 축약·`…` 메뉴 / FR-D4 그리드·그룹 탭 배지). §8에 결정 1줄(`D-9 대시보드 카드 리디자인 편입 — 2026-07-22 사용자 요청`) 추가.
- **미승인 시**: PRD를 그대로 두고 코드만 바꾸면 두 문서가 모순되므로, 그 경우 최소한 §7 줄에 "2026-07-22 사용자 요청으로 해제됨" 주석만 다는 대안도 가능하다.

이 정정은 **plan 승인과 함께 확인받는다**(코드 T1 착수 전에 PRD 먼저 수정). 정정 후 아래 `## PRD Coverage` 표가 Phase G 재검증의 기준이 된다.

## PRD Coverage

> 정정 승인 후 신설될 FR 기준(plan-reviewer M7). 정정이 승인되지 않으면 이 표와 상단 `**PRD**:` 줄을 함께 제거하고 PRD 무연결로 진행한다.

| PRD ID (신설 예정) | 우선순위 | 대응 task | 상태 |
|---|---|---|---|
| FR-D1 카드 시안 재구성(헤더 밴드·아바타·설명·본문 레이아웃) | Must | T4 | ✅ 커버 |
| FR-D2 프로젝트 카드 색상(영속 + 자동 배정 + 설정 UI) | Must | T1, T2, T3 | ✅ 커버 |
| FR-D3 액션 축약 + `…` 메뉴 + 스크립트 슬롯 시안화 | Must | T5, T6 | ✅ 커버 |
| FR-D4 그리드 폭 가변화 + 그룹 탭 개수 배지 | Should | T7, T8 | ✅ 커버 |
| FR-C1~C4 / FR-S* / FR-T* / FR-E* / FR-H* / FR-N* (기존 5개 영역) | Must/Should | — | 이번 범위 외 (기구현 또는 별도 Phase·Deferred — 예: `FR-S5`는 대장의 설정 다이얼로그 restyle 대기 항목. 이번 diff 무관) |
| NFR-1 (빌드 오류 0·신규 경고 0) | — | 전 task | 검증 대상(실측 baseline `CS0618` 1건 제외) |
| NFR-2 (계층 위반 0) | — | T1, T3, T8 | 검증 대상 |
| NFR-3 (DB 스키마 하위호환) | — | T1 | 검증 대상(`HasColumn` 가드 + `DEFAULT ''`) |
| NFR-4 (다국어 ko/en 대칭) | — | T2, T5, T8 | 검증 대상(신규 resw 전량 ko/en) |
| NFR-5 (테스트) | — | — | 조건 미발동(테스트 프로젝트 부재 — AGENTS.md) |

## Investigation Log (근거)

### 문서·대장
- **위키 참조**: vault 미설정 — 코드 1차 출처로 진행.
- **AGENTS.md 신선도 점검**: 이번에 참조하는 경로(`Domain/Entities/ProjectItem.cs`·`Infrastructure/Persistence/{DatabaseContext,SqliteProjectRepository}.cs`·`Presentation/Views/DashboardView.xaml(.cs)`·`Presentation/ViewModels/{ProjectCardViewModel,MainViewModel,ProjectSettingsDialogViewModel}.cs`·`Presentation/Views/Dialogs/ProjectSettingsDialog.xaml`·`Resources/{Styles,Converters,Palette}.xaml`·`Strings/{ko-KR,en-US}/Resources.resw`) 전부 실재. 빌드 명령·csproj 실재. **어긋남 1건**: "상시 기존 경고 5건(NU1903 1 + CS0612 4)" 기록이 실측(`CS0618` 1건)과 다름 — 대장에 이미 등재(`[AGENTS.md의 "상시 기존 경고 5건" 기록이 stale]`). 이번 "신규 경고 0" 판정은 **실측 baseline(CS0618 1건)** 기준.
  - 관련 함정 직결: **3**(resw 키 형식 — `x:Uid` 소비는 `.Text`/`.Content` 접미 필수)·**4**(`Palette.xaml`은 Default 딕셔너리 단일)·**5**(x:Bind **함수** 바인딩은 Converter 불가 — 일반 `{Binding}`+Converter는 가능하며 이 카드 템플릿이 전부 `{Binding}`이다)·**6**(`Border`는 점선 미지원 → `Rectangle StrokeDashArray`, `DashedAddButtonStyle` 선례)·**11**(공용 DataTemplate 소비처 전수 확인)·**⚠️ INSERT 컬럼 추가 시 파라미터 갱신**(과거 `@method` 누락으로 silent data loss).
- **Deferred 대장 확인**(`docs/plans/deferred.md`): 이번 작업과 직접 관련된 대기 항목은 **`[README/스크린샷 — 리디자인 5개 영역]`**(대시보드 카드가 바뀌면 스크린샷도 낡는다 — 이번에도 Deferred 유지)뿐이다. `[FR-S5 프로젝트 설정 다이얼로그 심화 restyle]`은 **다이얼로그 전체 재설계**를 미룬 항목이고 이번 T2는 **필드 1개(색 선택) 추가**라 별개 — 대장에 그대로 유지한다. 나머지(작업·테스트·알림·resw 고아 등)는 무관.
- **PRD 경량 확인**: §4의 FR은 공통 기반(C)·설정(S)·작업(T)·테스트(E)·기록(H)·알림(N) 6영역이며 **대시보드 카드 FR은 없다**. 여기에 더해 §7 Out of Scope가 이 작업을 영구 제외로 규정하고 있어(위 절) **PRD 정정이 선행 조건**이다. **정정이 승인되면 FR-D1~D4가 신설되므로 위 `## PRD Coverage` 표를 기준으로 Phase G(요구 재검증)를 수행한다**(미승인 시 표와 상단 `**PRD**:` 줄을 함께 제거하고 PRD 무연결로 진행). `FR-C4`(TagColorConverter 색 조정)는 인접하나 이번에 컨버터 색 로직을 바꾸지 않으므로 무관.

### 현행 구현 (직접 Read)

**`Domain/Entities/ProjectItem.cs`**(1~79줄 확인)
- 일반 프로퍼티 방식(`[ObservableProperty]` 아님). `Name`·`Description`·`IconPath`·`Path`·`DevToolName`·`Tags`·`GitStatus`·`IsPinned`·`PinOrder`·`RunAsAdmin`·`GroupId`·`Category`·`CommandScripts`(4칸 고정, null 허용)·`Todos`·`TestCategories`·`Histories`·`CreatedAt`.
- **카드 색상 필드 없음** — 시안 `iconBg`에 대응하는 값이 도메인에 존재하지 않는다.

**`Infrastructure/Persistence/DatabaseContext.cs`**
- `MigrateSchema`(:114-134)가 `AddColumnIfNotExists(connection, "<Table>", "<Col>", "<DDL>")` 나열 방식. `Todos`·`TestItems`·`Histories`에 컬럼을 붙인 선례가 8건. **`Projects` 테이블에 컬럼을 추가한 선례는 아직 없다**(테이블명은 `SqliteProjectRepository`의 SQL에서 확인 — INSERT 문이 `(Id, Name, Description, IconPath, Path, DevToolName, Options, Command, GitStatus, IsPinned, PinOrder, RunAsAdmin, GroupId, Category, CreatedAt, ...)`).

**`Infrastructure/Persistence/SqliteProjectRepository.cs`**
- **프로젝트 읽기 경로는 `ReadProjects`(:314) 1곳뿐이다** — 정적 `ReadAllFromDb`(:721-731, 가져오기가 소비)가 자체 리더 없이 **`ReadProjects(conn)`를 그대로 호출**한다(:731 직접 확인). 따라서 이 한 곳만 고치면 가져오기까지 자동 커버된다. (plan 초안이 "읽기 2경로"라 적었던 것은 `:837`·`:860`을 오독한 것으로 **정정** — 그 줄은 `ReadTodosForProject`의 `hasCategory` 가드(Todos 테이블)다. plan-reviewer B2)
- 쓰기 경로 2개: INSERT(:418-439)·UPDATE(:451-471). 함정대로 **컬럼과 파라미터를 짝으로** 추가해야 한다.
- `HasColumn` 가드 선례가 `Category`(:91)에 있다 — 구버전 DB 호환의 정본 패턴.

**⚠️ `DatabaseContext.AllowedIdentifiers` 화이트리스트 (T1의 성립 조건 — plan-reviewer B1)**
- `AddColumnIfNotExists`(:181-186)는 테이블·컬럼 이름을 `AllowedIdentifiers`(:167-179) 집합과 대조해 **없으면 `ArgumentException`을 던진다**(SQL Injection 방지). 현재 목록에 **`HeaderColor`가 없다**.
- 화이트리스트에 넣지 않고 `AddColumnIfNotExists`만 추가하면 **빌드는 통과하고 앱 기동 시 DB 초기화가 예외로 죽는다**(`InitializeDatabase` → `MigrateSchema`). AGENTS.md `:118-119`가 명시한 패턴이며, 정적 검증으로는 잡히지 않는다.

**`Presentation/ViewModels/ProjectCardViewModel.cs`**(805줄, 공개 멤버 grep 확인)
- 도메인 위임 프로퍼티(`Name`/`Description`/`IconPath`/`DevToolName`/`Category`/`GroupId`…)와 표시 상태(`IconSource`·`IsDevToolValid`·`IsProjectPathValid`·`IsGitRepo`·`EnableTagAnimation`)를 함께 노출.
- **커맨드 전부 실재**: `OpenInDevToolCommand`/`OpenInDevToolAsAdminCommand`·`OpenFolderCommand`·`OpenTerminalCommand`/`OpenTerminalWithPowerShellCommand`·`EditCommand`·`DeleteCommand`·`TogglePinCommand`·`ShowGitStatusCommand`·`OpenTodoCommand`·`OpenHistoryCommand`·`OpenTestListCommand`·`ExecuteCommandSlotCommand`·`ConfigureCommandSlotCommand`·`ChangeCommandIconCommand`·`ClearCommandSlotCommand` — **`…` 메뉴 구성에 신규 커맨드가 필요 없다**.
- 커맨드 슬롯: `IsCmdNConfigured`·`CmdNTooltip`·`CmdNIcon`·`HasCmdNIcon`·`IsCmdNRunAsAdmin`(N=0~3) + 파생 `IsCmdNSlotVisible`(:140-155 — "앞 슬롯이 차면 다음 슬롯 노출" 규칙, `OnIsCmdNConfiguredChanged`가 알림).

**`Presentation/Views/DashboardView.xaml`**(593줄)
- `ProjectCardTemplate`(:79-518) — `Border Width="330" MinHeight="204"`, 4행 Grid(헤더/설명 38px/태그/하단). 하단은 액션 8버튼 + 슬롯 4개.
- `AddCardTemplate`(:521-542)·`DropPlaceholderTemplate`(:545-554)도 **`Width="330"` 하드코딩** — 그리드 폭을 가변화하면 3개 모두 손봐야 한다.
- `CardTemplateSelector`(:558) → `ItemsRepeater`(:583) + `muxc:UniformGridLayout ItemsJustification="Start"`(:588). 소비처는 이 파일 1곳(grep 전수 — 함정 11 리스크 없음).
- 카드 `Border`에 드래그 배선(`CanDrag`/`AllowDrop`/`DragStarting`/`DropCompleted`/`DragOver`/`Drop`)이 걸려 있다 — **재구성 시 이 6개 속성을 반드시 승계**해야 핀 카드 재정렬이 죽지 않는다.
- 태그는 `ctrl:MarqueeTagsControl`(:177) — 이번에 유지.

**`Presentation/Views/DashboardView.xaml.cs`**(431줄)
- `InitializeLocalizedResources()`(:62-77): DataTemplate 안에서는 `x:Name`·`x:Uid` 접근이 제한되는 툴팁을 `this.Resources["ToolTip_*"]`에 **파싱 전 주입**한다(11개). 신규 툴팁이 필요하면 여기에 추가하는 것이 이 파일의 규약.
- 단 `MenuFlyoutItem`은 DataTemplate 안에서도 `x:Uid`를 쓰는 선례가 있다(`RunAsAdminMenuItem`·`CmdSlot*Item`) → **메뉴 항목 문구는 resw `x:Uid` 방식**(함정 3의 `.Text` 접미 필요).
- `AddCard_Click`(:178) → `Vm.RequestAddProjectCommand`.

**`Presentation/ViewModels/MainViewModel.cs`**
- 그룹: `Groups`(:37, `ObservableCollection<ProjectGroup>`)·`SelectedGroupId`(:40)·`CanAddGroup`(:508)·필터(:216-217 `GroupId == SelectedGroupId`). **그룹별 개수는 계산하는 곳이 없다** — T8이 추가 대상.
- `DisplayCards`(선두 `AddCardPlaceholder` + 카드) — 직전 작업(미커밋)에서 `Prepend`로 바뀜.

**`MainWindow.xaml`**
- 그룹 탭 영역(:258-368): "전체" `RadioButton`(x:Uid) + `Groups` `ItemsRepeater`(:302-327, `DataTemplate x:DataType="models:ProjectGroup"`) + 추가 버튼. 스타일 `GroupTabRadioButtonStyle`(`Styles.xaml:82`, 48px·밑줄 2px·`CheckStates` 분리 — 함정 7 준수).
- 하단 상태바(:376-404)는 이미 시안과 같은 구성(런처 수·프로젝트 수) → **이번 무변경**.

**`Presentation/Views/Dialogs/ProjectSettingsDialog.xaml` + VM**
- VM은 `[ObservableProperty] partial` 방식. 이름·설명·아이콘·경로·옵션·명령·도구·태그·카테고리·그룹·관리자 권한 + 검증(:272 카테고리 필수). `ToProjectItem` 대응부(:237 `Category = Category`)에서 도메인으로 옮긴다(:304 역방향).
- XAML은 `StackPanel Spacing="16"` 안에 `<StackPanel Spacing="4"><TextBlock x:Uid="...Label" Style="BodyStrongTextBlockStyle" /> <입력></StackPanel>` 반복 구조 — **색 팔레트도 이 관용구로 끼워 넣는다**.

**내보내기/가져오기**: `.db` 파일 복사 방식(`MainWindow.xaml.cs:662` — `SqliteProjectRepository.ReadAllFromDb`). **JSON 직렬화(`AppJsonContext`)는 `AppSettings` 전용이라 `ProjectItem`을 포함하지 않는다** → 색 필드는 DB 경로만 처리하면 내보내기/가져오기까지 자동 커버(단 위 `ReadAllFromDb` 갱신이 조건).

### 4-D. 재사용 확인

| 신규 심볼 | 유사 기존 구현 검색 결과 | 재사용/신규 사유 |
|---|---|---|
| `ProjectItem.HeaderColor` | `Category`(`ProjectItem.cs:58`) — 문자열 1개 + DB 컬럼 + 설정 다이얼로그 필드의 **동일 3점 세트** | **신규(선례 대칭 재현)** — 색을 담을 필드가 없어 재사용 불가. `Category`가 통과한 경로(마이그레이션·`HasColumn` 가드·INSERT/UPDATE·다이얼로그 왕복)를 그대로 따른다 |
| `ProjectCardViewModel.EffectiveHeaderColor` / `Initial` / `HasIcon` | `IconSource`(:58) — VM이 도메인 값에서 표시 값을 파생하는 선례 | **신규** — "색 미지정이면 이름 해시" 규칙(사용자 결정)과 이니셜 계산은 도메인에 없다. 파생 지점을 VM 한 곳으로 모은다 |
| 색 해시 함수 | **`TagColorConverter.GetFnv1aHash`+`HslToRgb`**(`DevToolConverters.cs:33-60`) — 이름 → 결정론적 색의 완성된 구현 | **재사용** — 새 해시를 만들지 않고 이 로직을 public static으로 노출해 호출한다(D3) |
| hex → 헤더 그라데이션 브러시 | `TagColorConverter`(string → Brush) 선례. `LinearGradientBrush` 사용처는 레포에 **0건** | **신규 컨버터 1개** (`HexToHeaderGradientConverter`) — 시안 헤더는 `linear-gradient(135deg, rgba(c,.55), rgba(c,.14))`라 단색 컨버터로 대체 불가 |
| hex → 단색 브러시(아바타) | `TagColorConverter`(도구명 해시 전용 — hex 문자열을 못 받음) | **신규 컨버터 1개** (`HexToBrushConverter`) — 입력 계약이 달라 재사용 불가. 두 컨버터는 같은 파일(`DevToolConverters.cs`)에 둔다 |
| `…` 더보기 메뉴 | 카드에 `MenuFlyout` 선례 다수(`Button.ContextFlyout` 4곳) | **마크업만 신규**(신규 C# 심볼 0) — 항목은 전부 기존 커맨드 바인딩 |
| 점선 `＋` 슬롯 추가 버튼 | `DashedAddButtonStyle`(`Styles.xaml` — 함정 6 대응 `Rectangle StrokeDashArray`) | **재사용 검토 우선**(T6에서 크기·모서리만 맞춰 소비, 부적합하면 같은 기법으로 인라인) |
| 그룹 탭 개수 배지 | `NotificationBadge`(`MainWindow.xaml:136` — `Border` + 숫자 `TextBlock`) | **마크업 신규**(신규 스타일 0) — 인라인 `Border`로 그린다. VM에 개수 계산만 추가 |

## 시각 요소 분해

> 기준: 시안 원문 `:346~394`(카드·그리드), `:1294`(탭 배지), `:1450`(색 팔레트), `:1285`(IDE 버튼)의 인라인 스타일 + 사용자 제공 렌더 이미지(2026-07-22). CSS px는 WinUI 논리 단위로 그대로 옮긴다. 최종 판정은 빌드 후 사용자 육안 대조(⏳ HUMAN-VERIFY).

| 요소 | 속성 | 디자인 값 | XAML 대응 수단 | 확인 방법 |
|---|---|---|---|---|
| 카드 그리드 | 열 규칙 | `repeat(auto-fill, minmax(310px,360px))`, `justify-content:start` | `UniformGridLayout MinItemWidth="310" ItemsStretch="Fill" ItemsJustification="Start"` + 카드 `MaxWidth="360"` (⚠️ `ItemsStretch` 실동작 확인 대상 — 미동작 시 `MinItemWidth`만 두고 고정폭 폴백) | HTML `:346` |
| 카드 그리드 | 간격 | `gap:14px` | `MinColumnSpacing="14" MinRowSpacing="14"` + 카드 `Margin="0"` | HTML `:346` |
| 카드 그리드 | 바깥 여백 | `padding:18px 24px 24px` | `ItemsRepeater Margin="24,18,24,24"` | HTML `:344` |
| 추가 카드 | 크기·배경 | `min-height:150px`, `#1a1a1f`, `1px #2a2a31`, `radius 14` | `MinHeight="150"` + 카드와 같은 배경/테두리/`CornerRadius="14"` | HTML `:347` |
| 추가 카드 | 내용 | `＋` 26px weight300 + "새 프로젝트 추가" 13px, 세로 중앙, `gap:12` | `StackPanel Spacing="12"` 중앙 정렬, 글리프 26 / 텍스트 13 | HTML `:348-349` |
| 추가 카드 | hover | `border-color:#8b7cf7; color:#a89dfa` | 앱 액센트(`AppAccentBrush`)로 대체 — 시안 보라는 이 앱 팔레트에 없다(함정 4, D7) | HTML `:347` |
| 카드 | 배경·테두리 | `#1a1a1f`, `1px #2a2a31`, `radius:14`, `overflow:hidden` | `AppCardBrush` 근사 + `CornerRadius="14"`, 헤더 Border는 `CornerRadius="13,13,0,0"`(WinUI Border는 자식을 클립하지 않음) | HTML `:352` |
| 카드 | hover | `border-color:#3d3d45`, `translateY(-2px)` | 테두리색만 변경(이동 애니메이션 미구현 — D8) | HTML `:352` |
| 카드 헤더 | 높이·여백 | `height:62px`, `padding:0 16px`, `gap:11px` | `Height="62"`, `Padding="16,0"`, `Spacing`/`Margin` 11 | HTML `:353` |
| 카드 헤더 | 배경 | `linear-gradient(135deg, rgba(색,.55), rgba(색,.14))` | `LinearGradientBrush StartPoint="0,0" EndPoint="1,1"` 2 stop(알파 0x8C / 0x24) — 신규 컨버터 | HTML `:353`, `:1281` |
| 아바타 | 크기·모양 | `38×38`, `radius:10`, 배경 = 카드 색 | `Border Width/Height=38 CornerRadius="10"` + hex→단색 브러시 컨버터 | HTML `:354` |
| 아바타 | 내용 | 이니셜 1자, `700`, `16px`, `#fff` | 아이콘 있으면 `Image`, 없으면 `TextBlock`(FontSize 16·Bold·White) — 사용자 결정 | HTML `:354` + 사용자 결정 |
| 카드 이름 | 글자 | `14.5px`, `700`, `letter-spacing:-.01em`, 남는 폭 말줄임 | `FontSize="14.5" FontWeight="Bold"`, `Grid` `*` 열 + `TextTrimming` | HTML `:355` |
| 핀·삭제 버튼 | 크기·색 | `28×28`, `radius:7`, 투명, `rgba(255,255,255,.72)`, `gap:4` | 28px 버튼 스타일 + `#B8FFFFFF` 전경, 아이콘 13px | HTML `:357-362` |
| 삭제 버튼 | hover | `background:rgba(0,0,0,.3)`, `color:#ff9d97` | VSM PointerOver에 `#4D000000` / `#FF9D97` | HTML `:360` |
| 본문 | 여백·간격 | `padding:14px 16px 16px`, 세로 `gap:12` | `Padding="16,14,16,16"`, `Spacing="12"` | HTML `:365` |
| 설명 | 글자·줄수 | `12px`, `#98959d`, **1줄** 말줄임(`nowrap`) | `FontSize="12"`, `TextWrapping="NoWrap"`, `MaxLines` 제거(현행 2줄에서 변경) | HTML `:366` |
| 태그 | — | 시안은 11px pill wrap | **현행 `MarqueeTagsControl` 유지**(사용자 미선택 — Deferred) | 사용자 결정 |
| IDE 실행 버튼 | 크기·색 | `flex:1`, `height:34`, `radius:9`, 배경 = 도구색, 흰 글자 `12.5px 700`, `gap:7` | `Grid` `*` 열 + `Height="34" CornerRadius="9"`, 배경은 기존 `TagColor` 컨버터(도구명 해시) 유지 | HTML `:373`, `:1285` |
| IDE 실행 버튼 | 아이콘 | ▶ `12×12` fill | `FontIcon Glyph="&#xE768;" FontSize="12"` | HTML `:374` |
| 액션 아이콘 버튼 | 크기·색 | `34×34`, `radius:9`, 투명, `#8a8890`, 아이콘 15px, hover `#26262c`/`#e8e6e3` | 기존 `CardIconButtonStyle`(템플릿·hover) 재사용 + **`Width`/`Height`/`CornerRadius`는 요소에 로컬 지정**(스타일 Setter를 바꾸면 28px여야 할 슬롯 버튼까지 34px가 된다 — 두 용도가 같은 스타일을 공유, plan-reviewer M4) | HTML `:377-386` |
| 액션 구분선 | 크기 | `1×18px`, `#2b2b31` | `Rectangle Width="1" Height="18"` | HTML `:383` |
| 하단 슬롯 행 | 구분선·여백 | `padding-top:10`, `border-top:1px #26262c`, `gap:6` | `Border BorderThickness="0,1,0,0" Padding="0,10,0,0"` + `Spacing="6"` | HTML `:388` |
| 스크립트 슬롯 | 크기·색 | `28×28`, `radius:8`, `1px #2b2b31`, `#202025`, `#a8a6ad`, 아이콘 10px | 28px 버튼 + `AppInputBrush` 근사 | HTML `:389-392` |
| 슬롯 추가(＋) | 테두리 | `1px dashed #35353c`, 투명 배경, `13px ＋` | `Rectangle StrokeDashArray`(함정 6, `DashedAddButtonStyle` 선례) | HTML `:393` |
| 그룹 탭 배지 | 크기·색 | `11px`, `padding:1px 7px`, `radius:8`; 선택 시 `rgba(액센트,.16)`/액센트색, 비선택 `#26262c`/`#8a8890` | `Border CornerRadius="8" Padding="7,1"` + 앱 액센트(시안 보라 대신 — D7) | HTML `:1294` |
| `…` 메뉴 | 항목 | 실행 / 폴더 열기 / 터미널 / — / GitHub / 메모 / 작업 기록 / 프로젝트 설정 / — / 삭제 | **현행 기능만**: 실행 / 폴더 열기 / **터미널(▸ 기본 · PowerShell)** / — / Git 상태 / 작업 기록 / 프로젝트 설정 / — / 삭제 = 항목 7 + 구분선 2 (GitHub·메모 제외 — 사용자 결정. 터미널 하위 2분기는 B3) | HTML `:450-487` + 사용자 결정 |

## Decisions

- **D1 (카드 색은 도메인 영속 필드)**: `ProjectItem.HeaderColor`(`string`, `#RRGGBB` 또는 빈 문자열)를 추가하고 DB 컬럼 `HeaderColor TEXT NOT NULL DEFAULT ''`로 영속한다. *Source*: 사용자 결정(2026-07-22, "색상 필드 추가"). `Category`(`ProjectItem.cs:58` + `AddColumnIfNotExists` + `HasColumn` 가드)가 같은 구조로 이미 통과한 경로다.
- **D2 (색 미지정 = 이름 해시 자동)**: 빈 문자열이면 **프로젝트 이름 해시**로 색을 정한다. *Source*: 사용자 결정(2026-07-22, "이름 해시로 자동 배정"). 기존 카드가 전부 같은 색이 되는 것을 피하고, 설정에서 고르면 그 값이 우선한다.
- **D3 (해시는 기존 구현 재사용)**: `TagColorConverter`의 FNV-1a + HSL(`DevToolConverters.cs:33-60`)을 `public static`으로 노출해 VM이 호출한다. *Source*: 같은 목적(문자열 → 결정론적 색)의 완성 구현이 이미 있고, 새 해시를 만들면 도구 배지와 카드 색의 색감 규칙이 갈린다. **채도·명도는 카드용으로 별도 조정하지 않는다**(YAGNI — 다르면 육안 확인 후 값만 조정).
- **D4 (표시 파생은 VM에서, 브러시 변환은 컨버터에서)**: VM은 `EffectiveHeaderColor`(hex 문자열)·`Initial`·`HasIcon`까지만 계산하고, hex→`Brush` 변환은 컨버터 2종이 맡는다. *Source*: 함정 5는 **함수 바인딩** 한정이고 이 템플릿은 전부 `{Binding}`이라 Converter가 동작한다(`TagColor` 선례). VM이 `Brush`를 직접 들면 Presentation 타입이 VM에 늘어난다.
- **D5 (액션 축약 + `…` 메뉴)**: 카드에는 IDE 실행 버튼 + 작업 + 테스트만 남기고, 폴더 열기·터미널·Git 상태·작업 기록·프로젝트 설정·삭제는 `…` 메뉴로 옮긴다. *Source*: 사용자 결정(2026-07-22). **신규 커맨드 0** — 전부 기존 커맨드 재바인딩이다.
- **D6 (슬롯은 "설정된 것만 + 끝에 점선 ＋")**: 설정된 슬롯만 표시하고 마지막에 점선 `＋`(다음 빈 슬롯 설정) 버튼을 둔다. 최대 4개는 유지하고 4개가 다 차면 `＋`를 숨긴다. *Source*: 사용자 결정(2026-07-22). 현행 `IsCmdNSlotVisible`(앞 슬롯이 차면 다음 노출)은 **빈 슬롯을 하나 보여주는 규칙**이라 새 규칙(`IsCmdNConfigured`로 표시 + 별도 `＋`)으로 대체한다.
- **D7 (시안 보라 액센트 대신 앱 액센트)**: 시안 대시보드의 hover·선택 강조는 `#8b7cf7`(보라)이나 이 앱의 확정 액센트는 `#F0716A`(`Palette.xaml`, PRD FR-C1)다. **앱 액센트를 쓴다.** *Source*: 팔레트는 다크 단일·전역 정책(함정 4)이고, 카드만 보라를 쓰면 앱 안에서 강조색이 둘로 갈린다. 시안 대비 의도적 차이로 기록.
- **D8 (카드 hover 이동 애니메이션 미구현)**: 시안 `transform:translateY(-2px)`는 넣지 않고 테두리색 변화만 구현한다. *Source*: 대장의 `[행 hover 트랜지션 미구현]`과 같은 이유(WinUI는 `Storyboard` 배선이 필요) — 작업·테스트 화면과 일관된다.
- **D9 (설명 1줄)**: 현행 2줄(`MaxLines="2"`)에서 시안대로 **1줄 말줄임**으로 바꾼다. *Source*: HTML `:366`의 `white-space:nowrap`. 카드 높이가 일정해져 그리드가 가지런해진다.
- **D10 (아이콘 우선, 없으면 이니셜)**: 아바타 자리는 `IconPath`가 있으면 아이콘 이미지, 없으면 이니셜 글자. *Source*: 사용자 결정(2026-07-22) — 기존 아이콘 등록 기능을 죽이지 않는다.
- **D11 (이니셜 = 이름 첫 글자 1자)**: 공백을 건너뛴 첫 문자를 대문자로. 이름이 비면 `?`. *Source*: 시안 데이터(`:1056` `initial:'A'` — 이름 첫 글자)가 전부 1자다. 영문 두 단어의 약자(예: "VS") 규칙은 시안에 근거가 없어 만들지 않는다.
- **D12 (그룹 탭 개수는 검색 결과 반영)**: 시안(`:1290`)은 그룹 개수를 **검색어가 적용된 결과 수**로 센다. 현행 필터 파이프라인(`MainViewModel:216`)과 같은 조건을 쓴다. *Source*: 시안 계산식 그대로.
- **D13 (하단 상태바·헤더는 무변경)**: 시안 footer(`:406-410`)의 "N개 런처 앱 / N개 프로젝트 / 모든 변경사항 저장됨"은 현행 상태바와 이미 같다. *Source*: `MainWindow.xaml:376-404` 직접 확인. 이번 diff에서 제외한다.

## 작업 단계

### T1 — 도메인·DB: 프로젝트 카드 색상 필드 `Type D`
- [x] 구현
- **Files**: `Domain/Entities/ProjectItem.cs`, `Infrastructure/Persistence/DatabaseContext.cs`, `Infrastructure/Persistence/SqliteProjectRepository.cs`
- **Design**: ① 배치 — 값은 `ProjectItem`(도메인), 스키마는 `DatabaseContext`의 **화이트리스트 + `MigrateSchema`**, 입출력은 `SqliteProjectRepository`의 INSERT/UPDATE + **읽기 1경로(`ReadProjects` — 가져오기가 재사용)**. ② 신규 심볼 — `ProjectItem.HeaderColor`(카드 헤더·아바타 색, `#RRGGBB` / 빈 문자열이면 이름 해시 자동). ③ 의존 방향 — Infrastructure → Domain(기존 그대로), 신규 의존 0. ④ 비추상화 — 색을 다루는 값 객체(`CardColor` 등)를 만들지 않는다. 문자열 1개이며 검증은 다이얼로그의 팔레트 선택으로 제한된다(자유 입력 없음).
- **구성**:
  - (D1) `ProjectItem`에 `public string HeaderColor { get; set; } = string.Empty;` + 한글 주석("카드 헤더·아바타 색. 빈 문자열이면 이름 해시로 자동 배정").
  - **`DatabaseContext.AllowedIdentifiers`(:167-179)에 `"HeaderColor"`를 먼저 추가**한다 — 빠뜨리면 `AddColumnIfNotExists`가 `ArgumentException`을 던져 **앱이 기동하지 못한다**(B1).
  - `MigrateSchema`에 `AddColumnIfNotExists(connection, "Projects", "HeaderColor", "TEXT NOT NULL DEFAULT ''");` 추가(테이블명 `Projects`는 화이트리스트에 이미 있음).
  - INSERT(:418-439)·UPDATE(:451-471)에 컬럼·파라미터를 **짝으로** 추가(⚠️ AGENTS.md 함정 — 파라미터 누락 시 silent data loss).
  - 읽기는 **`ReadProjects`(:314) 한 곳**에 `HasColumn` 가드 방식으로 추가(`Category` 선례와 동일). `ReadAllFromDb`는 이 메서드를 호출하므로(:731) 가져오기까지 자동 커버된다 — **`ReadAllFromDb`에 별도 읽기 코드를 만들지 않는다**(중복 유발, B2).
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0(실측 baseline `CS0618` 1건 제외).
  2. `ProjectItem.HeaderColor`가 정의되고 용도·기본값 규칙이 주석에 명시된다.
  3. **`AllowedIdentifiers`에 `"HeaderColor"`가 있다**(B1 — 이 항목이 없으면 앱이 기동하지 못한다) + `MigrateSchema`에 `Projects`/`HeaderColor` 항목 1건. → `DatabaseContext.cs`에 `HeaderColor` **2건**.
  4. INSERT·UPDATE **양쪽 모두** 컬럼 목록과 `AddWithValue`/파라미터가 짝으로 늘어난다(한쪽만 있으면 실패).
  5. `ReadProjects`가 `HasColumn` 가드로 읽는다 — 구버전 DB(컬럼 없음)에서도 예외 없이 빈 문자열. `ReadAllFromDb`에는 **읽기 코드가 추가되지 않는다**(재사용).
  6. `AppJsonContext`·`AppSettings`는 변경되지 않는다(내보내기/가져오기는 `.db` 경로라 무관 — 확인 사항).
  7. **앱을 기동해 대시보드가 뜬다**(마이그레이션 예외 없음) — 빌드만으로는 B1이 검출되지 않으므로 이 실행 확인이 T1의 필수 acceptance다(⏳ HUMAN-VERIFY).
- **Edge Cases**: 구버전 DB에 컬럼 없음 → `HasColumn` false → 빈 문자열(자동 색) / 값이 `#RRGGBB` 형식이 아님(수동 DB 편집) → T3의 파싱 실패 시 자동 색으로 폴백 / 대소문자 혼재 hex → 파싱은 대소문자 무시 / 마이그레이션 재실행 → `AddColumnIfNotExists`가 멱등 / 가져오기 시 구버전 `.db` → 읽기 가드로 무해 / **화이트리스트 누락 → 기동 즉시 예외**(위 acceptance 3·7이 방어).
- **Halt Forecast**: (ii-a) 사전 승인 — **DB 스키마 변경(컬럼 1개 추가)** + 도메인 공개 속성 1개 추가. 하위호환 가드 포함, 기존 데이터 파괴 없음.

### T2 — 프로젝트 설정 다이얼로그: 카드 색상 팔레트 `Type C`
- [ ] 구현
- **Files**: `Presentation/ViewModels/ProjectSettingsDialogViewModel.cs`, `Presentation/Views/Dialogs/ProjectSettingsDialog.xaml`, `Strings/{ko-KR,en-US}/Resources.resw`
- **Design**: ① 배치 — 선택 상태·팔레트 목록은 VM, 마크업은 다이얼로그 XAML. ② 신규 심볼 — `ProjectSettingsDialogViewModel.HeaderColor`(선택된 색 hex) + `HeaderColorOptions`(선택 가능한 10색). ③ 의존 방향 — VM → Domain(`ToProjectItem`/역방향에 매핑 1줄씩). ④ 비추상화 — 색 선택 전용 UserControl을 만들지 않는다(소비처 1곳). 색 팔레트는 VM의 정적 배열 1개로 둔다.
- **구성**:
  - VM에 `[ObservableProperty] partial string HeaderColor`(기본 빈 문자열) + 정적 `HeaderColorOptions` = 시안 `:1450`의 10색(`#4A5BD0`, `#B0553A`, `#3A8A8A`, `#C04A68`, `#7A4AC0`, `#8A7A2E`, `#4A6EA0`, `#3A6A4A`, `#8B7CF7`, `#5DB463`).
  - `ToProjectItem`(:237 부근)에 `HeaderColor = HeaderColor`, 로드부(:304 부근)에 `HeaderColor = item.HeaderColor` 추가.
  - XAML: 기존 `<StackPanel Spacing="4"><TextBlock x:Uid="...Label" .../> …` 관용구로 "카드 헤더 색상"(시안 `:674` 문구) 라벨 + 색 견본 행(`ItemsRepeater`/`StackPanel` 30×30 `Border`, 선택 항목만 2px 밝은 테두리 — 시안 `:1452`).
  - **"자동" 선택지 1칸**을 팔레트 맨 앞에 둔다(빈 문자열 = 이름 해시). 색을 고르지 않았던 기존 프로젝트가 이 상태이며, 되돌릴 수단이 없으면 한 번 고른 뒤 자동으로 못 돌아간다.
  - resw ko/en에 라벨 키 1개(+ "자동" 항목 문구 1개) — `x:Uid` 소비이므로 **`.Text` 접미**(함정 3), ko/en 양쪽(함정 2).
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0.
  2. 다이얼로그에서 색을 고르고 저장하면 `ProjectItem.HeaderColor`에 hex가 들어간다(코드 경로: `ToProjectItem`에 매핑 존재).
  3. 기존 프로젝트를 편집으로 열면 저장된 색이 선택 상태로 보인다(로드부 매핑 존재).
  4. "자동" 선택 시 빈 문자열이 저장된다.
  5. 신규 resw 키가 ko/en **양쪽**에 있고 `x:Uid` 소비 형식(`.Text`)을 따른다.
  6. 기존 필드(이름·설명·경로·도구·태그·카테고리·그룹)의 마크업·검증은 변경되지 않는다.
- **Edge Cases**: 저장된 색이 팔레트 10색에 없음(수동 편집·향후 팔레트 변경) → 선택 표시는 없지만 값은 보존(덮어쓰지 않는다) / 보기 모드(`IsViewMode`)에서 색 선택 비활성 여부는 기존 필드와 동일 규칙 적용 / 다이얼로그 높이 증가로 스크롤 발생 → 기존 `ScrollViewer` 안이라 무해.
- **Halt Forecast**: (i) 사전 해소 — 팔레트 색 값·"자동" 항목·라벨 문구를 위에서 확정. 신규 의존성·파괴적 작업 없음.

### T3 — VM·컨버터: 카드 색/이니셜 표시 값 `Type C`
- [ ] 구현
- **Files**: `Presentation/ViewModels/ProjectCardViewModel.cs`, `Presentation/Converters/DevToolConverters.cs`, `Resources/Converters.xaml`
- **Design**: ① 배치 — 파생 값은 `ProjectCardViewModel`, hex→Brush 변환은 기존 컨버터 파일에 2종 추가, 등록은 `Converters.xaml`. ② 신규 심볼 — `EffectiveHeaderColor`(실효 색 hex — 지정색 또는 이름 해시) / `Initial`(아바타 글자) / `HasIcon`(아이콘 유무) / `HexToHeaderGradientConverter`(→ 135° 2-stop `LinearGradientBrush`) / `HexToBrushConverter`(→ `SolidColorBrush`) / `TagColorConverter.ColorFromName`(기존 해시 로직의 public 노출). ③ 의존 방향 — VM은 `Microsoft.UI.Xaml` 타입을 참조하지 않는다(값은 `string`/`bool`). 컨버터만 Brush를 만든다. ④ 비추상화 — 색 팔레트 서비스·테마 토큰 클래스를 만들지 않는다.
- **구성**:
  - (D3) `TagColorConverter`의 FNV-1a+HSL을 `public static Color ColorFromName(string name)`으로 노출하고 기존 `Convert`도 그것을 호출하게 정리(동작 불변).
  - (D2·D4) `EffectiveHeaderColor` = `HeaderColor`가 `#RRGGBB`로 파싱되면 그대로, 아니면 `ColorFromName(Name)`을 hex 문자열로. `Name` 변경 시 갱신 알림.
  - (D11) `Initial` = 이름에서 공백 아닌 첫 문자 대문자, 없으면 `?`. `HasIcon` = `IconSource is not null`(현행 아이콘 로딩 결과 기준 — `IconPath`만 보면 로딩 실패 시 빈 칸이 된다).
  - 컨버터 2종을 `DevToolConverters.cs`에 추가하고 `Converters.xaml`에 키 등록(`HexToHeaderGradient`·`HexToBrush`). 그라데이션은 `StartPoint="0,0" EndPoint="1,1"`, stop 0에 알파 `0x8C`(=.55), stop 1에 `0x24`(=.14).
  - 파싱 실패·빈 입력 시 컨버터는 **투명이 아니라 회색 폴백**(카드가 사라져 보이지 않게).
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0.
  2. `EffectiveHeaderColor`가 지정색 우선 → 없으면 이름 해시로 결정되며, 같은 이름은 항상 같은 색(결정론적).
  3. 신규 해시 구현이 **추가되지 않는다** — `TagColorConverter`의 기존 함수를 재사용한다(중복 해시 0).
  4. `ProjectCardViewModel.cs`에 **`Brush`/`SolidColorBrush`/`LinearGradientBrush` 타입이 0건**이다(브러시는 컨버터 몫). ※ 이 파일은 이미 `Microsoft.UI.Xaml.Media.Imaging`(`:7`, `BitmapImage IconSource`)을 쓰므로 "`Microsoft.UI.Xaml` 0건"은 성립하지 않는 기준이다(plan-reviewer M1).
  5. 컨버터 2종이 `Converters.xaml`에 등록되고, 잘못된/빈 hex에 대해 예외 없이 폴백 브러시를 반환한다.
  6. 기존 `TagColorConverter`의 동작(도구 배지 색)은 변하지 않는다.
- **Edge Cases**: `HeaderColor`가 `#RGB` 단축형·`RRGGBB`(# 없음)·`#AARRGGBB` → 파싱 규칙을 `#RRGGBB` 1종으로 한정하고 나머지는 자동 색 폴백 / 이름이 빈 문자열 → 해시 입력이 빈 문자열이어도 결정론적 색 반환, `Initial`은 `?` / 이름이 이모지·한글 → 첫 문자 그대로(대문자 변환 무영향) / 아이콘 로딩 실패 → `HasIcon` false → 이니셜 표시 / 이름 변경 시 자동 색이 바뀐다(지정색이 있으면 안 바뀜).
- **Halt Forecast**: (i) 사전 해소 — 파싱 규칙·폴백·알파 값을 확정. 기존 `TagColorConverter` 리팩터링은 **동작 보존**이라 승인 대상 아님(호출부 계약 불변).

### T4 — 카드 상부 재구성: 헤더 밴드 + 아바타 + 이름 + 핀/삭제 + 설명 `Type C`
- [ ] 구현
- **Files**: `Presentation/Views/DashboardView.xaml`
- **Design**: ① 배치 — `ProjectCardTemplate` 상부(현행 :79-174 구간). ② 신규 심볼 — 없음(마크업 + T3 바인딩). ③ 의존 방향 — XAML → VM(T3 값)·컨버터. 코드비하인드 변경 없음. ④ 비추상화 — 카드 헤더용 UserControl·공용 스타일을 만들지 않는다(소비처 1곳).
- **구성**:
  - 카드 `Border`를 `CornerRadius="14"`로 하고 **드래그 6속성(`CanDrag`/`AllowDrop`/`DragStarting`/`DropCompleted`/`DragOver`/`Drop`)을 그대로 승계**(누락 시 핀 카드 재정렬이 죽는다).
  - 카드 안을 2행(헤더 62px / 본문 `*`)으로 재구성. 헤더 `Border`는 `CornerRadius="13,13,0,0"` + 그라데이션 배경(`EffectiveHeaderColor` + `HexToHeaderGradient`).
  - 헤더 내부: 아바타(38×38, `CornerRadius="10"`, 배경 `HexToBrush`) — `HasIcon`이면 `Image`, 아니면 이니셜 `TextBlock`(16px Bold White) / 이름(14.5px Bold, `*` 열 말줄임) / 핀·삭제 버튼(28×28, `CornerRadius=7`, 전경 `#B8FFFFFF`, 삭제 hover `#FF9D97`).
  - **경로 오류 경고 아이콘**(현행 :111-115)은 시안에 없지만 **기능이라 유지** — 헤더 이름 앞에 둔다(⏳ 위치는 육안 확인 대상).
  - (M4 동일 조치) 핀·삭제 버튼도 `CardIconButtonStyle` 소비처이므로 **스타일 정의는 수정하지 않고 `Width="28" Height="28" CornerRadius="7"`을 요소에 로컬 지정**한다(스타일을 고치면 T5의 34px 액션·T6의 28px 슬롯과 서로 덮어쓴다).
  - 본문 `Padding="16,14,16,16"`, 세로 `Spacing="12"`. 설명은 **1줄 말줄임**(D9), 태그는 현행 `MarqueeTagsControl` 유지.
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0.
  2. 카드 `Border`에 드래그 6속성이 **모두 남아 있다**(grep으로 확인).
  3. 헤더가 62px 고정이고 배경이 `EffectiveHeaderColor` + 그라데이션 컨버터로 바인딩된다.
  4. 아바타가 아이콘/이니셜 2분기로 그려지고, 둘 중 하나만 동시에 보인다.
  5. 설명이 1줄 말줄임이다(`MaxLines="2"` 잔존 0).
  6. 경로 오류 경고 아이콘·핀·삭제 기능이 유지된다(커맨드 바인딩 3건 존재).
- **Edge Cases**: 이름이 매우 김 → `*` 열에서 말줄임(아바타·버튼은 축소 안 됨) / 설명 없음 → 기존 `StringNotEmptyToVisibility`로 숨김(빈 줄 없음) / 태그 0개 → 마키 컨트롤이 빈 높이 / 아이콘이 정사각형이 아님 → `Stretch="Uniform"` 유지 / 밝은 헤더 색에서 흰 이니셜 대비 부족 → 시안도 흰 글자 고정(육안 확인 대상) / 드래그 중 헤더 밴드가 드래그 비주얼에 포함 → 기존 동작과 동일.
- **Halt Forecast**: (i) 사전 해소 — 소비처가 `DashboardView.xaml` 1곳임을 grep 전수 확인(함정 11). 파괴적·외부 작업 없음.

### T5 — 카드 액션 행: 큰 실행 버튼 + 작업/테스트 + `…` 메뉴 `Type C`
- [ ] 구현
- **Files**: `Presentation/Views/DashboardView.xaml`, `Presentation/Views/DashboardView.xaml.cs`, `Strings/{ko-KR,en-US}/Resources.resw`
- **Design**: ① 배치 — `ProjectCardTemplate` 본문의 액션 행(현행 :187-272 + :471-513 통합). ② 신규 심볼 — 없음(기존 커맨드 재바인딩 + resw 키). ③ 의존 방향 — XAML → 기존 커맨드. ④ 비추상화 — 메뉴 항목을 데이터로 생성하는 구조(`MenuItemViewModel` 등)를 만들지 않는다. 항목 7개(+ 터미널 하위 2) 고정이라 마크업이 더 명확하다.
- **구성**:
  - (D5) 한 행에: **IDE 실행 버튼**(`*` 열, `Height=34`, `CornerRadius=9`, 배경 `TagColor`(도구명), ▶ 12px + 도구명 12.5px Bold White, `Command=OpenInDevToolCommand`, 우클릭 `ContextFlyout`에 기존 관리자 실행 항목 유지) + **작업**(`OpenTodoCommand`) + **테스트**(`OpenTestListCommand`) 34px 아이콘 버튼 + 세로 구분선(1×18) + **`…`**(`E10C`) 버튼.
  - `…` 버튼은 `Flyout`(좌클릭)으로 `MenuFlyout` 표시: **실행 / 폴더 열기 / 터미널(▸ 기본 터미널 · PowerShell) / — / Git 상태(`IsGitRepo`일 때만) / 작업 기록 / 프로젝트 설정 / — / 삭제(위험색)** = 항목 7개 + 구분선 2개. 각 항목은 **기존 커맨드**에 바인딩하고 문구는 `x:Uid`(신규 resw 키 **9개**(항목 7 + 터미널 하위 2) × ko/en, `.Text` 접미).
  - **⚠️ 터미널은 `MenuFlyoutSubItem`으로 2분기**한다(plan-reviewer B3). `OpenTerminalWithPowerShellCommand`의 **유일한 UI 진입점이 삭제 대상인 터미널 아이콘 버튼의 `ContextFlyout`**(`DashboardView.xaml:259-265`)이라, 하위 항목을 만들지 않으면 그 커맨드가 화면에서 완전히 사라진다(Acceptance 4와 모순).
  - **도구 유효성 경고**(현행 `IsDevToolValid` 분기)와 **관리자 권한 표시**(`RunAsAdmin` 글리프)는 실행 버튼 안에 유지한다(기능 손실 방지).
  - (M4) 액션 아이콘 버튼은 `CardIconButtonStyle`을 그대로 쓰되 **`Width="34" Height="34" CornerRadius="9"`를 요소에 직접** 준다. **`Style` 정의(:19-25)는 수정하지 않는다** — 같은 스타일을 T6의 28px 슬롯 버튼이 공유하므로 Setter를 바꾸면 슬롯 크기가 함께 어긋난다.
  - 삭제·프로젝트 설정이 헤더 버튼과 중복되지만 시안도 그렇다(헤더 삭제 + 메뉴 삭제) — 그대로 둔다.
  - 제거되는 것: 별도 실행/폴더/편집/터미널 아이콘 버튼 4개, Git/작업/기록/테스트 아이콘 버튼 4개(→ 메뉴 또는 상단 2버튼으로 흡수). 소비처가 사라지는 `ToolTip_*` 리소스 주입(`DashboardView.xaml.cs:62-77`)도 **함께 정리**한다.
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0.
  2. 카드에 노출되는 액션 버튼이 **IDE 실행 + 작업 + 테스트 + `…` 4개**다(그 외 아이콘 버튼 0).
  3. `…` 메뉴가 **항목 7개 + 구분선 2개**로 구성된다 — 그중 6개는 `MenuFlyoutItem`이 직접 커맨드에 바인딩하고, 터미널은 `MenuFlyoutSubItem`(컨테이너, `Command` 없음) 아래 2개 항목이 바인딩한다. **커맨드 바인딩 항목 총 8개, 전부 기존 커맨드**(신규 커맨드 0).
  4. 기존 기능이 하나도 사라지지 않는다 — 실행·관리자 실행·폴더·터미널·**PowerShell 터미널**·Git 상태·작업·작업 기록·테스트·설정·삭제·핀 12종 전부 도달 가능(커맨드별 도달 경로를 표로 대조).
  5. 신규 resw 키 9개가 ko/en 양쪽에 있고 `.Text` 접미를 따른다.
  6. 소비처가 없어진 `this.Resources["ToolTip_*"]` 주입 줄이 남지 않는다(고아 0).
- **Edge Cases**: Git 레포가 아님 → Git 상태 항목 숨김(현행 `IsGitRepo` 조건 승계) / 도구 경로 무효 → 실행 버튼 안 경고 아이콘 유지 / 도구명이 매우 김 → 버튼 안에서 말줄임(`*` 열) / 관리자 실행은 실행 버튼 **우클릭** 유지 — 메뉴의 "실행"은 일반 실행 / **PowerShell 터미널은 `…` → 터미널 하위 항목**(B3) / 카드 우클릭 → 현행에 카드 전체 `ContextFlyout`은 없으므로 동작 변화 없음.
- **Halt Forecast**: (ii-a) 사전 승인 — **UX 동작 변경**(카드 버튼 8개 → 4개, 나머지는 2클릭). 사용자가 명시 결정한 항목이며 기능 제거는 없다.

### T6 — 카드 하단: 스크립트 슬롯 시안화 `Type C`
- [ ] 구현
- **Files**: `Presentation/Views/DashboardView.xaml`, `Presentation/ViewModels/ProjectCardViewModel.cs`
- **Design**: ① 배치 — 카드 본문 하단(구분선 아래). 표시 규칙은 VM. ② 신규 심볼 — `ProjectCardViewModel.CanAddCommandSlot`(빈 슬롯이 남았는가) + `AddCommandSlotCommand`(다음 빈 슬롯 설정 다이얼로그 열기 — 기존 `ConfigureCommandSlotCommand`에 위임). ③ 의존 방향 — XAML → VM. ④ 비추상화 — 슬롯을 컬렉션(`ObservableCollection<SlotViewModel>`)으로 바꾸지 않는다. 4칸 고정 + 개별 프로퍼티가 현행 구조이고, 컬렉션화는 이번 요구(표시 규칙 변경)에 필요 없다.
- **구성**:
  - (D6) 슬롯 0~3의 `Visibility`를 `IsCmdNSlotVisible` → **`IsCmdNConfigured`** 로 바꾼다(설정된 것만 표시).
  - 끝에 점선 `＋` 버튼(28×28, `Rectangle StrokeDashArray` — 함정 6). **`DashedAddButtonStyle`은 `TaskPage.xaml:523,540,557,574` 4곳이 공유하므로 스타일 정의를 수정하지 않는다**(함정 11 / plan-reviewer M5) — 재사용하되 `Width`/`Height`/`Padding`만 요소에 로컬 지정하고, 로컬 오버라이드로 시안 형태가 안 나오면 이 카드 전용 인라인 마크업으로 그린다(`Styles.xaml`·`TaskPage.xaml` 무변경이 조건).
  - 슬롯 버튼도 마찬가지로 `CardIconButtonStyle`을 쓰되 **`Width="28" Height="28" CornerRadius="8"`을 로컬 지정**한다(M4).
  - `Visibility`는 `CanAddCommandSlot`, 클릭 시 첫 빈 슬롯 인덱스로 설정 다이얼로그.
  - 행 전체를 `Border BorderThickness="0,1,0,0"`(상단 구분선) + `Padding="0,10,0,0"`, 버튼 간격 6.
  - 사용되지 않게 되는 `IsCmdNSlotVisible` 3개와 그 알림 코드(:140-155)를 **정리**한다(고아 방지).
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0.
  2. 설정된 슬롯만 표시되고, 미설정 슬롯은 자리도 차지하지 않는다.
  3. 슬롯이 4개 다 차면 `＋` 버튼이 사라진다.
  4. `＋` 클릭 시 **첫 번째 빈 슬롯**의 설정 다이얼로그가 열린다.
  5. 기존 슬롯 조작(실행·우클릭 설정/아이콘 변경/삭제·관리자 표시)이 전부 유지된다.
  6. `IsCmdNSlotVisible` 잔존 0(정의·소비 모두).
  7. **`Resources/Styles.xaml`·`Presentation/Views/TaskPage.xaml`이 변경되지 않는다**(M5 — 공용 스타일 회귀 0). 슬롯 버튼 크기는 28px로 지정된다(M4).
- **Edge Cases**: 슬롯 0개 → 슬롯 행에 `＋`만 (구분선은 유지) / 중간 슬롯만 설정됨(0 비고 1만 참) → 설정된 것만 순서대로 표시, `＋`는 여전히 첫 빈 슬롯(=0)을 연다 / 슬롯 삭제 후 `＋` 재등장 / 4개 다 참 → `＋` 숨김 / 아이콘 미지정 슬롯 → 기본 터미널 글리프(현행 규칙 유지).
- **Halt Forecast**: (ii-a) 사전 승인 — 공개 파생 프로퍼티 3개 제거(`IsCmdNSlotVisible`) + 신규 2개. 소비처는 `DashboardView.xaml` 1곳으로 같은 task에서 함께 교체하므로 빌드가 성립한다.

### T7 — 그리드·추가 카드: 가변 폭 + 시안 스타일 `Type C`
- [ ] 구현
- **Files**: `Presentation/Views/DashboardView.xaml`, `Presentation/Views/DashboardView.xaml.cs`(드롭 위치 판정 — B4)
- **Design**: ① 배치 — `UniformGridLayout`(:588) + 3개 DataTemplate의 크기 지정 + 드롭 판정(`ShowDropPlaceholder`). ② 신규 심볼 — 없음. ③ 의존 방향 — 마크업 + 코드비하인드 내부 계산. ④ 비추상화 — 커스텀 `Layout` 클래스를 만들지 않는다.
- **구성**:
  - `UniformGridLayout`에 `MinItemWidth="310"`·`MinColumnSpacing="14"`·`MinRowSpacing="14"`·`ItemsStretch="Fill"`(가변 폭) 지정, `ItemsJustification="Start"` 유지. `ItemsRepeater Margin="24,18,24,24"`.
  - 3개 템플릿(`ProjectCard`/`AddCard`/`DropPlaceholder`)의 `Width="330" Margin="8"`을 제거하고 `MaxWidth="360"`으로 통일(간격은 레이아웃이 담당).
  - 추가 카드: `MinHeight="150"`, 카드와 같은 배경·`CornerRadius="14"`, `＋` 26px + 문구 13px, `Spacing="12"`, hover 테두리 = 앱 액센트(D7).
  - **⚠️ (B4) 드롭 위치 판정의 하드코딩 상수를 함께 고친다** — `DashboardView.xaml.cs:402`가 `var isLeft = posInTarget.X < 165; // 카드 폭(330)의 절반`이다. 폭이 가변이 되면 이 상수가 실제 폭과 어긋나 **핀 카드가 엉뚱한 위치에 떨어진다**(빌드·grep으로 검출 불가). 대상 요소의 실제 폭 절반과 비교하도록 바꾼다(호출부 `Card_DragOver`에서 폭 또는 요소를 함께 넘기거나 `ShowDropPlaceholder` 시그니처에 폭을 추가).
  - **폴백**: `ItemsStretch="Fill"`이 기대대로 늘어나지 않으면 `MinItemWidth`만 두고 **카드 폭을 330 고정으로 되돌린다**. 이 경우 아래 Acceptance 2는 "3개 템플릿의 폭 지정이 **서로 동일**"로 갈음하고, 드롭 상수 수정은 그대로 유지하며(고정폭에서도 옳다), 사실을 Deferred에 기록한다(M6).
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0.
  2. 3개 템플릿에 `Width="330"` 잔존 0 — **폴백 경로에서는** 3개 템플릿이 같은 고정폭을 쓰고 그 사실이 Deferred에 기록된다(둘 중 하나 충족).
  3. 레이아웃에 `MinItemWidth`·행/열 간격 14가 설정된다.
  4. 추가 카드가 시안 값(150 높이·14 라운드·26px `＋`)을 따른다.
  5. 드롭 플레이스홀더가 카드와 같은 폭으로 그려진다(드래그 재정렬 시 칸이 어긋나지 않음).
  6. **`ShowDropPlaceholder`에 `165` 같은 폭 하드코딩 상수가 남지 않는다**(B4).
- **Edge Cases**: 창 폭이 최소(1000)일 때 열 수 → `MinItemWidth=310`이면 사이드바 60 제외 2~3열 / 카드 1장 → 좌측 정렬 유지 / `ItemsStretch` 미지원 동작 → 위 폴백 / 드래그 중 플레이스홀더 삽입으로 열 재배치 → 기존과 동일 / **카드 폭이 360일 때 좌/우 드롭 판정** → 실제 폭 절반 기준이라 정확(B4) / 대상 요소 폭이 0으로 측정됨(레이아웃 전) → 이전 동작대로 좌측 판정 폴백 / 태그·설명 길이에 따른 카드 높이 차 → `UniformGridLayout`은 행 높이를 최대에 맞춘다(시안 `align-items:start`와 다를 수 있음 — 육안 확인).
- **Halt Forecast**: (i) 사전 해소 — 폴백 규칙을 미리 정해 실패 시에도 멈추지 않는다.

### T8 — 그룹 탭 개수 배지 `Type D`
- [ ] 구현
- **Files**: `Presentation/ViewModels/MainViewModel.cs`, `Presentation/ViewModels/GroupTabViewModel.cs`(신규), `MainWindow.xaml`, `MainWindow.xaml.cs`, `Strings/{ko-KR,en-US}/Resources.resw`(M3)
- **Design**: ① 배치 — 개수 계산은 `MainViewModel`(필터 파이프라인과 같은 조건), 탭 항목은 신규 `GroupTabViewModel`, 마크업은 `MainWindow.xaml` 탭 템플릿. ② 신규 심볼 — `GroupTabViewModel`(`Group`(ProjectGroup) + `[ObservableProperty] Count` — 탭 1개의 표시 상태) / `MainViewModel.GroupTabs`(탭 소스) / `MainViewModel.AllGroupCount`("전체" 탭용) / 개수 갱신 메서드 1개. ③ 의존 방향 — Presentation → Domain(읽기). ④ 비추상화 — 탭 전용 스타일·컨버터를 만들지 않는다(배지는 인라인 `Border`).
- **구성**:
  - **⚠️ `ProjectGroup`은 `record` + `init` 전용 불변 값 객체이며(`Domain/ValueObjects/ProjectGroup.cs` 직접 확인) `AppSettings.Groups`로 **JSON 직렬화**된다.** 여기에 가변 `CardCount`를 넣으면 ① record 값 동등성에 개수가 섞이고 ② 설정 JSON에 표시 값이 새어 나가며 ③ `with` 복사 시 유실된다 → **도메인을 건드리지 않고 wrapper VM을 둔다**(이 확인이 T8 설계의 성립 조건이라 계획 단계에서 완료).
  - `GroupTabViewModel`을 `Groups`와 1:1로 만들어 `GroupTabs` 컬렉션에 담고, 탭 `ItemsRepeater`의 `ItemsSource`를 `Groups` → `GroupTabs`로, `DataTemplate x:DataType`을 `vm:GroupTabViewModel`로 바꾼다. `Tag`는 `{Binding Group.Id}`, 이름은 `{Binding Group.Name}`, 기본 그룹 조건은 `{Binding Group.IsDefault}`.
  - **`MainWindow.xaml.cs`의 `DataContext: ProjectGroup` 패턴 3곳**(`GroupTab_DoubleTapped`:362, `GetGroupFromMenuFlyoutItem`:406, 그 소비처)을 `GroupTabViewModel`에서 `Group`을 꺼내도록 수정한다(grep 전수 — `GroupTab_Checked`:356은 `Tag is string`이라 무영향).
  - (D12) 필터(`ApplyFilterAndSort`)와 같은 검색 조건으로 그룹별 개수를 세어 각 `GroupTabViewModel.Count`와 `AllGroupCount`에 대입한다. 호출 지점은 필터 재계산 지점(카드 추가·삭제·그룹 이동·검색어 변경이 모두 여기로 수렴).
  - 탭 템플릿(`MainWindow.xaml:308-325`)과 "전체" 탭(:294-299)에 배지 `Border`(11px·`CornerRadius=8`·`Padding="7,1"`) 추가. 선택 탭은 액센트 계열, 비선택은 회색(D7).
  - **⚠️ (M3) "전체" 탭은 `x:Uid="AllGroupTab"`으로 resw가 `Content`를 통째로 채운다**(`RadioButton`은 `ContentControl`이라 Content 1개). 배지를 넣으려면 Content를 `StackPanel`(라벨 `TextBlock` + 배지 `Border`)로 바꿔야 하는데, 그 순간 `AllGroupTab.Content` 주입이 마크업을 덮어써 **빌드 오류 없이 라벨/배지가 사라진다**(함정 3). → 라벨 `TextBlock`에 **새 `x:Uid`(`.Text` 접미) 키를 ko/en 양쪽에 추가**하고 `RadioButton`의 `x:Uid`는 제거한다. 구 `AllGroupTab.Content` 키는 고아가 되므로 Deferred에 기록.
  - (m2) `MainWindow.xaml.cs:165-170`이 `_viewModel.Groups`의 인덱스와 `GroupTabsRepeater.TryGetElement(i)`를 짝지어 쓴다 → **`GroupTabs`는 `Groups`와 같은 순서·같은 개수로 유지**한다(재구성 시 통째로 다시 만들어 순서 보장).
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0.
  2. "전체"를 포함한 모든 탭에 개수 배지가 표시된다.
  3. 개수가 **검색어 필터를 반영**한다(D12 — 검색 중이면 결과 수).
  4. 카드 추가·삭제·그룹 이동 후 개수가 갱신된다.
  5. 기존 탭 동작(선택·더블탭 이름수정·우클릭 메뉴·스크롤 버튼)이 유지된다 — `DataContext: ProjectGroup` 패턴 잔존 0(전부 wrapper 경유).
  6. `Domain/ValueObjects/ProjectGroup.cs`가 **변경되지 않는다**(record 불변성·JSON 직렬화 보존).
- **Edge Cases**: 그룹이 0개(기본 그룹만) → "전체" 배지만 / 개수 0 → `0` 표시(숨기지 않음 — 시안도 표시) / 3자리 이상 → 배지 폭이 늘어남(상한 없음) / 검색 결과 0 → 모든 배지 0 / 그룹 삭제 시 소속 카드가 미분류로 이동 → 개수 재계산 / 그룹 이름수정·추가로 `Groups`가 바뀜 → `GroupTabs`도 함께 재구성해야 탭이 어긋나지 않는다(동기화 지점 1곳으로 유지) / 가져오기로 그룹이 대량 추가됨 → 같은 재구성 경로.
- **Halt Forecast**: (ii-a) 사전 승인 — 탭 `ItemsSource` 타입 교체에 따른 **코드비하인드 3곳 수정**(공개 시그니처 변경은 없고 내부 캐스팅만). 파괴적·외부 작업 없음.

## 사전 승인 항목 (일괄 승인 대상)

- **PRD 정정**(위 `## PRD 충돌`) — §7 Out of Scope 줄 삭제 + §4에 대시보드 카드 FR 신설 + §8 결정 1줄. **코드 T1보다 먼저 수행**.
- **DB 스키마 변경** — `Projects.HeaderColor` 컬럼 추가(T1). 하위호환 가드 포함, 기존 데이터 무손실.
- **도메인·VM 공개 멤버 변경** — `ProjectItem.HeaderColor` 추가(T1), `ProjectCardViewModel`의 `IsCmdNSlotVisible` 3개 제거·신규 5개 추가(T3·T6), **신규 `GroupTabViewModel` + `MainViewModel.GroupTabs`/`AllGroupCount` 추가 및 그룹 탭 `ItemsSource` 타입 교체**(T8 — `ProjectGroup`(record·JSON 직렬화 대상)은 **수정하지 않는다**).
- **UX 동작 변경** — 카드 액션 버튼 8개 → 4개, 나머지는 `…` 메뉴 2클릭(T5). 사용자 결정 항목.
- 로컬 작업 브랜치에서 task별 commit(신규 브랜치 `task/dashboard-card-redesign` — 직전 `task/taskpage-list-view`와 주제가 다르다).

## 불가피한 Halt (위임 불가)

- master 병합·push·태그·릴리즈·PR — 별도 승인.
- **시안 대조 최종 시각 판정** — 헤더 밴드 그라데이션의 농도·아바타 대비·카드 높이 균형·그리드 열 수·배지 색은 사용자만 판정(⏳ HUMAN-VERIFY).
- **실동작 확인** — 색 저장/불러오기, `…` 메뉴 각 항목, 슬롯 `＋` 흐름, 드래그 재정렬, 가져오기 후 색 보존(⏳ HUMAN-VERIFY).

## Deferred / Follow-up

- **[태그 pill 시안화 미적용]** — 시안 태그는 `#26262c` 배경 pill의 줄바꿈 배치이나, 현행 `MarqueeTagsControl`(흐르는 애니메이션 + 설정 토글)을 유지했다(사용자 미선택, 2026-07-22). 애니메이션 기능을 접고 시안대로 갈지는 사용시 판단.
- **[카드 hover 이동 애니메이션 미구현]** — D8. 작업·테스트 화면의 동일 항목과 함께 다룬다.
- **[시안 보라 액센트 미채택]** — D7. 시안 대시보드는 `#8b7cf7`, 앱은 `#F0716A`. 앱 전역 액센트를 바꾸는 것은 별개 논의.
- **[GitHub 연결·프로젝트 메모 미구현]** — 시안 `…` 메뉴의 2항목은 현행에 기능이 없어 제외(사용자 결정). 필요해지면 도메인 필드부터 논의.
- **[README/스크린샷 갱신]** — 대장의 기존 항목에 **대시보드 카드**가 추가된다(이번 변경으로 메인 화면 스크린샷이 낡음).
- **[구 `ToolTip_*`·`AllGroupTab.Content` resw 고아]** — T5에서 소비처가 사라지는 툴팁 키와 T8에서 대체되는 `AllGroupTab.Content`(M3)가 resw에 남는다(빌드·런타임 무해). 대장의 resw 고아 audit 항목과 함께 처리.
- **[그리드 가변 폭 폴백 시]** — `ItemsStretch="Fill"`이 기대대로 동작하지 않아 고정폭으로 되돌린 경우(T7 폴백), 시안의 `minmax(310,360)` 정합이 미달인 채로 남는다 — 대안(커스텀 Layout 등) 검토.

## Out of Scope

- 하단 상태바·헤더(검색·정렬·알림·설정) 변경 — 시안과 이미 정합(D13).
- 작업/테스트/기록/알림 페이지 — 이번 diff 무관.
- `Palette.xaml` 신규 색 추가(함정 4) — 카드 색은 데이터이지 팔레트 토큰이 아니다.
- 프로젝트 설정 다이얼로그의 **전체 재설계**(대장 `[FR-S5]`) — 이번엔 색 필드 1개 추가만.
- 카드 색을 그룹·카테고리에서 자동 상속하는 규칙 — 요구에 없다.

## Open Questions

- [x] 헤더 색 데이터 처리 → **도메인 필드 추가**(사용자, 2026-07-22)
- [x] 아이콘 있는 프로젝트의 아바타 → **아이콘 우선, 없으면 이니셜**
- [x] 액션 버튼 축약 → **시안대로 축약 + `…` 메뉴 신설**
- [x] 주변 범위 → **스크립트 슬롯 시안화 + 그리드 폭 가변화 + 그룹 탭 개수 배지**(태그 pill 교체는 미선택)
- [x] `…` 메뉴 구성 → **현행 기능만 이동**(GitHub·메모 제외)
- [x] 슬롯 표시 규칙 → **설정된 것만 + 끝에 점선 `＋`**
- [x] 기존 프로젝트 기본색 → **이름 해시로 자동 배정**
- [ ] **PRD §7 Out of Scope 정정 승인** — 위 `## PRD 충돌` 절. plan 승인 시 함께 확인.

## 검증 방법

- 빌드: `"C:/Program Files/Microsoft Visual Studio/18/Professional/MSBuild/Current/Bin/MSBuild.exe" "DevDashboard_WinUI/DevDashboard.csproj" -t:Build -p:Configuration=Debug -p:Platform=x64` → 오류 0 + 실측 baseline(`CS0618` 1건) 외 신규 0
- 회귀 방지 grep(소스 `*.cs`/`*.xaml`/`*.resw`, `obj/`·`bin/` 제외):
  - **T1**: `ProjectItem.cs`에 `HeaderColor` 1 / **`DatabaseContext.cs`에 `HeaderColor` 2건**(`AllowedIdentifiers` + `AddColumnIfNotExists` — 화이트리스트 누락이 곧 기동 실패, B1) / `SqliteProjectRepository.cs`에 `HeaderColor` **5건 이상**(INSERT 컬럼·파라미터, UPDATE 컬럼·파라미터 4건 + `ReadProjects` 읽기 블록 — `HasColumn`+`GetOrdinal`+대입으로 3건까지 나올 수 있어 총 7건 전후가 정상) + `ReadAllFromDb` 본문에 `HeaderColor` **0건**(재사용 확인, B2)
  - **T2**: VM에 `HeaderColor` 3건(속성·`ToProjectItem`·로드) / resw ko·en 대칭
  - **T3**: `ProjectCardViewModel.cs`에 `EffectiveHeaderColor`·`Initial`·`HasIcon` 정의 각 1 + **`Brush` 계열 타입 0건**(M1) / `Converters.xaml`에 신규 키 2 / 해시 함수 정의 **1개뿐**(중복 0)
  - **T4**: `DashboardView.xaml`에 `CanDrag`·`DragStarting`·`DropCompleted`·`DragOver`·`Drop`·`AllowDrop` 각 1 이상 / `MaxLines="2"` 0
  - **T5**: 카드 액션 아이콘 버튼 3개(작업·테스트·`…`)에 `Width="34"` / `MenuFlyoutItem`+`MenuFlyoutSubItem` 신규 `x:Uid` **9건** / `OpenTerminalWithPowerShellCommand` 소비 **1건 이상**(B3) / `ToolTip_*` 주입 줄과 소비처 개수 일치(고아 0) / `Resources/Styles.xaml` diff 없음
  - **T6**: `IsCmdNSlotVisible` 잔존 **0**(정의·소비) / `IsCmd0Configured`~`IsCmd3Configured`가 각 슬롯 `Visibility`에 사용 / 슬롯 버튼 `Width="28"` / `Styles.xaml`·`TaskPage.xaml` diff 없음(M5)
  - **T7**: `Width="330"` 잔존 0(또는 폴백 시 3템플릿 동일) / `MinItemWidth` 1 / **`DashboardView.xaml.cs`에 `165` 상수 0건**(B4)
  - **T8**: `GroupTabViewModel` 정의 1 + `GroupTabs` 소비(ItemsSource) 1 / `DataContext: ProjectGroup` 패턴 **0건** / `Domain/ValueObjects/ProjectGroup.cs` diff 없음(M2) / 신규 "전체" 탭 라벨 resw 키 ko·en 대칭(M3)
- 동작 확인(빌드로 검증 불가 → ⏳ HUMAN-VERIFY):
  - 카드가 색상 헤더 밴드 + 아바타로 그려지는지, 색 미지정 카드가 서로 다른 색인지
  - 설정에서 색을 골라 저장 → 카드에 즉시 반영, 앱 재시작 후에도 유지, "자동"으로 되돌리기
  - `…` 메뉴 7항목(터미널은 하위 2개 포함) 각각의 동작 / 실행 버튼 클릭·우클릭(관리자)
  - 슬롯 `＋`로 추가 → 슬롯 표시 → 삭제 시 `＋` 재등장
  - 창 크기를 바꿀 때 열 수·카드 폭 변화, 핀 카드 드래그 재정렬
  - 그룹 탭 배지 숫자가 검색·추가·삭제에 따라 갱신되는지
  - **다른 PC/구버전 DB 가져오기** → 색 컬럼이 없어도 정상 동작
