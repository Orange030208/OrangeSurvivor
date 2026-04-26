using UnityEngine;

public class EntityRenderer : EntityComponentBase
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Entity owner;

    public override Entity Owner => owner;

    public SpriteRenderer SpriteRenderer => spriteRenderer;

    public override void Initialize(Entity owner)
    {
        this.owner = owner;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    public void SetSprite(Sprite sprite)
    {
        if (spriteRenderer == null)
        {
            throw new MissingComponentException(
                $"{nameof(SpriteRenderer)} is null on {name}. " +
                $"Ensure {nameof(SpriteRenderer)} is assigned or present on the GameObject.");
        }

        spriteRenderer.sprite = sprite;
    }
}
