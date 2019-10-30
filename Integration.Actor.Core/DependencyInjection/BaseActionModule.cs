using Autofac;
using Integration.Common.Actor.UnifiedActor;
using Integration.Common.Interface;
using System;
using System.Linq;
using System.Reflection;

namespace Integration.Actor.Core.DependencyInjection
{
    public class BaseActionModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            //get assemblies to register with attribute action name
            var assembly = Assembly.GetEntryAssembly();
            var data = assembly.GetTypes();
            builder.RegisterAssemblyTypes(assembly)
                .AssignableTo<IAction>()
                .AssignableTo<IRemotableAction>()
                .Where(t => !t.IsInterface && !t.IsAbstract)
                .Keyed<IAction>(t => CalculateIActionKey(t)).InstancePerLifetimeScope();
        }

        private static string CalculateIActionKey(Type actionType)
        {
            //get the first interface implemented the IRemotableAction but not the IRemotableAction itself
            var interfaces = actionType.GetInterfaces().Where(i => i.Name != nameof(IRemotableAction) && i.GetInterfaces().Select(c => c.Name).Contains(nameof(IRemotableAction)));
            if (interfaces != null && interfaces.Count() > 1) throw new InvalidOperationException($"Type {actionType.FullName } has implemented more than 1 remoteable interface which cant be registered. Please make sure only one interface is found on this type.");
            var correctInterface = interfaces.FirstOrDefault();

            //if not found
            if (correctInterface == null) return string.Empty;
            //if a generic type
            if (correctInterface.IsGenericType) throw new NotSupportedException($"Type {correctInterface.FullName } is a generic interface implemented {nameof(IRemotableAction)} which currently not suppored.");

            //Logger.LogInformation($"Registering IAction class for interface of name {correctInterface.Name}");
            return correctInterface.Name ?? string.Empty;
        }
    }
}
