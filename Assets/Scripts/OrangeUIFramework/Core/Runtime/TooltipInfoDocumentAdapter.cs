using System;
using System.Linq;
using System.Reflection;

namespace Orange.UIFramework
{
    public sealed class TooltipInfoDocumentAdapter
    {
        private object infoDocumentService;
        private Type infoDocumentType;
        private Type infoDocumentServiceType;
        private MethodInfo infoDocumentServiceTryBuildMethod;

        public TooltipInfoDocumentAdapter(object infoDocumentService = null)
        {
            this.infoDocumentService = infoDocumentService;
        }

        public bool TryBuildDocument(object source, out object document)
        {
            if (source == null)
            {
                document = null;
                return false;
            }

            if (IsInfoDocument(source))
            {
                document = source;
                return true;
            }

            if (TryBuildFromInfoDocumentSource(source, out document))
            {
                return true;
            }

            if (TryBuildFromInfoDocumentService(source, out document))
            {
                return true;
            }

            document = null;
            return false;
        }

        private bool IsInfoDocument(object source)
        {
            Type documentType = ResolveInfoDocumentType();
            return documentType != null && documentType.IsInstanceOfType(source);
        }

        private static bool TryBuildFromInfoDocumentSource(object source, out object document)
        {
            Type sourceType = source.GetType();
            Type sourceInterface = sourceType
                .GetInterfaces()
                .FirstOrDefault(type => string.Equals(type.Name, "IInfoDocumentSource", StringComparison.Ordinal));

            if (sourceInterface == null)
            {
                document = null;
                return false;
            }

            MethodInfo buildMethod = sourceInterface.GetMethod("BuildInfoDocument", Type.EmptyTypes);
            document = buildMethod != null ? buildMethod.Invoke(source, null) : null;
            return document != null;
        }

        private bool TryBuildFromInfoDocumentService(object source, out object document)
        {
            object service = ResolveInfoDocumentService();
            MethodInfo tryBuildMethod = ResolveInfoDocumentServiceTryBuildMethod();
            if (service == null || tryBuildMethod == null)
            {
                document = null;
                return false;
            }

            object[] args = { source, null };
            bool success = (bool)tryBuildMethod.Invoke(service, args);
            document = success ? args[1] : null;
            return success && document != null;
        }

        private object ResolveInfoDocumentService()
        {
            if (infoDocumentService != null)
            {
                return infoDocumentService;
            }

            Type serviceType = ResolveInfoDocumentServiceType();
            if (serviceType == null)
            {
                return null;
            }

            infoDocumentService = Activator.CreateInstance(serviceType);
            return infoDocumentService;
        }

        private MethodInfo ResolveInfoDocumentServiceTryBuildMethod()
        {
            if (infoDocumentServiceTryBuildMethod != null)
            {
                return infoDocumentServiceTryBuildMethod;
            }

            Type serviceType = ResolveInfoDocumentServiceType();
            Type documentType = ResolveInfoDocumentType();
            if (serviceType == null || documentType == null)
            {
                return null;
            }

            infoDocumentServiceTryBuildMethod = serviceType.GetMethod(
                "TryBuild",
                new[] { typeof(object), documentType.MakeByRefType() });

            return infoDocumentServiceTryBuildMethod;
        }

        private Type ResolveInfoDocumentType()
        {
            return infoDocumentType ??= ResolveProjectType("InfoDocument");
        }

        private Type ResolveInfoDocumentServiceType()
        {
            return infoDocumentServiceType ??= ResolveProjectType("InfoDocumentService");
        }

        private static Type ResolveProjectType(string typeName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type = assemblies[i].GetType(typeName, throwOnError: false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
