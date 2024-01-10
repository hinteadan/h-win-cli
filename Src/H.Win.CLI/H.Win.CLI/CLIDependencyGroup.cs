using H.Necessaire;
using WindowsFirewallHelper;

namespace H.Win.CLI
{
    internal class CLIDependencyGroup : ImADependencyGroup
    {
        public void RegisterDependencies(ImADependencyRegistry dependencyRegistry)
        {
            dependencyRegistry
                .Register<IFirewall>(() => FirewallManager.Instance)
                .Register<BLL.DependencyGroup>(() => new BLL.DependencyGroup())
                ;
        }
    }
}
