using H.Necessaire;

namespace H.Win.CLI.BLL
{
    internal class DependencyGroup : ImADependencyGroup
    {
        public void RegisterDependencies(ImADependencyRegistry dependencyRegistry)
        {
            dependencyRegistry
                .Register<IPDetailsProvider>(() => new IPDetailsProvider())
                ;
        }
    }
}
