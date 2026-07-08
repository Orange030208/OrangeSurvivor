namespace Orange.GameServices
{
    public static class GameServiceDiagnostics
    {
        public static bool TryCaptureDefault(out GameServiceSnapshot snapshot)
        {
            if (GameServices.TryGetHost(out GameServiceHost host))
            {
                snapshot = host.CaptureSnapshot();
                return true;
            }

            snapshot = null;
            return false;
        }

        public static bool TryCapture(string scopeId, out GameServiceSnapshot snapshot)
        {
            if (GameServices.TryGetHost(scopeId, out GameServiceHost host))
            {
                snapshot = host.CaptureSnapshot();
                return true;
            }

            snapshot = null;
            return false;
        }
    }
}
