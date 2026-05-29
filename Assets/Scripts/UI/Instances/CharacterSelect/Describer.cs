using Orange.UIFramework;

public abstract class Describer : ViewPartBase
{
    public abstract void Display(InfoDocument document);

    protected static bool TryBuildInfoDocument(object source, out InfoDocument document)
    {
        return InfoDocumentServiceHolder.Service.TryBuild(source, out document);
    }

    private static class InfoDocumentServiceHolder
    {
        public static readonly InfoDocumentService Service = new();
    }
}
