# plan.md — Git 상태 다이얼로그: 커밋 목록 + 상세 페이지 2단 구조 개편

**기준 이미지**: 사용자 제공 2장(2026-09-02)을 레포에 고정했다 — ① `docs/design/uploads/git-status-commit-list.png`(GitHub 커밋 목록 스타일: 제목 1줄 + 아바타·작성자·상대시간·단축해시 1줄, 날짜 그룹 없음) ② `docs/design/uploads/git-status-commit-detail.png`(커밋 상세: 아바타 + 작성자명 + 커밋일자 · 이메일 헤더 + 메시지 전문). **대화에만 있던 이미지는 후속 세션이 대조할 수 없어** 계획 단계에서 파일로 남긴다. `## 시각 요소 분해` 참조.
**PRD**: 연결 없음 — `docs/prd.md`의 active FR 중 Git 상태 다이얼로그 **자체**를 규정한 항목이 없다(FR-D3이 "더 보기 메뉴에 Git 상태를 넣는다"고만 언급하며, 이번 변경은 그 메뉴 항목 존재를 건드리지 않는다). Step 1 경량 확인 결과 닿는 FR 0건이라 `**PRD**:` 연결을 만들지 않는다.

## 요구 이해

> 원문(사용자, 2026-09-02): "git 상태 화면에서 1번 이미지처럼 목록을 표시하고 클릭시 페이지 전환을 해서 2번 화면처럼 내용을 표시 하도록 수정해줘"

이해한 요구:
- **① 목록 restyle**: 현행 Git 상태 화면의 커밋 목록을 참고 이미지 ①처럼 바꾼다 — 커밋 **제목(첫 줄)**을 큰 글씨 1줄로, 그 아래 **아바타 · 작성자 · 상대시간 · 단축해시**를 작은 글씨 1줄로. 현행의 **날짜 그룹 헤더는 제거**하고 최신순 평면 목록으로 만든다(사용자 확정 Q3).
- **② 상세 전환**: 목록 항목을 클릭하면 **같은 다이얼로그 안에서** 상세 화면으로 전환된다(새 창·중첩 다이얼로그가 아니다 — WinUI 3은 중첩 ContentDialog를 지원하지 않고, 레포에 `HistoryDialog`의 목록↔폼 뷰 전환 선례가 있다).
- **③ 상세 내용**: 참고 이미지 ②처럼 **아바타 + 작성자명 + 커밋일자 · 이메일** 헤더와 **커밋 메시지 전문**을 표시하고, 목록으로 돌아가는 버튼을 둔다. 전문은 **줄번호 없이 모노스페이스 블록 하나**로 그려 본문 전체를 한 번에 드래그 선택·복사할 수 있게 한다(사용자 확정 Q2 — 줄 단위로 쪼개면 WinUI 제약상 한 줄 안에서만 선택된다).
- **아바타는 이니셜 원형**이다(사용자 확정 Q1). 로컬 `git log`는 작성자 이름·이메일까지만 주므로 참고 이미지의 GitHub 프로필 사진은 원리상 재현할 수 없고, 네트워크(Gravatar)로 가져오는 방식은 작성자 이메일을 외부로 보내게 되므로 택하지 않았다.

## Investigation Log (근거)

- **위키 참조**: `20_projects/personal/devdashboard-winui/feat-project-launch-actions` — "Git 상태 다이얼로그: **현재 브랜치 + 날짜별 커밋 목록** 표시(변경 파일 목록 기능은 없음). 다이얼로그는 열 때마다 git을 다시 조회." 코드와 일치하며 이번 설계를 바꾸는 새 정보 없음. 허브(`20_projects/personal/devdashboard-winui`) `synced_commit: 3d1f19d`로 최신.
- **결정 이력**: 대상 프로젝트 위키에 `decisions.md` 없음 — 과거 기각 이력 확인 불가(허브 「최근 주요 변경」에도 Git 상태 관련 보류 기록 없음).
- **기존 plan 처리**: 직전 "카드 메모 + 작업 기록 시안" plan은 `## Phase Ledger`에 **Phase G 통과(Must 100%)** 기록 → 완료 판정. 그 `## Deferred / Follow-up` 12건 중 대장 미등재분을 이관 완료(신규 2건 등재 + 기존 2항목에 이번 분 합산, 사용자 승인).
- **작업 공간**: 무관한 미커밋 잔재 3종(csproj NuGet 버전 / ProjectSettingsDialog 색 견본 클릭 버그 / notes.md)을 사용자 승인 후 커밋 3건으로 분리 정리(`88a3716`·`579eb44`·`7e8b2a0`). 착수 시점 워킹트리 clean.
- **Deferred 대장 조회**(`docs/plans/deferred.md` `## 대기` 16건):
  - **할 일 후보(주제 매칭)**: Git 상태 다이얼로그를 다루는 대기 항목 **0건**.
  - **전제 반증(제목 스캔 전건 + 걸린 항목 전문)**: **[함정] XAML 리소스 키 오타는 빌드가 잡지 못한다**(`:76`)가 이번 plan의 전제에 걸린다 — T2가 `ThemeResource App*Brush`를 새로 참조하므로 **키 실재를 grep으로 확인**해야 한다(아래 `### 전제 검증` P4). **[README/스크린샷]**(`:18`)은 이번 T3와 겹치나 T3는 문구 1줄 정정이고 그 항목은 스크린샷 갱신이라 별개 — 대장 유지.
  - **소진 batch 판정**: `## 대기` 16건으로 잔량 임계(100건) 미달 → batch task 없음.
- **스킬 개선 큐**: 대상 아님 — 이 세션의 cwd에 `plugins/pjc/.claude-plugin/plugin.json` 없음(하네스 레포가 아니다).
- **현행 구현 (직접 Read)**:
  - `Domain/ValueObjects/GitCommit.cs`: `record GitCommit(Hash, AuthorName, AuthorEmail, Date, Message)` + `ShortHash`(7자리). `Message`는 `git log --format=%B` 결과라 **제목·본문이 합쳐진 전문**이다.
  - `Infrastructure/Services/GitHelper.cs:58` `GetCommitsAsync`: `--no-pager log --no-color --date=iso-strict --format=%H%n%an%n%ae%n%ad%n%B%n`. **개수 제한 없음**(전체 이력). 이번에 건드리지 않는다.
  - `Presentation/ViewModels/ProjectCardViewModel.cs:77` `GitCommitGroups`(`IReadOnlyList<GitCommitGroup>`), `:319` `GroupCommitsByDate`가 `Date.LocalDateTime.Date` 기준 그룹핑 후 내림차순.
  - `Presentation/Views/Dialogs/GitStatusDialog.xaml`: `ScrollViewer > StackPanel` 안에 로딩 패널 / `InfoBar` 에러 / 브랜치 행 / `ItemsControl`(2중첩: 그룹 → 커밋). 커밋 카드는 `Border` + 해시·메시지·작성자. 다이얼로그 크기 700×700 고정(`ContentDialogMinWidth/MaxWidth/MinHeight/MaxHeight` 리소스 오버라이드).
  - `GitStatusDialog.xaml.cs`: `OnOpened`에서 `_card.LoadGitStatusAsync` → `BranchText.Text`·`CommitList.ItemsSource` 채우고 `ContentPanel` 표시. `_cts`로 취소.
- **소비처 전수 (grep, 레포 전체 `.cs`/`.xaml`, `obj/` 제외)**:
  - `GitCommitGroup` — **9 hit / 4파일**(실측): 정의(`Presentation/Models/GitCommitGroup.cs` 2곳), `ProjectCardViewModel`(프로퍼티·그룹핑 메서드·생성 4곳), `GitStatusDialog.xaml`(DataTemplate `x:DataType`), `GitStatusDialog.xaml.cs`(2곳). **Git 상태 다이얼로그 전용** — 그룹을 없애면 완전 고아가 된다.
  - `GitCommit` — **7 hit / 4파일**(실측): 정의(`GitCommit.cs:6`), `GitHelper`(`:57`·`:58`·`:67`·`:106`), `GitStatusDialog.xaml:71`(DataTemplate). 나머지 1건(`GitStatusDialog.xaml:55` `GitCommitHistoryLabel`)은 **부분 문자열 일치일 뿐 무관어**라 실질 소비처는 6건이다. 전건 Read 완료.
- **뷰 전환 선례 (직접 Read)**: `HistoryDialog.xaml`이 `ListArea`/`FormArea` 두 `Grid`를 겹쳐 두고 `.xaml.cs`의 `ShowFormView()`(`:142`)/`ShowListView()`(`:171`)가 `Visibility`를 토글하고 `BackToList_Click`(`:185`)이 뒤로를 처리한다. 뒤로 버튼은 `Button`(`HistoryDialog.xaml:326`) + `FontIcon Glyph="&#xE76B;"` + `TextBlock x:Name="BackText"`(`:329`)이며, **라벨은 `x:Uid`가 아니라 코드비하인드가 채운다**(`.xaml.cs:79` `BackText.Text = LocalizationService.Get("HistoryForm_Back")` — `:325` 주석에 "아이콘과 함께 놓아야 해서 x:Uid 미사용"이라 적혀 있다). **T2가 이 패턴을 그대로 따른다.**
- **행 클릭 선례 (직접 Read)**: `HistoryDialog.xaml:130~139` — `Border`에 `Tapped` + `PointerEntered`/`PointerExited` + `Tag="{x:Bind}"`. `Tag`로 항목을 넘기는 이유는 `ItemsRepeater`가 `x:Bind` 템플릿에 `DataContext`를 채우지 않기 때문(커밋 `579eb44`에서 같은 함정을 겪었다). `ItemsControl`은 `DataContext`를 채우지만 **선례 일치를 위해 `Tag`를 쓴다**.
- **부재 판정 (신규 작성의 근거)**:
  - **상대시간 포맷터 없음** — 검색어 ①`RelativeTime|TimeAgo|Humanize`(레포 전체 `.cs`) **0건** ②`분 전|시간 전`(레포 전체 `.cs`/`.resw`) **0건**. 재사용할 기존 구현이 없다.
  - **아바타/이니셜 표시 없음** — 검색어 ①`PersonPicture`(레포 전체 `.xaml`) **0건** ②`Avatar`(레포 전체 `.cs`/`.xaml`) **0건**(`Initial` 검색의 hit는 전부 `InitializeComponent`·`_isInitialized` 등 무관어).
- **resw 현황**: ko-KR / en-US 각 **447 항목(실측, 대칭)**. Git 관련 기존 키 8개(`GitStatusDialog_TitleFormat`·`GitStatusDialog.Title`·`GitStatusDialog.CloseButtonText`·`GitLoadingText.Text`·`GitErrorBar.Title`·`GitCommitHistoryLabel.Text`·`NoCommitsText.Text`·`GitPathNotResolved`).
- **팔레트 확인**: `Resources/Palette.xaml`은 `Default`(다크) 딕셔너리 하나. 아바타 색에 쓸 기존 색은 `AppInfoColor`(#5B93D8)·`AppSuccessColor`(#5DB463)·`AppWarningColor`(#D9954A)·`AppAccentColor`(#F0716A) 4종이 실재한다 — **신규 색 리터럴을 추가하지 않는다**(대장 `:70` 색 리터럴 누적 항목을 더 키우지 않기 위함).
- **테스트**: 테스트 프로젝트 부재(AGENTS.md) — 4-C 해당 없음, 검증은 빌드 + 사용자 육안.

### 4-D. 재사용 확인

| 신규 심볼 | 검색어·스코프 | hit | 유사 기존 구현 | 재사용/신규 사유 |
|---|---|---|---|---|
| `RelativeTime(DateTimeOffset)` | `RelativeTime\|TimeAgo\|Humanize` / `분 전\|시간 전` — 레포 전체 `.cs`·`.resw` | 0 / 0 | 없음 | **신규** — 재사용할 구현이 없다. 사용처 1곳이라 공용 서비스로 빼지 않는다 |
| `AvatarInitial(string)` / `AvatarBrush(string)` | `PersonPicture` / `Avatar` — 레포 전체 `.xaml`·`.cs` | 0 / 0 | 없음 | **신규** — 색은 팔레트 기존 4색을 재사용하고 새 리터럴을 만들지 않는다 |
| `ShowDetail`/`ShowList` 뷰 전환 | `ShowListView\|ShowFormView` — 레포 전체 `.cs` | 7 (전부 `HistoryDialog.xaml.cs` — `:137`·`:142`·`:171`·`:185`·`:234`·`:240`·`:258`) | `HistoryDialog.ShowFormView/ShowListView` | **패턴 재사용** — 다이얼로그마다 자기 코드비하인드가 갖는 구조라 심볼 자체를 공유하지 않는다 |
| `GitCommit.Subject` | `Subject` — 레포 전체 `.cs` | 0 | 없음 | **신규** — 값 객체 내부 계산 |

### 전제 검증

| # | 이 plan이 참으로 삼는 전제 | 확인 근거 | 판정 |
|---|---|---|---|
| P1 | `GitCommit.Message`가 제목·본문을 모두 담은 전문이다(제목/본문 분리를 파생 프로퍼티로 만들 수 있다) | `GitHelper.cs:60`의 포맷 문자열이 `%B`(raw body = subject + body). `%s`(subject only)가 아님 | 확인 |
| P2 | `GitCommitGroup`을 없애도 다른 화면이 깨지지 않는다 | grep 전수 9 hit / 4파일 전건 Read — 소비처가 Git 상태 다이얼로그 경로뿐 | 확인 |
| P3 | 같은 `ContentDialog` 안에서 Visibility 토글로 뷰를 전환하는 것이 이 레포의 검증된 방식이다 | `HistoryDialog.xaml.cs:142`·`:171`·`:185` 직접 Read(2026-07-24 회차에서 구현·검증됨) | 확인 |
| P4 | T2가 참조할 `App*Brush` 키가 `Palette.xaml`에 실재한다 | `AppCardAltBrush`(`:48`)·`AppInputBrush`(`:49`)·`AppBorderBrush`(`:50`)·`AppHoverFillBrush`(`:52`) 확인. 아바타용 `AppAccentBrush`(`:56`)·`AppSuccessBrush`(`:62`)·`AppInfoBrush`(`:64`)·`AppWarningBrush`(`:65`)도 Brush 키로 실재. **Color만 있고 Brush가 없는 키**(`AppTextDisabledColor` 등)는 쓰지 않는다 — 대장 `:76` 함정 | 확인 |
| P5 | `x:Bind` 함수 바인딩으로 `Brush`·문자열을 코드비하인드 정적 메서드에서 직접 받을 수 있다 | AGENTS.md 함정 5 + `TestPage.StatusBrush`·`TaskPage.PriorityBrush` 선례 명시 | 확인 |
| P6 | 커밋 개수가 많아도(수천 건) 평면 `ItemsControl`이 실용적이다 | **미확인** — 현행도 같은 `ItemsControl`에 전체 이력을 그려 왔으므로 **이번 변경이 성능을 악화시키지 않는다**는 것만 확인됐다(그룹 2중첩 → 1중첩으로 오히려 단순해진다). 절대 성능은 미측정 — 성립을 좌우하지 않으므로 Deferred |

## 동반 변경 판정

| # | 대상 | 축 | 구분 | 처리 |
|---|---|---|---|---|
| 1 | `README.md:32` "Git 상태 다이얼로그로 **변경 파일** 확인" | ① 서술 문서 | **선택** | 이번 변경 이전부터 실제와 어긋난 서술(위키도 "변경 파일 목록 기능은 없음"으로 적었다). **사용자가 함께 수정을 선택 → T3으로 편입** |
| 2 | `DevDashboard_WinUI/notes.md` | ① 서술 문서 | **필수** | **T3 Files에 편입**(AGENTS.md:96이 "작업 내역 영구 기록"으로 규정 — 갱신하지 않으면 이번 개편이 기록에서 빠진다) |
| 3 | 위키 `feat-project-launch-actions.md:35` "브랜치 + **날짜별** 커밋 목록" | ① 서술 문서 | **무관(이번 범위 밖)** | 위키 본문 갱신은 ingest 세션 소관(plan-feature Step 1: "위키 본문 갱신은 하지 않는다"). Deferred에 등재 |
| 4 | 규약 복제본 | ② | **무관** | Git 상태 화면 규약을 복제한 파일 없음(grep `GitCommit` 4파일이 전부) |
| 5 | 검증 자산(테스트·골든) | ③ | **무관** | 테스트 프로젝트 부재(AGENTS.md) |
| 6 | 버전·매니페스트 | ④ | **무관** | AGENTS.md에 릴리즈·버전 규약 없음. csproj 버전 조작 대상 아님 |
| 7 | 날짜 그룹 표시 제거로 무효화되는 기존 표시 | ⑤ | **필수(의도됨)** | 사용자 확정 Q3. 날짜는 각 행의 상대시간·상세의 절대 일시가 대신한다 — T2 범위 안 |

## 시각 요소 분해

**기준**: `docs/design/uploads/git-status-commit-list.png`(① 목록) · `docs/design/uploads/git-status-commit-detail.png`(② 상세) — 사용자가 2026-09-02에 제공한 참고 화면 캡처를 레포에 고정한 것이다. 이식(①-b)이 아니라 **시각 충실도(①-a)** 요청이므로 `### 참조 정합 인벤토리` 표는 만들지 않는다(원본 소스가 없는 외부 서비스 화면이라 항목별 `원본 근거`를 적을 수 없다 — 근거 없는 행을 만들지 않는다는 규칙).

**기준의 성격과 한계**: 기준이 코드가 아니라 **렌더된 PNG**라 폰트 크기·간격은 캡처에서 읽은 **관측 추정치**다. 아래 표의 「디자인 값」에 `(관측)`을 붙인 항목이 그것이며, 정확한 CSS 값을 대조할 원본이 없다. 또한 기준이 웹 화면이라 CSS 개념이 XAML로 1:1 대응하지 않으므로 **XAML 대응 수단을 함께** 적는다.

### 시각 속성

| 요소 | 속성 | 디자인 값 | XAML 대응 수단 | 확인 방법 | 구현 근거 (V-9) | 판정 (V-9) |
|---|---|---|---|---|---|---|
| 목록 행 | 레이아웃 | 세로 2줄(제목 / 메타) | `StackPanel Spacing="5"` | ① 캡처 | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml:84` | ⏳ 미확인 — 속성 반영 확인 |
| 목록 행 | 배경 | 페이지와 같은 어두운 면, 카드 테두리 없음 (관측) | `Border Background="Transparent"` | ① 캡처 | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml:76`(`RowRestBrush`=Transparent) | ⏳ 미확인 — 속성 반영 확인 |
| 목록 행 | 행 구분 | 행 사이 1px 가로 구분선 (관측) | `BorderBrush="{ThemeResource AppBorderBrush}" BorderThickness="0,0,0,1"` | ① 캡처 | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml:77~78` | ⏳ 미확인 — 속성 반영 확인 |
| 목록 행 | 안쪽 여백 | 좌우 12px · 상하 11px (관측) | `Padding="12,11"` | ① 캡처 | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml:79` | ⏳ 미확인 — 속성 반영 확인 |
| 목록 행 | hover | 배경이 한 단계 밝아짐 (② 행이 그 상태로 캡처됨) | `PointerEntered`에서 `AppHoverFillBrush`로 교체(`HistoryDialog` 선례) | ① 캡처 2행 | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml.cs:129`(`RowHoverBrush`=`AppHoverFillColor`) | ⏳ 미확인 — 속성 반영 확인 |
| 커밋 제목 | 폰트 크기·굵기 | 13px / 일반 (관측) | `FontSize="13"` | ① 캡처 | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml:88` | ⏳ 미확인 — 속성 반영 확인 |
| 커밋 제목 | 색 | 본문 밝은색 | `{ThemeResource AppTextPrimaryBrush}` | ① 캡처 | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml:89` | ⏳ 미확인 — 속성 반영 확인 |
| 커밋 제목 | 넘침 | 한 줄 말줄임 | `MaxLines="1" TextTrimming="CharacterEllipsis"` | ① 캡처(긴 제목 행) | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml:90~91` | ⏳ 미확인 — 속성 반영 확인 |
| 메타 줄 | 구성·순서 | 아바타 → 작성자 → `·` → 상대시간 → `·` → 단축해시 | `StackPanel Orientation="Horizontal" Spacing="6"` | ① 캡처 | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml:94~129` | ⏳ 미확인 — 속성 반영 확인 |
| 메타 줄 | 폰트 크기 | 11.5px (관측) | `FontSize="11.5"` | ① 캡처 | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml:106`·`:111`·`:116`·`:121`·`:127` | ⏳ 미확인 — 속성 반영 확인 |
| 메타 줄 | 색 | 보조 회색 | `{ThemeResource AppTextTertiaryBrush}` | ① 캡처 | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml:107` 외 4곳 | ⏳ 미확인 — 속성 반영 확인 |
| 메타 줄 | 세로 정렬 | 아바타·텍스트 중앙 정렬 | `VerticalAlignment="Center"` | ① 캡처 | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml:95` 외 5곳 | ⏳ 미확인 — 속성 반영 확인 |
| 단축해시 | 서체 | 모노스페이스 | `FontFamily="Consolas"` | ① 캡처(자간 균일) | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml:126` | ⏳ 미확인 — 속성 반영 확인 |
| 목록 아바타 | 크기·모양 | 지름 16px 원 (관측) | `Ellipse Width="16" Height="16"` + 중앙 `TextBlock`(9px) | ① 캡처 | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml:95~102` | ⏳ 미확인 — 속성 반영 확인 |
| 상세 아바타 | 크기·모양 | 지름 40px 원 (관측) | `Ellipse Width="40" Height="40"` + 중앙 `TextBlock`(16px) | ② 캡처 | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml:175~181` | ⏳ 미확인 — 속성 반영 확인 |
| 상세 헤더 | 레이아웃 | 아바타 좌측 · 우측에 2줄(이름 / 날짜 · 이메일) | `Grid` 2열 + `StackPanel Spacing="2"`, `ColumnSpacing="12"` | ② 캡처 | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml:169~193` | ⏳ 미확인 — 속성 반영 확인 |
| 상세 작성자명 | 폰트 | 13px / 약간 굵게 / 밝은색 (관측) | `FontSize="13" FontWeight="SemiBold"` + `AppTextPrimaryBrush` | ② 캡처 | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml:186~187` | ⏳ 미확인 — 속성 반영 확인 |
| 상세 부제 | 구성 | `{날짜} · {이메일}` 한 줄 | 단일 `TextBlock` | ② 캡처 | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml.cs:107~110` | ⏳ 미확인 — 속성 반영 확인 |
| 상세 부제 | 날짜 형식 | 기준은 로케일 장식형 `2026년 9월 2일` | **`yyyy-MM-dd HH:mm`로 대체**(아래 「기준과 다르게 가는 항목」 V1) | ② 캡처 | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml.cs:107` | ✅ 의도된 이탈 (V1) |
| 상세 부제 | 폰트·색 | 12px / 보조 회색 (관측) | `FontSize="12"` + `AppTextTertiaryBrush` | ② 캡처 | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml:190~191` | ⏳ 미확인 — 속성 반영 확인 |
| 상세 본문 | 서체 | 모노스페이스 | `FontFamily="Consolas"` | ② 캡처 | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml:203` | ⏳ 미확인 — 속성 반영 확인 |
| 상세 본문 | 폰트 크기 | 13px (관측) | `FontSize="13"` | ② 캡처 | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml:204` | ⏳ 미확인 — 속성 반영 확인 |
| 상세 본문 | 줄 간격 | 넓음 — 글자 높이의 약 1.5배 (관측) | `LineHeight="20"` | ② 캡처 | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml:205` | ⏳ 미확인 — 속성 반영 확인 |
| 상세 본문 | 줄번호 | 좌측에 우측정렬 회색 번호 열 | **구현하지 않음**(아래 V2) | ② 캡처 | 해당 없음 — 구현하지 않음 | ✅ 의도된 이탈 (V2) |
| 상세 본문 | 배경 | 페이지보다 약간 어두운 면 (관측) | `Border Background="{ThemeResource AppInputBrush}" CornerRadius="8" Padding="16"` | ② 캡처 | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml:196~200` | ⏳ 미확인 — 속성 반영 확인 |
| 상세 본문 | 선택 | 드래그 선택 가능 | `IsTextSelectionEnabled="True"` | 사용자 직전 요청 | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml:208` | ⏳ 미확인 — 속성 반영 확인 |
| 날짜 그룹 헤더 | 존재 여부 | 없음 | 현행 `ItemsControl` 2중첩을 1단으로 축소 | ① 캡처 | `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml:71`(`ItemsControl` 1단) | ✅ 소스로 판정 — 그룹 템플릿 제거 확인 |

### 기준과 다르게 가는 항목 (의도된 이탈 — V-9 대조 시 이 표를 근거로 판정)

| # | 기준 이미지 | 이번 구현 | 사유 |
|---|---|---|---|
| V1 | 상세 부제 날짜가 `2026년 9월 2일`(로케일 장식형) | `yyyy-MM-dd HH:mm` | 레포의 확립된 날짜 표기 관례다 — `HistoryDialogViewModel.cs:59`·`:60`, `ProjectSettingsDialogViewModel.cs:314`가 모두 `yyyy-MM-dd HH:mm`이고 구 `GroupCommitsByDate`도 `yyyy-MM-dd`였다. 장식형은 ko/en 문화권마다 형태가 갈려(en-US는 요일까지 붙는다) 다국어 레이아웃이 흔들린다 |
| V2 | 상세 본문에 줄번호 열 | 줄번호 없음 | 사용자 결정 Q2 — 줄 단위로 쪼개면 WinUI에서 한 줄 안에서만 드래그 선택된다(직전 요청이 "드래그 선택"이었다) |
| V3 | 아바타가 실제 프로필 사진 | 이니셜 원형(이메일 해시 색) — `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml.cs:176` `AvatarInitial` | 사용자 결정 Q1 — 로컬 `git log`로는 이미지를 얻을 수 없다 |
| V4 | 아바타 원 색이 사진마다 다름 | 팔레트 기존 4색 로테이션 — `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml:22~25` 별칭 + `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml.cs:184` `AvatarBrush` | 신규 색 리터럴을 추가하지 않는다(대장 `:70` 색 리터럴 누적 항목을 키우지 않기 위함) |

## F-8 인계 목록 (렌더 육안 확인 대기)

V-9는 `### 시각 속성` 표 27행 전건에 구현 근거(파일:라인)를 지목했고, 그중 **24행이 `⏳ 미확인`**이다 — WinUI 데스크톱 앱이라 자율 루프에 앱 기동·창 캡처 수단이 없다(`references/phase-v9-detail.md` 렌더 3분기의 「데스크톱 UI 대상」). **속성이 표대로 들어갔다는 것은 소스로 확인했고, 그 값이 기준 이미지와 같아 보이는지는 확인하지 못했다** — 폰트 크기·간격은 캡처에서 읽은 관측 추정치라 특히 그렇다.

확인 완료 3행: 날짜 그룹 헤더 제거(소스 판정) · V1 날짜 형식 · V2 줄번호 미구현(둘 다 의도된 이탈).

**사용자 확인이 필요한 것** — 앱을 실행해 카드 ⋯ → Git 상태를 열고:
1. 목록이 기준 이미지 ①처럼 보이는가(행 구분선·2줄 구성·글자 크기·아바타 지름·간격)
2. 행에 마우스를 올리면 배경이 밝아지고, 클릭하면 상세로 전환되는가
3. 상세가 기준 이미지 ②처럼 보이는가(아바타 40px·작성자·날짜·이메일·모노스페이스 본문)
4. 상세 본문 전체를 한 번에 드래그 선택할 수 있는가
5. 뒤로 버튼으로 목록에 복귀하는가

## 리뷰 이력 (plan-reviewer)

**종결 형태: 「반영 미검증 종결」** — 재호출 상한(2회)을 소진했고 **마지막 라운드 지적을 전건 반영해 미해결 잔존 0**이지만, **그 반영을 검증한 리뷰 라운드는 없다**(리뷰어가 OK를 낸 것이 아니라 고친 결과를 볼 라운드가 남지 않은 것이다). 아래 ⓐ 대조는 메인이 실물(파일·라인·명령 출력)에서 직접 확인한 결과다.

| 라운드 | 지적 | 심각도 | 처리 | 메인 실물 대조 |
|---|---|---|---|---|
| 1R | B1 `## 시각 요소 분해` 섹션 부재 | BLOCKER | 수용 — 섹션 신설 + 기준 이미지 2장을 레포에 고정 | 2R이 "닫힘" 확인. 단 1R이 근거로 든 `docs/design/uploads/1.png`·`2.png`는 **2026-07-23 이전 회차 파일**로 이번 이미지가 아니다(`ls -la` 확인) — 발동 조건 판정 자체는 옳아 지적을 수용했다 |
| 1R | B2 필수 동반 변경 `notes.md` 미편입 | BLOCKER | 수용 — T3 Files·acceptance 확장 | 2R이 "닫힘" 확인 |
| 1R | M1 `x:Uid` 부착 대상 불명확 | MAJOR | 수용 — `x:Uid` 미사용 확정(베어네임 키 + 코드비하인드) | 2R이 "대체로 닫힘" |
| 1R | m1 hit 수치 오류 / m2 대장 라인 / m3 선례 라인 / m4 baseline / m5 README 인용 | MINOR ×5 | 전건 수용·정정 | m2~m5는 2R이 "닫힘" 확인 |
| 2R | B1 `### 시각 속성` 표와 T2 본문의 시각 수치 **이중 정의**(카드형 vs 구분선형 등 5행 충돌) | BLOCKER | **수용** — 표를 **유일 정본**으로 선언하고 T2 본문에서 수치를 전부 제거(구조·구성 요소만 서술) | `plan.md` T2 「목록 뷰」·「상세 뷰」 재확인 — 배경·테두리·`CornerRadius`·폰트 크기·아바타 지름이 본문에서 사라지고 표 행을 가리키는 참조만 남았다. 표의 `상세 작성자명` 행에 굵기를 보강해 T2에서 뺀 "굵게"가 유실되지 않게 했다 |
| 2R | m1 4-D `ShowListView\|ShowFormView` hit 5 → 실측 7 (**RECURRING** — 1R m1과 같은 축이 2회 잔존) | MINOR | **수용** — 7건으로 정정하고 라인 7개를 모두 명기 | Grep 도구로 재실측: `:137`·`:142`·`:171`·`:185`·`:234`·`:240`·`:258` = **7건**. 잔존 원인은 Bash `grep \| wc -l`이 같은 파일에서 2건을 빠뜨린 것이었고(같은 사고가 `GitCommitGroup` 7→9에서도 있었다), 이후 수치는 Grep 도구로 검증했다 |
| 2R | m2 T2 `Depends on` 문구가 `x:Uid` 언급으로 stale | MINOR | 수용 — "코드의 `LocalizationService.Get()`이 참조 가능"으로 정정 | `plan.md:149` 확인 |
| 2R | m3 `HistoryDialog.xaml.cs:79`는 `OnOpened`가 아니라 **생성자** | MINOR | 수용 — 인용을 "생성자" 선례로 정정하고, 이 다이얼로그가 `OnOpened`를 택하는 사유(뷰 상태 리셋과 같은 자리)를 명시 | `sed -n '71,80p'`로 `HistoryDialog` 생성자 안의 `BackText.Text` 대입 확인 |
| 2R | m4 `GitStatusDialog.xaml:54`의 stale 주석 `<!-- 변경 파일 목록 -->` 미처리 | MINOR | 수용 — T2 본문에 정정 지시 + acceptance 3-1 신설 | `sed -n '52,58p'`로 해당 주석 실재 확인(1건) |

**사용자 판단이 필요한 사항**: 2R m1이 `RECURRING`으로 표시됐다(같은 축의 수치 오류가 2라운드 연속). 원인은 계획 내용이 아니라 **검증 도구**였고(Bash `grep` 누락 → Grep 도구로 교체) 등급이 MINOR이며 판정(패턴 재사용)도 바뀌지 않았으므로 계획을 되돌릴 사유로 보지 않았다.

## Tasks

- [x] T1. **상대시간·상세 화면 문구 resw 등록 (ko/en 양쪽)** — Type **B**
  - Files: `DevDashboard_WinUI/Strings/ko-KR/Resources.resw`, `DevDashboard_WinUI/Strings/en-US/Resources.resw`
  - 신규 8키 — **전부 코드에서 `LocalizationService.Get()`으로 소비하므로 베어네임**(접미 없음)이다(AGENTS.md 함정 3: `x:Uid` 소비만 `.Text` 접미가 필요하고, 접미를 잘못 붙이면 빌드는 통과하면서 빈 라벨이 된다). 뒤로 라벨을 `x:Uid`가 아니라 코드로 채우는 이유는 **선례가 그렇기 때문**이다 — `HistoryDialog.xaml:325`가 "아이콘과 함께 놓아야 해서 x:Uid 미사용"이라 명시하고 `.xaml.cs:79`에서 `BackText.Text`를 채운다.

    | 키 | 소비 | ko | en |
    |---|---|---|---|
    | `GitRelative_JustNow` | 코드 | `방금 전` | `just now` |
    | `GitRelative_MinuteFormat` | 코드 | `{0}분 전` | `{0} minute ago` |
    | `GitRelative_MinutesFormat` | 코드 | `{0}분 전` | `{0} minutes ago` |
    | `GitRelative_HourFormat` | 코드 | `{0}시간 전` | `{0} hour ago` |
    | `GitRelative_HoursFormat` | 코드 | `{0}시간 전` | `{0} hours ago` |
    | `GitRelative_DayFormat` | 코드 | `{0}일 전` | `{0} day ago` |
    | `GitRelative_DaysFormat` | 코드 | `{0}일 전` | `{0} days ago` |
    | `GitDetail_Back` | 코드(`LocalizationService.Get`) | `목록` | `Back` |

  - 단수/복수 키를 나눈 이유: 영어에서 "1 minutes ago"가 어색하다. 한국어는 두 키의 문구가 같지만 **양쪽 언어에 8키를 모두 등록**한다(함정 2 — 한쪽만 넣으면 다른 언어에서 빈 문자열이 되고 빌드는 통과한다).
  - Edge Cases: 빈 문자열 값 없음 / `{0}` 플레이스홀더가 양쪽 언어에 모두 존재해야 함
  - Halt Forecast: 없음(가산 등록, 기존 키 무변경)
  - **Acceptance**:
    1. `grep -c "<data name=" Strings/ko-KR/Resources.resw` = **작성 시점 447건** → 수정 후 **455건**. en-US도 447 → **455건**(대칭)
    2. `grep -c "GitRelative_\|GitDetail_Back"`가 ko/en 각 **8건**이고, **8키 모두 접미(`.Text` 등) 없는 베어네임**이다
    3. 빌드 통과(신규 경고 0)

- [ ] T2. **Git 상태 다이얼로그를 목록 + 상세 2뷰로 개편** — Type **D** · Depends on T1(문구 키가 있어야 코드의 `LocalizationService.Get()`이 참조 가능 — `x:Uid`는 쓰지 않는다)
  - Files:
    - `DevDashboard_WinUI/Domain/ValueObjects/GitCommit.cs` — `Subject` 파생 프로퍼티 추가
    - `DevDashboard_WinUI/Presentation/ViewModels/ProjectCardViewModel.cs` — `GitCommitGroups` → `GitCommits` 평면 목록 교체, `GroupCommitsByDate` 제거
    - `DevDashboard_WinUI/Presentation/Models/GitCommitGroup.cs` — **삭제**(P2에서 소비처 전수 확인 완료)
    - `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml` — 목록 restyle + 상세 뷰 신설
    - `DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml.cs` — 뷰 전환 + 표시 헬퍼
  - **Design**:
    - **배치**: 데이터 파생은 `Domain/ValueObjects/GitCommit.cs`(순수 문자열 분해라 외부 의존 없음), **표시 변환**(상대시간·이니셜·아바타 색)은 `GitStatusDialog.xaml.cs`의 **정적 헬퍼**에 둔다 — `x:Bind` 함수 바인딩은 Converter도 ThemeResource도 받지 못하므로 코드비하인드가 `string`/`Brush`를 직접 반환하는 것이 이 레포의 확립된 방식이다(P5, AGENTS.md 함정 5).
    - **신규 심볼과 책임**:
      - `GitCommit.Subject` — 메시지 첫 줄(목록 제목). **본문만 떼는 `Body`는 만들지 않는다** — 상세 뷰가 제목·본문을 나누지 않고 `Message` 전문을 한 덩어리로 그리므로 소비처가 없다(구현 중 확인, spec 리뷰 M1).
      - `ProjectCardViewModel.GitCommits` — 최신순 평면 커밋 목록(기존 `GitCommitGroups` 대체).
      - `GitStatusDialog.RelativeTime(DateTimeOffset)` — 상대시간 문자열. `GitStatusDialog.AvatarInitial(string)` — 이니셜 1글자. `GitStatusDialog.AvatarBrush(string)` — 이메일 해시로 고른 팔레트 4색 중 하나.
      - `GitStatusDialog.ShowDetail(GitCommit)` / `ShowList()` — 뷰 전환(`HistoryDialog.ShowFormView`/`ShowListView` 선례와 동형). 뒤로 라벨은 `BackText.Text`에 코드로 채운다(같은 선례).
    - **의존 방향**: `GitStatusDialog`(Presentation/View) → `ProjectCardViewModel` → `GitHelper`(Infrastructure) → `GitCommit`(Domain). 새 방향을 만들지 않는다. `GitCommit`은 `LocalizationService`를 참조하지 않는다(Domain → Infrastructure 역참조 금지 — 그래서 상대시간이 View에 있다).
    - **비추상화 선언**: 커밋 행 표시용 ViewModel 래퍼(`GitCommitRow` 등)를 만들지 않는다 — 소비처가 이 다이얼로그 하나뿐이라 래퍼는 파일만 늘리고 추적 경로를 벌린다. 상대시간 포맷터도 공용 서비스로 빼지 않는다(사용처 1곳 — 3회 미만).
  - > **시각 값의 정본은 `## 시각 요소 분해`의 `### 시각 속성` 표 하나다.** 아래 목록·상세 서술은 **구조와 구성 요소**만 정하고 배경·테두리·폰트 크기·간격·아바타 지름 같은 수치는 반복하지 않는다 — 두 곳에 적으면 서로 어긋나고 어느 쪽이 정본인지 알 수 없게 된다(acceptance 6이 그 표를 판정 기준으로 쓴다).
  - **목록 뷰**(기준 이미지 ①): 행은 `Border` + `Tapped`·`PointerEntered`·`PointerExited`·`Tag="{x:Bind}"`(행 클릭 선례). 배경·테두리·여백·hover 처리는 표의 `목록 행` 5행을 따른다(기준이 **카드형이 아니라 하단 구분선형**이라는 점이 표에 있다). 내용 2줄 —
    - 1줄: `Subject` — 한 줄 말줄임(표의 `커밋 제목` 3행)
    - 2줄: 이니셜 원형 · `AuthorName` · 가운뎃점 · 상대시간 · 가운뎃점 · `ShortHash`(표의 `메타 줄` 4행 + `단축해시` + `목록 아바타`)
    - 날짜 그룹 `ItemsControl` 중첩을 제거해 `ItemsControl` 1단으로 만든다.
    - `GitStatusDialog.xaml:54`의 stale 주석 `<!-- 변경 파일 목록 -->`을 실제 내용(커밋 목록)에 맞게 정정한다 — README:32의 같은 오기를 T3이 고치는데 코드 주석만 남으면 어긋난 서술이 그대로 남는다.
  - **상세 뷰**(기준 이미지 ②): 목록과 형제로 놓인 `Grid`(초기 `Collapsed`) —
    - 뒤로 버튼: `Button` + `FontIcon Glyph="&#xE76B;"` + `TextBlock x:Name="BackText"`. 라벨은 코드로 채운다 — `BackText.Text = LocalizationService.Get("GitDetail_Back")`(선례는 `HistoryDialog.xaml.cs:79`, **생성자** 안이다). 이 다이얼로그는 **`OnOpened`에 둔다** — 같은 자리에서 뷰 상태를 목록으로 리셋해야 해서(아래 Edge Cases) 초기화가 한곳에 모인다. `x:Uid`를 쓰지 않으므로 부착 대상 혼동·빈 라벨 위험이 없다
    - 헤더: 이니셜 원형 + `AuthorName` + `커밋일자 · 이메일` 2줄(표의 `상세 아바타`·`상세 헤더`·`상세 작성자명`·`상세 부제` 4행). 일자 형식은 `yyyy-MM-dd HH:mm`이며 기준 이미지의 로케일 장식형과 다른 이유는 **V1**에 있다.
    - 본문: `Border` > `ScrollViewer` > `TextBlock`(`TextWrapping=Wrap`, `IsTextSelectionEnabled=True`)에 **메시지 전문**(`Message`) 한 덩어리. 서체·크기·줄간격·배경은 표의 `상세 본문` 6행을 따른다. 줄번호 없음(**V2**).
  - **에러·로딩·브랜치 행은 현행 유지** — 이번 요청 범위가 커밋 목록/상세다.
  - Edge Cases:
    - 커밋 0건 → `NoCommitsText` 표시(판정을 `GitCommitGroups.Count` → `GitCommits.Count`로 교체)
    - 메시지가 한 줄뿐 → `Subject`가 그 줄 전체가 된다. 상세 본문은 `Message` 전문을 그리므로 빈 화면이 되지 않는다
    - 제목이 매우 긺 → `CharacterEllipsis` 말줄임(목록), 상세에서는 전문 표시
    - `AuthorName`이 빈 문자열 → 이니셜 `?` 폴백(인덱스 접근 예외 방지)
    - `AuthorEmail`이 빈 문자열 → 해시 0 → 4색 중 첫 색. 상세 헤더의 이메일 부분은 빈 값이면 구분점째로 생략
    - 커밋 시각이 미래(시계 오차·리베이스) → 음수 경과 → `GitRelative_JustNow`
    - 30일 초과 → 상대시간 대신 절대 날짜 `yyyy-MM-dd`(문구 키 불요)
    - 상세를 열어둔 채 다이얼로그를 닫음 → `Closing`의 `_cts` 취소 경로는 그대로. 다시 열면 `OnOpened`가 **목록 뷰로 초기화**해야 한다(뷰 상태를 매 `OnOpened`에서 리셋)
    - detached HEAD → 브랜치 라벨 `(detached HEAD)` — 현행 동작 유지
  - Halt Forecast:
    - **(ii-a) 사전 승인**: `Presentation/Models/GitCommitGroup.cs` **파일 삭제**(git 이력에 복구 경로 있음, 소비처 전수 확인 완료) + `ProjectCardViewModel.GitCommitGroups` **공개 프로퍼티 제거**(소비처 1파일 2곳, 전수 확인)
    - **(i) 사전 해소**: 아바타 색 신규 리터럴 필요 여부 → 기존 팔레트 4색만 쓰기로 확정(Investigation Log)
    - 파괴적·외부 작업 없음
  - **Acceptance**:
    1. 빌드 통과(`MSBuild -p:Platform=x64`), 오류 0 + **신규 경고 0**(baseline: 자동 생성 `XamlTypeInfo.g.cs`의 `CS0618` 1건만 — AGENTS.md의 "5건" 기록은 stale이며 대장 `:64`에 등재됨)
    2. `grep -rn "GitCommitGroup" --include=*.cs --include=*.xaml`(obj 제외) = **작성 시점 9건** → 수정 후 **0건**
    3. `grep -rn "GroupCommitsByDate"`(obj 제외) = **작성 시점 2건** → 수정 후 **0건**
    3-1. `grep -n "변경 파일" DevDashboard_WinUI/Presentation/Views/Dialogs/GitStatusDialog.xaml` = **작성 시점 1건**(`:54` stale 주석) → 수정 후 **0건**
    4. `GitStatusDialog.xaml`에 목록 뷰·상세 뷰 두 컨테이너가 `x:Name`으로 존재하고 `.xaml.cs`에 `ShowDetail`/`ShowList`가 존재한다
    5. T2가 새로 참조하는 `ThemeResource App*Brush` 키를 전건 grep해 `Palette.xaml`/`Styles.xaml`에 실재함을 확인(대장 `:76` 함정 — Color 키와 Brush 키는 별개다)
    6. `## 시각 요소 분해`의 `### 시각 속성` 표 전 행을 구현 결과와 대조해 일치 또는 「기준과 다르게 가는 항목」(V1~V4)의 의도된 이탈로 판정된다
    7. 육안(사용자): 목록이 기준 이미지 ① 형태이고, 행 클릭 시 ② 형태 상세로 전환되며, 뒤로 버튼으로 목록에 복귀한다. 상세 본문 전체를 한 번에 드래그 선택할 수 있다

- [ ] T3. **문서 갱신 — README 서술 정정 + notes 작업 기록** — Type **A** · Depends on T2(문구가 실제 산출물과 일치해야 함)
  - Files: `README.md`, `DevDashboard_WinUI/notes.md`
  - `:32` "Git 상태 다이얼로그로 변경 파일 확인" → 실제 기능(현재 브랜치 · 커밋 목록 · 커밋 상세)에 맞게 정정. **존재하지 않는 기능 서술을 제거**하는 것이며 새 절을 만들지 않는다.
  - Edge Cases: 같은 문구가 다른 줄에도 있는지 grep 확인(`:29`·`:102`는 "Git 상태"를 **더보기 메뉴 항목명**으로 언급, `:22`는 "새로고침 / Hard 새로고침으로 Git 상태 즉시 갱신"으로 **카드의 git 저장소 재탐지**를 가리킨다 — 셋 다 이번 다이얼로그 개편과 무관해 수정 대상 아님)
  - Halt Forecast: 없음
  - `notes.md` `## 최근 변경` 맨 위에 이번 회차 항목을 추가한다 — 무엇을·왜·어떤 결정을 했는지(아바타 이니셜 대체, 줄번호 제외, 날짜 그룹 제거 사유). AGENTS.md:96이 이 파일을 "작업 내역 영구 기록"으로 규정한다.
  - **Acceptance**:
    1. `grep -n "변경 파일" README.md` = **작성 시점 1건** → 수정 후 **0건**. 정정 문구가 T2 산출물과 일치(브랜치 · 커밋 목록 · 커밋 상세)
    2. `notes.md` `## 최근 변경` 최상단 항목의 날짜가 `2026-09-02`이고 본문이 T2 결정 3건(아바타·줄번호·날짜 그룹)을 담는다

## 검증 방법

```
"C:/Program Files/Microsoft Visual Studio/18/Professional/MSBuild/Current/Bin/MSBuild.exe" \
  "DevDashboard_WinUI/DevDashboard.csproj" -t:Build -p:Configuration=Debug -p:Platform=x64
```
- Phase F는 `-t:Rebuild`로 클린 리빌드.
- **경고 baseline**: 자동 생성 `obj/.../XamlTypeInfo.g.cs`의 `CS0618` 1건(실측 2026-09-02, 이 세션의 빌드 3회에서 재현). **`NU1903`(`SQLitePCLRaw.lib.e_sqlite3` 취약점 권고)은 NuGet 복원 상태에 따라 다시 출력될 수 있으므로 함께 baseline으로 인정**한다 — AGENTS.md:29~33은 baseline을 5건(`NU1903` 1 + `CS0612` 4)으로 적고 있고 그 기록의 stale 여부는 대장 `:64`에 이미 등재돼 있다. 이번 plan은 **AGENTS.md를 고치지 않으며**(별도 `pjc:record-project-fact` 대상), 판정은 "`CS0618`·`NU1903`·`CS0612`를 제외한 신규 경고 0"으로 한다.
- 테스트 없음(AGENTS.md) — 기능 확인은 사용자 육안.

## Deferred / Follow-up
- **[위키 `feat-project-launch-actions` 서술 갱신]** — `:35`의 "브랜치 + **날짜별** 커밋 목록"이 이번 개편으로 어긋난다(평면 목록 + 상세 페이지). 위키 본문 갱신은 ingest 세션 소관이므로 다음 "위키 업데이트" 때 반영.
- **[커밋 목록 개수 제한 미검토]**(P6) — `GetCommitsAsync`가 전체 이력을 가져오고 `ItemsControl`이 전부 그린다. 현행과 같은 부하라 이번에 악화되지는 않으나, 커밋 수천 건 저장소에서의 첫 렌더 비용은 측정된 바 없다. 느려지면 `git log -n <N>` 제한이나 가상화(`ItemsRepeater`) 도입 검토.
- **[아바타는 이니셜 원형 — 실제 프로필 사진 아님]** — 로컬 git으로는 이미지를 얻을 수 없어 이름 이니셜 + 이메일 해시 색으로 대체했다(사용자 결정 Q1). GitHub 연동이 생기면 실제 아바타로 승격 가능.
- **[README/스크린샷]**(deferred.md `:18`) — Git 상태 화면이 바뀌어 스크린샷이 또 낡는다. T3은 문구 1줄만 고치므로 스크린샷 갱신은 대장 유지.

## Out of Scope
- **변경 파일(working tree) 목록 표시** — 현행에 없는 기능이며 이번 요청도 커밋 목록/상세에 한한다(`GitFileStatus` 값 객체는 정의만 있고 미사용 — 위키 기록과 일치).
- **커밋 diff 표시** — 참고 이미지 ②는 커밋 메시지 전문이지 diff가 아니다.
- **커밋 메시지 줄번호** — 사용자 결정 Q2(줄 단위 분해 시 전체 드래그 선택이 불가능해진다).
- **Gravatar 등 네트워크 아바타 조회** — 사용자 결정 Q1(작성자 이메일 외부 전송).
- **브랜치 목록·전환 UI** — 현행대로 현재 체크아웃 브랜치 1개만 표시한다.

## 사전 승인 항목 (일괄 승인 대상)
- **기준 이미지 2파일 추가(계획 단계에서 이미 수행)**: `docs/design/uploads/git-status-commit-list.png`·`git-status-commit-detail.png`. 사용자가 대화로 제공한 캡처를 그대로 복사한 것으로, 후속 세션·리뷰어가 대조할 기준을 레포에 고정하기 위함이다(기존 파일 덮어쓰기 없음 — 신규 파일명). 커밋 포함 여부는 최종 커밋 승인에서 함께 결정한다.
- **파일 삭제**(T2): `DevDashboard_WinUI/Presentation/Models/GitCommitGroup.cs`. 소비처 전수 확인 완료(9 hit / 4파일, 전부 이번에 함께 수정), git 이력에 복구 경로 있음.
- **공개 프로퍼티 제거·교체**(T2): `ProjectCardViewModel.GitCommitGroups`(`IReadOnlyList<GitCommitGroup>`) → `GitCommits`(`IReadOnlyList<GitCommit>`). 소비처는 `GitStatusDialog.xaml.cs` 2곳뿐(전수 확인).
- **private 메서드 제거**(T2): `ProjectCardViewModel.GroupCommitsByDate`.
- **도메인 값 객체 가산**(T2): `GitCommit.Subject` 파생 프로퍼티(계산 전용, 생성자·직렬화 무변경).

## 불가피한 Halt (위임 불가)
- commit / push / 병합 / 태그 / 릴리즈 / PR — 구현·검증 완료 후 최종 보고에서 별도 승인.
- 계획에 없던 파괴적 작업이 필요해지는 경우.

## Open Questions
- [x] **Q1. 아바타를 어떻게 처리할 것인가** — ★A) 이니셜 원형〔근본〕(로컬 데이터만으로 재현 가능한 표현을 택해 "가져올 수 없는 것을 가져오는 척"하지 않는다) / B) Gravatar 네트워크 조회〔우회〕(작성자 이메일이 외부로 나가고 오프라인·미등록 폴백이 또 필요해 문제를 옮길 뿐) / C) 아바타 생략〔근본〕(요구된 시각 요소를 버림). → **사용자 선택: A(★와 일치)**.
- [x] **Q2. 상세 본문에 줄번호를 붙일 것인가** — ★A) 줄번호 없이 모노스페이스 블록〔근본〕(직전 요청이 "드래그 선택"이었는데 줄 단위 분해는 WinUI 제약상 그 요구를 깨뜨린다 — 두 요구가 충돌하지 않는 쪽) / B) 줄번호 포함〔우회〕(참고 이미지의 겉모습은 맞지만 선택 기능을 잃는다). → **사용자 선택: A(★와 일치)**.
- [x] **Q3. 날짜 그룹 헤더를 유지할 것인가** — ★A) 그룹 제거〔근본〕(참고 이미지 ①이 평면 목록이고, 날짜 정보는 행의 상대시간·상세의 절대 일시가 대신한다) / B) 그룹 유지〔우회〕(요청한 형태와 다른 화면이 남는다). → **사용자 선택: A(★와 일치)**.
- [x] **Q4. README:32의 어긋난 서술을 이번에 고칠 것인가** (4-E 선택 동반 변경 — 채택하지 않아도 요청은 성립) — A) 함께 수정 / B) 이번 제외(Deferred). → **사용자 선택: A → T3 신설**.

## Progress Log
- **T1 완료** (Type B): resw ko/en 각 447 → 455건(대칭), 신규 8키 전부 베어네임. spec-prefilter **PASS**.
  - V-7 연결 확인: 신규 8키의 코드 참조 0건 → **ⓑ 분기(T2가 연결 예정)**. T2에서 `LocalizationService.Get()`·`BackText.Text`로 소비된다.
  - 결정: 뒤로 라벨을 `x:Uid`가 아니라 코드로 채우기로 확정(선례 `HistoryDialog.xaml.cs:79`) — 계획 리뷰 M1 반영분.

## Phase Ledger
(구현 중 갱신)
