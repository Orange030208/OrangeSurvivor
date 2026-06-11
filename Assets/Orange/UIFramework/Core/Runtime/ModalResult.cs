namespace Orange.UIFramework
{
    public readonly struct ModalResult
    {
        public ModalResult(bool confirmed, CloseReason closeReason)
        {
            Confirmed = confirmed;
            CloseReason = closeReason;
        }

        public bool Confirmed { get; }
        public CloseReason CloseReason { get; }
        public bool Cancelled => !Confirmed;

        public static ModalResult Confirm()
        {
            return new ModalResult(true, CloseReason.Completed);
        }

        public static ModalResult Cancel(CloseReason reason = CloseReason.Cancel)
        {
            return new ModalResult(false, reason);
        }
    }

    public readonly struct ModalResult<TResult>
    {
        public ModalResult(bool confirmed, TResult value, CloseReason closeReason)
        {
            Confirmed = confirmed;
            Value = value;
            CloseReason = closeReason;
        }

        public bool Confirmed { get; }
        public TResult Value { get; }
        public CloseReason CloseReason { get; }
        public bool Cancelled => !Confirmed;

        public static ModalResult<TResult> Confirm(TResult value)
        {
            return new ModalResult<TResult>(true, value, CloseReason.Completed);
        }

        public static ModalResult<TResult> Cancel(CloseReason reason = CloseReason.Cancel)
        {
            return new ModalResult<TResult>(false, default, reason);
        }
    }
}
