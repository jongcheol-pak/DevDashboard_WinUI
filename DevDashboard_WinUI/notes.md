## 최근 변경
- 2026-04-26: README.md 리뷰 보강 — (1) 다국어 표기를 본문/설정 섹션 모두 "시스템 기본 / 한국어 / English (재시작 후 반영)"로 통일, (2) 빌드 결과 경로를 `bin/{Platform}/{Configuration}/net10.0-windows10.0.26100.0/` 일반형 + Debug/Release 예시로 보강, (3) 아이콘 캐시 표 항목을 실제 경로(`LauncherIcons\` 폴더 + `launcher_icon_cache.json` 인덱스, LRU 500개·20% 정리)로 명시. 코드 변경 없음
- 2026-04-26: 솔루션 루트에 `LICENSE` (MIT, Copyright © 2026 JongCheol Pak) 추가, `README.md` 라이선스 섹션을 `[MIT License](LICENSE) 하에 배포됩니다.` 로 갱신
- 2026-04-26: 솔루션 루트 `README.md` 신규 작성 — MailTrayNotifier README 스타일을 따라 헤더 아이콘(`docs/screenshots/appicon.png`) + 주요 기능(카드/실행 액션/To-Do·테스트·기록/런처/설정) + 설치(MSBuild x64) + 사용 방법(스크린샷 6종 임베드) + 설정 파일 위치 + 주요 의존성 + 제한 사항 + 라이선스 섹션으로 구성. 이미지 경로는 솔루션 루트 기준 `docs/screenshots/`. 코드/기능 변경은 없음 (문서 신규 작성)
- 2026-04-20: 런처 중복 클릭 방지 (커맨드 레벨) — `Launch`/`LaunchAsAdmin`을 `async Task` + `[RelayCommand(AllowConcurrentExecutions = false)]`로 전환. 내부 구현은 `LaunchAppAsync`로 분리하고 `Task.Run` 이후 `Task.Delay(800ms)` 쿨다운을 두어 `Process.Start`가 즉시 리턴되는 케이스에서도 더블클릭/연타 시 추가 실행이 차단됨. 커맨드 이름(`LaunchCommand`/`LaunchAsAdminCommand`) 유지로 `MainWindow.xaml.cs` 호출부 변경 불필요
- 2026-04-20: 런처 앱 실행 UI 프리징 개선 — `LauncherViewModel.LaunchApp`에서 `Process.Start(UseShellExecute=true)` 호출을 `Task.Run`으로 감싸 백그라운드 스레드에서 수행. UAC 프롬프트/shell 초기화/느린 앱 로딩 시 UI 스레드 블록 방지. Fire-and-forget 패턴, 공개 API 시그니처 변경 없음
- 2026-04-20: 태그 마키 CPU/GPU 부하 최적화 — `MarqueeTagsControl` 내부 로직만 수정. 평상시에는 정적 트리밍 + "+N" 배지로 표시하고, 포인터가 **카드 전체** 영역에 올라온 경우에만 마키 재생 (Loaded 시 시각 트리에서 DataContext 경계로 카드 루트를 찾아 PointerEntered/Exited 구독, Unloaded 시 해제). `EffectiveViewportChanged`로 뷰포트 밖(스크롤로 가려진) 카드는 자동 정지, `UISettings.AnimationsEnabled=false`면 재생 억제. 공개 API/DP 시그니처·DashboardView 바인딩 변경 없음
- 2026-07-02: To-Do 상태 모델 변경 — CheckBox(완료/미완료) → ComboBox 3상태(대기/진행 중/완료)로 전환, 탭 4개(대기/진행 중/완료/전체)로 변경, 탭에 항목 개수 표시, TodoItem.Status 프로퍼티 추가, DB Todos.Status 컬럼 마이그레이션, 새 할 일 입력을 대기 탭으로 이동, "완료됨" → "완료" 문구 변경
- 2026-07-01: 테스트 목록 상태 모델 변경 — CheckBox(완료/미완료) → ComboBox 3상태(테스트/수정/완료)로 전환, 탭 4개(테스트/수정/완료/전체), 전체 탭에서 수정 항목 빨간색 표시, TestItem.IsCompleted → Status 프로퍼티 변경, DB Status 컬럼 마이그레이션
- 2026-07-01: 테스트 목록 카테고리 기반 구조 전환 — TestCategory 엔티티 추가, TestItem에 CategoryId 필드 추가, ProjectItem.Tests → TestCategories로 변경, DB 스키마(TestCategories 테이블/마이그레이션), IProjectRepository/SqliteProjectRepository 카테고리 CRUD, TestListDialogViewModel 카테고리 기반 재작성, TestListDialog XAML/코드비하인드 카테고리 카드 UI, 다국어 리소스 추가
- 2026-06-30: 테스트 목록 기능 구현 — 프로젝트 카드별 테스트 항목 관리 다이얼로그 추가 (TestItem 엔티티, TestItems DB 테이블, TestListDialog, 진행 내용(ProgressNote) 편집, 탭 필터링/날짜 그룹화, 중첩 다이얼로그 지원)
- 2026-03-20: 모든 동기 DB 호출 백그라운드 전환
- 2026-03-20: 가져오기(Import) 전면 개선 — DB 읽기/쓰기 Task.Run 백그라운드 실행(UI 프리징 방지), 중복 체크 일괄 수행, 런처 아이콘 추출 병렬화(최대 4개 동시), UWP 미설치 앱 PackageManager 필터링, null ExecutablePath 가드
- 2026-03-20: 앱 안정성 개선 — UnhandledException/UnobservedTaskException 전역 핸들러 추가, DialogService 동시 표시 방지(SemaphoreSlim), ContentDialog 예외 안전 처리, Process 객체 미해제 수정(LauncherViewModel/VersionCheckService), PropertyChanged 람다 및 Timer Tick 핸들러 해제 추가, NullToVisibilityConverter 주석 수정, 아이콘 캐시 크기 제한(500) 추가

## 미해결 이슈
- (없음)

## 주의사항
- System.Drawing.Common NuGet 패키지 추가됨 (아이콘 추출용)
- LauncherItems 테이블이 SQLite에 추가됨 (기존 DB 자동 마이그레이션)
- MainWindow 생성자에 LauncherRepository 파라미터 추가됨
- 런처 아이템은 Button 대신 Grid 사용 (CanDrag 호환성 — Button은 포인터 캡처로 드래그 불가)
- 런처 드래그&드롭: DashboardView와 동일한 DropPlaceholder 패턴 사용 (DisplayItems: ObservableCollection&lt;object&gt;)
