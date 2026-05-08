using System.Collections.Generic;
using System.Text;

namespace Orange.UIFramework
{
    public sealed class ValidationReport
    {
        private readonly List<ValidationMessage> messages = new List<ValidationMessage>();

        public IReadOnlyList<ValidationMessage> Messages => messages;
        public bool HasErrors { get; private set; }
        public bool HasWarnings { get; private set; }

        public void AddInfo(string message)
        {
            Add(ValidationSeverity.Info, message);
        }

        public void AddWarning(string message)
        {
            Add(ValidationSeverity.Warning, message);
        }

        public void AddError(string message)
        {
            Add(ValidationSeverity.Error, message);
        }

        public void Add(ValidationSeverity severity, string message)
        {
            ValidationMessage validationMessage = new ValidationMessage(severity, message);
            messages.Add(validationMessage);

            if (severity == ValidationSeverity.Error)
            {
                HasErrors = true;
            }
            else if (severity == ValidationSeverity.Warning)
            {
                HasWarnings = true;
            }
        }

        public void Append(ValidationReport report)
        {
            if (report == null)
            {
                return;
            }

            IReadOnlyList<ValidationMessage> sourceMessages = report.Messages;
            for (int i = 0; i < sourceMessages.Count; i++)
            {
                ValidationMessage message = sourceMessages[i];
                Add(message.Severity, message.Message);
            }
        }

        public string ToDisplayString()
        {
            if (messages.Count == 0)
            {
                return "Validation passed.";
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
