namespace Orange.GameServices
{
    public readonly struct GameServiceResolver
    {
        private readonly GameServiceHost host;

        internal GameServiceResolver(GameServiceHost host)
        {
            this.host = host;
        }

        public bool IsValid => host != null;

        public T Get<T>() where T : class
        {
            if (host == null)
            {
                throw new GameServiceException("GameServices scope is not bound.");
            }

            return host.Get<T>();
        }

        public bool TryGet<T>(out T service) where T : class
        {
            if (host == null)
            {
                service = null;
                return false;
            }

            return host.TryGet(out service);
        }
    }
}
