namespace Orange.UIFramework
{
    public readonly struct ValidationMessage
    {
        public ValidationMessage(ValidationSeverity severity, string message)
        {
            Severity = severity;
            Message = message ?? string.Empty;
        }

        public ValidationSeverity Severity { get; }
        public string Message { get; }

        public override string ToString()
        {
            return $"[{Severity}] {Message}";
        }
    }
}
