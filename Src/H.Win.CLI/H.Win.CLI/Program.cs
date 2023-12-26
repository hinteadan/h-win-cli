using H.Necessaire.CLI;
using H.Necessaire.Runtime.CLI;

namespace H.Win.CLI
{
    internal class Program
    {
        public static void Main()
        {
            new CliApp()
                .WithEverything()
                .WithDefaultRuntimeConfig()
                .With(x => x.Register<CLIDependencyGroup>(() => new CLIDependencyGroup()))
                .Run(askForCommandIfEmpty: true)
                .GetAwaiter()
                .GetResult()
                ;
        }
    }
}