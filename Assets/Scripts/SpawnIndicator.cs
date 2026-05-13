using System;
using DG.Tweening;
using UnityEngine;

public class SpawnIndicator : MonoBehaviour
{
    [Header("视觉表现")]
    [SerializeField] private GameObject visualRoot;

    [Header("动画")]
    [SerializeField] private float targetScale = 1.2f;
    [SerializeField] private float pulseDuration = 0.3f;
    [SerializeField] private int pulseLoops = 5;
    [SerializeField] private Ease pulseEase = Ease.InOutSine;

    private Tween pulseTween;
    private Vector3 defaultScale;
    private bool isPlaying;

    private void Awake()
    {
        defaultScale = transform.localScale;
        if (visualRoot == null)
        {
            visualRoot = gameObject;
        }
    }

    private void OnDisable()
    {
        pulseTween?.Kill();
        pulseTween = null;
        isPlaying = false;
        transform.localScale = defaultScale;
    }

    public void Play(Action onCompleted)
    {
        if (isPlaying)
        {
            return;
        }

        isPlaying = true;
        SetVisualVisible(true);
        transform.localScale = defaultScale;
        pulseTween = transform.DOScale(targetScale, pulseDuration)
            .SetLoops(pulseLoops, LoopType.Yoyo)
            .SetEase(pulseEase)
            .OnComplete(() =>
            {
                isPlaying = false;
                transform.localScale = defaultScale;
                onCompleted?.Invoke();
                Destroy(gameObject);
            });
    }

    public void PlayAndSpawn(GameObject prefab, Vector3 spawnPosition, Quaternion spawnRotation, Transform spawnParent, Action<GameObject> onSpawned = null)
    {
        if (prefab == null)
        {
            throw new MissingReferenceException($"{nameof(SpawnIndicator)} requires a non-null prefab to spawn.");
        }

        Play(() =>
        {
            GameObject spawnedObject = Instantiate(prefab, spawnPosition, spawnRotation, spawnParent);
            onSpawned?.Invoke(spawnedObject);
        });
    }

    public void Cancel()
    {
        if (!isPlaying)
        {
            Destroy(gameObject);
            return;
        }

        pulseTween?.Kill();
        pulseTween = null;
        isPlaying = false;
        Destroy(gameObject);
    }

    public void PrepareForWaveCleanup()
    {
        pulseTween?.Kill();
        pulseTween = null;
        isPlaying = false;
    }

    public void ReleaseForWaveCleanup()
    {
        PrepareForWaveCleanup();
        Destroy(gameObject);
    }

    private void SetVisualVisible(bool visible)
    {
        if (visualRoot != null)
        {
            visualRoot.SetActive(visible);
        }
    }
}
