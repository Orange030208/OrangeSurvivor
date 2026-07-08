using System;
using System.Collections.Generic;

namespace Orange.GameServices
{
    public sealed class GameServiceDependencyBuilder
    {
        private readonly List<GameServiceDependencyRule> rules = new List<GameServiceDependencyRule>();

        public IReadOnlyList<GameServiceDependencyRule> Rules => rules;

        public void Require<T>() where T : class
        {
            Require(typeof(T));
        }

        public void Require(Type contractType)
        {
            Add(contractType, true);
        }

        public void Optional<T>() where T : class
        {
            Optional(typeof(T));
        }

        public void Optional(Type contractType)
        {
            Add(contractType, false);
        }

        private void Add(Type contractType, bool required)
        {
            if (contractType == null)
            {
                throw new ArgumentNullException(nameof(contractType));
            }

            rules.Add(new GameServiceDependencyRule(contractType, required));
        }
    }
}
