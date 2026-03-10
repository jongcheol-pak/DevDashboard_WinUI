## 최근 변경
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
