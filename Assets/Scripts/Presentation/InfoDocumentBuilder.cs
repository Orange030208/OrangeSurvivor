using System;
using System.Collections.Generic;

public interface IInfoDocumentBuilder<in TSource>
{
    InfoDocument Build(TSource source);
}

public interface IInfoDocumentSource
{
    InfoDocument BuildInfoDocument();
}

public sealed class InfoDocumentService
{
    private readonly Dictionary<Type, BuilderRegistration> builders = new();

    public InfoDocumentService(bool registerDefaultBuilders = true)
    {
        if (!registerDefaultBuilders)
        {
            return;
        }

        PropertiesInfoBuilder propertiesInfoBuilder = new();
        BuffInfoBuilder buffInfoBuilder = new();
        Register(new WeaponInfoBuilder());
        Register<PropertiesInfoSource>(propertiesInfoBuilder);
        Register<AttributeManager>(propertiesInfoBuilder);
        Register(new AccessoryInfoBuilder());
        Register(new RewardCardInfoBuilder());
        Register<BuffDataSO>(buffInfoBuilder);
        Register<BuffInfoSource>(buffInfoBuilder);
        Register<ActiveBuffViewData>(buffInfoBuilder);
    }

    public void Register<TSource>(IInfoDocumentBuilder<TSource> builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builders[typeof(TSource)] = new BuilderRegistration(
            typeof(TSource),
            source => builder.Build((TSource)source));
    }

    public bool TryBuild<TSource>(TSource source, out InfoDocument document)
    {
        return TryBuild((object)source, out document);
    }

    public bool TryBuild(object source, out InfoDocument document)
    {
        if (source is null)
        {
            document = null;
            return false;
        }

        if (source is IInfoDocumentSource infoDocumentSource)
        {
            document = infoDocumentSource.BuildInfoDocument();
            return document != null;
        }

        Type sourceType = source.GetType();
        if (TryBuildWithRuntimeBuilder(source, sourceType, out document))
        {
            return true;
        }

        document = null;
        return false;
    }

    public InfoDocument BuildOrThrow<TSource>(TSource source)
    {
        if (TryBuild(source, out InfoDocument document))
        {
            return document;
        }

        throw new InvalidOperationException($"No InfoDocument builder registered for {typeof(TSource).Name}.");
    }

    private bool TryBuildWithRuntimeBuilder(object source, Type sourceType, out InfoDocument document)
    {
        if (builders.TryGetValue(sourceType, out BuilderRegistration exactBuilder) &&
            exactBuilder.TryBuild(source, out document))
        {
            return true;
        }

        foreach (KeyValuePair<Type, BuilderRegistration> pair in builders)
        {
            if (pair.Key == sourceType || !pair.Key.IsAssignableFrom(sourceType))
            {
                continue;
            }

            if (pair.Value.TryBuild(source, out document))
            {
                return true;
            }
        }

        document = null;
        return false;
    }

    private readonly struct BuilderRegistration
    {
        private readonly Type sourceType;
        private readonly Func<object, InfoDocument> build;

        public BuilderRegistration(Type sourceType, Func<object, InfoDocument> build)
        {
            this.sourceType = sourceType;
            this.build = build;
        }

        public bool TryBuild(object source, out InfoDocument document)
        {
            if (source == null || !sourceType.IsInstanceOfType(source))
            {
                document = null;
                return false;
            }

            document = build.Invoke(source);
            return document != null;
        }
    }
}
