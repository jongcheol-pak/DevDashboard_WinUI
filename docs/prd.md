# PRD — DevDashboard 리디자인 (claude.ai/design 시안 반영)

> 상태: **초안 (승인 대기)** · 작성일 2026-07-18
> 디자인 출처: claude.ai/design 프로젝트 "DevDashboard WinUI 검토" — `DevDashboard Redesign.dc.html` (13개 화면)
> 이 PRD 승인 후 고정. 이후 요구 변경은 PRD → plan → 코드 순서로만 반영.

## 1. 개요·목표

WinUI 3 DevDashboard의 5개 영역(작업/테스트/작업 기록/알림/설정)을 디자인 시안에 맞춰 리디자인한다. 단순 restyle이 아니라 시안이 도입하는 **신규 기능(작업 칸반·드래그앤드롭, 작업 카테고리, 테스트 상태 배지, 마감 알림, 페이지네이션 등)까지 전부 구현**한다. 색상은 디자인 다크 팔레트로 통일하고, 작업/테스트/알림은 다이얼로그에서 **전체 페이지**로 전환한다.

**사용자 확정 방침 (2026-07-18 스코핑):**
- 범위: restyle + 신규기능 전부
- 화면 구조: 작업/테스트/알림 → 전체 페이지 전환 (작업 기록·설정은 다이얼로그 유지)
- 색상: 디자인 팔레트로 통일
- 진행: 영역별 단계 분할 (Phase별 plan.md 별도 실행)

## 2. 현재 구현 기준선 (조사 완료 — Investigation 근거)

| 영역 | 현재 상태 | 시안 대비 격차 |
|---|---|---|
| 셸/네비 | MainWindow.xaml이 실제 셸(헤더/런처사이드바/그룹탭/footer). 콘텐츠 스왑 앵커 = `ContentControl DashboardContent`(MainWindow.xaml:345). 페이지 개념 없음, 다이얼로그는 code-behind `new+ShowAsync` 3경로(DashboardView.xaml.cs 카드 핸들러 / MainWindow.xaml.cs / DialogService) | 페이지 네비게이션 컨테이너 신규 필요 |
| 색상/폰트 | 100% 시스템 `{ThemeResource}` 의존, 커스텀 팔레트·ThemeDictionaries 없음. 테마는 `RequestedTheme` 런타임 스위칭(Light/Dark/System). 하드코딩 색은 `TagColorConverter` 1곳. 폰트 미지정(Segoe UI Variable) | 커스텀 다크 팔레트 리소스 + Noto Sans KR 전역 폰트 신규 |
| 작업(Todo) | `TodoItem`(Text/IsCompleted/`TodoStatus`(Waiting/Active/Completed)/CompletedAt/Description/CreatedAt), `ProjectItem.Todos` 종속. 우선순위·카테고리·시작/종료일·담당자·칸반 없음. 로직이 TodoDialog.xaml.cs 집중. `Description`+`TodoDetailDialogViewModel`은 미연결 고아 | 4번째 상태(보류)·카테고리·우선순위·시작/종료일·담당자·칸반·드래그·테스트연결 신규 |
| 테스트 | `TestCategory→TestItem`, 상태 string `Testing/Fix/Done`. `ProjectItem.TestCategories` 종속. 작업↔테스트 연결·카드 배지 미구현(`HasInProgressTest` 계산 프로퍼티만 준비) | 상태 모델 전환(통과/실패/미실행), 작업 연결, 카드 배지, 통계 신규 |
| 작업 기록 | `HistoryEntry`(Title/Description/CompletedAt/CreatedAt) 5필드, 유형 없음. `HistoryDialog`(프로젝트별)+`ProjectHistoryDialog`(전체) 중복. 커밋 연동 없음 | 유형(kind)·페이지네이션 신규(검색은 있음) |
| 알림 | 없음 | 전체 신규(마감 감지·읽음상태·페이지+패널) |
| 설정 | AppSettingsDialog 좌측 메뉴(설정/도구/코드/정보). AppSettings에 ThemeMode/SortOrder/Groups/Tools/TechStackTags/Categories/Language 등. 페이지네이션·작업카테고리·작업기록유형 없음 | 페이지당 표시 개수, 작업 카테고리 관리, 작업 기록 유형 관리 신규 |

## 3. 디자인 시안 확정 데이터 (시안 스크립트에서 추출)

- **칸반 상태(4)**: 예정 / 진행 중 / 완료 / 보류
- **작업 우선순위(3)**: 높음 / 보통 / 낮음 (기본 보통)
- **기본 작업 카테고리**: UI·UX, 프론트엔드, 백엔드 (각 색상 dot, 기본은 삭제 불가)
- **기본 작업 기록 유형**: 기능, 수정, 리팩터링, 문서 (기본은 삭제 불가)
- **테스트 상태(3)**: 통과(#5aa3e8 ✓) / 실패(#e8b45a ✕) / 미실행(#8a8890 ○)
- **페이지당 표시 개수**: 100~1000, step 10, 기본 100 (작업 기록 목록에 적용)
- **알림 대상**: 종료일 D-3 임박 + 오늘 마감 + 경과, `완료` 상태 제외, 읽음/안읽음 추적
- **팔레트**: 배경 #131316 / 카드 #1c1c20 / 테두리 #26262c~#2e2e34 / 강조 코랄 #f0716a / 본문 #e8e6e3 / 보조텍스트 #8a8890 등. 폰트: 시안은 Noto Sans KR → **Malgun Gothic으로 대체**(D-8)

## 4. 기능 요구 (FR) — 영역별

우선순위: **Must** / Should / Could. Phase는 §6 로드맵 참조.

### 공통 기반 (Phase 0)
- **FR-C1 (Must)**: 디자인 다크 팔레트를 커스텀 색상 리소스로 정의하고 앱 전역 시스템 브러시를 다크 값으로 오버라이드해 **다크 단일**로 고정한다(D-4 — ThemeDictionaries 분기 불필요).
- **FR-C2 (Must)**: 전역 기본 폰트를 **Malgun Gothic**(Windows 기본 한글 폰트)으로 지정한다(아이콘용 Segoe MDL2 Assets·코드용 Consolas는 유지). ※ 시안은 Noto Sans KR이나 번들 자산 회피를 위해 사용자 결정(2026-07-18)으로 Malgun Gothic 채택 — 유사 고딕체.
- **FR-C3 (Must, Phase 2에서 구현)**: 작업/테스트/알림을 전체 페이지로 표시할 수 있는 페이지 네비게이션 컨테이너를 `DashboardContent` 앵커에 도입한다(대시보드 ↔ 페이지 전환, "대시보드" 뒤로가기). 실제 첫 페이지(작업)와 함께 Phase 2에서 구축(빈 컨테이너 선구현 회피).
- **FR-C4 (Should)**: `TagColorConverter` 하드코딩 색을 팔레트와 정합되게 조정.

### 설정 (Phase 1)
- **FR-S1 (Must)**: 앱 설정 "설정" 메뉴에 "페이지당 표시 개수"(number 100~1000, step 10, 기본 100)를 추가하고 `AppSettings`에 영속화한다.
- **FR-S2 (Must)**: 앱 설정 "코드" 메뉴에 "작업 카테고리" 관리(기본+사용자 정의, 추가/삭제, 기본은 삭제 불가, 색 dot)를 추가하고 `AppSettings`에 영속화한다.
- **FR-S3 (Must)**: 앱 설정 "코드" 메뉴에 "작업 기록 유형" 관리(기본+사용자 정의, 추가/삭제)를 추가하고 `AppSettings`에 영속화한다.
- **FR-S4 (Must)**: 앱 설정 다이얼로그 전체를 디자인 팔레트/레이아웃으로 restyle. **테마 선택 UI 제거**(D-4 다크 단일 고정).
- **FR-S5 (Should)**: 프로젝트 설정(추가/편집) 다이얼로그를 디자인 레이아웃으로 restyle.

### 작업 (Phase 2)
- **FR-T1 (Must)**: `TodoItem`에 상태 4값(예정/진행 중/완료/보류), 카테고리, 우선순위(높음/보통/낮음), 시작일/종료일 필드를 추가하고 영속화(마이그레이션 포함).
- **FR-T2 (Must)**: 작업을 전체 페이지로 전환하고 칸반(4열 상태)/목록 뷰 전환을 제공한다.
- **FR-T3 (Must)**: 칸반 카드 드래그앤드롭으로 상태를 변경한다.
- **FR-T4 (Must)**: 작업 카테고리 필터, 카테고리별 그룹핑, 상태별 개수 표시.
- **FR-T5 (Must)**: 새 작업/작업 편집 다이얼로그(제목/설명/카테고리/우선순위/시작·종료일) + 삭제 확인 다이얼로그.
- **FR-T6 (Should)**: 새 작업 다이얼로그의 "테스트 추가" 토글 — 작업을 테스트 목록에도 등록(작업↔테스트 연결). (Phase 3 테스트 연결과 연동)
- **FR-T7 (Could)**: 담당자(who) 필드 표시/편집.
- **FR-T8 (Should)**: 작업 카드에 연결 테스트 상태 배지 표시(예: 테스트 미실행/통과/실패). (Phase 3와 연동)

### 테스트 (Phase 3)
- **FR-E1 (Must)**: 테스트를 전체 페이지로 전환하고, 상태별 통계 카드 + 상태 필터 탭을 제공한다.
- **FR-E2 (Must)**: 테스트 상태 모델을 디자인 기준(통과/실패/미실행)으로 정렬한다(현재 Testing/Fix/Done에서 전환, 마이그레이션 포함).
- **FR-E3 (Must)**: 테스트 등록/편집 다이얼로그(이름/스위트/방법), 메모 다이얼로그, 삭제 확인 다이얼로그.
- **FR-E4 (Should)**: 테스트↔작업 연결(연결 배지·링크), 작업 카드 배지 배선(FR-T8).
- **FR-E5 (Should)**: 테스트 목록 restyle(스위트 그룹, 통과율 바, 상태 아이콘/배지, 에러/메모 표시).

### 작업 기록 (Phase 4)
- **FR-H1 (Must)**: `HistoryEntry`에 유형(kind) 필드 추가, 앱 설정의 작업 기록 유형(+작업 카테고리)에서 선택. 영속화(마이그레이션 포함).
- **FR-H2 (Must)**: 작업 기록 다이얼로그에 페이지네이션 적용(페이지당 표시 개수 = FR-S1).
- **FR-H3 (Must)**: 작업 기록 다이얼로그를 디자인 레이아웃으로 restyle(검색·유형 배지·날짜 그룹·펼침 상세·내보내기·인라인 새 기록 폼).
- **FR-H4 (Should)**: HistoryDialog·ProjectHistoryDialog 중복 정리(공통 컴포넌트화 또는 통합).

### 알림 (Phase 5)
- **FR-N1 (Must)**: 마감 알림 감지 로직 — 미완료 작업 중 종료일 D-3 임박·오늘 마감·경과 항목 집계(완료 제외).
- **FR-N2 (Must)**: 알림 전체 페이지(프로젝트별 그룹, 읽음/안읽음, 모두 읽음, 항목 클릭 시 해당 작업으로 이동).
- **FR-N3 (Must)**: 헤더 알림 드롭다운 패널(요약 목록 + "모든 알림 보기").
- **FR-N4 (Must)**: 읽음 상태 영속화.

## 5. 비기능 요구 (NFR)
- **NFR-1**: MSBuild x64 빌드 경고/에러 0 유지(AnyCPU 금지 — MSIX 오류).
- **NFR-2**: DDD 4계층 구조 준수(Domain/Infrastructure/Presentation/Shared). 도메인 로직은 Domain, 영속성은 Infrastructure.
- **NFR-3**: 기존 데이터 하위호환 — 스키마 변경은 `AddColumnIfNotExists` + `AllowedIdentifiers` 화이트리스트 방식 마이그레이션, 기존 사용자 데이터 무손실.
- **NFR-4**: 다국어(x:Uid/.resw ko-KR·en-US) 유지 — 신규 UI 문자열은 양 언어 리소스에 등록.
- **NFR-5**: CommunityToolkit.Mvvm 패턴 준수, 신규 비즈니스 로직·공개 API에 단위 테스트(테스트 프로젝트 유무 확인 후).

## 6. Phase 로드맵 (영역별 단계 분할)

각 Phase는 승인 후 별도 plan.md로 실행. Phase 0(기반)을 먼저 두어 이후 Phase가 공통 팔레트/폰트/네비 위에서 작업하게 한다.

| Phase | 영역 | 핵심 | 의존 |
|---|---|---|---|
| 0 | 공통 기반 | 다크 팔레트 통일·Malgun Gothic 전역 폰트·TagColorConverter 정합 | — |
| 1 | 설정 | 페이지당 표시 개수·작업 카테고리·작업 기록 유형·테마셀렉터 제거·restyle | 0 |
| 2 | 작업 | 페이지 네비 컨테이너(FR-C3)·칸반 페이지·드래그·카테고리·우선순위·날짜·다이얼로그 | 0,1(작업 카테고리) |
| 3 | 테스트 | 테스트 페이지·상태 전환·작업 연결·배지 | 0,1,2 |
| 4 | 작업 기록 | 유형·페이지네이션·restyle | 0,1(유형·페이지크기) |
| 5 | 알림 | 마감 감지·페이지·패널·읽음상태 | 0,2(종료일) |

> Phase 0과 1은 규모가 작으면 한 plan으로 묶을 수 있다(승인 시 결정).

## 7. Out of Scope (영구 제외)
- git 커밋 ↔ 작업 기록 자동 연동 (현재 분리, 신규 설계 필요 — 이번 리디자인 범위 밖).
- 대시보드 카드 자체의 대규모 기능 변경(카드 액션 재배치 등 시안의 대시보드 화면은 참고만, 이번 5개 영역 외).
- 실제 테스트 러너 연동(테스트 자동 실행·dur성능 측정) — 상태는 수동 관리.

## 8. 결정 사항 (2026-07-18 확정)
- **D-1 테스트 상태 전환**: ✅ **디자인대로 전환** — 통과/실패/미실행. 마이그레이션 매핑 Done→통과, Fix→실패, Testing→미실행. (FR-E2)
- **D-2 작업/테스트 소유 범위**: ✅ **현재 프로젝트 종속 유지 + 교차(cross-project) 집계 페이지 추가**(전체/프로젝트 스코프 필터, ProjectHistoryDialog 패턴 참고). 전역 재설계 안 함.
- **D-3 테스트 그룹핑**: ✅ **현재 per-project TestCategory(명명 스위트) 유지**. 시안 라벨만 '스위트'로. 기존 테스트 데이터 보존.
- **D-4 테마**: ✅ **다크 단일 고정** — 테마 선택(Light/Dark/System) 제거. Phase 0에서 전역 다크 팔레트 고정(ThemeDictionaries 불필요, 시스템 브러시를 다크 값으로 오버라이드), Phase 1에서 설정의 테마 셀렉터 UI 제거. **시안의 설정 화면에 남아 있는 테마 셀렉터는 이 결정으로 의도적으로 제외**(사용자 결정이 시안에 우선).
- **D-5 작업 기록 다이얼로그 통합**: Phase 4 계획 시 확정(기본 제안: 공통 컴포넌트화).
- **D-6 고아 자산 처리**: Phase 2 계획 시 확정(기본 제안: `Description`을 작업 설명으로 활용, `TodoDetailDialogViewModel`은 정리 또는 통합).
- **D-7 시작 Phase**: ✅ **Phase 0부터 순차**.
- **D-8 전역 폰트**: ✅ **Malgun Gothic** — 시안의 Noto Sans KR 번들 대신 Windows 기본 한글 폰트 채택(자산 불필요). (FR-C2)

## 9. 성공 기준
- 5개 영역이 디자인 시안과 시각·기능적으로 정합(시각 요소 분해 기준 대조).
- 신규 도메인/데이터가 기존 데이터 손실 없이 마이그레이션됨.
- MSBuild x64 경고/에러 0, 다국어 리소스 정합.
- 각 Phase가 독립적으로 빌드·동작.
