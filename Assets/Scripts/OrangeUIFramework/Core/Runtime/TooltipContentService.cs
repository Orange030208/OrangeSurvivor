using System;

namespace Orange.UIFramework
{
    public sealed class TooltipContentService
    {
        private readonly TooltipInfoDocumentAdapter infoDocumentAdapter;

        public TooltipContentService(object infoDocumentService = null)
        {
            infoDocumentAdapter = new TooltipInfoDocumentAdapter(infoDocumentService);
        }

        public bool TryBuild(TooltipRequest request, out TooltipContent content)
        {
            request.Validate();

            TooltipBuildContext context = new TooltipBuildContext(
                request.Source,
                request.ViewIdOverride,
                request.ChromeOptions,
                request.SessionMode);

            if (request.Content != null)
            {
                content = ApplyRequestOverrides(request.Content, request);
                return true;
            }

            return TryBuild(request.Source, context, out content);
        }

        public bool TryBuild(object source, TooltipBuildContext context, out TooltipContent content)
        {
            if (source is null)
            {
                content = null;
                return false;
            }

            if (source is TooltipContent explicitContent)
            {
                content = ApplyContextOverrides(explicitContent, context);
                return true;
            }

            if (source is ITooltipContentSource tooltipContentSource &&
                tooltipContentSource.TryBuildTooltipContent(context, out TooltipContent sourcedContent) &&
                sourcedContent != null)
            {
                content = ApplyContextOverrides(sourcedContent, context);
                return true;
            }

            if (source is string text)
            {
                content = CreateTextContent(text, context);
                return true;
            }

            if (infoDocumentAdapter.TryBuildDocument(source, out object documentPayload))
            {
                content = CreateDocumentContent(documentPayload, context);
                return true;
            }

            content = null;
            return false;
        }

        private static TooltipContent CreateTextContent(string text, TooltipBuildContext context)
        {
            string viewId = !string.IsNullOrWhiteSpace(context.ViewIdOverride)
                ? context.ViewIdOverride
                : TooltipViewIds.TEXT;

            return new TooltipContent(viewId, text ?? string.Empty, context.ChromeOptions);
        }

        private static TooltipContent CreateDocumentContent(object document, TooltipBuildContext context)
        {
            string viewId = !string.IsNullOrWhiteSpace(context.ViewIdOverride)
                ? context.ViewIdOverride
                : TooltipViewIds.DOCUMENT;

            return new TooltipContent(viewId, document, context.ChromeOptions);
        }

        private static TooltipContent ApplyRequestOverrides(TooltipContent content, TooltipRequest request)
        {
            TooltipBuildContext context = new TooltipBuildContext(
                request.Source,
                request.ViewIdOverride,
                request.ChromeOptions,
                request.SessionMode);

            return ApplyContextOverrides(content, context);
        }

        private static TooltipContent ApplyContextOverrides(TooltipContent content, TooltipBuildContext context)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            TooltipContent result = content;
            if (!string.IsNullOrWhiteSpace(context.ViewIdOverride))
            {
                result = result.WithViewId(context.ViewIdOverride);
            }

            return context.ChromeOptions.HasAssignedValues
                ? result.WithChromeOptions(context.ChromeOptions)
                : result;
        }
    }
}
