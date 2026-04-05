using System;
using UnityEngine;

namespace UniversalUI.Integration.Game.ScriptableObjects
{
    [CreateAssetMenu(fileName = "Prop Icons", menuName = "SO/Prop Icons", order = 0)]
    public class PropIconDataSO : ScriptableObject
    {
        [field: SerializeField] public PropIcon[] PropIcons { get; private set; }
    }

    [Serializable]
    public struct PropIcon
    {
        public PropType propType;
        public Sprite icon;
    }
}