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

- **[목록 뷰 시안 대조]** — 사용자 제공 시안 이미지가 칸반만 담고 있어 목록 뷰는 무변경(기존 `TaskCardTemplate`·상태 콤보 존치). 목록 뷰 시안 확보 시 별도 진행. (원 plan: TaskPage 시안 정합, 2026-07-20)
- **[TestPage·NotificationPage 시안 대조]** — 사용자가 "작업 페이지만 먼저"로 범위 한정. 두 페이지도 같은 이유(목업 부재)로 시안과 갈라졌을 가능성이 크다. 각 페이지 시안 이미지 확보 후 진행. (원 plan: TaskPage 시안 정합, 2026-07-20)
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

## 종결
- [2026-07-19 → 2026-07-20] **[미사용 심볼 정리]** — 반영(TaskPage 시안 정합 T4: `ShowKanban`/`ShowList`/`TotalCount`/고아 using, TaskEditDialog 시안 정합 T3: `x:Name="TitleBox"`)

## 완료·재수용
- [2026-07-18 → 2026-07-19] FR-H2 선행 PageSize 실제 소비(작업 기록 페이지네이션) — 반영(Phase 4 plan T3 로직·T5 UI 완료)
