using UnityEngine;

public abstract class EnemySO : ScriptableObject
{
    [Header("预制体")] 
    public Enemy prefab;
    [Header("基础属性")] 
    public EnemyRole role;
    public float maxHp = 100;
    public float moveSpeed = 5f;
}
