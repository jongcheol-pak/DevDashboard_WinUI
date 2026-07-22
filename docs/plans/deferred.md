# Deferred / Follow-up 대장

프로젝트 전반의 보류·후속 작업 대장. `plan.md` 교체 시 이번 작업과 무관한 Deferred 항목을 여기로 이관해 유실을 막는다. 재수용 시 해당 plan의 `## Deferred / Follow-up`로 옮긴다.

## 대기
- **[FR-S5] 프로젝트 설정(추가/편집) 다이얼로그 심화 restyle** — Should. 현재 다이얼로그가 시안(screen_11)보다 필드가 많고 Phase 0 팔레트로 이미 다크 적용됨 → 구조 재설계는 기능 손실 위험. 기능 손실 없는 재설계 방안 확정 후 별도 진행. (원 plan: Phase 1 설정, 2026-07-18)
- **[Todo* resw 고아 정리]** — 삭제된 TodoDialog/TodoDialogViewModel 전용 resw 키(`TodoDialog.*`·`TodoTab*`·`TodoStatus_*`·`TodoLabel_*`·`TodoEditTitle`·`TodoUncomplete*`·`TodoGroupBy*`·`TodoStatusChange*` ko/en ~40항목, `TodoButton`·`TodoPopupCheck`도 별도 확인)가 소스 미사용 상태로 잔존. 빌드·런타임 무해 — audit 후 제거. (원 plan: Phase 2 작업, 2026-07-19)
- **[교차 작업/테스트 집계 페이지]** (PRD D-2) — 작업·테스트 모두 현재 프로젝트 종속만 구현됨. 전체/프로젝트 스코프 필터 교차 집계 페이지(ProjectHistoryDialog 패턴)는 별도 진행. (원 plan: Phase 2·3, 2026-07-18/19)
- **[FR-T7 담당자(who) 필드]** — Could. 작업 항목 담당자 표시/편집. 사용자 제외 결정. (원 plan: Phase 2 작업, 2026-07-18)
- **[칸반 열 내 카드 재정렬]** — 작업 칸반은 상태 열 이동만 구현. 열 내 정렬 순서 영속화는 별도. (원 plan: Phase 2 작업, 2026-07-19)
- **[테스트→작업 역방향 링크/배지]** (FR-E4 확장) — 테스트 목록에서 연결된 작업 배지/링크 표시. 현재 TodoItem.LinkedTestId 단방향만. TestItem→Todo 역참조 조회 필요. 사용자 결정으로 Phase 3 제외. (원 plan: Phase 3 테스트, 2026-07-19)
- **[FR-T6/E4 "방법" 필드 확장 소비]** — Phase 3에서 TestItem.Method 추가·다이얼로그 노출. 향후 필터/통계에 활용 여부는 별도. (원 plan: Phase 3 테스트, 2026-07-19)
- **[Test* 구 resw 고아 정리]** — 삭제된 TestListDialog 전용 resw 키(`TestListDialogTitle`·`TestStatusTesting/Fix/Done.Content`·`TestTab*`·`TestGroupBy*`·`NewTestBox`·`NewCategoryBox`·`TestAddNoteLink`·`EmptyTestText`·`TestDeleteCategoryConfirm` 등)가 소스 미사용 상태로 잔존. 빌드·런타임 무해 — audit 후 제거. (원 plan: Phase 3 테스트, 2026-07-19)
- **[`TestDateGroup` 고아 정리]** — `Presentation/Models/TestDateGroup.cs`가 소스 미참조(구 테스트 다이얼로그 이전 버전 잔재, T6 이전부터 존재). 별도 정리. (원 plan: Phase 3 테스트, 2026-07-19)
- **[`HistoryEntryDialogViewModel` 고아 정리]** — `Presentation/ViewModels/HistoryEntryDialogViewModel.cs`가 소스 미참조(정의만 존재, 실제 작업 기록 편집 UI는 인라인 폼 + 중첩 편집 다이얼로그 코드비하인드가 담당). Phase 4 T4에서 미수정, 별도 정리 세션. (원 plan: Phase 4 작업 기록, 2026-07-19)
- **[`InitKindCombo`/`KindFromCombo` 중복]** — HistoryDialog·ProjectHistoryDialog 코드비하인드 2곳 대칭 중복(3회 미만이라 현재 결함 아님). 3번째 사용처 생기면 공용 static helper로 추출. (원 plan: Phase 4 작업 기록, 2026-07-19)
- **[알림 배지 상한·요약 개수]** — 헤더 벨 안읽음 배지에 "9+" 상한 미적용(두 자릿수 시 14px 원형 폭 확장은 되나 시각 확인 필요), 드롭다운 요약 상위 5개 고정. 시각 확인 후 수치 조정. 순수 값이라 저위험. (원 plan: Phase 5 알림, 2026-07-19)
- **[BOM/no-BOM 인코딩 통일]** — 레거시 파일(.cs/.xaml/.resw 다수)은 BOM, 신규 파일은 no-BOM으로 혼재. check-utf8-and-lines hook은 no-BOM 권고, CLAUDE.md는 기존 인코딩 유지 권고(Phase 4에서 BOM 손실이 MAJOR였음). 전체 통일은 별도 세션. (원 plan: Phase 5 알림, 2026-07-19)
- **[README/스크린샷 — 리디자인 5개 영역]** — Phase 0~5로 리디자인 로드맵 완료. 시각 렌더 사용자 확인 후 README·스크린샷을 5개 영역(작업/테스트/작업 기록/알림/설정) 신규 UI로 통합 갱신. (원 plan: Phase 5 알림, 2026-07-19)
- **[알림 배지 실시간 갱신]** — 안읽음 배지는 초기 로드 1회 + 헤더 Flyout 열 때만 재계산(온디맨드 설계). 작업 페이지에서 작업 완료/편집 후 Flyout 재오픈 전까지 배지가 stale일 수 있음(상호작용 시점엔 Flyout Opening이 먼저 재계산해 정확). 실시간 원하면 TaskPageViewModel 변경 콜백에 RebuildNotifications 배선. (원 plan: Phase 5 알림, 2026-07-19)

- **[NotificationPage 시안 대조]** — (구 `[TestPage·NotificationPage 시안 대조]`에서 **TestPage 몫은 2026-07-21 해소** — 시안 2장 확보 후 테스트 화면·등록 다이얼로그 재구현 완료). NotificationPage는 시안 미확보로 잔여. 시안 이미지 확보 후 진행. (원 plan: TaskPage 시안 정합, 2026-07-20)
- **[`BuildPassRateBadge` 조회 테이블화]** — 그룹마다 `_project.TestCategories`를 선형 탐색하고 `Rebuild()`는 드래그·편집마다 호출된다. 개인 대시보드 규모에선 무시할 비용이라 현재는 과한 최적화(같은 클래스의 `_testStatusById` 딕셔너리가 선례). 카테고리 수가 커지면 캐싱 검토. (원 plan: TaskPage 시안 정합 T3 quality 리뷰, 2026-07-20)
- **[완료 열 즉시 생성 시 작업기록 팝업 미발생]** — `AddTodo()`는 `MoveToStatus()`와 달리 `WorkLogRequested`를 발생시키지 않아, 칸반 "완료" 열의 `+새 작업`으로 만든 작업은 작업기록 팝업이 뜨지 않는다. 신규 결함이 아니라 기존 `AddTodo` 설계가 "완료 상태로 즉시 생성" 경로에 처음 노출된 것이며 `IsCompleted`/`CompletedAt` 동기화는 정상. UX 일관성 관점의 후속. (원 plan: TaskPage 시안 정합 T3 quality 리뷰, 2026-07-20)
- **[NU1903 의존성 취약점]** — `SQLitePCLRaw.lib.e_sqlite3` 2.1.11에 알려진 높음 심각도 권고(GHSA-2m69-gcr7-jv3q). 빌드 경고로 상시 노출되나 이번 작업과 무관한 기존 경고. 패키지 버전 업데이트 검토 필요. (F-7 리뷰 m3, 2026-07-20)
- **[칸반/목록 "미분류" 그룹 정렬 불일치]** — 칸반 `BuildColumnGroups`는 raw key(빈 문자열)로 정렬해 미분류가 항상 최상단, 목록 `RebuildCategoryGroups`는 표시명("미분류")으로 정렬해 한글 순서 중간에 온다. 시안에 그룹 정렬 규칙이 없어 결함 여부는 해석에 달림 — 통일 여부 결정 필요. (F-7 리뷰 m2, 2026-07-20)
- **[`TaskPageViewModel.*Items` 개명 검토]** — `WaitingItems`/`ActiveItems`/`CompletedItems`/`HoldItems`가 실제로는 `TaskColumnGroup` 목록이라 이름과 내용이 어긋난다. XAML 바인딩 경로 안정을 위해 이름을 유지했고 `CountItems()` 주석이 함정을 방어하지만, 이 불일치가 `*Count` 무성 오작동 위험의 근원이다. `*Groups`로 개명 검토. (F-7 리뷰 m4, 2026-07-20)
- **[AGENTS.md는 git 미추적 — PC 간 미공유]** — 2026-07-20에 `pjc:bootstrap-agents-md`로 루트에 AGENTS.md를 **생성 완료**(빌드 명령·구조·이 레포 함정 11건·컨벤션). 단 사용자의 **전역 `~/.gitignore_global:5`에 `AGENTS.md`가 등록**돼 있어 의도적으로 커밋하지 않는다(사용자 결정 2026-07-20 — 전역 정책 존중). 결과적으로 **이 PC에서만 유효**하며 다른 PC·새 클론에는 없다. 다른 PC에서 작업하게 되면 그 PC에서 다시 생성하거나, 그때 이 레포 `.gitignore`에 `!AGENTS.md` 예외를 넣어 추적으로 전환할 수 있다. (원 plan: TaskPage 시안 정합, 2026-07-20)

- **[시작일을 지울 수단 부재]** — 새 작업의 시작일 기본값이 오늘로 채워지면서(TaskEditDialog 시안 정합 D6, 사용자 결정) **"시작일 없는 작업"을 새로 만들 방법이 사라졌다** — `CalendarDatePicker`에는 선택한 날짜를 비우는 UI가 없다(편집 모드의 기존 빈 값은 그대로 유지). 지우기 버튼 또는 "날짜 없음" 옵션 추가 검토. (원 plan: TaskEditDialog 시안 정합, 2026-07-20)
- **[다른 다이얼로그의 라벨 스타일 불일치]** — `InputLabelStyle`(12px·저강도)이 `Styles.xaml`에 정의만 되고 **소비처가 0이었다**는 것은 다른 다이얼로그들이 모두 굵은 본문체(`BodyStrongTextBlockStyle`) 라벨을 쓴다는 뜻이다. TaskEditDialog만 시안 정합을 마쳐 **화면 간 라벨 스타일이 갈린 상태**. 다른 다이얼로그 시안 확보 후 일괄 정합 검토. (원 plan: TaskEditDialog 시안 정합 T1 조사, 2026-07-20)
- **[빈 제목 오류 문구 제거에 따른 접근성 약화]** — `TaskEdit_TitleRequired` 문구를 없애고 테두리 색으로 대체해(D3), 빈 제목으로 등록을 누르면 **아무 문구 없이 닫히지 않기만** 한다. 스크린 리더 등 시각 외 피드백이 사라졌다. `TextBox.Description` 또는 자동화 속성으로 보완 검토. (원 plan: TaskEditDialog 시안 정합, 2026-07-20)
- **[SUGGEST] `TaskEditDialog`의 선택적 `status` 파라미터** — C# 선택적 매개변수는 누락해도 컴파일 경고가 없어, **제3의 호출부가 새 작업을 만들며 `status`를 빠뜨리면 조용히 `Waiting`으로 생성**된다. 현재 호출부 2곳은 정확하고 편집 경로는 `existing?.Status ?? status`로 기본값을 항상 무시하므로 지금은 결함이 아니다. 3번째 호출부가 생기면 명시적 오버로드 분리 검토. (T2 quality 리뷰 SUGGEST, 2026-07-20)

- **[카테고리 필터에서 "미분류"를 고를 수 없음]** — `TaskPage`의 카테고리 필터 콤보는 `AvailableCategories`(실제 카테고리만)로 구성돼(`TaskPage.xaml.cs:72-75`) "미분류" 그룹만 골라 보는 필터가 없다. "미분류"가 표시 전용이 되면서 이 비대칭이 두드러진다. 필터에 "미분류" 항목 추가 검토. (원 plan: 다이얼로그 후속 수정 T2 조사, 2026-07-21)
- **[삭제된 카테고리를 가진 작업을 편집하면 카테고리가 사라짐]** — 앱 설정에서 카테고리를 지운 뒤 그 카테고리를 쓰던 작업을 편집하면, 콤보에 해당 값이 없어 미선택(null)으로 열리고 그대로 저장 시 **빈 카테고리가 된다**. 기존 코드도 같은 성질이었으나 D6으로 미선택이 정식 표현이 되면서 관찰 가능해졌다. 콤보에 "현재 값"을 임시 항목으로 추가하는 방식 검토. (원 plan: 다이얼로그 후속 수정 T2 Edge Case, 2026-07-21)
- **[카테고리 없는 작업을 새로 만들 수단 부재]** — 새 작업은 항상 첫 카테고리("UI·UX")가 자동 선택돼(D5, 사용자 결정) "미분류"로 두고 싶으면 콤보를 비울 방법이 필요하다(선택 해제 항목 또는 지우기 버튼). 기존 빈 카테고리 항목은 그대로 유지되므로 표시 규칙 자체는 계속 유효. (원 plan: 다이얼로그 후속 수정 D5의 귀결, 2026-07-21)

- [2026-07-21] 테스트 화면 실패 상태 색이 시안에선 붉게 보이나 PRD §3·코드는 호박(#E8B45A) 유지 — 붉은색을 원하면 순수 값 치환. (출처: 테스트 화면 시안 정합 D5)

- **[FR-E5 에러 표시 미착수]** — PRD FR-E5(Should) "목록 restyle(… 에러/메모 표시)" 중 메모 몫만 2026-07-21에 회수됐다. 에러 표시는 `TestItem`에 에러 필드가 없어 도메인 변경이 선행돼야 하고 시안에도 표현이 없어 미착수. 실패 사유를 목록에서 보고 싶다는 요구가 생기면 필드 추가부터 논의. (F-7 리뷰 M1, 2026-07-21)
- **[스위트 정리 경로 부재]** — 테스트 스위트(`TestCategory`)는 등록 시 작업 카테고리 **이름을 복사해 프로젝트 DB에 별도 생성**되므로 앱 설정에서 카테고리를 고치거나 지워도 전파되지 않는다. 여기에 스위트 이름수정·삭제 UI를 제거(사용자 확정)하고 빈 스위트를 화면에서 숨기면서, 오타·구 이름 스위트가 **DB에 남은 채 지울 방법이 없어졌다**. 정리 수단이 필요해지면 위 `[기존 자유명/"작업" 스위트 마이그레이션]`과 함께 다룬다. (원 plan: 테스트 화면 시안 정합 후속 D7, 2026-07-21)
- **[전체 필터 가설 기각 시 런타임 재조사]** — "미실행 → 전체" 필터 미동작의 원인을 `Tag=""`가 null로 파싱돼 패턴 매칭이 실패하는 것으로 보고 수정했으나(정적 근거), 빈 문자열로 파싱되고 있었다면 그 수정은 동작상 no-op다. 사용자 확인에서 증상이 재현되면 `pjc:pjc-systematic-debugging`으로 런타임 재조사한다. (원 plan: 테스트 화면 시안 정합 후속 D1, 2026-07-21)
- **[`AddSuite` 고아 정리]** — `TestPageViewModel.AddSuite`는 소비처 0인 채 남아 있다(`RenameSuite`/`DeleteSuite`는 2026-07-21에 제거). 이번 요청 범위 밖이라 보류. (원 plan: 테스트 화면 시안 정합 후속, 2026-07-21)
- **[SUGGEST] 진행바 폭 상수 이중화** — `TestPage.xaml.cs`의 `ProgressBarWidth`(120)와 `TestPage.xaml`의 트랙 `Grid Width="120"`이 주석으로만 동기화된다. 한쪽만 바꾸면 인디케이터 비율이 조용히 어긋난다. (T3 quality 리뷰 SUGGEST, 2026-07-21)
- **[테스트 행에서 방법(Method) 미표시]** — 메모는 2026-07-21에 행에 표시하도록 복원했으나 `TestItem.Method`는 시안에 없어 행 미표시 유지(편집 다이얼로그에서만 다룸). 목록에서 방법을 보고 싶다는 요구가 생기면 재검토. (원 plan: 테스트 화면 시안 정합 후속 T2, 2026-07-21)

> **[Test* 구 resw 고아 정리] 항목 보강(2026-07-21)**: 위 항목의 대상에 `TestEditCategoryTitle`·`TestEditNoteTitle`이 추가됐다(스위트 이름수정 다이얼로그·인라인 메모 다이얼로그가 제거되면서 고아화). 일괄 audit 시 함께 처리.
- [2026-07-21] 기존 자유명·"작업" 스위트에 들어간 테스트는 작업 카테고리 스위트로 마이그레이션하지 않음(go-forward) — 과거 데이터는 칸반 배지에 반영 안 됨. 필요 시 마이그레이션 별도 논의. (출처: 테스트 화면 시안 정합 D8)
- [2026-07-21] [SUGGEST] 테스트 상태색 3종(PRD §3)과 soft 3종(0x28 알파)이 `TestPage.xaml.cs`에 고정 — 색이 더 늘면 `Palette.xaml` Default 딕셔너리로 이관 검토. (출처: 테스트 화면 시안 정합 T5 quality S1)
- [2026-07-21] 테스트 행에서 방법(Method)·메모(ProgressNote) 표시 제거(시안 반영, 데이터·편집 경로는 유지) — 메모 보유 항목을 목록에서 구분할 표식이 필요한지 사용 후 판단. (출처: 테스트 화면 시안 정합 T5)
- [2026-07-21] 스위트 이름수정·삭제를 헤더 우클릭 메뉴로 이동(시안에 버튼 없음) — 발견성이 낮다는 피드백이 있으면 hover "…" 버튼 등 대안 검토. (출처: 테스트 화면 시안 정합 T5)
- [2026-07-21] 테스트 스위트 필터가 작업 카테고리만 담아 **레거시 스위트("작업"·자유명)를 골라볼 수 없고**, 실재하지 않는 카테고리를 고르면 안내 없이 빈 화면이 된다 — 항목을 `작업 카테고리 ∪ 실재 스위트명`으로 넓히거나 빈 결과 안내 문구 추가. (출처: F-7 리뷰 m2)
- [2026-07-21] 새 테스트 등록 시 스위트 기본값이 항상 첫 작업 카테고리라, 스위트 필터가 다른 값이면 **등록 직후 목록에 안 보여** 아무 일도 안 일어난 것처럼 보인다 — 등록 후 필터를 결과 스위트로 맞추거나 해제 검토. (출처: F-7 리뷰 m3)
- [2026-07-21] PRD `D-3`(per-project 명명 스위트 유지)이 이번 D1(스위트=작업 카테고리 고정, 자유 입력 제거)과 충돌 — PRD만 읽는 후속 세션이 오해할 수 있어 D-3에 갱신 노트 필요(**사용자 승인 대기**). (출처: F-7 리뷰 m1)

- **[테스트 화면 F-8 육안 확인 미완]** — 2026-07-21 테스트 화면 시안 정합 plan은 task T1~T4·Phase F·G(Must 100%)를 통과했으나 **F-8(렌더 육안 확인)이 미완**인 채 plan.md가 교체됐다(사용자 결정 2026-07-22 — 목록 뷰 작업 우선). 확인 대기 항목: ① 상태 아이콘 3종이 동일 크기 라운드 사각형(통과·실패는 연한 틴트 배경 + 색 글리프, 미실행은 회색 배경 + "○")으로 보이는지 ② 행 구분선·pill 폭·hover 배경·메모 블록 ③ 진행바 두께가 통과율과 무관하게 일정한지·빈 스위트 미표시 ④ **"미실행 → 전체" 필터 실동작**(재현 시 `[전체 필터 가설 기각 시 런타임 재조사]` 항목 발동) ⑤ 메모 다이얼로그 레이아웃·비우고 저장 시 삭제 ⑥ 선택 탭 굵기·배지 미굵음·hover 시 밑줄 유지 ⑦ 우클릭 메뉴 후 hover 배경 잔존 여부. (원 plan: 테스트 화면 시안 정합 후속 F-8, 2026-07-21)

## 종결
- [2026-07-20 → 2026-07-22] **[목록 뷰 시안 대조]** — 재수용(시안 원본 HTML의 `taskViewList` 분기 확보 → TaskPage 목록 뷰 시안 정합 plan)
- [2026-07-19 → 2026-07-20] **[미사용 심볼 정리]** — 반영(TaskPage 시안 정합 T4: `ShowKanban`/`ShowList`/`TotalCount`/고아 using, TaskEditDialog 시안 정합 T3: `x:Name="TitleBox"`)

## 완료·재수용
- [2026-07-18 → 2026-07-19] FR-H2 선행 PageSize 실제 소비(작업 기록 페이지네이션) — 반영(Phase 4 plan T3 로직·T5 UI 완료)
