using Autofac;

namespace ServiceFabric.Integration.Actor.Core.Helpers
{
    public class CoreDependencyResolver
    {
        public static IContainer Container;
        public static ContainerBuilder Builder;

        static CoreDependencyResolver()
        {
            Builder = new ContainerBuilder();
        }

        public static void Build()
        {
            Container = Builder.Build();
        }

        public static ILifetimeScope CreateLifetimeScope(string scopeName = null)
        {
            if (!string.IsNullOrEmpty(scopeName))
            {
                return Container.BeginLifetimeScope(scopeName);
            }
            else
                return Container.BeginLifetimeScope();
        }
    }
}
