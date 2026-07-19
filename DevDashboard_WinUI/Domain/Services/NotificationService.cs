namespace DevDashboard.Domain.Services;

/// <summary>
/// 마감 알림 감지 순수 로직입니다.
/// 전체 프로젝트의 미완료 작업 중 종료일이 임박·오늘·경과인 항목을 집계합니다.
/// 상태·외부 의존이 없어 정적 함수로 제공합니다(today를 인자로 주입해 재현 가능).
/// </summary>
public static class NotificationService
{
    /// <summary>마감 임박으로 간주하는 최대 잔여 일수(D-3).</summary>
    private const int ImminentDays = 3;

    /// <summary>
    /// 전체 프로젝트의 미완료 작업 중 종료일이 임박(1~3일)·오늘·경과인 항목을 알림으로 집계합니다.
    /// 종료일 미지정·완료(Status == Completed) 작업은 제외하며, 종료일 오름차순(가장 급한 것 먼저)으로 정렬합니다.
    /// </summary>
    /// <param name="projects">Todos가 로드된 프로젝트 목록</param>
    /// <param name="today">기준 날짜(시각 무시 — Date만 비교)</param>
    public static List<Notification> Detect(IEnumerable<ProjectItem> projects, DateTime today)
    {
        ArgumentNullException.ThrowIfNull(projects);

        var todayDate = today.Date;
        var result = new List<Notification>();

        foreach (var project in projects)
        {
            if (project.Todos is null)
                continue;

            foreach (var todo in project.Todos)
            {
                if (todo.EndDate is not { } endDate || todo.Status == TodoStatus.Completed)
                    continue;

                var daysLeft = (endDate.Date - todayDate).Days;
                DeadlineStatus status;
                if (daysLeft < 0)
                    status = DeadlineStatus.Overdue;
                else if (daysLeft == 0)
                    status = DeadlineStatus.DueToday;
                else if (daysLeft <= ImminentDays)
                    status = DeadlineStatus.Imminent;
                else
                    continue; // 4일 이상 남음 → 알림 대상 아님

                result.Add(new Notification
                {
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    TodoId = todo.Id,
                    TodoText = todo.Text,
                    EndDate = endDate,
                    Status = status
                });
            }
        }

        return result.OrderBy(n => n.EndDate).ToList();
    }

    /// <summary>
    /// 알림의 읽음 상태 추적 키를 생성합니다.
    /// 작업 Id + 마감상태 조합이라, 마감이 임박→오늘→경과로 악화되면 새 키가 되어 안읽음으로 재환기됩니다.
    /// </summary>
    public static string BuildKey(Notification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);
        return $"{notification.TodoId}:{notification.Status}";
    }
}
