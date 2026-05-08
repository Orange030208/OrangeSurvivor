using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Orange.UIFramework
{
    public interface ILocalizationService
    {
        string CurrentLanguage { get; }
        event Action LanguageChanged;

        UniTask SetLanguageAsync(string language, CancellationToken cancellationToken = default);
        string GetText(string key);
        string GetText(string key, IReadOnlyDictionary<string, object> args);
    }
}
