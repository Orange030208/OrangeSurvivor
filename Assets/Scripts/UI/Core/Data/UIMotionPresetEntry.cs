using System;
using UnityEngine;

[Serializable]
public sealed class UIMotionPresetEntry
{
    [SerializeField] private string option;
    [SerializeField] private UIMotionPreset preset;

    public string Option => option;
    public UIMotionPreset Preset => preset;
}
