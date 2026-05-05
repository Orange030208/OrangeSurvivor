using System;

public static class PageContextBinding
{
    public static void Release<TContext>(ref TContext context)
        where TContext : class, IDisposable
    {
        context?.Dispose();
        context = null;
    }
}
