public static class PageContextBinding
{
    public static void Release<TContext>(ref TContext context)
        where TContext : class, IPageContext
    {
        context?.Dispose();
        context = null;
    }
}
