using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EntityRenderer : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    public SpriteRenderer SpriteRenderer => spriteRenderer;

    private void Awake()
    {
        if(spriteRenderer == null) 
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    public void SetSprite(Sprite sprite)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.sprite = sprite;
    }
}
