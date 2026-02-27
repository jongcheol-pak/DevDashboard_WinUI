namespace DevDashboard.Models;

/// <summary>프로젝트 그룹 모델</summary>
public class ProjectGroup
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
}
