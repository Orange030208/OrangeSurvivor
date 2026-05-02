using AXR.Framework.UI;
using System;

public static class PageContextBinding
{
    public static TContext Resolve<TContext>(UIPageOpenContext openContext, Func<TContext> fallbackFactory)
        where TContext : class, IPageContext
    {
        if (fallbackFactory == null)
        {
            throw new ArgumentNullException(nameof(fallbackFactory));
        }

        return openContext?.GetPayload<TContext>() ?? fallbackFactory();
    }

    public static void Release<TContext>(ref TContext context)
        where TContext : class, IPageContext
    {
        context?.Dispose();
        context = null;
    }
}
