using System;
using UnityEngine;

[Serializable]
public class AudioSfxClipVariant
{
    [SerializeField] private AudioClip clip;
    [SerializeField] [Min(0f)] private float weight = 1f;

    public AudioClip Clip => clip;
    public float Weight => Mathf.Max(0f, weight);

    public bool IsValid => clip != null && Weight > 0f;

    public void OnValidate()
    {
        weight = Mathf.Max(0f, weight);
    }
}
