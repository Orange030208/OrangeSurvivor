using System;

/// <summary>
/// 武器候选快照更新后广播，供UI刷新列表显示。
/// </summary>
public struct SelectableWeaponsSnapshotEvent : IGameEvent
{
    public WeaponInfo[] SelectableWeapons;

    public SelectableWeaponsSnapshotEvent(WeaponInfo[] selectableWeapons)
    {
        SelectableWeapons = selectableWeapons;
    }
}

/// <summary>
/// 玩家选中了某个武器候选项，通知业务记录当前选择。
/// </summary>
public struct SelectWeaponEvent : IGameEvent
{
    public int Index;

    public SelectWeaponEvent(int index)
    {
        Index = index;
    }
}

/// <summary>
/// 玩家点击“开始”确认武器选择，通知业务推进流程。
/// </summary>
public struct SelectedWeaponConfirmEvent : IGameEvent
{
}

/// <summary>
/// 请求武器候选快照，常用于UI打开后主动拉取当前状态。
/// </summary>
public struct UISelectableWeaponsSnapshotEvent : IGameEvent
{
}