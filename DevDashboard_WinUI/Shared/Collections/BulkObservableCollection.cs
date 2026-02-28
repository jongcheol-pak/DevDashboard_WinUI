using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace DevDashboard.Shared.Collections;

/// <summary>
/// 대량 항목 교체 시 개별 Add/Remove 대신 단일 Reset 이벤트로 알림을 처리하는 컬렉션.
/// WrapPanel처럼 가상화되지 않은 패널에서 N번의 레이아웃 재계산을 방지합니다.
/// </summary>
public sealed class BulkObservableCollection<T> : ObservableCollection<T>
{
    private bool _suppressNotifications;

    /// <summary>기존 항목을 모두 교체하고 단일 Reset 이벤트를 발생시킵니다.</summary>
    public void ResetWith(IEnumerable<T> items)
    {
        _suppressNotifications = true;
        try
        {
            Items.Clear();
            foreach (var item in items)
                Items.Add(item);
        }
        finally
        {
            _suppressNotifications = false;
        }

        // Count, 인덱서, 컬렉션 변경을 한 번에 알림
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_suppressNotifications)
            base.OnCollectionChanged(e);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (!_suppressNotifications)
            base.OnPropertyChanged(e);
    }
}
