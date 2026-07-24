# plan.md — 카드 더보기 메뉴: 프로젝트 메모 신규 + 작업 기록 다이얼로그 시안 전면 재구현

**기준 디자인**: `docs/design/DevDashboard Redesign.dc.html` — 카드 메뉴(`:464~500`), 프로젝트 메모 다이얼로그(`:519~535`), 작업 기록 다이얼로그(`:753~880`), 유형 배지 색(`:1375~1379`), 페이지 버튼(`:1398~1413`). 사용자 렌더 이미지 3장(2026-07-24): 메모 다이얼로그 / 작업 기록 목록 / 새 기록 추가 폼.
**PRD**: `docs/prd.md` — **FR-D5**(카드 메모, 2026-07-24 신설) + **FR-H3**(작업 기록 restyle, 2026-07-24 문구 정정). 둘 다 이번 요청으로 D-10에서 갱신했다(코드 착수 전 PRD 선갱신). `## PRD Coverage` 참조.

## 요구 이해

> 원문(사용자, 2026-07-24): "다자인 시안을 확인해서 카드의 더보기 메뉴에 메모, 작업 기록 구현" (+ 렌더 이미지 3장: 메모 다이얼로그 / 작업 기록 목록 / 새 기록 추가 폼)

이해한 요구:
- **① 프로젝트 메모(신규)**: 카드 `⋯` 메뉴에 "메모" 항목을 추가하고, 누르면 시안대로 `{프로젝트명} — 메모` 다이얼로그가 열려 자유 텍스트를 저장·수정·삭제한다. 현행에 **대응 기능이 전무**하므로 도메인 필드 → DB 컬럼 → 다이얼로그 → 메뉴 순으로 새로 만든다. 메뉴 라벨은 메모 유무에 따라 "메모"/"메모 보기"로 바뀌고, 카드 본문에는 메모를 표시하지 않는다(시안 준수 — Q3).
- **② 작업 기록(기존 → 시안 전면 재구현)**: 카드 `⋯` 메뉴의 "작업 기록"은 **이미 존재**하며 `HistoryDialog`를 연다. 다만 시안과 차이가 커서(시각 HH:MM 없음, 유형이 색 배지가 아닌 dot+텍스트, 헤더 프로젝트 pill·총 건수 없음, 날짜 그룹 건수·구분선 없음, 새 기록이 뷰 전환이 아닌 인라인 패널, 페이지 번호 이동 없음) **시안대로 전면 재구현**한다(Q1-A). 적용 대상은 **카드별 `HistoryDialog`만**이고 전체 `ProjectHistoryDialog`(MainWindow 헤더)는 이번 대상이 아니다(Q2).
- **③ 시각(HH:MM) 도입**: 시안이 기록 행에 시각을 표시하고 폼에 시간 입력을 두므로 `CompletedAt`에 시각까지 담는다. 기존 기록은 시각이 00:00이므로 **등록일시(`CreatedAt`)의 시각으로 폴백** 표시한다(Q4-A) — 데이터 변형 없이 기존 기록도 의미 있는 시각을 갖는다.

## PRD Coverage

| PRD ID | 우선순위 | 대응 task | 상태 |
|---|---|---|---|
| FR-D5 카드 메모(2026-07-24 신설) | Must | T1, T2 | ✅ 커버 |
| FR-H3 작업 기록 restyle(2026-07-24 정정) | Must | T3, T4, T5, T6 | ✅ 커버 |
| FR-H1 유형(kind) 필드·선택 | Must | — | 이번 범위 외 (기구현 — T4/T5가 표시·입력만 시안화) |
| FR-H2 페이지네이션 | Must | — | 이번 범위 외 (기구현 — T6이 번호 이동 UI만 추가) |
| FR-H4 History/ProjectHistory 중복 정리 | Should | — | 이번 범위 외 (Q2로 카드별만 — Deferred 유지) |
| FR-D1~D4 (대시보드 카드) | Must/Should | — | 이번 범위 외 (기구현) |
| FR-C* / FR-S* / FR-T* / FR-E* / FR-N* | Must/Should | — | 이번 범위 외 (기구현) |
| NFR-1 (빌드 오류 0·신규 경고 0) | — | 전 task | 검증 대상(baseline 정의는 `## 검증 방법` 참조) |
| NFR-2 (계층 위반 0) | — | T1~T6 | 검증 대상(Brush는 View 코드비하인드, VM 무참조) |
| NFR-3 (하위호환 마이그레이션) | — | T1, T3 | 검증 대상(`AddColumnIfNotExists` + 기존 기록 폴백) |
| NFR-4 (다국어 ko/en 양쪽 등록) | — | T2, T4, T5, T6 | 검증 대상(신규 resw 키 다수 — 함정 2) |
| NFR-5 (테스트) | — | — | 조건 미발동(테스트 프로젝트 부재 — AGENTS.md) |

## Investigation Log (근거)

- **위키 참조**: vault 미설정 — 코드·시안 1차 출처로 진행.
- **기존 plan**: 직전 "대시보드 카드 플레이스홀더·hover·핀" plan은 `## Phase Ledger`에 **Phase G 통과(Must 100%)** 기록 → 완료로 판정, 새 계획으로 교체. 미커밋 변경 0(clean).
- **Deferred 대장 확인** (`docs/plans/deferred.md`):
  - **[GitHub 연결·프로젝트 메모 미구현]**(`:90`) — "시안 카드 메뉴(`:466-473`)의 두 항목은 현행에 대응 기능이 없어 제외(사용자 결정). 필요해지면 도메인 필드부터 논의." → **이번 T1·T2가 메모 몫을 재수용**. GitHub 연결 몫은 대장 유지(이번 범위 밖).
  - **[`HistoryEntryDialogViewModel` 고아 정리]**(`:14`) — 소스 미참조 고아. 이번 T5가 편집 폼을 다루므로 **재사용 가능성을 4-D에서 확인**(결과: 미사용 — 아래 4-D 참조). 대장 유지.
  - **[`InitKindCombo`/`KindFromCombo` 중복]**(`:15`) — HistoryDialog·ProjectHistoryDialog 2곳 대칭 중복. "3번째 사용처 생기면 공용 static helper로 추출". 이번은 3번째가 생기지 않으므로(카드별만 수정) 대장 유지.
  - **[SUGGEST] 링크 배지 색을 팔레트 근접색으로 통일 검토**(`:70`) — 시안 색 리터럴 누적 이슈. 이번 T4의 유형 배지 4색이 같은 성질이라 **대장 항목에 이번 분을 합산 기록**(Deferred 절에 명시).
  - **[테스트 상태색 팔레트 이관 검토]**(`:48`) — 동일 성질(색 상수가 코드비하인드 고정). 대장 유지.
  - 나머지 대기 항목은 이번 작업과 무관 — 대장 유지.
- **시안 명세 (직접 Read)**:
  - 카드 메뉴(`:464~500`): 실행 / 폴더 열기 / 터미널 / ─ / GitHub / **메모**(`:485`, `cardMemoLabel`) / 작업 기록(`:489`) / 프로젝트 설정 / ─ / 삭제. → 메모는 **작업 기록 바로 앞**.
  - 메뉴 라벨(`:1599`): `cardMemoLabel = 메모 있으면 '메모 보기' : '메모'`.
  - 메모 다이얼로그(`:519~535`): 폭 `min(680px,94vw)`, 제목 `{프로젝트명} — 메모`(17px/700), textarea `rows 16` placeholder "프로젝트 메모를 입력하세요…", 하단 좌측 **[메모 삭제]**(`cardMemoHas`일 때만, 위험색) + 우측 [취소][저장](저장=액센트).
  - 메모 저장(`:1607`): `saveCardMemo`가 `trim()` 후 저장. 삭제(`:1608`)는 키 제거.
  - 작업 기록 목록(`:753~825`): 헤더 "작업 기록"(19px/700) + scope pill(`historyScope`, `:1502` = 프로젝트명) + `{historyTotal}건`(`:1491` = **검색 필터 후** 건수). 검색행(검색 flex1 / [＋ 새 기록] 액센트 / 내보내기 38×38). 날짜 그룹(날짜 monospace + `{hg.count}건` + 구분선). 행(chevron 회전 + `he.time` monospace + 제목 말줄임 + 유형 배지 + 프로젝트 pill + 편집·삭제 27×27). 펼침(`border-top` + 좌측 50px 들여쓰기). 빈 상태 "검색 결과가 없습니다". 하단 우측 [닫기].
  - 유형 배지 색(`:1375~1379`): 기능 `#7ab5ec`/bg `rgba(90,163,232,.16)`, 수정 `#e8b45a`/bg `rgba(232,180,90,.16)`, 리팩터링 `#b79ae8`/bg `rgba(122,74,192,.22)`, 문서 `#2b2b31`/`#8a8890`. **미정의 유형은 '문서'(회색) 폴백**(`:1393`).
  - 새 기록 폼(`:826~876`): 목록을 **대체**하는 뷰 전환. [< 목록] + "새 기록 추가"(14.5px/700), 제목\*(placeholder "무엇을 작업했나요?", 하단 2px 액센트 밑줄), 날짜/시간 2열, (프로젝트 select는 `showProjectSelect`일 때만 — **카드별은 미표시**), 유형 select, 내용(placeholder "작업 내용을 기록해 두면 나중에 찾기 쉽습니다 (선택)"), 하단 [저장][취소] 각 flex1. 저장 버튼은 **제목이 비면 비활성**(`:1519` `saveHistStyle`).
  - 페이지 버튼(`:1398~1413`): `{1, last, cur-1, cur, cur+1}` 집합을 정렬해 표시하고 간격이 벌어지면 `…` 삽입. 현재 페이지는 액센트 배경. 좌측에 `{cur} / {total} 페이지`(`:1499`).
  - **카드 본문에 메모 표시 없음**: `projMemos` 소비처는 `:1599~1608`(메뉴 라벨·다이얼로그)뿐 — 카드 `article`에 메모 표현이 없다(grep 전수).
- **현행 구현 (직접 Read)**:
  - **카드 메뉴** `DashboardView.xaml:344~394`: `MenuFlyout`에 실행/폴더/터미널(Sub)/─/Git 상태/**작업 기록**(`:372` `CardMenu_History` → `OpenHistoryCommand`)/프로젝트 설정/─/삭제. **메모 항목 없음** → T2가 작업 기록 앞에 삽입.
  - **작업 기록 진입** `ProjectCardViewModel`: `OpenHistory()`(`:551`)가 `OpenHistoryRequested` 이벤트 발생 → `DashboardView.xaml.cs:215~218`이 `CreateHistoryDialogViewModel()`(`:591`, `EnsureHistoriesLoaded` 포함) → `new HistoryDialog(vm)` → `ShowAsync()` → `OnHistoryDialogClosed(vm)`(`:598`, `SaveToModel()` + `HistoryChanged` 발생).
  - **⚠️ 제2 진입점** `TaskPage.xaml.cs:308~314` `OnWorkLogRequested`: 작업을 완료로 옮기면 `new HistoryDialog(historyVm)` → **`dialog.OpenAddPanel(todo.Text)`** → `ShowAsync()` → `Vm.CommitWorkLog(historyVm.NewEntries)`. **`OpenAddPanel(string)` 계약을 T5가 반드시 보존**해야 한다(뷰 전환형 폼을 제목이 채워진 채로 연다).
  - **`HistoryDialog.xaml`**(235줄): 검색+추가+내보내기 행 / 인라인 추가 `Border`(Collapsed) / `ItemsControl` 날짜그룹 → 항목 `Border`(chevron·제목·유형 dot+텍스트·편집·삭제) / 펼침(설명 + 등록일시) / 하단 중앙 이전·`N / M`·다음. **시각·건수·구분선·프로젝트 pill·색 배지·번호 버튼 전부 없음**.
  - **`HistoryDialog.xaml.cs`**(256줄): 동적 Title(`HistoryDialog_TitleFormat` = "작업 기록 — {0}"), `CloseButtonText`, `OpenAddPanel`, `InitKindCombo`/`KindFromCombo`, `SaveAdd_Click`(제목 필수 검증 → `AddErrorText`), `EditEntry_Click`(**중첩 ContentDialog**를 `ShowNestedDialogAsync`로 띄워 제목·유형·설명 편집 — 날짜/시각은 편집 불가), `Export_Click`(FileSavePicker + 마크다운).
  - **`HistoryDialogViewModel.cs`**: `HistoryDateGroup`(Date/DateText/Entries), `HistoryEntryViewModel`(Title/Description/`CompletedAtText`("yyyy-MM-dd HH:mm")/`CreatedAtText`/Kind/`KindVisibility`/IsExpanded/`ExpandChevron`/툴팁), `HistoryDialogViewModel`(NewEntries/ProjectName/SaveToModel).
  - **`HistoryDialogViewModelBase.cs`**: 검색(`FilteredEntries` — Title·Description 대상), 페이지네이션(`_pageSize`·`TotalPages`·`PageInfoText`·`CanPrev/NextPage`), `RebuildGroups`(정렬 `CompletedAt.Date` desc → `CreatedAt` desc, 그룹핑 `CompletedAt.Date`), `AddEntry`/`UpdateEntry(vm,title,desc,kind,completedAt)`/`DeleteEntry`, `AvailableKinds`(`BuildKinds` = 기본 4 + 사용자 정의), `ExportToMarkdown`. **`ProjectHistoryDialogViewModel`도 이 베이스를 상속** → 베이스 변경은 전체 다이얼로그에 파급된다. 어떤 변경이 파급되고 어떤 것이 순수 가산인지는 **D10 표에서 멤버별로 확정**한다.
  - **도메인**: `HistoryEntry`(Id/Title/Description/Kind/`CompletedAt`(=DateTime, 기본 `DateTime.Today`)/`CreatedAt`(=`DateTime.Now`)). `ProjectItem`(23필드, **메모 필드 없음**).
  - **영속**: `DatabaseContext` — `Histories` 테이블(`CompletedAt TEXT NOT NULL DEFAULT ''`), 마이그레이션은 `AddColumnIfNotExists` + `AllowedIdentifiers` 화이트리스트(`:170~198`). `SqliteProjectRepository` — `DateTimeFormat = "o"`(**ISO 8601 round-trip → 시각 손실 없음, 스키마 변경 불필요**), `InsertHistories`(`:590`)가 `h.CompletedAt.ToString("o")` 저장, `GetHistories`(`:128`)가 `ParseDateTime`으로 복원. `Projects` 테이블은 `InsertProject`(`:417`)/`UpdateProject`(`:450~480`)에 컬럼이 명시 나열 → **메모 컬럼 추가 시 CREATE·마이그레이션·`AllowedIdentifiers`·INSERT·UPDATE·읽기(`ReadProjects` `:314`)를 모두 갱신해야 한다**. `HeaderColor`가 같은 방식으로 6곳에 걸쳐 추가된 선례(CREATE `:220` / 마이그레이션 `:135` / 화이트리스트 `:175` / INSERT `:423,444` / UPDATE `:459,477` / 읽기 `:321,342`)를 그대로 따른다. **가져오기(Import)는 별도 심볼이 아니라 같은 경로를 공유한다** — `ReadAllFromDb`(`:727`) → `ReadProjects`(`:314`)로 읽고 `InsertProject`(`:417`)/`DeleteByNameAndInsert`(`:811`)로 쓴다(초안의 `ImportProjectsFromDb`는 실재하지 않는 이름이라 정정).
  - **⚠️ 프로젝트 저장 = 전체 덮어쓰기 (메모 소실 경로)**: `ProjectSettingsDialogViewModel.ToProjectItem()`(`:245~270`)은 **새 `ProjectItem`을 통째로 새로 만들며** 다이얼로그가 다루지 않는 필드는 채우지 않는다(`LoadFrom`도 읽지 않는다). 이 결과가 `MainViewModel.AddOrUpdateProjectAsync`(`:456`)의 편집 분기에서 `_projectRepository.Update(item)`으로 저장되므로, **보존 처리가 없는 필드는 프로젝트 설정을 한 번 편집·저장하는 순간 조용히 초기화된다**. 실제로 같은 분기가 `CommandScripts`·`IsPinned`·`PinOrder`를 `existingModel`에서 되살리는 보존 코드를 이미 갖고 있다(`:461~466`, 직전 커밋 87dadef "편집 저장 시 핀 상태 유지"가 같은 사고를 고친 것). → **`Memo`도 같은 자리에서 보존해야 한다**(D11).
  - **부분 저장 API 선례**: `IProjectRepository`(`Domain/Repositories/`)에 `UpdatePinned(projectId, bool)`(`:43`)·`UpdatePinOrder(...)`(`:46`)·`SaveHistories(...)`(`:34`) 등 **필드/하위목록 단위 저장 메서드**가 이미 있다. 반면 `Update(ProjectItem)`(`:25`)은 구현이 태그·커맨드 스크립트를 삭제 후 재삽입한다(`SqliteProjectRepository.cs:173~189`)라 메모 한 필드를 저장하려고 쓰기엔 부작용이 크다. → 메모 전용 부분 저장 메서드를 추가한다(D12).
  - **Brush 선례**(AGENTS.md 함정 5): `x:Bind` 함수 바인딩은 Converter·ThemeResource 불가 → `TestPage.xaml.cs:32~90`이 `static readonly SolidColorBrush` 상수 + `public static Brush StatusBrush(string)` 헬퍼로 **Brush를 직접 반환**한다. T4의 유형 배지가 같은 패턴을 따른다(NFR-2: VM에 Brush 미참조).
  - **메모 다이얼로그 선례**: `TestNoteDialog`(테스트 메모, 29줄 XAML + 32줄 코드비하인드) — `ContentDialog` + `Title`/`PrimaryButtonText`/`CloseButtonText`를 코드비하인드에서 `LocalizationService.Get`으로 설정, `ResultNote` 프로퍼티로 결과 반환, `ShowAsync()` 섀도잉. **T2가 이 패턴을 그대로 따른다**(구조 동일, 삭제 버튼만 추가).
  - **팔레트**(`Palette.xaml`, Default 딕셔너리 단일): 시안 색 상당수가 이미 존재 — `#1C1C20`=`AppCardBrush`, `#222227`=`AppCardAltBrush`, `#26262C`=`AppBorderBrush`, `#2E2E34`=`AppBorderStrongBrush`, `#2B2B31`=`AppHoverFillBrush`, `#8A8890`=`AppTextTertiaryBrush`, `#B3B0B8`=`AppTextSecondaryBrush`, `#6F6D75`=`AppTextMutedBrush`, 액센트 `#F0716A`=`AppAccentBrush`. **`AppInputAltColor`(#242429)는 Color만 있고 Brush 키가 없다**(`:27` — Brush 목록 `:45~52`에 부재) → 입력 배경이 필요하면 Brush 키 1개 추가(Default에만, 함정 4 준수).
  - **resw**: `HistoryDialog_TitleFormat`·`HistorySearchBox.PlaceholderText`·`AddHistoryBtn.Content`·`HistoryAddNewHeader.Text`·`HistoryEditHeader`·`HistoryKind_None`·`HistoryPage_Prev/NextPage`·`EmptyHistoryText.Text`·`HistoryDialog_TitleRequired`·`CardMenu_History.Text` 존재. **메모 관련 키는 전무**(`TestNote_*`는 테스트 메모 전용 — 재사용 불가, 문구가 다름).
- **baseline 경고 (판정 기준 불일치 확인)**: `AGENTS.md:29~33`은 상시 경고를 **`NU1903` 1건 + `CS0612` 4건 = 5건**으로 적었으나, `docs/plans/deferred.md:64`가 이 기록을 **stale로 등재**했다(2026-07-22 클린 리빌드 실측은 `CS0618` 1건뿐). 직전 plan도 실측(`CS0618` 1건) 기준으로 Phase F를 통과했다. → 이번 판정은 `## 검증 방법`의 baseline 정의를 따르고(둘 다 baseline으로 인정), AGENTS.md 정정은 승인 항목으로 올린다.
- **AGENTS.md**: 빌드 `MSBuild x64`(`dotnet build` 금지), 함정 2(resw ko/en 양쪽 필수), 함정 3(`x:Uid` 소비는 속성 접미 필수 / 코드 `Get()` 소비는 베어네임), 함정 4(Palette Default만), 함정 5(x:Bind 함수 바인딩 → 코드비하인드 Brush 반환), 함정 10(중첩 ContentDialog 미지원 → Hide/TCS 패턴), 함정 11(공용 DataTemplate 소비처 확인), DB 규약(`AddColumnIfNotExists`·`HasColumn` 가드·**INSERT 컬럼 추가 시 파라미터 값 갱신**).

### 4-A. 심볼/타입 추적 (전수 grep 후 해당 파일 Read)

| 변경 대상 | 사용처 전수 | 처리 |
|---|---|---|
| `HistoryDialog`(클래스) | ① `DashboardView.xaml.cs:215~218`(카드 메뉴 진입) ② **`TaskPage.xaml.cs:308~314`(작업 완료 → 작업기록 팝업, `OpenAddPanel` 호출)** — grep `new HistoryDialog` 결과 2건 전부 | T5가 `OpenAddPanel(string)` 시그니처·동작을 보존(D9). ①은 무변경 |
| `HistoryDialog.OpenAddPanel` | `TaskPage.xaml.cs:312` 1건 | 위와 동일 — 내부 구현만 폼 전환으로 교체 |
| `HistoryDialogViewModel` | `ProjectCardViewModel.cs:591`(생성), `DashboardView.xaml.cs:215`, `TaskPage.xaml.cs:310` | 생성자 시그니처 무변경. T3·T4가 프로퍼티만 가산 |
| `HistoryDialogViewModelBase`(추상) | **상속 2개**: `HistoryDialogViewModel`(카드별) · `ProjectHistoryDialogViewModel`(전체) | T3·T6이 손대므로 **전체 다이얼로그 파급 여부를 멤버별로 판정**(D10 표) |
| `HistoryEntryViewModel` | 생성은 베이스 `RebuildGroups:118` 1곳, 소비는 `HistoryDialog.xaml:106`·`ProjectHistoryDialog.xaml:118` DataTemplate 2곳 | 생성자에 프로젝트명 인자 추가(D13) — 생성처가 1곳이라 파급 국소. **DataTemplate 2곳은 공용 리소스가 아니라 각 파일에 독립 정의**(함정 11 확인 — T4가 카드별만 고쳐도 전체 다이얼로그는 불변) |
| `HistoryDialogViewModelBase`(생성자) | `base(...)` 호출 2곳: `HistoryDialogViewModel.cs:98`, `ProjectHistoryDialogViewModel.cs:25` | D13이 인자 1개를 추가하므로 **둘 다 갱신** — T3 Files에 두 파일 모두 포함 |
| `ProjectItem`(엔티티) | 영속 6경로(CREATE·마이그레이션·화이트리스트·INSERT·UPDATE·읽기) + **재구축 경로 `ProjectSettingsDialogViewModel.ToProjectItem`** + 병합 지점 `MainViewModel.AddOrUpdateProjectAsync` | T1이 영속 6경로, D11이 재구축 소실 방어 |
| 카드 `MenuFlyout` | `DashboardView.xaml:344~394` 1곳(카드 템플릿 내부) | T2가 항목 1개 삽입 |

### 4-B. 계약·직렬화 변경

- **DB 스키마**: `Projects`에 `Memo TEXT NOT NULL DEFAULT ''` **가산**. 기존 컬럼·데이터 무변경, 구버전 DB는 `AddColumnIfNotExists`로 자동 마이그레이션(NFR-3). 읽기는 `HasColumn` 가드. 별도 마이그레이션 task 불요.
- **`HistoryEntry.CompletedAt` 의미 확장**: 종전 "날짜(00:00)" → "날짜+시각". **직렬화 형식은 불변**(`DateTimeFormat="o"`가 이미 시각을 왕복 — `SqliteProjectRepository.cs:14`), 스키마 변경 없음. 기존 값은 그대로 읽히고 표시 단계에서 폴백(D5).
- **`IProjectRepository`(공개 인터페이스)**: 메서드 1개 **가산**(D12). 구현체는 `SqliteProjectRepository` 1개(grep 전수 — 다른 구현·목 없음)라 파급이 닫혀 있다. 공개 API 변경이므로 사전 승인 항목에 등재.
- **`HistoryEntryViewModel` 생성자**: 인자 1개 추가(D13). 생성처 1곳(베이스)이라 **호출부 누락**은 컴파일 시점에 잡힌다. 다만 컴파일러가 잡지 못하는 것이 **초기화 순서**다 — 넘길 값을 파생 필드에서 읽으면 베이스 생성자 실행 시점에 그 필드가 아직 null이라 런타임에만 터진다(D13이 베이스 자기 필드로 해결).
- **`HistoryDialogViewModelBase` 생성자**: 인자 1개 추가(D13). 파생 2곳(`HistoryDialogViewModel`·`ProjectHistoryDialogViewModel`)의 `base(...)` 호출을 함께 고친다 — 둘 다 T3 Files에 있다.

### 4-C. 영향 받는 테스트

- 테스트 프로젝트 **부재**(AGENTS.md "Test: 없음"). 변경 대상을 호출하는 자동 테스트 0건. 신규 테스트 추가는 사용자 승인 사항이라 이번 범위 밖(NFR-5 조건 미발동) — 검증은 빌드 + 회귀 grep + 육안 확인으로 대체한다.

### 4-D. 재사용 확인

| 신규 심볼 | 유사 기존 구현 검색 결과 | 재사용/신규 사유 |
|---|---|---|
| (T1) `ProjectItem.Memo` | `ProjectItem` 23필드 전수 확인 — 메모/노트 성격 필드 없음. `TestItem.ProgressNote`는 테스트 전용 하위 엔티티 | **신규 필드 1개** — 대체 가능한 기존 필드 없음(`Description`은 카드 본문 표시용으로 이미 다른 의미) |
| (T2) `ProjectMemoDialog` | `TestNoteDialog`(테스트 메모)가 구조상 최근접 — ContentDialog + 단일 TextBox + Save/Cancel | **신규 클래스** — 대상 엔티티·문구·삭제 버튼이 달라 `TestNoteDialog` 재사용 불가. 단 **구현 패턴(코드비하인드 Title 설정·`ResultXxx`·`ShowAsync` 섀도잉)은 그대로 따른다** |
| (T2) 카드 메모 커맨드·라벨 | `ProjectCardViewModel`에 `OpenHistoryCommand`(이벤트 발생 → View가 다이얼로그 표시) 패턴 존재(`:551`) | **재사용(동일 패턴 확장)** — `OpenMemoCommand` + `OpenMemoRequested` 이벤트로 같은 흐름 |
| (T2) 메모 부분 저장 `UpdateMemo` | `IProjectRepository.UpdatePinned`(`:43`)·`UpdatePinOrder`(`:46`)·`SaveHistories`(`:34`) = 필드/하위목록 단위 저장 선례 다수 | **신규 메서드 1개** — 기존 `Update(ProjectItem)`은 태그·스크립트를 삭제 후 재삽입(`SqliteProjectRepository.cs:173~189`)해 메모 한 필드 저장에 부적합. 선례와 같은 모양의 부분 저장 추가(D12) |
| (T4) 행의 프로젝트명 | `HistoryDialogViewModelBase.ExportProjectName`(protected abstract, 내보내기 제목용)이 두 파생에서 프로젝트명을 제공하나 **파생 필드에 의존해 베이스 생성자 시점엔 쓸 수 없다** | **베이스 생성자 인자 + 베이스 필드로 보관**해 `RebuildGroups`가 그 필드를 항목 VM에 넘긴다(D13). ⚠️ `ExportProjectName`을 그대로 넘기면 NRE — 폐기된 초안이다. 신규 심볼은 베이스 인자·필드·`protected` 세터와 항목 VM의 `ProjectName` |
| (T3) 시각 표시 프로퍼티 | `HistoryEntryViewModel.CompletedAtText`("yyyy-MM-dd HH:mm")·`CreatedAtText` 존재하나 **행 표시용 HH:MM 단독 포맷은 없음** | **신규 프로퍼티 1개**(`TimeText`) — 기존 두 프로퍼티는 포맷이 달라 재사용 불가 |
| (T4) 유형 배지 Brush | `TestPage.xaml.cs`의 `static Brush StatusBrush(string)` 선례(상태 3종) — **다른 화면·다른 도메인 값**이라 직접 호출 불가 | **신규 static 헬퍼 2개**(`KindBrush`/`KindBackBrush`, HistoryDialog 코드비하인드) — 선례와 같은 패턴, 소비처 1곳이라 공용 유틸로 추상화하지 않음 |
| (T4) pill·배지 스타일 | `Styles.xaml`의 `TagBadgeStyle`(`:241`)이 태그 pill용 — 카드 태그 전용 소비 | **인라인 Border 사용** — `TagBadgeStyle`은 카드 태그 소비처가 있어(함정 11) 치수·색이 다른 기록 pill을 위해 수정하지 않는다. 2회 이상 반복되는 pill(유형·프로젝트)만 이 다이얼로그 안의 로컬 스타일로 묶는다 |
| (T5) 새 기록/편집 폼 | **`HistoryEntryDialogViewModel`**(`Presentation/ViewModels/`) 존재 — deferred `:14`가 "소스 미참조 고아"로 등재 | **미사용 확정** — grep 전수 결과 소비처 0이고 뷰 전환형 폼(제목/날짜/시각/유형/내용 + 저장 활성 조건)과 구조가 맞지 않는다. 되살리지 않고 **폼 상태는 코드비하인드가 직접 관리**(현행 `OpenAddPanel`·`SaveAdd_Click`과 동일 방식). 고아 정리는 대장 유지 |
| (T6) 페이지 번호 목록 | 베이스에 `TotalPages`·`CurrentPage`·`CanPrev/NextPage` 존재, **번호 목록은 없음** | **신규 프로퍼티 1개**(`PageButtons`, 베이스에 **가산**) — 베이스를 상속하는 `ProjectHistoryDialogViewModel`은 이 프로퍼티를 소비하지 않으므로 기존 동작 불변 |

## 시각 요소 분해

> ⚠️ **라인 참조는 ±3줄 오차가 있을 수 있다**(시안 HTML이 갱신되며 밀림 — 직전 plan에서도 같은 지적). 각 행의 "디자인 값"에 실제 속성을 적어 두었으니 **라인이 어긋나면 서술 내용으로 대상을 찾는다**.
>
> 기준: 시안 인라인 스타일 + 렌더 이미지 3장(2026-07-24). CSS px는 WinUI 논리 단위로 옮긴다. 시안 보라 액센트(`#8b7cf7`)는 앱 액센트 산호(`AppAccentBrush`)로 대체한다(기존 결정 — deferred `:89`). **유형 배지 4색은 예외로 시안 값 그대로**(Q3 — 강조색이 아니라 유형 구분색). 최종 판정은 빌드 후 육안 대조(⏳ HUMAN-VERIFY).

### 메모 다이얼로그 (시안 `:519~535`)

| 요소 | 속성 | 디자인 값 | XAML 대응 수단 | 확인 방법 |
|---|---|---|---|---|
| 컨테이너 | 폭 | `min(680px, 94vw)` | `ContentDialogMinWidth`/`MaxWidth` = 680 리소스 오버라이드 | HTML `:521` |
| 제목 | 문구·크기 | `{프로젝트명} — 메모`, 17px/700 | `Title` = `string.Format(resw)` (ContentDialog 기본 제목 스타일) | HTML `:522`, 렌더 2.png |
| 입력 | 형태 | `rows 16`, bg `#242429`, border 1px `#2e2e34`, radius 10, padding 12, 13.5px | `TextBox AcceptsReturn MinHeight≈320 TextWrapping=Wrap` + 스크롤 Auto | HTML `:523` |
| 입력 | placeholder | "프로젝트 메모를 입력하세요…" | `PlaceholderText` = resw(베어네임, 코드비하인드 — 함정 3) | HTML `:523`, 렌더 2.png |
| 삭제 버튼 | 조건·색 | 메모가 있을 때만, 좌측, 테두리 `rgba(240,113,106,.4)` + bg `rgba(240,113,106,.1)` + 글자 `#f0716a` | `Button`(위험색) — `Visibility`로 조건부. 색은 `AppAccentSoftBrush`(#29F0716A)·`AppDangerBrush` 근사 | HTML `:526` |
| 하단 버튼 | 배치 | 좌 [메모 삭제] · spacer · [취소] [저장], h42 radius 10 | `ContentDialog` Primary=저장/Close=취소 + 삭제는 `Content` 하단 좌측에 배치 | HTML `:525~531`, 렌더 2.png |
| 저장 버튼 | 강조 | 액센트 배경 | `PrimaryButtonStyle` = `AccentButtonStyle` | HTML `:530` |

### 작업 기록 — 목록 화면 (시안 `:753~825`, 렌더 4.png)

| 요소 | 속성 | 디자인 값 | XAML 대응 수단 | 확인 방법 |
|---|---|---|---|---|
| 컨테이너 | 크기 | `min(760px,94vw)` × `min(680px,90vh)`, padding 28 | `ContentDialogMin/MaxWidth` 760 · `Min/MaxHeight` 680 (현행 800×700에서 변경) | HTML `:755` |
| 헤더 | 구성 | "작업 기록"(19px/700) + 프로젝트 pill + "N건" | `StackPanel Horizontal Spacing=10`을 `Content` 최상단에 배치(ContentDialog `Title` 미사용으로 전환) | HTML `:756~760`, 렌더 4.png |
| 헤더 pill | 형태 | 12px `#b3b0b8`, bg `#26262c`, radius 7, padding 4/10 | `Border Background=AppBorderBrush CornerRadius=7 Padding=10,4` + `TextBlock` 12 | HTML `:758` |
| 헤더 건수 | 값 | `{historyTotal}건` — **검색 필터 적용 후** 건수 | VM 신규 프로퍼티(필터 결과 수) + resw 포맷 | HTML `:759` + `:1491` |
| 검색행 | 배치 | 상단 여백 16, gap 10: 검색(flex1) / [＋ 새 기록] / 내보내기 | 현행 `Grid` 3열 유지(`*`/Auto/Auto) + Spacing 10 | HTML `:762~772` |
| 검색박스 | 형태 | h38, bg `#242429`, border `#2e2e34`, radius 9, 아이콘 14 | 현행 `AutoSuggestBox QueryIcon=Find` 유지 + Height 38 | HTML `:764~767` |
| 새 기록 버튼 | 형태·문구 | h38 padding 0/15 radius 9, 액센트 배경, 12.5px/700, "＋ 새 기록" | `Button Style=AccentButtonStyle` + resw 문구 갱신 | HTML `:768`, 렌더 4.png |
| 내보내기 | 형태 | 38×38 radius 9, border `#2e2e34`, bg `#242429`, 아이콘 14 | 현행 `Button`+`FontIcon` 유지 + 크기·radius 지정 | HTML `:769~771` |
| 날짜 그룹 헤더 | 구성 | 날짜(12.5px/700 `#c9c6ce`, monospace) + "N건"(11px `#8a8890`) + 구분선(1px `#26262c`, 남는 폭) | `Grid` 3열: `TextBlock`(Consolas) / `TextBlock` / `Rectangle Fill=AppBorderBrush Height=1` | HTML `:781~785`, 렌더 4.png |
| 기록 행 카드 | 형태 | bg `#222227`, border 1px `#2a2a30`, radius 10 | `Border Background=AppCardAltBrush BorderBrush=AppBorderBrush CornerRadius=10` | HTML `:787` |
| 행 | 배치 | padding 12/14, gap 11, hover bg `#26262b` | `Grid ColumnSpacing=11 Padding=14,12` + `PointerEntered/Exited` 배경 교체 | HTML `:788` |
| chevron | 회전 | 11px `#8a8890`, 접힘 0° → 펼침 90° | `FontIcon` 글리프 교체(현행 `ExpandChevron` 유지 — WinUI에 회전 트랜지션 미도입) | HTML `:789~791` |
| 시각 | 형태 | 11.5px `#8a8890`, monospace, 축소 안 함 | `TextBlock FontFamily=Consolas FontSize=11.5 Foreground=AppTextTertiaryBrush` | HTML `:792`, 렌더 4.png |
| 제목 | 형태 | 13.5px/500, 남는 폭 채우고 말줄임 | `TextBlock TextTrimming=CharacterEllipsis` (Grid `*` 열) | HTML `:793` |
| 유형 배지 | 형태 | 11px/700, radius 6, padding 3/9, 유형별 색+연한 배경 | `Border` + `TextBlock`, 색은 코드비하인드 static `KindBrush`/`KindBackBrush`(함정 5) | HTML `:794` + `:1375~1379` |
| 프로젝트 pill | 형태 | 11.5px `#8a8890`, bg `#2b2b31`, radius 6, padding 3/9 | `Border Background=AppHoverFillBrush CornerRadius=6` + `TextBlock` | HTML `:795`, 렌더 4.png |
| 편집·삭제 | 형태 | 27×27 radius 7, 평소 `#6f6d75`, hover(편집 회색배경/삭제 위험색) | `Button Width=27 Height=27 CornerRadius=7` + `CardIconButtonStyle` 계열 | HTML `:796~801` |
| 펼침 상세 | 형태 | 상단 구분선 1px `#26262c`, padding `0 14 13 50`(좌 들여쓰기 50), 12.5px `#98959d`, 행간 1.65 | `Border BorderThickness=0,1,0,0` + `TextBlock Margin=50,11,14,13 LineHeight` | HTML `:803~805`, 렌더 4.png |
| 빈 상태 | 문구·여백 | "검색 결과가 없습니다", 상하 60, `#6f6d75` 13px | 현행 `EmptyText` 문구·여백 갱신 | HTML `:777` |
| 하단 | 배치 | 우측 [닫기] h40 padding 0/40 radius 10 | `ContentDialog CloseButtonText` 유지(현행과 동일) | HTML `:877~879`, 렌더 4.png |
| 하단 [닫기] | **조건** | **목록 화면에서만 표시** — 폼 화면에서는 사라지고 [저장][취소]가 대신한다 | `CloseButtonText`는 Content 밖 고정 버튼 → 폼 진입 시 빈 문자열, 목록 복귀 시 복원 | HTML: 닫기 블록·검색행이 모두 `sc-if histList` 안 |
| 검색행·목록·페이지 | **조건** | **목록 화면에서만 표시**(폼 화면에서 전부 숨김) | 목록 영역 전체를 하나의 `Grid`로 묶어 `Visibility` 토글 | HTML: `sc-if histList` 범위 |

### 작업 기록 — 새 기록/편집 폼 (시안 `:826~876`, 렌더 3.png)

| 요소 | 속성 | 디자인 값 | XAML 대응 수단 | 확인 방법 |
|---|---|---|---|---|
| 전환 | 방식 | 목록 영역을 **폼이 대체**(헤더는 유지) | 목록 `Grid`/폼 `Grid`의 `Visibility` 토글 | HTML `:825`↔`:826` |
| 뒤로 | 형태·문구 | [‹ 목록] h32 padding 0/12 radius 8, border `#2e2e34`, bg `#242429`, 12px | `Button` + `FontIcon`(ChevronLeft) + resw "목록" | HTML `:829~832`, 렌더 3.png |
| 폼 제목 | 형태 | "새 기록 추가" 14.5px/700 | `TextBlock` (편집 시 "기록 수정" — 기존 `HistoryEditHeader` 재사용) | HTML `:833` |
| 라벨 | 형태 | 12.5px `#b3b0b8`, 입력과 gap 7 | `InputLabelStyle`(12px·저강도, `Styles.xaml:262`) 재사용 | HTML `:835~` |
| 제목 입력 | 필수 표시 | 라벨에 빨간 `*`, 입력 하단 2px 액센트 밑줄(**입력 여부와 무관하게 항상**), h38 radius 8 | `TextBlock` 2조각(라벨 + `*` 액센트색) + `TextBox` + 하단 `Rectangle`(액센트, 상시) | HTML `:834~836`(`border-bottom:2px solid #f0716a` 무조건), 렌더 3.png |
| 제목 | placeholder | "무엇을 작업했나요?" | `PlaceholderText` = resw | HTML `:836` |
| 날짜·시간 | 배치 | 2열 grid, gap 14, 각 h38 | `Grid` 2열 + `CalendarDatePicker` / `TimePicker` | HTML `:838~845`, 렌더 3.png |
| 프로젝트 | 조건 | `showProjectSelect`일 때만 | **미표시**(카드별 다이얼로그는 프로젝트 고정 — Q2) | HTML `:846~852` |
| 유형 | 형태·위치 | select h38. **날짜·시간과 같은 2열 그리드 안**에 놓인다(프로젝트 select가 없는 카드별에서는 날짜/시간 다음 칸) | 현행 `ComboBox` + `InitKindCombo` 재사용(미분류 + `AvailableKinds`)을 2열 그리드의 3번째 칸에 배치 | HTML `:853~862`(같은 `grid-template-columns:1fr 1fr` 내부), 렌더 3.png |
| 내용 | 형태·문구 | `rows 6`, radius 8, placeholder "작업 내용을 기록해 두면 나중에 찾기 쉽습니다 (선택)" | `TextBox AcceptsReturn MinHeight≈120` + resw placeholder | HTML `:866~868` |
| 하단 버튼 | 배치·상태 | [저장][취소] 각 flex1 h42 radius 10. **제목 비면 저장 비활성**(회색 `#3a3a41`/`#8a8890`) | `Grid` 2열 균등 + `Button IsEnabled`를 제목 TextChanged로 갱신 | HTML `:871~874` + `:1519`, 렌더 3.png |

### 작업 기록 — 페이지네이션 (시안 `:807~823`, `:1398~1413`)

| 요소 | 속성 | 디자인 값 | XAML 대응 수단 | 확인 방법 |
|---|---|---|---|---|
| 영역 | 배치 | 상단 구분선 1px `#26262c`, 여백 14, gap 6. **버튼 묶음은 가로 중앙**(`justify-content:center`), 정보는 좌측으로 밀림(`margin-right:auto` + 끝 spacer) | `Border BorderThickness=0,1,0,0` + `Grid` 3열(정보 `Auto` 좌 / 버튼 `*` 중앙 / 더미 `Auto`) | HTML `:809` |
| 페이지 정보 | 위치·문구 | **좌측 끝**, "N / M 페이지", 11.5px `#8a8890` | `TextBlock` 좌측 정렬 + resw 포맷 | HTML `:810` + `:1499` |
| 이전·다음 | 형태 | 32×32 radius 8, border `#2e2e34`, bg `#242429`, 비활성 시 `#4a4850` | 현행 `Button`+`FontIcon` 크기·radius 지정, `IsEnabled` 유지 | HTML `:811`, `:1497~1498` |
| 번호 버튼 | 규칙 | `{1, 마지막, 현재±1}` 집합 정렬, 간격 벌어지면 `…` 삽입 | VM `PageButtons`(번호/생략 항목) + `ItemsControl` | HTML `:1398~1413` |
| 번호 버튼 | 형태 | min-w 32 h32 radius 8, 현재=액센트 배경, 그 외 bg `#242429`/글자 `#b3b0b8` | `Button` + 현재 페이지만 `AccentButtonStyle` 계열 | HTML `:1409` |

## Decisions

- **D1 (메모는 `ProjectItem`의 신규 필드 + `Projects` 컬럼)**: 메모를 `ProjectItem.Memo`(string, 기본 빈 문자열)로 두고 `Projects` 테이블에 `Memo TEXT NOT NULL DEFAULT ''` 컬럼을 `AddColumnIfNotExists`로 추가한다. *Source*: 메모는 프로젝트 1:1 속성이라 별도 테이블(1:N)이 과하고, `ProjectItem`은 이미 Aggregate Root로 `HeaderColor` 등 단순 스칼라를 같은 방식으로 보유(`DatabaseContext.cs:135` 선례). NFR-3 하위호환은 화이트리스트 마이그레이션으로 충족.
- **D2 (메모 다이얼로그는 `TestNoteDialog` 패턴의 신규 ContentDialog)**: `ProjectMemoDialog`를 새로 만들되 구조·관례는 `TestNoteDialog`를 그대로 따른다(코드비하인드에서 `Title`/버튼 문구 설정, `ResultMemo` 프로퍼티, `ShowAsync()` 섀도잉). *Source*: 두 다이얼로그는 대상 엔티티·문구·삭제 버튼 유무가 달라 한 클래스로 합치면 분기가 늘어난다(재사용 조사 4-D). 패턴만 따르면 학습 비용 0.
- **D3 (메모 삭제 = 명시 버튼 + 빈 저장 둘 다 허용)**: 시안대로 메모가 있을 때만 [메모 삭제] 버튼을 노출하고, 그와 별개로 **입력을 비우고 저장해도 메모가 지워진다**(저장 시 `Trim()` 결과가 빈 문자열이면 빈 값 저장 = 삭제와 동일 결과). *Source*: 시안 `:526`(삭제 버튼)과 `:1607`(`saveCardMemo`의 `trim()`)이 모두 존재하며 저장 경로가 빈 문자열을 그대로 받는다. `TestNoteDialog`도 "비워 두고 저장하면 삭제" 규칙을 쓴다(문구로 안내) — 앱 내 일관.
- **D4 (메뉴 라벨 조건부 = resw 2키 + 코드비하인드 갱신)**: `⋯` 메뉴의 메모 항목 텍스트를 메모 유무에 따라 "메모"/"메모 보기"로 바꾼다. *Source*: 시안 `:1599`. `MenuFlyout`은 `Opening` 이벤트가 이미 다른 곳에서 쓰이는 패턴(`CmdFlyout_Opening`, `DashboardView.xaml:418`)이라 같은 방식으로 열릴 때 라벨을 갱신한다 — 카드 본문 표시가 없으므로(Q3) 라벨이 유일한 메모 유무 표식이다.
- **D5 (시각은 `CompletedAt`에 저장, 표시 폴백은 `CreatedAt`)**: `CompletedAt`에 날짜+시각을 함께 담고(스키마 변경 불필요 — `DateTimeFormat="o"`), 행 표시 시 **`CompletedAt`의 시각이 00:00이면 `CreatedAt`의 시각**을 보여준다. *Source*: Q4-A(사용자 결정) + `SqliteProjectRepository.cs:14`의 round-trip 포맷 확인. 기존 기록은 전부 00:00이라 폴백이 없으면 모두 "00:00"으로 보인다. 데이터는 변형하지 않는다(마이그레이션으로 값을 덮어쓰면 되돌릴 수 없음).
- **D6 (유형 배지 색은 시안 값 그대로, 코드비하인드 static 상수)**: 기능 `#7AB5EC`/수정 `#E8B45A`/리팩터링 `#B79AE8`/문서(및 그 외 전부) 회색을 `HistoryDialog.xaml.cs`의 `static readonly SolidColorBrush`로 두고 `KindBrush(string)`/`KindBackBrush(string)`가 반환한다. *Source*: Q3(사용자 결정) + 시안 `:1375~1379`(미정의 유형은 '문서' 폴백) + AGENTS.md 함정 5(x:Bind 함수 바인딩은 Converter·ThemeResource 불가) + `TestPage.xaml.cs:32~90` 선례. 팔레트에 보라 계열이 없어 신규 색이지만 **전역 액센트가 아니라 유형 구분색**이라 deferred `:89`(시안 보라 액센트 미채택)와 충돌하지 않는다.
- **D7 (뷰 전환은 `Visibility` 토글, 폼 상태는 코드비하인드)**: 목록 영역과 폼 영역을 같은 `ContentDialog` 안에 두고 `Visibility`로 교체하며, 폼 입력 상태는 현행처럼 코드비하인드가 직접 다룬다(`HistoryEntryDialogViewModel`은 되살리지 않는다). *Source*: 4-D 조사 — 그 VM은 소비처 0의 고아이고 구조가 맞지 않는다. 현행 `OpenAddPanel`/`SaveAdd_Click`이 이미 코드비하인드 방식이라 전환 비용이 가장 낮고, 중첩 ContentDialog 제약(함정 10)을 폼 내장으로 회피한다.
- **D8 (편집도 같은 폼으로 통합 — 중첩 다이얼로그 제거)**: 현행 편집은 `ShowNestedDialogAsync`로 중첩 ContentDialog를 띄워 제목·유형·설명만 고친다(날짜·시각 편집 불가). 이를 **새 기록과 같은 뷰 전환 폼**으로 통합해 날짜·시각까지 편집한다. *Source*: 시각 필드가 생기면(D5) 편집 경로에도 시각 수정 수단이 필요한데, 중첩 다이얼로그를 유지하면 `Hide`→재표시 왕복(함정 10)이 폼 하나 더 늘어난다. 통합 시 `ShowNestedDialogAsync`는 **내보내기 오류 표시용으로 계속 필요**하므로 제거하지 않는다.
- **D9 (`OpenAddPanel(string)` 계약 보존)**: `TaskPage`의 작업 완료 → 작업기록 팝업 경로가 이 메서드를 호출하므로 시그니처와 "제목이 채워진 채 새 기록 입력 상태로 열린다"는 동작을 유지한다(내부 구현만 뷰 전환 폼으로 교체). *Source*: `TaskPage.xaml.cs:310~312` 직접 확인(호출부 전수 grep 결과 이 1곳). 계약을 바꾸면 작업 페이지의 완료 흐름이 조용히 깨진다.
- **D10 (베이스 뷰모델 변경의 파급을 멤버별로 확정)**: `HistoryDialogViewModelBase`는 카드별·전체 두 다이얼로그가 공유하므로(상속 2개), 이번 변경을 **"카드별에만 보이는 가산"과 "두 화면에 함께 적용되는 개선"으로 나눠 명시**한다. Q2("카드별만")는 **UI 재구현의 범위**를 뜻하며, 공유 로직의 정확도 개선까지 갈라놓는다는 뜻이 아니다 — 갈라놓으려면 정렬 로직을 파생별로 이중화해야 해서 오히려 결함 위험이 커진다.

  | 베이스 변경 | 성격 | 전체 다이얼로그(`ProjectHistoryDialog`)에 미치는 영향 |
  |---|---|---|
  | `FilteredCount` 추가 | 순수 가산 | 없음(미소비) |
  | `PageButtons`·`GoToPage` 추가 | 순수 가산 | 없음(미소비 — XAML 무변경) |
  | `HistoryEntryViewModel` 생성자에 프로젝트명 인자 추가 | 가산(생성처 1곳) | 없음(전체 다이얼로그 템플릿이 새 프로퍼티를 바인딩하지 않음) |
  | `RebuildGroups` **정렬**을 `CompletedAt` 전체(날짜+시각) 기준으로 — 그룹 간(`:101~104`)·**그룹 내부(`:118`)** 둘 다 | **공유 동작 변경** | **의도된 파급** — 같은 날 기록이 시각 순으로 정렬된다. 시각 없는 기존 기록은 00:00로 동률이라 `CreatedAt` 2차 정렬이 종전 순서를 유지 |
  | `ExportToMarkdown`에 완료 시각 추가 | **공유 동작 변경** | **의도된 파급** — 전체 내보내기에도 시각이 찍힌다(정보 추가, 손실 없음) |

  *Source*: 상속 관계(`ProjectHistoryDialogViewModel.cs:8`)와 소비처(`ProjectHistoryDialog.xaml.cs`의 `ExportToMarkdown` 호출, `ProjectHistoryDialog.xaml`의 자체 DataTemplate) 직접 확인. **표시 순서를 실제로 결정하는 것은 `:118`의 그룹 내부 정렬**이므로 여기를 바꾸지 않으면 시각 정렬이 성립하지 않는다(초안 누락 — plan-reviewer B2). 두 공유 변경은 전체 다이얼로그를 **깨뜨리지 않고 같은 방향으로 개선**하므로 되돌리지 않고 회귀 확인 대상으로 둔다. `UpdateEntry`는 이미 `completedAt`을 받으므로 시각 전달에 시그니처 변경이 불필요하다(확인 완료).
- **D11 (설정 편집 저장 시 메모 보존은 `MainViewModel` 병합)**: `AddOrUpdateProjectAsync` 편집 분기에서 `existingModel.Memo`를 결과 `item`에 되살린다. *Source*: `ProjectSettingsDialogViewModel.ToProjectItem()`(`:245~270`)이 **새 `ProjectItem`을 통째로 새로 만들고** `Memo`를 채우지 않아, 보존이 없으면 프로젝트 설정을 한 번 저장하는 순간 메모가 사라진다. 같은 분기가 **이미 `CommandScripts`·`IsPinned`·`PinOrder`를 같은 방식으로 보존**하고 있어(`:461~466`, 직전 커밋 87dadef가 핀 소실을 고친 것) 한 줄 추가가 가장 국소적이고 선례와 일관하다. 대안(다이얼로그 VM에 메모를 왕복시키기)은 설정 다이얼로그에 메모 UI가 없는데도 필드를 들고 다니게 만들어 더 나쁘다.
- **D12 (메모 저장은 `IProjectRepository.UpdateMemo` 부분 저장)**: 인터페이스에 `void UpdateMemo(string projectId, string memo)`를 추가하고 카드 VM이 이를 호출한다. *Source*: `UpdatePinned`(`:43`)·`UpdatePinOrder`(`:46`) 등 **필드 단위 저장 선례**가 이미 있고, 대안인 `Update(ProjectItem)`은 구현이 태그·커맨드 스크립트를 삭제 후 재삽입해(`SqliteProjectRepository.cs:173~189`) 메모 한 필드 저장에 부작용이 크다. 구현체는 1개(전수 확인)라 파급이 닫혀 있다. 공개 인터페이스 변경이므로 사전 승인 항목에 올린다.
- **D13 (행의 프로젝트명은 베이스가 보유한 값을 항목 VM에 주입 — 초기화 순서 안전한 형태)**: 베이스 생성자에 `string entryProjectName` 인자를 추가해 **베이스 자신의 필드**로 보관하고, `RebuildGroups`가 `new HistoryEntryViewModel(entry, _entryProjectName)`으로 생성한다. 항목 VM은 `ProjectName`을 노출하고 템플릿이 직접 바인딩한다.
  - 카드별(`HistoryDialogViewModel`): `base(..., projectItem.Name)` — 생성자 파라미터라 base 호출식에서 바로 쓸 수 있다(이미 `projectItem.Histories`를 같은 자리에서 쓰고 있다). 프로젝트가 고정이라 이후 갱신이 필요 없다.
  - 전체(`ProjectHistoryDialogViewModel`): `base(..., string.Empty)`로 시작하고 프로젝트 전환 시 `LoadEntries()`에서 `SelectedProject?.Name ?? string.Empty`로 갱신한다(베이스에 `protected` 세터 1개). **세터 호출은 `_entries.AddRange` 직후·`CurrentPage = 1`(`:60`) 앞**에 둔다 — `CurrentPage` 대입이 `OnCurrentPageChanged`(`:76`)를 통해 `RebuildGroups`를 먼저 트리거할 수 있어서다(늦어도 값이 `string.Empty`라 예외는 없고 전체 다이얼로그는 `ProjectName`을 바인딩하지 않아 화면 영향은 없지만, 순서를 못박아 둔다).

  *Source*: **`ExportProjectName`을 그대로 인자로 넘기는 초안은 런타임 크래시를 낳는다** — 베이스 생성자가 마지막에 `RebuildGroups()`를 호출하는데(`:55~61`), 카드별 파생은 `base(...)`가 **끝난 뒤에야** `_projectItem`을 대입하므로(`HistoryDialogViewModel.cs:97~101`) 그 시점의 `ExportProjectName`(`:108` `_projectItem.Name`, null 가드 없음)이 `NullReferenceException`을 던진다. 기록이 0건이면 `:118` 루프에 들어가지 않아 우연히 통과하므로 **빈 프로젝트로 테스트하면 놓치는 결함**이다(plan-reviewer 2회차 B1). 베이스가 값을 직접 들고 있으면 파생 필드 초기화 순서와 무관해진다. 시안 행에 프로젝트 pill이 있고(`:795`, 렌더 4.png) `DataTemplate` 안의 조상·`ElementName` 바인딩은 신뢰하기 어려우므로 항목 VM 프로퍼티 방식 자체는 유지한다.

## 작업 단계

### T1 — 프로젝트 메모 도메인 필드 + DB 영속 `Type C`
- [x] 구현
- **Files**: `Domain/Entities/ProjectItem.cs`, `Domain/Repositories/IProjectRepository.cs`, `Infrastructure/Persistence/DatabaseContext.cs`, `Infrastructure/Persistence/SqliteProjectRepository.cs`, `Presentation/ViewModels/MainViewModel.cs`
- **Design**: ① 배치 — 필드는 Domain 엔티티, 저장 계약은 `IProjectRepository`(Domain), 스키마·마이그레이션은 `DatabaseContext`, 읽기/쓰기는 `SqliteProjectRepository`, 편집 병합은 `MainViewModel`(기존 계층 그대로). ② 신규 심볼 — `ProjectItem.Memo`(string, 프로젝트 자유 메모) + `IProjectRepository.UpdateMemo(string projectId, string memo)`(메모 단독 저장) 2개. ③ 의존 방향 — Infrastructure → Domain 단방향 유지(인터페이스는 Domain에, 구현은 Infrastructure — 기존 DIP 그대로). ④ 비추상화 — 메모 전용 테이블·리포지토리·도메인 서비스를 만들지 않는다(1:1 스칼라 필드 + 기존 인터페이스에 메서드 1개).
- **구성**:
  - `ProjectItem`에 `public string Memo { get; set; } = string.Empty;` 추가(한글 요약 주석).
  - `DatabaseContext`: `CREATE TABLE Projects`에 `Memo TEXT NOT NULL DEFAULT ''` 추가 + `MigrateSchema`에 `AddColumnIfNotExists(connection, "Projects", "Memo", "TEXT NOT NULL DEFAULT ''")` + `AllowedIdentifiers`에 `"Memo"` 등록(화이트리스트 통과 필수). `HeaderColor` 선례와 동일 위치·형식.
  - `SqliteProjectRepository`: `InsertProject`·`UpdateProject`의 컬럼 목록과 **파라미터 값 양쪽** 갱신(AGENTS.md DB 규약 — 컬럼만 넣고 값 누락 시 silent data loss), 읽기(`ReadProjects`)에 `HasColumn` 가드로 `Memo` 복원. **가져오기는 별도 심볼이 아니라 같은 경로 공유**(`ReadAllFromDb`→`ReadProjects`, `InsertProject`/`DeleteByNameAndInsert`)라 추가 작업 없음.
  - `SqliteProjectRepository.UpdateMemo`: `UPDATE Projects SET Memo = @memo WHERE Id = @id`(`UpdatePinned` 구현과 같은 모양).
  - **`MainViewModel.AddOrUpdateProjectAsync` 편집 분기에 `item.Memo = existingModel.Memo;` 추가**(D11) — 기존 `CommandScripts`·`IsPinned`·`PinOrder` 보존 바로 옆에, 같은 주석 맥락으로. 설정 다이얼로그가 메모를 다루지 않아 저장 시 초기화되는 것을 막는다.
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0(baseline 제외 — 판정 기준은 `## 검증 방법`의 baseline 정의).
  2. `ProjectItem.Memo`가 존재하고 기본값이 빈 문자열이다.
  3. `Projects` CREATE·`AddColumnIfNotExists`·`AllowedIdentifiers` 3곳에 `Memo`가 등록돼 구버전 DB도 마이그레이션된다(NFR-3).
  4. `InsertProject`·`UpdateProject`가 컬럼과 파라미터 값을 **둘 다** 포함하고, 읽기 경로가 `HasColumn` 가드로 복원한다(왕복 저장 경로 성립).
  5. `IProjectRepository.UpdateMemo`가 선언되고 `SqliteProjectRepository`가 메모 컬럼만 UPDATE하도록 구현한다(태그·스크립트 재삽입 부작용 없음 — D12).
  6. **`AddOrUpdateProjectAsync` 편집 분기가 `item.Memo`를 기존 값으로 보존한다** — 프로젝트 설정을 편집·저장해도 메모가 남는다(D11, 코드 경로 성립).
- **Edge Cases**: 구버전 DB(컬럼 없음) → `AddColumnIfNotExists`로 추가되고 기존 행은 `''` / 읽기 시 컬럼 부재 → `HasColumn` 가드로 빈 문자열 / 메모 매우 김 → TEXT 무제한 / 가져온 DB에 컬럼 없음 → 같은 가드 경로 / **메모가 있는 프로젝트를 설정에서 편집·저장** → D11 보존으로 유지 / 신규 프로젝트 추가(편집 아님) → `existing` null이라 보존 분기 미진입, 메모는 빈 문자열(정상).
- **Halt Forecast**: 없음 — 스키마 **가산** 변경(기존 컬럼·데이터 무변경), 파괴적·외부 작업 없음. 사전 승인 항목의 "비파괴 스키마 추가"에 해당.

### T2 — 프로젝트 메모 다이얼로그 + 카드 ⋯ 메뉴 항목 `Type C`
- [x] 구현
- **Files**: `Presentation/Views/Dialogs/ProjectMemoDialog.xaml`(신규), `Presentation/Views/Dialogs/ProjectMemoDialog.xaml.cs`(신규), `Presentation/ViewModels/ProjectCardViewModel.cs`, `Presentation/Views/DashboardView.xaml`, `Presentation/Views/DashboardView.xaml.cs`, `Strings/ko-KR/Resources.resw`, `Strings/en-US/Resources.resw`
- **Design**: ① 배치 — 다이얼로그는 `Presentation/Views/Dialogs/`(기존 10개와 동일), 진입은 카드 VM 커맨드 → View가 표시(기존 작업 기록 흐름과 동형). ② 신규 심볼 — `ProjectMemoDialog`(메모 편집 ContentDialog, `ResultMemo`/`IsDeleteRequested` 노출), `ProjectCardViewModel.OpenMemoCommand` + `OpenMemoRequested` 이벤트 + `HasMemo`(라벨 분기용) + `CurrentMemo`(다이얼로그 초기값) + `ApplyMemo(string)`(모델 반영 + `UpdateMemo` 호출). ③ 의존 방향 — View → VM → Domain, VM은 다이얼로그 타입을 참조하지 않는다(이벤트로 역전 — `OpenHistoryRequested` 선례). ④ 비추상화 — 메모 전용 서비스·인터페이스를 만들지 않는다(카드 VM이 직접 리포지토리 저장 경로를 쓴다, 기존 카드 편집과 동일).
- **구성**:
  - `ProjectMemoDialog.xaml`: `ContentDialog` + `ContentDialogMinWidth/MaxWidth` 680 + 멀티라인 `TextBox`(`AcceptsReturn`, `MinHeight` ≈320, 스크롤 Auto, **길이 상한 없음** — 시안·PRD 어디에도 상한이 없어 임의 제약을 두지 않는다) + 하단 좌측 [메모 삭제] 버튼(조건부).
  - `ProjectMemoDialog.xaml.cs`: 생성자 `(string projectName, string currentMemo)` → `Title = string.Format(Get("ProjectMemo_TitleFormat"), projectName)`, `PrimaryButtonText`=저장/`CloseButtonText`=취소, placeholder 코드비하인드 설정(함정 3), 삭제 버튼은 `currentMemo` 비었으면 `Collapsed`. `ShowAsync()` 섀도잉(`XamlRoot` 설정).
  - `ProjectCardViewModel`: `[RelayCommand] OpenMemo()` → `OpenMemoRequested` 발생. `HasMemo` = 메모 비지 않음. `CurrentMemo` = `_item.Memo`(다이얼로그 초기값). `ApplyMemo(string memo)`가 `_item.Memo`에 반영하고 **`_repository.UpdateMemo(_item.Id, memo)`로 영속화**(D12 — 이 VM은 이미 `_repository`를 보유하며 `GetTodos`/`GetHistories` 등에 쓰고 있다).
  - `DashboardView.xaml`: `MenuFlyout`의 **작업 기록 앞**에 `MenuFlyoutItem x:Uid="CardMenu_Memo"`(아이콘 `&#xE70B;` 계열 메모 글리프) + `Command="{Binding OpenMemoCommand}"` 삽입(시안 `:485` 순서).
  - `DashboardView.xaml.cs`: `OpenMemoRequested` 구독 → 다이얼로그 표시 → Primary면 `ApplyMemo(dialog.ResultMemo.Trim())`, 삭제 요청이면 `ApplyMemo("")`. `MenuFlyout` `Opening`에서 라벨을 `HasMemo`에 따라 "메모"/"메모 보기"로 갱신.
  - resw **ko/en 양쪽**(함정 2): `ProjectMemo_TitleFormat`("{0} — 메모"), `ProjectMemo_Placeholder`, `ProjectMemo_Delete`, `CardMenu_Memo.Text`("메모"), `CardMenu_MemoView`("메모 보기" — 코드 `Get()` 소비라 베어네임, 함정 3).
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0(baseline 제외 — 판정 기준은 `## 검증 방법`의 baseline 정의).
  2. 카드 `⋯` 메뉴에 메모 항목이 **작업 기록 바로 앞**에 있고 `OpenMemoCommand`에 배선된다(시안 `:485` 순서).
  3. `ProjectMemoDialog`가 제목 `{프로젝트명} — 메모`·placeholder·[취소][저장]을 갖고, 메모가 있을 때만 [메모 삭제]가 보인다.
  4. 저장 시 `ProjectCardViewModel.ApplyMemo`가 `_item.Memo`를 갱신하고 **`UpdateMemo`(T1의 부분 저장 API)로 DB에 영속**한다 — 태그·커맨드 스크립트를 건드리지 않는다(D12). 빈 문자열 저장·삭제 버튼 모두 메모를 지운다(D3).
  5. 신규 resw 키가 **ko/en 양쪽**에 등록되고, `x:Uid` 소비 키는 속성 접미(`.Text`)를, 코드 `Get()` 소비 키는 베어네임을 쓴다(함정 3).
  6. 카드 본문에는 메모 표시가 추가되지 않는다(시안 준수 — Q3).
- **Edge Cases**: 메모 없음 상태로 열기 → placeholder만, 삭제 버튼 숨김 / 공백만 입력 후 저장 → `Trim()` 결과 빈 문자열 = 삭제와 동일 / 아주 긴 메모 → 길이 제한 없이 스크롤로 처리 / 취소 → 모델 무변경 / 메뉴를 두 번 열 때 라벨 → `Opening`마다 재계산이라 저장 직후에도 정확 / 다른 다이얼로그가 열린 중 호출 → 카드 메뉴는 단독 진입이라 중첩 없음.
- **Halt Forecast**: 없음 — 신규 파일 2개는 계획된 산출물, 파괴적·외부 작업 없음.

### T3 — 작업 기록 시각(HH:MM) 도입 `Type C`
- [ ] 구현
- **Files**: `Presentation/ViewModels/HistoryDialogViewModel.cs`, `Presentation/ViewModels/HistoryDialogViewModelBase.cs`, `Presentation/ViewModels/ProjectHistoryDialogViewModel.cs`(베이스 생성자 인자 추가에 따른 `base(...)` 호출 갱신 + 프로젝트 전환 시 이름 갱신 — D13)
- **Design**: ① 배치 — 표시 포맷은 항목 VM(`HistoryEntryViewModel`), 목록 정렬·건수는 베이스 VM. ② 신규 심볼 — `HistoryEntryViewModel.TimeText`(행에 표시할 HH:MM, 00:00이면 `CreatedAt` 시각으로 폴백) + `HistoryEntryViewModel.ProjectName`(행 pill용, 생성자 주입 — D13) + `HistoryDialogViewModelBase.FilteredCount`(헤더 "N건" — 검색 반영). ③ 의존 방향 — VM → Domain만(Brush·Visibility 등 View 타입 신규 참조 없음, NFR-2). ④ 비추상화 — 시각 전용 값 객체·포맷터 클래스를 만들지 않고, 프로젝트명 전달을 위해 새 추상 멤버를 만들지 않는다(기존 `ExportProjectName` 재사용).
- **구성**:
  - `HistoryEntryViewModel.TimeText`: `Model.CompletedAt.TimeOfDay == TimeSpan.Zero ? Model.CreatedAt.ToString("HH:mm") : Model.CompletedAt.ToString("HH:mm")` — 폴백 이유를 한글 주석으로 명시(D5).
  - `HistoryEntryViewModel` 생성자에 프로젝트명 인자 추가 + `ProjectName` 노출(D13). 생성처는 베이스 `RebuildGroups` **1곳뿐**이다.
  - **초기화 순서 안전 배선**(D13): 베이스 생성자에 `string entryProjectName` 인자를 추가해 베이스 필드에 보관하고 `RebuildGroups`가 **그 필드**를 넘긴다(`ExportProjectName`을 넘기면 카드별 파생에서 `_projectItem`이 아직 null이라 NRE). 카드별은 `base(..., projectItem.Name)`, 전체는 `base(..., string.Empty)` + `LoadEntries()`에서 `protected` 세터로 `SelectedProject?.Name ?? string.Empty` 갱신.
  - 베이스 `RebuildGroups` 정렬을 **`CompletedAt` 전체(날짜+시각) 내림차순 → `CreatedAt` 내림차순**으로 조정한다. **그룹 간 정렬(`:101~104`)과 그룹 내부 정렬(`:118` `group.OrderByDescending(e => e.CreatedAt)`)을 둘 다 고친다** — 실제 행 표시 순서를 정하는 것은 `:118`이라 여기를 빼면 시각 정렬이 성립하지 않는다. 그룹핑 키는 `CompletedAt.Date` 유지.
  - 베이스에 `FilteredCount`(검색 필터 적용 결과 수) 추가 + `RebuildGroups`에서 변경 통지.
  - `ExportToMarkdown`의 항목 줄에 완료 시각을 포함(`HH:mm`).
  - **공유 파급 명시**(D10 표): 정렬·내보내기 2건은 `ProjectHistoryDialog`에도 함께 적용되는 **의도된 개선**이고, `FilteredCount`·`ProjectName`은 전체 다이얼로그가 소비하지 않아 화면 변화가 없다.
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0(baseline 제외 — 판정 기준은 `## 검증 방법`의 baseline 정의).
  2. `TimeText`가 `CompletedAt`의 시각을 반환하되, 00:00이면 `CreatedAt`의 시각으로 폴백한다(D5 — 기존 기록도 의미 있는 시각).
  3. 같은 날짜 그룹 안에서 늦은 시각 기록이 먼저 나온다 — **그룹 간·그룹 내부 정렬이 둘 다 `CompletedAt` 기준**이다(`:101~104` + `:118`).
  4. `FilteredCount`가 검색어 적용 후 건수를 반환하고, `HistoryEntryViewModel.ProjectName`이 프로젝트명을 노출한다. **베이스가 프로젝트명을 자기 필드로 보유**해 `RebuildGroups`가 파생 필드에 접근하지 않는다(D13 — 초기화 순서 NRE 방지).
  5. **기록이 1건 이상인 프로젝트에서 카드 작업 기록 다이얼로그를 열어도 예외가 나지 않는다** — 기록 0건이면 항목 생성 루프에 들어가지 않아 이 결함이 드러나지 않으므로, **반드시 기록이 있는 프로젝트로 확인**한다(⏳ HUMAN-VERIFY).
  6. 공유 변경의 파급이 **D10 표와 일치**한다 — `ProjectHistoryDialog`는 정렬·내보내기만 같은 방향으로 바뀌고(시각 반영), 화면 구성과 기존 public 멤버 시그니처는 그대로다. 전체 다이얼로그의 목록·검색·페이지·내보내기가 정상 동작하는지 확인한다(⏳ HUMAN-VERIFY).
- **Edge Cases**: `CompletedAt`·`CreatedAt` 둘 다 00:00 → "00:00" 표시(정상) / 같은 시각 기록 2건 → `CreatedAt` 2차 정렬로 안정 / 검색 결과 0건 → `FilteredCount`=0, 빈 상태 문구 표시 / 날짜만 바꾼 기존 기록 → `CompletedAt` 시각이 00:00이라 `CreatedAt` 폴백(D5의 알려진 성질, Deferred에 명시) / **기존 기록만 있는 DB(전부 00:00)** → 정렬이 사실상 종전과 동일(`CreatedAt` 2차 정렬이 순서 유지) — 전체 다이얼로그 회귀 위험 낮음.
- **Halt Forecast**: 없음 — VM 2파일. 정렬·내보내기가 전체 다이얼로그와 공유되지만 **D10에서 의도된 파급으로 사전 확정**했으므로 구현 중 결정 분기가 아니다.

### T4 — 작업 기록 목록 화면 시안 재구현 `Type D`
- [ ] 구현
- **Files**: `Presentation/Views/Dialogs/HistoryDialog.xaml`, `Presentation/Views/Dialogs/HistoryDialog.xaml.cs`, `Presentation/ViewModels/HistoryDialogViewModel.cs`(행 pill이 소비하는 `ProjectName` — T3에서 추가, T4가 바인딩), `Strings/ko-KR/Resources.resw`, `Strings/en-US/Resources.resw`, `Resources/Palette.xaml`(입력 배경 Brush 키 1개, 필요 시)
- **Design**: ① 배치 — 레이아웃은 `HistoryDialog.xaml`, 색·상태 계산은 같은 다이얼로그 코드비하인드(NFR-2: VM에 Brush 미참조). ② 신규 심볼 — `HistoryDialog.KindBrush(string)`·`KindBackBrush(string)`(유형 배지 전경·배경 Brush를 직접 반환하는 public static, 함정 5) + `static readonly SolidColorBrush` 색 상수 4쌍 + 행 hover 핸들러 2개. ③ 의존 방향 — XAML `x:Bind` 함수 바인딩 → 코드비하인드 static(선례 `TestPage.StatusBrush`), VM은 문자열 `Kind`만 노출. ④ 비추상화 — 공용 배지 컨트롤·Converter·팔레트 이관을 만들지 않는다(소비처 이 다이얼로그 1곳, deferred에 색 이관 검토만 등재).
- **구성**:
  - 헤더 행 신설: `ContentDialog.Title`(현행 "작업 기록 — {0}") 대신 `Content` 최상단에 "작업 기록" + 프로젝트 pill + `{FilteredCount}건`(T3) 배치. 다이얼로그 크기를 760×680으로 조정.
  - 검색행: 현행 3열 유지, [＋ 새 기록] 버튼을 `AccentButtonStyle`·문구 "＋ 새 기록"으로, 내보내기 38×38·radius 9.
  - 날짜 그룹 헤더: `Grid` 3열(날짜 monospace / "N건" / `Rectangle` 구분선). 그룹 건수는 `HistoryDateGroup.Entries.Count` 사용(기존 컬렉션 — 신규 VM 심볼 불요).
  - 기록 행: `Border`(`AppCardAltBrush` + `AppBorderBrush` + radius 10) 안에 `Grid` 6열 — chevron / 시각(`TimeText`) / 제목(`*`, 말줄임) / 유형 배지 / 프로젝트 pill / 편집·삭제. 유형 배지는 `Border Background="{x:Bind KindBackBrush(Kind)}"` + `TextBlock Foreground="{x:Bind KindBrush(Kind)}"`(함수 바인딩). 프로젝트 pill은 **항목 VM의 `ProjectName`을 직접 `x:Bind`**한다(D13 — T3이 생성자 주입으로 채운다. `Tag`·조상 바인딩 우회를 쓰지 않는다).
  - 펼침 영역: 상단 `BorderThickness="0,1,0,0"` + 좌측 들여쓰기 50 + 본문 12.5px. 기존 "등록일시" 줄은 시안에 없으므로 제거.
  - 행 hover: `PointerEntered/Exited`로 배경 교체(카드 hover 선례와 동일 방식).
  - 빈 상태 문구를 "검색 결과가 없습니다"로 갱신(ko/en) + 여백 60.
  - resw ko/en: `HistoryDialog_Header`("작업 기록"), `HistoryDialog_CountFormat`("{0}건"), `AddHistoryBtn.Content`("＋ 새 기록"), `EmptyHistoryText.Text` 문구 갱신.
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0(baseline 제외 — 판정 기준은 `## 검증 방법`의 baseline 정의).
  2. 헤더에 제목 + 프로젝트 pill + 총 건수가 있고, 건수는 검색 결과에 연동된다(T3 `FilteredCount`).
  3. 각 기록 행이 시각(`TimeText`) · 제목(말줄임) · 유형 색 배지 · 프로젝트 pill · 편집/삭제 아이콘을 이 순서로 갖는다.
  4. 유형 배지 색이 기능/수정/리팩터링에 각각 다른 색으로, 그 외(문서·사용자 정의·미분류)는 회색으로 나온다 — `KindBrush`/`KindBackBrush`가 `x:Bind` 함수 바인딩으로 소비된다(Converter·ThemeResource 미사용, 함정 5).
  5. 날짜 그룹 헤더에 날짜 + 건수 + 남는 폭을 채우는 구분선이 있다.
  6. 펼침 영역이 상단 구분선 + 좌측 들여쓰기로 표시된다.
  7. 신규·변경 resw 키가 ko/en 양쪽에 있다(함정 2).
  8. 렌더가 시안 4.png와 일치한다(⏳ HUMAN-VERIFY).
- **Edge Cases**: 유형 미지정(빈 문자열) → 배지 숨김(기존 `KindVisibility` 유지) / 사용자 정의 유형 → 회색 폴백(시안 `:1393`) / 제목 매우 김 → `*` 열 말줄임, 다른 요소 밀리지 않음 / 프로젝트명 매우 김 → pill 말줄임 / 기록 0건 → 빈 상태 문구, 그룹·페이지 영역 숨김 / 검색 결과 0건 → 같은 빈 상태 / 행 hover 중 우클릭·버튼 클릭 → 자식에서 `Tapped` 차단(현행 `EditBtn_Tapped`/`DeleteBtn_Tapped` 유지).
- **Halt Forecast**: 없음 — 기존 파일 재구성 + resw 추가. `Palette.xaml`에 Brush 키를 추가하더라도 **Default 딕셔너리에만**(함정 4) 가산이라 파괴적이지 않다.

### T5 — 뷰 전환형 새 기록/편집 폼 `Type D`
- [ ] 구현
- **Files**: `Presentation/Views/Dialogs/HistoryDialog.xaml`, `Presentation/Views/Dialogs/HistoryDialog.xaml.cs`, `Strings/ko-KR/Resources.resw`, `Strings/en-US/Resources.resw`
- **Design**: ① 배치 — 목록 영역과 폼 영역을 같은 `ContentDialog` `Content` 안에 두고 `Visibility`로 교체(D7), 폼 상태는 코드비하인드가 보유. ② 신규 심볼 — `ShowFormView(HistoryEntryViewModel? editing, string initialTitle)`(폼을 열고 필드를 채우는 private), `ShowListView()`(목록 복귀), `UpdateSaveEnabled()`(제목 유무로 저장 활성 갱신). ③ 의존 방향 — 코드비하인드 → VM(`AddEntry`/`UpdateEntry`), VM은 폼을 모른다. ④ 비추상화 — 폼 전용 ViewModel(고아 `HistoryEntryDialogViewModel`)을 되살리지 않고, 폼을 별도 UserControl로 분리하지 않는다(소비처 1곳).
- **구성**:
  - XAML: **목록 영역 전체(검색행 + 목록 + 페이지네이션)를 하나의 `Grid`로 묶고**, 그 옆에 폼 `Grid`(기본 `Collapsed`)를 둬 `Visibility`로 교체한다(헤더는 두 화면 공용으로 유지). 폼 구성 — [‹ 목록] + 헤더 텍스트 / 제목 라벨(`*` 액센트) + `TextBox` + 필수 밑줄 `Rectangle`(상시) / **날짜·시간·유형을 2열 그리드**(`CalendarDatePicker` + `TimePicker` + `ComboBox`) / 내용 `TextBox`(멀티라인) / 하단 [저장][취소] 균등 2열. 프로젝트 선택은 두지 않는다(카드별 고정 — Q2).
  - **하단 [닫기] 처리**: `ContentDialog.CloseButtonText`는 `Content` 밖 고정 버튼이라 폼 화면에서도 남는다. 시안은 폼 화면에 [닫기]가 없고 [저장][취소]만 있으므로, **폼 진입 시 `CloseButtonText`를 빈 문자열로 비우고 목록 복귀 시 복원**한다.
  - 코드비하인드: `ToggleAddPanel_Click` → `ShowFormView(null, "")`, `EditEntry_Click` → **중첩 다이얼로그 대신** `ShowFormView(entryVm, ...)`(D8, 날짜·시각까지 편집), `SaveAdd_Click` → 신규면 `Vm.AddEntry(new HistoryEntry{... CompletedAt = 날짜+시각 ...})`, 편집이면 `Vm.UpdateEntry(vm, title, desc, kind, 날짜+시각)` 후 `ShowListView()`. 취소·[‹ 목록]은 `ShowListView()`.
  - `OpenAddPanel(string initialTitle)`는 **시그니처 유지**하고 내부에서 `ShowFormView(null, initialTitle)` 호출(D9 — `TaskPage` 완료 팝업 경로 보존).
  - 저장 버튼은 제목 `TextChanged`에 따라 `IsEnabled` 갱신(시안 `:1519`). 현행 `AddErrorText`(빈 제목 오류 문구)는 비활성 버튼으로 대체되므로 제거하고, resw 키는 고아 목록에 등재.
  - `ShowNestedDialogAsync`/`_nestedTcs`는 **내보내기 오류 표시에 계속 쓰이므로 유지**(D8).
  - resw ko/en: `HistoryForm_Back`("목록"), `HistoryForm_TitleLabel`("제목"), `HistoryForm_TitlePlaceholder`("무엇을 작업했나요?"), `HistoryForm_DateLabel`("날짜"), `HistoryForm_TimeLabel`("시간"), `HistoryForm_KindLabel`("유형"), `HistoryForm_DetailLabel`("내용"), `HistoryForm_DetailPlaceholder`("작업 내용을 기록해 두면 나중에 찾기 쉽습니다 (선택)").
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0(baseline 제외 — 판정 기준은 `## 검증 방법`의 baseline 정의).
  2. [＋ 새 기록]을 누르면 **검색행·목록·페이지네이션·하단 [닫기]가 모두 사라지고** 폼으로 교체되며, [‹ 목록]·[취소]로 복귀 시 전부 되살아난다(같은 다이얼로그 안, 중첩 다이얼로그 미사용).
  3. 폼이 제목\*·날짜·시간·유형·내용을 갖고, 제목이 비면 저장 버튼이 비활성이다.
  4. 저장 시 날짜와 시각이 합쳐져 `CompletedAt`에 저장되고 목록 행의 시각에 반영된다(T3 표시 경로).
  5. 편집(연필)도 같은 폼으로 열려 **날짜·시각까지** 수정된다(D8).
  6. **`OpenAddPanel(string)` 시그니처와 동작이 보존**되어 `TaskPage`의 작업 완료 → 작업기록 팝업이 제목이 채워진 폼으로 열린다(D9, 회귀 없음).
  7. 신규 resw 키가 ko/en 양쪽에 있다.
  8. 렌더가 시안 3.png와 일치한다(⏳ HUMAN-VERIFY).
- **Edge Cases**: 제목만 입력하고 저장 → 내용·유형 없이 저장(내용은 선택) / 시간 미선택 → 현재 시각 기본값(폼 열 때 채움) / 편집 중 [‹ 목록] → 변경 폐기(저장 전이므로) / `OpenAddPanel` 경로에서 곧바로 저장 → 새 기록 1건 추가 후 `NewEntries`에 담겨 `CommitWorkLog`로 전달(기존 계약) / 폼이 열린 채 다이얼로그 닫힘 → 저장 안 된 입력은 폐기(현행과 동일) / 편집 대상이 검색·페이지 이동으로 목록에서 사라짐 → 폼은 대상 VM 참조를 유지하므로 저장은 성립.
- **Halt Forecast**: 없음 — 기존 파일 2개 재구성. 편집 UX가 중첩 다이얼로그 → 인폼으로 바뀌지만 **D8로 사전 확정**됐고 공개 API·데이터 계약은 불변.

### T6 — 페이지네이션 번호 이동 `Type C`
- [ ] 구현
- **Files**: `Presentation/ViewModels/HistoryDialogViewModelBase.cs`, `Presentation/Views/Dialogs/HistoryDialog.xaml`, `Presentation/Views/Dialogs/HistoryDialog.xaml.cs`, `Strings/ko-KR/Resources.resw`, `Strings/en-US/Resources.resw`
- **Design**: ① 배치 — 번호 집합 계산은 베이스 VM(순수 계산), 표시는 `HistoryDialog.xaml`. ② 신규 심볼 — `HistoryDialogViewModelBase.PageButtons`(표시할 페이지 번호·생략 표식 목록) + 그 항목 타입 1개(`HistoryPageButton`: 번호 / 생략 여부 / 현재 여부) + `GoToPage(int)` 커맨드. ③ 의존 방향 — VM은 숫자만 노출, 현재 페이지 강조 색은 View가 결정(NFR-2). ④ 비추상화 — 범용 페이지네이션 컨트롤을 만들지 않는다(소비처 1곳, 전체 다이얼로그는 이번 미적용).
- **구성**:
  - 베이스에 `PageButtons` 계산: `{1, TotalPages, CurrentPage-1, CurrentPage, CurrentPage+1}` 중 유효 범위만 정렬하고, 번호 간격이 1을 넘으면 생략 표식을 끼운다(시안 `:1398~1413`). `RebuildGroups`에서 재계산·통지. **기존 멤버 불변**(D10).
  - XAML: 하단 영역을 상단 구분선 + **좌측 `{현재}/{전체} 페이지` + 가로 중앙에 [이전][번호…][다음]** 묶음으로 재배치(시안은 버튼 묶음이 중앙이고 정보만 좌측으로 밀린다). 번호는 `ItemsControl`(가로), 현재 페이지만 액센트 배경.
  - 코드비하인드: 번호 버튼 `Click` → `Vm.GoToPageCommand.Execute(번호)`.
  - resw ko/en: `HistoryPage_InfoFormat`("{0} / {1} 페이지").
- **Acceptance**:
  1. 빌드 오류 0 · 신규 경고 0(baseline 제외 — 판정 기준은 `## 검증 방법`의 baseline 정의).
  2. `PageButtons`가 `{1, 마지막, 현재±1}` 규칙으로 구성되고 간격이 벌어지면 생략 표식이 들어간다.
  3. 번호를 누르면 해당 페이지로 이동하고 현재 페이지 버튼이 액센트로 강조된다.
  4. 페이지 정보가 좌측에 "N / M 페이지"로 표시된다(신규 resw, ko/en).
  5. 기존 이전·다음 버튼과 `CanPrev/NextPage` 동작이 유지된다(회귀 없음).
  6. `ProjectHistoryDialog`(전체)는 `PageButtons`를 소비하지 않아 화면·동작이 그대로다(D10).
- **Edge Cases**: 전체 1페이지 → 번호 영역 숨김(시안 `pageCount<=1`이면 빈 배열) / 2~5페이지 → 생략 없이 전부 표시 / 현재가 1 또는 마지막 → 한쪽 이웃만 존재 / 검색으로 페이지 수 급감 → `RebuildGroups`의 기존 페이지 보정이 먼저 동작한 뒤 번호 재계산 / 페이지 크기 1000·기록 수천 건 → 번호는 최대 5개 + 생략 2개로 상한.
- **Halt Forecast**: 없음 — VM 가산 + View 재배치.

## Progress Log

- **baseline 경고 실측 확정 (T1 첫 빌드)**: 빌드 경고는 **`CS0618` 1건**(자동 생성 `XamlTypeInfo.g.cs`의 `Icon` obsolete)뿐이다. `AGENTS.md:29~33`의 "`NU1903` 1건 + `CS0612` 4건 = 5건" 기록이 **stale**임이 재확인됐다(`deferred.md:64`와 일치). 이후 task의 "신규 경고 0" 판정 기준은 `CS0618` 1건.
- **T1-T2 완료** (커밋 `b259467`, `8e7aaf8`): 메모 도메인 필드·DB 컬럼·`UpdateMemo` 부분 저장 + 설정 편집 시 메모 보존(D11) / 메모 다이얼로그 신규 + 카드 ⋯ 메뉴 항목. 빌드 OK, spec·quality 리뷰 전부 OK.
  - **결정 변경(D4 구현 방식)**: 메뉴 라벨 조건부 전환을 plan의 "MenuFlyout `Opening` 이벤트" 대신 **`MenuFlyoutItem.Text="{Binding MemoMenuLabel}"` 바인딩**으로 구현했다. 같은 `MenuFlyout`의 다른 항목들이 이미 `{Binding}`으로 카드 VM DataContext를 소비하는 검증된 패턴이라 더 단순하고 결과가 같다(spec 리뷰가 Design 실질 충족으로 판정). 가역적 내부 구현이라 Halt 없이 진행.
  - **F-8 인계 시작**: T2 메모 다이얼로그의 렌더 관련 행을 아래 `## F-8 인계 목록`에 등재(데스크톱이라 자동 캡처 불가).

## F-8 인계 목록 (렌더 육안 확인 대기 — V-9가 `⏳ 미확인`으로 넘긴 행)

> WinUI 3 데스크톱이라 자율 루프에서 창을 캡처해 대조할 수단이 없다(V-9 3분기 중 데스크톱 경로). 코드 경로는 아래 파일:라인에서 충족되나 **화면 결과는 사용자만 판정**할 수 있다.

### T2 — 메모 다이얼로그 (시안 `:519~535`, 렌더 2.png)

| 요소 | 디자인 값 | 구현 위치 (코드 경로 충족) | 렌더 판정 |
|---|---|---|---|
| 컨테이너 폭 | `min(680px,94vw)` | `ProjectMemoDialog.xaml:10-11` `ContentDialogMinWidth`/`MaxWidth` = 680 | ⏳ 미확인 |
| 제목 문구 | `{프로젝트명} — 메모` | `ProjectMemoDialog.xaml.cs:24` + resw `ProjectMemo_TitleFormat` | ✅ 문구 확인 |
| 제목 크기 | 17px/700 | `ContentDialog` 기본 제목 스타일 사용(별도 지정 없음) | ⏳ 미확인 |
| 입력 크기 | `rows 16`(≈320) | `ProjectMemoDialog.xaml:27` `MinHeight="320"` | ⏳ 미확인 |
| 입력 외형 | bg `#242429`, border `#2e2e34`, radius 10, 13.5px | 앱 기본 `TextBox` 스타일 사용 — 시안 값과 미세차 가능 | ⏳ 미확인 |
| placeholder | "프로젝트 메모를 입력하세요…" | `ProjectMemoDialog.xaml.cs:28` + resw | ✅ 문구 확인 |
| 삭제 버튼 조건 | 메모가 있을 때만 노출 | `ProjectMemoDialog.xaml.cs:32-35` | ✅ 코드 경로 |
| 삭제 버튼 색 | 테두리·배경 연한 적색 + 글자 `#f0716a` | `ProjectMemoDialog.xaml:36-38` `AppAccentSoftBrush`/`AppDangerBrush` 근사 | ⏳ 미확인 |
| 하단 배치 | 좌 [메모 삭제] · 우 [취소][저장] | 삭제는 `Content` 하단 좌측(`:33-40`), 취소·저장은 `ContentDialog` 기본 버튼 | ⏳ 미확인 |
| 저장 강조 | 액센트 배경 | `ProjectMemoDialog.xaml:6` `DefaultButton="Primary"`(WinUI가 기본 버튼에 액센트 적용) | ⏳ 미확인 |

## Deferred / Follow-up
- **[전체 작업 기록(`ProjectHistoryDialog`) 시안 미적용]** — 이번은 카드별 `HistoryDialog`만 재구현했다(Q2). 전체 다이얼로그는 구 레이아웃(dot+텍스트 유형, 시각 없음, 인라인 폼)으로 남아 **두 화면의 모양이 갈린다**. PRD **FR-H4**(두 다이얼로그 중복 정리)와 함께 다룰 후속.
- **[`InitKindCombo`/`KindFromCombo` 중복]**(deferred.md:15) — 이번에도 3번째 사용처가 생기지 않아 대장 유지.
- **[`HistoryEntryDialogViewModel` 고아 정리]**(deferred.md:14) — 4-D에서 미사용 재확인(D7). 대장 유지.
- **[SUGGEST] 시안 색 리터럴 누적**(deferred.md:70의 연장) — T4의 유형 배지 4색(기능/수정/리팩터링/회색)이 코드비하인드 상수로 추가된다. 테스트 상태색(deferred.md:48)·링크 배지색과 함께 팔레트 이관을 일괄 검토.
- **[날짜만 수정한 기존 기록의 시각 폴백]** — D5의 알려진 성질: `CompletedAt` 시각이 00:00이면 `CreatedAt` 시각을 보여주므로, 과거에 날짜를 수정한 기록은 표시 시각이 실제 완료 시점과 어긋날 수 있다. T5 이후 편집하면 사용자가 시각을 직접 지정해 해소된다.
- **[빈 제목 오류 문구 제거에 따른 접근성 약화]** — T5가 `HistoryDialog_TitleRequired` 문구를 비활성 버튼으로 대체한다(시안 준수). `TaskEditDialog`에서 같은 성질이 이미 등재돼 있으므로(deferred.md:30) 함께 보완 검토.
- **[resw 고아 audit에 이번 분 합산]** — T5로 `HistoryDialog_TitleRequired`·`HistoryAddNewHeader.Text`가, T4로 구 `EmptyHistoryText` 문구가 고아화/치환된다. 기존 고아 정리 항목들과 일괄 처리.
- **[README/스크린샷 갱신]**(deferred.md:18) — 카드 메뉴에 메모가 추가되고 작업 기록 화면이 바뀌어 스크린샷이 또 낡는다. 계속 Deferred.
- **[GitHub 연결 미구현]**(deferred.md:90의 잔여) — 시안 카드 메뉴의 GitHub 항목은 이번에도 대상 밖. 대장 유지.
- **[AGENTS.md baseline 경고 기록 stale]**(deferred.md:64) — AGENTS.md는 5건(`NU1903`+`CS0612`×4), 실측은 `CS0618` 1건. 이번 plan은 둘 다 baseline으로 인정해 우회했으나 기록 자체는 여전히 어긋난다. 승인 시 지시가 있으면 `pjc:record-project-fact`로 정정, 없으면 대장 유지.
- **[프로젝트 재구축 경로의 필드 소실 취약성]** — `ProjectSettingsDialogViewModel.ToProjectItem()`이 새 `ProjectItem`을 통째로 만들기 때문에, 다이얼로그가 다루지 않는 필드는 `MainViewModel`이 **일일이 되살려야** 보존된다(현재 `CommandScripts`·`IsPinned`·`PinOrder`, 이번에 `Memo` 추가 = 4개). 앞으로 필드가 늘 때마다 같은 사고가 반복될 구조다(실제로 핀 소실이 한 번 발생했다 — 커밋 87dadef). 근본 해결은 재구축 대신 **기존 인스턴스에 편집분만 반영**하는 방식으로 바꾸는 것이나 다이얼로그 VM 구조 변경이라 별도 작업.

## Out of Scope
- 카드 본문에 메모 표시(아이콘·요약) — 시안에 없음, 사용자 결정(Q3).
- 새 기록 폼의 프로젝트 선택 — 카드별 다이얼로그는 프로젝트가 고정(시안 `showProjectSelect` 분기의 false 쪽).
- 유형 select의 "작업 카테고리" optgroup — 현행 `AvailableKinds`(기본 4 + 사용자 정의)를 유지하며 작업 카테고리 병합은 이번 범위 밖(FR-H1 기구현 범위 준수).
- git 커밋 ↔ 작업 기록 자동 연동 — PRD §7 영구 제외.
- 메모 검색·목록화 — 시안에 없음.

## 사전 승인 항목 (일괄 승인 대상)
- **DB 스키마 가산 변경**(T1): `Projects` 테이블에 `Memo TEXT NOT NULL DEFAULT ''` 컬럼 추가 + `AllowedIdentifiers` 등록. 기존 컬럼·데이터 무변경(비파괴), 구버전 DB는 `AddColumnIfNotExists`로 자동 마이그레이션.
- **도메인 엔티티 필드 추가**(T1): `ProjectItem.Memo`.
- **공개 인터페이스 가산 변경**(T1): `IProjectRepository`에 `UpdateMemo(string, string)` 추가(D12). 구현체 1개(`SqliteProjectRepository`, 전수 확인)라 파급이 닫혀 있다.
- **공유 뷰모델 동작 변경**(T3): `HistoryDialogViewModelBase`의 **정렬**(그룹 간·그룹 내부)과 **마크다운 내보내기**에 시각을 반영한다 — 이번 UI 범위 밖인 **전체 작업 기록 다이얼로그에도 함께 적용**된다(D10 표의 "의도된 파급"). 화면 구성·기존 멤버 시그니처는 불변.
- **신규 파일 2개**(T2): `ProjectMemoDialog.xaml(.cs)`.
- **편집 UX 변경**(T5): 기록 편집이 중첩 ContentDialog → 인폼 뷰 전환으로 바뀐다(D8). 공개 API·데이터 계약 불변.
- 로컬 작업 브랜치(`task/dashboard-card-redesign`)에서 task별 commit.

> **별도 승인 요청(코드와 무관한 문서 정정)**: `AGENTS.md:29~33`의 "상시 기존 경고 5건(`NU1903`+`CS0612`×4)" 기록이 실측(`CS0618` 1건)과 어긋난다는 것이 `deferred.md:64`에 이미 등재돼 있다. 이번 작업 중 `pjc:record-project-fact`로 정정할지 여부는 **승인 시 알려주면 함께 처리**하고, 답이 없으면 손대지 않는다(이번 판정은 아래 baseline 정의를 따르므로 작업 진행에 지장 없음).

## 불가피한 Halt (위임 불가)
- master 병합·push·PR — 별도 승인.
- **시안 대조 최종 시각 판정** — 메모 다이얼로그·작업 기록 목록·새 기록 폼의 렌더 일치는 사용자만 판정(⏳ HUMAN-VERIFY).
- 기존 사용자 DB의 데이터 변형(마이그레이션으로 기존 `CompletedAt` 값 덮어쓰기) — **이번 계획에 없음**(D5는 표시 폴백만). 구현 중 필요해지면 중단하고 승인받는다.

## Open Questions
- [x] 작업 기록 범위 → **시안대로 전면 재구현**(Q1, 2026-07-24).
- [x] 적용 대상 → **카드별 `HistoryDialog`만**, 전체 `ProjectHistoryDialog`는 후속(Q2, 2026-07-24).
- [x] 카드 메모 표시 → **시안 그대로**(카드 본문 미표시, 메뉴 라벨만 조건부)(Q3, 2026-07-24).
- [x] PRD FR-H3 문구 → **갱신 승인**(뷰 전환형·시각·건수·색 배지 반영, 2026-07-24 D-10 반영 완료).
- [x] 메모 PRD 연결 → **FR-D5 신설 승인**(2026-07-24 D-10 반영 완료).
- [x] 유형 배지 색 → **시안 값 그대로**(보라 포함, 회색 폴백)(2026-07-24).
- [x] 기존 기록 시각 → **`CreatedAt` 시각으로 폴백**(데이터 변형 없음)(2026-07-24).

## 검증 방법
- 빌드: `"C:/Program Files/Microsoft Visual Studio/18/Professional/MSBuild/Current/Bin/MSBuild.exe" "DevDashboard_WinUI/DevDashboard.csproj" -t:Build -p:Configuration=Debug -p:Platform=x64` → 오류 0 + **baseline 외 신규 경고 0**
- **baseline 경고 정의(판정 기준)**: `AGENTS.md:29~33`은 `NU1903` 1건 + `CS0612` 4건을, `deferred.md:64`는 그 기록이 stale이며 실측은 `CS0618` 1건뿐이라고 적는다(직전 plan도 실측 기준으로 통과). → **`NU1903`·`CS0612`·`CS0618` 중 무엇이 나오든 baseline으로 인정**하고, 그 외 경고만 신규로 판정한다. 실측 목록은 첫 빌드 결과를 Progress Log에 기록해 이후 판정의 기준으로 삼는다(둘 중 어느 기록이 맞는지 실측으로 확정).
- 회귀 방지 grep(소스, `obj/`·`bin/` 제외):
  - **T1**: `ProjectItem.cs`에 `Memo` 1건 / `DatabaseContext.cs`의 CREATE·`AddColumnIfNotExists`·`AllowedIdentifiers` 3곳에 `Memo` / `SqliteProjectRepository.cs`의 INSERT·UPDATE 컬럼과 파라미터 값 쌍 + 읽기 `HasColumn` 가드 / `IProjectRepository.cs`에 `UpdateMemo` 선언 + 구현 1건 / **`MainViewModel.cs` 편집 분기에 `item.Memo = existingModel.Memo` 1건**(메모 소실 방어 — 기존 `IsPinned` 보존 줄 옆)
  - **T2**: `DashboardView.xaml`에 `CardMenu_Memo` 1건이 `CardMenu_History` **앞** / `OpenMemoCommand` 정의·바인딩 / `ProjectMemoDialog` 신규 2파일 / resw ko/en에 `ProjectMemo_*`·`CardMenu_Memo.Text`·`CardMenu_MemoView` 각 양쪽 존재
  - **T3**: `TimeText`·`FilteredCount`·`ProjectName` 정의 / **정렬이 `CompletedAt` 전체 기준으로 2곳(`RebuildGroups`의 그룹 간 + 그룹 내부 `:118`) 모두 반영** / `HistoryEntryViewModel` 생성처가 베이스 1곳뿐(grep) / **`RebuildGroups`가 넘기는 프로젝트명이 베이스 자기 필드이고 `ExportProjectName`(파생 필드 의존)이 아님**(D13 NRE 방지 — grep으로 확인) / `base(` 호출 2곳 모두 새 인자 전달 / `HistoryDialogViewModelBase`의 기존 public 멤버 **시그니처** 무변경(diff 확인 — 동작 변경은 D10 표에 열거된 2건만)
  - **T4**: `KindBrush`·`KindBackBrush` 정의와 `x:Bind` 함수 바인딩 소비 / 헤더 pill·`FilteredCount` 바인딩 / 그룹 헤더 `Entries.Count` / 행에 `TimeText` / `Converter=` 없이 배지 색 처리(함정 5 준수)
  - **T5**: `OpenAddPanel(string` 시그니처 잔존 + `TaskPage.xaml.cs`의 호출 1건 유지 / `ShowFormView`·`ShowListView` 정의 / `EditEntry_Click`이 중첩 다이얼로그 대신 폼 호출 / `ShowNestedDialogAsync`는 내보내기 오류 경로에 잔존
  - **T6**: `PageButtons`·`GoToPage` 정의 / `HistoryPage_InfoFormat` ko/en / `ProjectHistoryDialog.xaml`이 `PageButtons` 미소비(무변경 확인)
  - **공통**: 신규·변경 resw 키가 ko/en **양쪽** 파일에 동수로 존재(함정 2), `x:Uid` 소비 키에 속성 접미 존재(함정 3)
- 동작 확인(빌드로 검증 불가 → ⏳ HUMAN-VERIFY):
  - 카드 `⋯` → 메모: 다이얼로그 제목·placeholder·저장·재열기 시 내용 유지·[메모 삭제] 노출 조건·메뉴 라벨 "메모"↔"메모 보기" 전환·앱 재시작 후에도 메모 유지(DB 영속)
  - **메모를 저장한 뒤 그 프로젝트를 `⋯` → 프로젝트 설정에서 편집·저장 → 메모가 그대로 남아 있는지**(D11 소실 방어 — BLOCKER로 지적된 경로)
  - **기록이 여러 건 있는 프로젝트**에서 카드 `⋯` → 작업 기록을 열어 예외 없이 뜨는지(D13 초기화 순서 — 기록 0건으로는 검증되지 않는다)
  - 카드 `⋯` → 작업 기록: 헤더 pill·건수, 행의 시각·유형 색 배지·프로젝트 pill, 날짜 그룹 건수·구분선, 펼침 구분선·들여쓰기, 빈/검색 결과 0건 문구
  - [＋ 새 기록] → 폼 전환, 제목 빈 상태에서 저장 비활성, 날짜·시간 저장 후 행에 반영, [‹ 목록] 복귀
  - 편집(연필) → 같은 폼에서 날짜·시각까지 수정
  - 페이지 번호 이동(기록을 페이지 크기 이상 만든 뒤)·현재 페이지 강조
  - **작업 페이지에서 작업을 완료로 옮겼을 때** 작업기록 팝업이 제목이 채워진 폼으로 열리는지(D9 회귀 확인)
  - **전체 작업 기록(헤더 버튼) 회귀 확인**(D10): 화면 구성은 종전 그대로이고, 목록·검색·페이지 이동·마크다운 내보내기가 정상인지. 정렬과 내보내기에 시각이 반영되는 것은 의도된 변화다

## 통과 체크리스트
- [x] 근거 없는 단정 0 (시안 HTML·현행 코드·DB 스키마 직접 Read)
- [x] `## 요구 이해` 작성됨
- [x] Impact Analysis 4-A~4-D (전용 섹션 신설 — 심볼 추적 7종·계약 변경 4종·테스트 영향·재사용 10종. 1회차 리뷰가 지적한 프로젝트 재구축 경로 누락을 반영)
- [x] plan-reviewer 이슈 0 — 1회차 BLOCKER 2·MAJOR 5·MINOR 6 → 전건 반영 / 2회차 BLOCKER 1(D13 초기화 순서 NRE)·MINOR 4 → 전건 반영 / **3회차 BLOCKER 0·MAJOR 0·MINOR 2(문구 정합) → 반영 후 승인 가능 판정**. incomplete·미검증 잔여분 없음
- [x] 각 task에 검증 가능한 acceptance + 동시 만족 가능
- [x] Open Questions 없음(7건 해결)
- [x] 코드 중 결정 분기 0
- [x] Type 분류 명시(T1·T2·T3·T6 = C / T4·T5 = D)
- [x] Design 필드(전 task — 신규 심볼 보유)
- [x] Edge Cases 명시
- [x] Halt Forecast 명시
- [x] `## 시각 요소 분해` 작성됨(시각 충실도 요청 + 기준 디자인·렌더 이미지 3장 확보)
