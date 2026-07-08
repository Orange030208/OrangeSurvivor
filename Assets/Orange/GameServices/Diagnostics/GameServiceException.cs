using System;

namespace Orange.GameServices
{
    public sealed class GameServiceException : Exception
    {
        public GameServiceException(string message)
            : base(message)
        {
        }

        public GameServiceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
