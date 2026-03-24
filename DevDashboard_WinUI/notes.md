## 최근 변경
- 2026-07-01: 테스트 목록 상태 모델 변경 — CheckBox(완료/미완료) → ComboBox 3상태(테스트/수정/완료)로 전환, 탭 4개(테스트/수정/완료/전체), 전체 탭에서 수정 항목 빨간색 표시, TestItem.IsCompleted → Status 프로퍼티 변경, DB Status 컬럼 마이그레이션
- 2026-07-01: 테스트 목록 카테고리 기반 구조 전환 — TestCategory 엔티티 추가, TestItem에 CategoryId 필드 추가, ProjectItem.Tests → TestCategories로 변경, DB 스키마(TestCategories 테이블/마이그레이션), IProjectRepository/SqliteProjectRepository 카테고리 CRUD, TestListDialogViewModel 카테고리 기반 재작성, TestListDialog XAML/코드비하인드 카테고리 카드 UI, 다국어 리소스 추가
- 2026-06-30: 테스트 목록 기능 구현 — 프로젝트 카드별 테스트 항목 관리 다이얼로그 추가 (TestItem 엔티티, TestItems DB 테이블, TestListDialog, 진행 내용(ProgressNote) 편집, 탭 필터링/날짜 그룹화, 중첩 다이얼로그 지원)
- 2026-03-20: 모든 동기 DB 호출 백그라운드 전환
- 2026-03-20: 가져오기(Import) 전면 개선 — DB 읽기/쓰기 Task.Run 백그라운드 실행(UI 프리징 방지), 중복 체크 일괄 수행, 런처 아이콘 추출 병렬화(최대 4개 동시), UWP 미설치 앱 PackageManager 필터링, null ExecutablePath 가드
- 2026-03-20: 앱 안정성 개선 — UnhandledException/UnobservedTaskException 전역 핸들러 추가, DialogService 동시 표시 방지(SemaphoreSlim), ContentDialog 예외 안전 처리, Process 객체 미해제 수정(LauncherViewModel/VersionCheckService), PropertyChanged 람다 및 Timer Tick 핸들러 해제 추가, NullToVisibilityConverter 주석 수정, 아이콘 캐시 크기 제한(500) 추가
- 2026-03-19: 런처 호버 애니메이션 성능 최적화 — Easing/Duration 캐싱, CenterPoint fallback, 인접 아이템만 애니메이션(범위 3), SetIsTranslationEnabled 1회 호출, 검색 디바운스(300ms) 추가
- 2026-03-10: 가져오기 후 그룹 미인식 버그 수정 — 초기화 후 DB Groups 테이블 비어 있을 때 SyncGroupsToDb 호출 추가
- 2026-03-10: 배포 전 코드 리뷰 — 14건 수정 (COM 이중 해제, null! 반환, COM 미해제, Closing async, DB 재사용, 캐시 키, CTS 미Dispose 등)
- 2026-03-10: 설정에서 런처 앱 초기화 버튼 추가 (프로젝트 데이터 유지, 런처만 삭제)
- 2026-03-10: 프로젝트 초기화를 DB 파일 삭제 → ClearProjectData() (테이블 DELETE)로 변경
- 2026-03-10: 설정에서 런처 사이드바 표시/숨김 토글 추가 (AppSettings.ShowLauncherSidebar)
- 2026-03-10: 런처 사이드바 — 파일 드래그&드롭 등록 (.lnk 바로가기 대상 resolve), AddAsync/AddItemAsync에 100개 제한·중복 가드 보강
- 2026-03-10: 가져오기 흐름 개선 — 프로젝트/런처 앱 각각 확인 팝업 후 선택적 추가, 미설치 앱 자동 제외
- 2026-03-10: 런처 드래그&드롭을 DropPlaceholder 패턴으로 전면 재작성 (아이콘 사이 드롭 지원)
- 2026-03-10: 좌측 런처 사이드바 기능 구현 (설치된 앱 등록/실행/삭제)
- 2026-03-09: ContentDialog 전환 및 중첩 다이얼로그 지원

## 미해결 이슈
- (없음)

## 주의사항
- System.Drawing.Common NuGet 패키지 추가됨 (아이콘 추출용)
- LauncherItems 테이블이 SQLite에 추가됨 (기존 DB 자동 마이그레이션)
- MainWindow 생성자에 LauncherRepository 파라미터 추가됨
- 런처 아이템은 Button 대신 Grid 사용 (CanDrag 호환성 — Button은 포인터 캡처로 드래그 불가)
- 런처 드래그&드롭: DashboardView와 동일한 DropPlaceholder 패턴 사용 (DisplayItems: ObservableCollection&lt;object&gt;)
