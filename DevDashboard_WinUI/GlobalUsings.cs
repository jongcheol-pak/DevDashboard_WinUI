// ============================================================
// 전역 Using 선언 — DDD 계층별 네임스페이스를 전 프로젝트에서
// 명시적 using 없이 사용 가능하도록 등록합니다.
// ============================================================

// 도메인 엔티티 (ProjectItem, TodoItem, HistoryEntry 등)
global using DevDashboard.Domain.Entities;

// 도메인 값 객체 (ProjectGroup, GitFileStatus 등)
global using DevDashboard.Domain.ValueObjects;

// 도메인 열거형 (SortOrder, ViewMode, ThemeMode 등)
global using DevDashboard.Domain.Enums;

// 도메인 리포지토리 인터페이스 (IProjectRepository)
global using DevDashboard.Domain.Repositories;
