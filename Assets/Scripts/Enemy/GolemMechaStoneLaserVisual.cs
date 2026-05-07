using UnityEngine;

public sealed class GolemMechaStoneLaserVisual : MonoBehaviour
{
    [SerializeField] private LineRenderer glowLine;
    [SerializeField] private LineRenderer coreLine;

    public bool IsConfigured => glowLine != null && coreLine != null;

    public void Show(
        Vector3 startPosition,
        Vector3 endPosition,
        bool showCore)
    {
        ResolveReferences();
        if (!IsConfigured)
        {
            Debug.LogWarning($"{nameof(GolemMechaStoneLaserVisual)} on {name} is missing LineRenderer references.", this);
            return;
        }

        ConfigureLine(glowLine, startPosition, endPosition);
        coreLine.enabled = showCore;
        if (showCore)
        {
            ConfigureLine(coreLine, startPosition, endPosition);
        }
    }

    public void Hide()
    {
        ResolveReferences();
        if (glowLine != null)
        {
            glowLine.enabled = false;
        }

        if (coreLine != null)
        {
            coreLine.enabled = false;
        }
    }

    private static void ConfigureLine(
        LineRenderer lineRenderer,
        Vector3 startPosition,
        Vector3 endPosition)
    {
        lineRenderer.enabled = true;
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, endPosition);
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
        Hide();
    }

    private void ResolveReferences()
    {
        glowLine ??= ResolveChildLine("Glow", "GolemMechaStoneLaserGlow");
        coreLine ??= ResolveChildLine("Core", "GolemMechaStoneLaserCore");
    }

    private LineRenderer ResolveChildLine(params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform child = transform.Find(names[i]);
            if (child != null && child.TryGetComponent(out LineRenderer lineRenderer))
            {
                return lineRenderer;
            }
        }

        return null;
    }

}
