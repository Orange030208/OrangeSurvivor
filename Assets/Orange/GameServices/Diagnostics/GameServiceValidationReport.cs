using System;
using System.Collections.Generic;
using System.Text;

namespace Orange.GameServices
{
    /// <summary>
    /// 汇总 Host 在校验期和运行期收集到的诊断消息。
    /// </summary>
    public sealed class GameServiceValidationReport
    {
        private readonly List<GameServiceValidationMessage> messages = new List<GameServiceValidationMessage>();

        public IReadOnlyList<GameServiceValidationMessage> Messages => messages;
        public int Count => messages.Count;

        public bool HasErrors
        {
            get
            {
                for (int i = 0; i < messages.Count; i++)
                {
                    if (messages[i].Severity == GameServiceValidationSeverity.Error)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void AddInfo(string message, Type serviceType = null, Type contractType = null)
        {
            messages.Add(new GameServiceValidationMessage(GameServiceValidationSeverity.Info, message, serviceType, contractType));
        }

        public void AddWarning(string message, Type serviceType = null, Type contractType = null)
        {
            messages.Add(new GameServiceValidationMessage(GameServiceValidationSeverity.Warning, message, serviceType, contractType));
        }

        public void AddError(string message, Type serviceType = null, Type contractType = null)
        {
            messages.Add(new GameServiceValidationMessage(GameServiceValidationSeverity.Error, message, serviceType, contractType));
        }

        public string FormatSummary()
        {
            if (messages.Count == 0)
            {
                return "GameServices validation completed without messages.";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < messages.Count; i++)
            {
                builder.AppendLine(messages[i].ToString());
            }

            return builder.ToString();
        }
    }
}
