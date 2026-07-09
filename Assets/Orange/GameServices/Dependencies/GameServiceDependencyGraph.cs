using System.Collections.Generic;

namespace Orange.GameServices
{
    /// <summary>
    /// 通过深度优先的依赖遍历解析服务启动顺序。
    /// </summary>
    internal sealed class GameServiceDependencyGraph
    {
        private readonly List<GameService> services;
        private readonly HashSet<GameService> activeServices;
        private readonly GameServiceRegistry registry;
        private readonly Dictionary<GameService, IReadOnlyList<GameServiceDependencyRule>> dependencyRules;
        private readonly GameServiceValidationReport report;
        private readonly Dictionary<GameService, VisitState> visitStates = new Dictionary<GameService, VisitState>();
        private readonly List<GameService> sortedServices = new List<GameService>();

        public GameServiceDependencyGraph(
            List<GameService> services,
            GameServiceRegistry registry,
            Dictionary<GameService, IReadOnlyList<GameServiceDependencyRule>> dependencyRules,
            GameServiceValidationReport report)
        {
            this.services = services;
            this.registry = registry;
            this.dependencyRules = dependencyRules;
            this.report = report;
            activeServices = new HashSet<GameService>(services);
        }

        public List<GameService> Sort()
        {
            sortedServices.Clear();
            visitStates.Clear();

            for (int i = 0; i < services.Count; i++)
            {
                Visit(services[i]);
            }

            return new List<GameService>(sortedServices);
        }

        private void Visit(GameService service)
        {
            if (service == null)
            {
                return;
            }

            if (visitStates.TryGetValue(service, out VisitState state))
            {
                if (state == VisitState.Visiting)
                {
                    report.AddError("Circular service dependency detected.", service.GetType());
                }

                return;
            }

            visitStates[service] = VisitState.Visiting;
            VisitDependencies(service);
            visitStates[service] = VisitState.Visited;
            // 依赖总是先入结果列表，因此最终顺序可直接用于 Attach/Start 遍历。
            sortedServices.Add(service);
        }

        private void VisitDependencies(GameService service)
        {
            if (!dependencyRules.TryGetValue(service, out IReadOnlyList<GameServiceDependencyRule> rules))
            {
                return;
            }

            for (int i = 0; i < rules.Count; i++)
            {
                GameServiceDependencyRule rule = rules[i];
                if (!registry.TryResolve(rule.ContractType, out GameService dependency))
                {
                    if (rule.Required)
                    {
                        report.AddError("Required service dependency is missing.", service.GetType(), rule.ContractType);
                    }

                    continue;
                }

                if (dependency == service)
                {
                    report.AddError("Service cannot require itself.", service.GetType(), rule.ContractType);
                    continue;
                }

                if (!activeServices.Contains(dependency))
                {
                    if (rule.Required)
                    {
                        // 合同可能已经注册，但对应服务仍可能因为禁用而不在有效运行图里。
                        report.AddError("Required service dependency is disabled or unavailable.", service.GetType(), rule.ContractType);
                    }

                    continue;
                }

                Visit(dependency);
            }
        }

        private enum VisitState
        {
            Visiting,
            Visited
        }
    }
}
