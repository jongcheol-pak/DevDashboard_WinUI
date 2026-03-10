## 최근 변경
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
- 2026-03-09: 크래시 로그/예외 핸들러 제거 및 진입점 구조 단순화
- 2026-03-09: 부팅 시 자동실행 크래시 대응

## 미해결 이슈
- (없음)

## 주의사항
- System.Drawing.Common NuGet 패키지 추가됨 (아이콘 추출용)
- LauncherItems 테이블이 SQLite에 추가됨 (기존 DB 자동 마이그레이션)
- MainWindow 생성자에 LauncherRepository 파라미터 추가됨
- 런처 아이템은 Button 대신 Grid 사용 (CanDrag 호환성 — Button은 포인터 캡처로 드래그 불가)
- 런처 드래그&드롭: DashboardView와 동일한 DropPlaceholder 패턴 사용 (DisplayItems: ObservableCollection&lt;object&gt;)
