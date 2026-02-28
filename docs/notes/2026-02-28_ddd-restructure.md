# 2026-02-28 DDD 4계층 구조 적용

## 작업 목적
- WinUI 3 마이그레이션 프로젝트에 DDD(Domain-Driven Design) 계층 구조를 상세 적용
- 영문 주석 한글화, 한글 주석 보강

## 폴더 구조 변경

### 이전
```
DevDashboard_WinUI/
├── Models/           (모든 모델 혼재)
├── Services/         (인프라+도메인 서비스 혼재)
├── ViewModels/
├── Views/
├── Converters/
├── Controls/
└── Collections/
```

### 이후
```
DevDashboard_WinUI/
├── GlobalUsings.cs           (전역 Domain using 선언)
├── Domain/
│   ├── Entities/             (ProjectItem, TodoItem, HistoryEntry, CommandScript, ExternalTool, AppSettings)
│   ├── ValueObjects/         (ProjectGroup, GitFileStatus, OpenSourceItem, AddCardPlaceholder, DropPlaceholder)
│   ├── Enums/                (SortOrder, ViewMode, ThemeMode, ShellType, LanguageSetting)
│   └── Repositories/         (IProjectRepository — DIP 인터페이스)
├── Infrastructure/
│   ├── Persistence/          (DatabaseContext, SqliteProjectRepository)
│   └── Services/             (GitHelper, VersionCheckService, LocalizationService, DialogWindowHost, ...)
├── Presentation/
│   ├── ViewModels/           (12개 ViewModel)
│   ├── Views/                (DashboardView + 9개 Dialog)
│   ├── Converters/           (5개 Converter)
│   └── Controls/             (DashboardCardTemplateSelector)
└── Shared/
    └── Collections/          (BulkObservableCollection)
```

## 변경된 파일 목록

### 신규 생성 (Domain 계층)
- `Domain/Entities/ProjectItem.cs`, `TodoItem.cs`, `HistoryEntry.cs`, `CommandScript.cs`, `ExternalTool.cs`, `AppSettings.cs`
- `Domain/ValueObjects/ProjectGroup.cs` (class → **record** 전환), `GitFileStatus.cs`, `OpenSourceItem.cs`, `AddCardPlaceholder.cs`, `DropPlaceholder.cs`
- `Domain/Enums/Enums.cs` (ShellType 통합), `LanguageSetting.cs`
- `Domain/Repositories/IProjectRepository.cs`
- `GlobalUsings.cs`

### 신규 생성 (Infrastructure 계층)
- `Infrastructure/Persistence/DatabaseContext.cs`, `SqliteProjectRepository.cs`
- `Infrastructure/Services/` 7개 파일 (한글 주석 보강 완료)

### 신규 생성 (Presentation 계층)
- `Presentation/ViewModels/` 14개 파일
- `Presentation/Views/DashboardView.xaml`, `DashboardView.xaml.cs`
- `Presentation/Views/Dialogs/` 9쌍(XAML + CS) = 18개 파일
- `Presentation/Converters/` 5개 파일
- `Presentation/Controls/DashboardCardTemplateSelector.cs`

### 신규 생성 (Shared 계층)
- `Shared/Collections/BulkObservableCollection.cs`

### 인플레이스 수정
- `App.xaml.cs` — using 구문 업데이트 + `Infrastructure.Persistence` using 추가
- `MainWindow.xaml` — xmlns 업데이트 (models → Domain.ValueObjects, vm → Presentation.ViewModels)
- `MainWindow.xaml.cs` — using 구문 업데이트
- `Resources/Converters.xaml` — xmlns 업데이트 (Converters → Presentation.Converters)

### git rm (삭제)
- `Models/` 폴더 전체 (13개 파일)
- `Services/` 폴더 전체 (10개 파일)
- `ViewModels/` 폴더 전체
- `Views/` 폴더 전체
- `Converters/` 폴더 전체
- `Controls/` 폴더 전체
- `Collections/` 폴더 전체

## 핵심 기술 결정

### ProjectGroup: class → record 전환
- DDD Value Object는 불변(immutable)이어야 함
- `init` 프로퍼티로 object initializer 문법 유지
- GroupDialog.xaml.cs에서 `new ProjectGroup { Id = ..., Name = ... }` 호환성 유지됨

### XAML xmlns 분리 전략
- 기존 단일 `xmlns:models="using:DevDashboard.Models"` → 타입별 네임스페이스로 분리
- 단일 타입 사용 파일: xmlns:models를 해당 도메인 네임스페이스로 직접 변경
  - `MainWindow.xaml`, `AppSettingsDialog.xaml`, `GitStatusDialog.xaml` → ValueObjects
  - `CommandScriptDialog.xaml` → Enums
- 복수 타입 사용 파일: 추가 xmlns 선언
  - `ProjectSettingsDialog.xaml`: `xmlns:models="using:DevDashboard.Domain.Entities"` (ExternalTool) + `xmlns:vo="using:DevDashboard.Domain.ValueObjects"` (ProjectGroup) 추가

### GlobalUsings.cs 도입
- 56개 파일의 `using DevDashboard.Models;` 반복 제거
- Domain 4개 네임스페이스 전역 등록 (Entities, ValueObjects, Enums, Repositories)

## 검증 결과

### 빌드
- **오류 0개** — MSBuild x64 Debug 성공
- 경고 92개 (MVVMTK0045 기존 경고 — 기능에 영향 없음)

## 알려진 제한사항
- MVVMTK0045: [ObservableProperty] field → partial property 전환 미완료 (기능에 영향 없음)
