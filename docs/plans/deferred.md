# Deferred / Follow-up 대장

프로젝트 전반의 보류·후속 작업 대장. `plan.md` 교체 시 이번 작업과 무관한 Deferred 항목을 여기로 이관해 유실을 막는다. 재수용 시 해당 plan의 `## Deferred / Follow-up`로 옮긴다.

## 대기
- **[FR-S5] 프로젝트 설정(추가/편집) 다이얼로그 심화 restyle** — Should. 현재 다이얼로그가 시안(screen_11)보다 필드가 많고 Phase 0 팔레트로 이미 다크 적용됨 → 구조 재설계는 기능 손실 위험. 기능 손실 없는 재설계 방안 확정 후 별도 진행. (원 plan: Phase 1 설정, 2026-07-18)
- **[FR-H2 선행] 페이지당 표시 개수(PageSize) 실제 소비** — 작업 기록 다이얼로그 페이지네이션. PRD 로드맵 Phase 4에서 구현 예정(설정값은 Phase 1에서 이미 영속화됨). (원 plan: Phase 1 설정, 2026-07-18)
- **[Todo* resw 고아 정리]** — 삭제된 TodoDialog/TodoDialogViewModel 전용 resw 키(`TodoDialog.*`·`TodoTab*`·`TodoStatus_*`·`TodoLabel_*`·`TodoEditTitle`·`TodoUncomplete*`·`TodoGroupBy*`·`TodoStatusChange*` ko/en ~40항목, `TodoButton`·`TodoPopupCheck`도 별도 확인)가 소스 미사용 상태로 잔존. 빌드·런타임 무해 — audit 후 제거. (원 plan: Phase 2 작업, 2026-07-19)
- **[교차 작업/테스트 집계 페이지]** (PRD D-2) — 작업·테스트 모두 현재 프로젝트 종속만 구현됨. 전체/프로젝트 스코프 필터 교차 집계 페이지(ProjectHistoryDialog 패턴)는 별도 진행. (원 plan: Phase 2·3, 2026-07-18/19)
- **[FR-T7 담당자(who) 필드]** — Could. 작업 항목 담당자 표시/편집. 사용자 제외 결정. (원 plan: Phase 2 작업, 2026-07-18)
- **[칸반 열 내 카드 재정렬]** — 작업 칸반은 상태 열 이동만 구현. 열 내 정렬 순서 영속화는 별도. (원 plan: Phase 2 작업, 2026-07-19)
- **[미사용 심볼 정리]** — `TaskPageViewModel.ShowKanban`/`ShowList` RelayCommand 미사용(RadioButton TwoWay 직접 바인딩), `TaskEditDialog.xaml`의 `x:Name="TitleBox"` 미사용. quality 리뷰 지적, 무해 — 정리 검토. (원 plan: Phase 2 작업, 2026-07-19)
- **[테스트→작업 역방향 링크/배지]** (FR-E4 확장) — 테스트 목록에서 연결된 작업 배지/링크 표시. 현재 TodoItem.LinkedTestId 단방향만. TestItem→Todo 역참조 조회 필요. 사용자 결정으로 Phase 3 제외. (원 plan: Phase 3 테스트, 2026-07-19)
- **[FR-T6/E4 "방법" 필드 확장 소비]** — Phase 3에서 TestItem.Method 추가·다이얼로그 노출. 향후 필터/통계에 활용 여부는 별도. (원 plan: Phase 3 테스트, 2026-07-19)
- **[Test* 구 resw 고아 정리]** — 삭제된 TestListDialog 전용 resw 키(`TestListDialogTitle`·`TestStatusTesting/Fix/Done.Content`·`TestTab*`·`TestGroupBy*`·`NewTestBox`·`NewCategoryBox`·`TestAddNoteLink`·`EmptyTestText`·`TestDeleteCategoryConfirm` 등)가 소스 미사용 상태로 잔존. 빌드·런타임 무해 — audit 후 제거. (원 plan: Phase 3 테스트, 2026-07-19)
- **[`TestDateGroup` 고아 정리]** — `Presentation/Models/TestDateGroup.cs`가 소스 미참조(구 테스트 다이얼로그 이전 버전 잔재, T6 이전부터 존재). 별도 정리. (원 plan: Phase 3 테스트, 2026-07-19)

## 완료·재수용
- (없음)
