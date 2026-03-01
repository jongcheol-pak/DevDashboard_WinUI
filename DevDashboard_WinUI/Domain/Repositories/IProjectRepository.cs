namespace DevDashboard.Domain.Repositories;

/// <summary>
/// 프로젝트 데이터 저장소 인터페이스 — Domain이 Infrastructure 구현에 의존하지 않도록
/// 계약(Contract)을 도메인 계층에 정의합니다 (의존성 역전 원칙).
/// </summary>
public interface IProjectRepository
{
    /// <summary>모든 프로젝트를 로드합니다 (Tags, CommandScripts 포함. Todos/Histories는 지연 로딩).</summary>
    List<ProjectItem> GetAll();

    /// <summary>특정 프로젝트의 To-Do 목록을 로드합니다 (다이얼로그 열기 시 지연 로딩).</summary>
    List<TodoItem> GetTodos(string projectId);

    /// <summary>특정 프로젝트의 작업 기록을 로드합니다 (다이얼로그 열기 시 지연 로딩).</summary>
    List<HistoryEntry> GetHistories(string projectId);

    /// <summary>프로젝트를 추가합니다 (Tags, CommandScripts, Todos, Histories 포함).</summary>
    void Add(ProjectItem project);

    /// <summary>프로젝트 기본 정보를 갱신합니다 (Tags, CommandScripts 포함).</summary>
    void Update(ProjectItem project);

    /// <summary>프로젝트를 삭제합니다 (관련 하위 데이터 모두 삭제).</summary>
    void Delete(string projectId);

    /// <summary>프로젝트의 To-Do 목록만 저장합니다.</summary>
    void SaveTodos(string projectId, List<TodoItem> todos);

    /// <summary>프로젝트의 작업 기록만 저장합니다.</summary>
    void SaveHistories(string projectId, List<HistoryEntry> histories);

    /// <summary>프로젝트의 커맨드 스크립트만 저장합니다.</summary>
    void SaveCommandScripts(string projectId, List<CommandScript?> scripts);

    /// <summary>프로젝트의 핀 상태만 갱신합니다.</summary>
    void UpdatePinned(string projectId, bool isPinned);

    /// <summary>핀 고정 카드들의 표시 순서를 저장합니다. 전달된 ID 목록의 인덱스가 순서가 됩니다.</summary>
    void UpdatePinOrder(IReadOnlyList<string> orderedPinnedIds);

    /// <summary>이름이 동일한 기존 프로젝트의 ID를 반환합니다. 없으면 null.</summary>
    string? FindProjectIdByName(string name);

    /// <summary>기존 프로젝트를 삭제하고 새 프로젝트를 추가합니다 (덮어쓰기).</summary>
    void DeleteByNameAndInsert(string existingId, ProjectItem project);
}
