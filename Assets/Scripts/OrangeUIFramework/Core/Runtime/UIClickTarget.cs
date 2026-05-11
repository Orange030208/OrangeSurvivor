
namespace Orange.UIFramework
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIClickTarget : MonoBehaviour, IPointerClickHandler, ISubmitHandler, IMoveHandler
{
    [SerializeField] private bool interactable = true;
    private static readonly List<GameObject> navigationCandidates = new();

    public event UnityAction OnClicked;

    public bool Interactable
    {
        get => interactable;
        set => interactable = value;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!interactable || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        OnClicked?.Invoke();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (!interactable)
        {
            return;
        }

        OnClicked?.Invoke();
    }

    public void OnMove(AxisEventData eventData)
    {
        if (!interactable || eventData == null || eventData.moveVector.sqrMagnitude < 0.01f)
        {
            return;
        }

        GameObject next = FindNearestNavigationTarget(eventData.moveVector.normalized);
        if (next == null)
        {
            return;
        }

        eventData.Use();
        EventSystem.current?.SetSelectedGameObject(next);
    }

    public void Select()
    {
        if (!interactable || !isActiveAndEnabled)
        {
            return;
        }

        EventSystem.current?.SetSelectedGameObject(gameObject);
    }

    public void ClearListeners()
    {
        OnClicked = null;
    }

    private GameObject FindNearestNavigationTarget(Vector2 direction)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform searchRoot = canvas != null ? canvas.transform : transform.root;
        Camera eventCamera = canvas != null ? canvas.worldCamera : null;
        Vector2 origin = GetScreenPosition(transform, eventCamera);

        navigationCandidates.Clear();
        CollectClickTargets(searchRoot);
        CollectSelectables(searchRoot);

        GameObject bestTarget = null;
        float bestScore = float.MaxValue;
        for (int i = 0; i < navigationCandidates.Count; i++)
        {
            GameObject candidate = navigationCandidates[i];
            if (candidate == null || candidate == gameObject || !candidate.activeInHierarchy)
            {
                continue;
            }

            Vector2 delta = GetScreenPosition(candidate.transform, eventCamera) - origin;
            float distance = delta.magnitude;
            if (distance <= 0.001f)
            {
                continue;
            }

            float dot = Vector2.Dot(direction, delta / distance);
            if (dot <= 0.35f)
            {
                continue;
            }

            float score = distance / dot;
            if (score >= bestScore)
            {
                continue;
            }

            bestScore = score;
            bestTarget = candidate;
        }

        navigationCandidates.Clear();
        return bestTarget;
    }

    private void CollectClickTargets(Transform searchRoot)
    {
        UIClickTarget[] clickTargets = searchRoot.GetComponentsInChildren<UIClickTarget>(false);
        for (int i = 0; i < clickTargets.Length; i++)
        {
            UIClickTarget target = clickTargets[i];
            if (target != null && target.interactable && target.isActiveAndEnabled)
            {
                AddNavigationCandidate(target.gameObject);
            }
        }
    }

    private static void CollectSelectables(Transform searchRoot)
    {
        Selectable[] selectables = searchRoot.GetComponentsInChildren<Selectable>(false);
        for (int i = 0; i < selectables.Length; i++)
        {
            Selectable selectable = selectables[i];
            if (selectable != null && selectable.IsInteractable() && selectable.gameObject.activeInHierarchy)
            {
                AddNavigationCandidate(selectable.gameObject);
            }
        }
    }

    private static void AddNavigationCandidate(GameObject candidate)
    {
        if (!navigationCandidates.Contains(candidate))
        {
            navigationCandidates.Add(candidate);
        }
    }

    private static Vector2 GetScreenPosition(Transform target, Camera eventCamera)
    {
        RectTransform rectTransform = target as RectTransform;
        Vector3 worldPosition = rectTransform != null ? rectTransform.TransformPoint(rectTransform.rect.center) : target.position;
        return RectTransformUtility.WorldToScreenPoint(eventCamera, worldPosition);
    }
}
}
