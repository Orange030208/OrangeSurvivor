using System.Collections.Generic;
using UnityEngine;

namespace Orange.GameServices
{
    [CreateAssetMenu(menuName = "Orange/Game Services/Profile", fileName = "Game Service Profile")]
    /// <summary>
    /// 可复用的服务定义集合，可被一个或多个 Root 挂载。
    /// </summary>
    public sealed class GameServiceProfile : ScriptableObject
    {
        [SerializeReference] private List<GameService> services = new List<GameService>();

        public IReadOnlyList<GameService> Services => services;

        internal void AppendServices(List<GameService> target)
        {
            if (target == null)
            {
                return;
            }

            for (int i = 0; i < services.Count; i++)
            {
                target.Add(services[i]);
            }
        }
    }
}
