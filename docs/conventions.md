# DevDashboard 작업 함정

> 규약: pjc 하니스 플러그인 `skills/REPO-CONVENTIONS.md` — 위치·항목 형식·작성 규칙·읽기·쓰기의 정본이다.
> 2026-10-20 에 위키 vault `20_projects/personal/devdashboard-winui/conventions*.md` 1파일에서 항목 원문 그대로 옮겼다(위키 링크만 평문으로 바꿨다). 절 제목은 언제 읽는가다 — 하위 파일에서 온 절은 `## conventions — <하위 문서 제목>` 아래에 원래 절을 한 단계 내려 두었다.

## 초기화·예외 경로

- [2026-09-07] **`ApplicationData.Current.LocalFolder` 를 «정적 필드 초기화자» 에 두면 비패키지 실행에서 `TypeInitializationException` 이 난다 — 그 타입을 전역 예외 핸들러가 쓰고 있으면 `e.Handled = true` 에 닿지 못한 채 앱이 죽는다.** 로거처럼 **예외 경로에서 불리는 타입**은 경로 계산을 `Lazy<T>` 로 감싸 `try` 안에서 평가한다(`Infrastructure/Services/AppLog.cs` 선례).

## 계층 배치

- [2026-09-07] **서비스를 `Presentation/Services` 에 둘지 `Infrastructure/Services` 에 둘지 가르는 기준은 «UI 타입 사용 여부가 아니다»** — 이 레포는 `DialogService` 가 Infrastructure 에서 이미 `ContentDialog` 를 띄운다. 기준은 **Presentation 계층 타입(ViewModel)에 의존하는가** 이고, 의존하면 Infrastructure 에 둘 수 없다(의존 역류).
