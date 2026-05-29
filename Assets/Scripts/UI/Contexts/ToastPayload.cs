using UnityEngine;

public sealed class ToastPayload
{
    public ToastPayload(string message)
        : this(message, null)
    {
    }

    public ToastPayload(string message, Sprite icon)
    {
        Message = message ?? string.Empty;
        Icon = icon;
    }

    public string Message { get; }
    public Sprite Icon { get; }
}
