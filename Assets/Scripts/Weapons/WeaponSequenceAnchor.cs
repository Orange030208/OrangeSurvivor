using UnityEngine;

public class WeaponSequenceAnchor : MonoBehaviour
{
    [SerializeField] private Transform animatedTransform;

    public Transform AnimatedTransform => animatedTransform != null ? animatedTransform : transform;

    private void Reset()
    {
        animatedTransform = transform;
    }
}
