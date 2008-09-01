using System;
using System.Configuration;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Reflection;
using System.Reflection.Emit;

namespace IServiceOriented.ServiceBus
{
    /// <summary>
    /// Creates WCF service hosts. Used by WcfListener.
    /// </summary>
    public static class WcfServiceHostFactory
    {
        /// <summary>
        /// Determines whether the specified method is marked as OperationContract and supported.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static bool IsServiceMethod(MethodInfo info)
        {
            // Only single parameter calls are supported currently
            if (info.GetParameters().Length != 1)
            {
                return false;
            }
            // Only one way methods are supported
            if (info.ReturnType != typeof(void))
            {
                return false; 
            }
            object[] attributes = info.GetCustomAttributes(typeof(OperationContractAttribute), false);
            if (attributes.Length > 0)
            {
                return true;
            }
            return false;                        
        }

        /// <summary>
        /// Gets a list of the message types that a service contract exposes.
        /// </summary>
        /// <param name="contractType"></param>
        /// <returns></returns>
        public static Type[] GetMessageTypes(Type contractType)
        {
            List<Type> list = new List<Type>();

            foreach (MethodInfo info in contractType.GetMethods())
            {
                if (IsServiceMethod(info))
                {
                    list.Add(info.GetParameters()[0].ParameterType);
                }
            }

            return list.ToArray();
        }
        
        /// <summary>
        /// Dynamically generate a service implementation type for use with a ServiceHost
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <returns></returns>
        public static Type CreateImplementationType(Type interfaceType)
        {
            ServiceBusRuntime.VerifyContract(interfaceType);

            AssemblyName assemblyName = new AssemblyName("ServiceBusTmpOf"+interfaceType.FullName);
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("ServiceBusTmpModule");
            
            TypeBuilder typeBuilder = moduleBuilder.DefineType("ImplementationOf" + interfaceType.Name, TypeAttributes.Class | TypeAttributes.Public, typeof(WcfListenerServiceImplementationBase), new Type[] { interfaceType });
            
            CustomAttributeBuilder attributeBuilder = new CustomAttributeBuilder(typeof(ServiceBehaviorAttribute).GetConstructor(new Type[] { }), new object[] { }, new PropertyInfo[] { typeof(ServiceBehaviorAttribute).GetProperty("InstanceContextMode"), typeof(ServiceBehaviorAttribute).GetProperty("ConcurrencyMode"), typeof(ServiceBehaviorAttribute).GetProperty("Namespace") }, new object[] { InstanceContextMode.Single, ConcurrencyMode.Multiple, "http://iserviceoriented.com/serviceBus/2008/" });                    
            typeBuilder.SetCustomAttribute(attributeBuilder);

            foreach (MethodInfo methodInfo in interfaceType.GetMethods())
            {
                // Only add methods with OperationContractAttribute
                string action = methodInfo.Name;
                object[] attributes = methodInfo.GetCustomAttributes(typeof(OperationContractAttribute), false);
                if (attributes.Length > 0)
                {
                    OperationContractAttribute oca = (OperationContractAttribute)attributes[0];
                    string ocAction = oca.Action;
                    if (ocAction != null)
                    {
                        action = ocAction;
                    }

                    if (!oca.IsOneWay)
                    {
                        throw new InvalidOperationException("Only one way operations are supported for WCF contracts");
                    }

                    Type[] parameters = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
                    MethodBuilder methodBuilder = typeBuilder.DefineMethod(methodInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, methodInfo.ReturnType, parameters);

                    ILGenerator generator = methodBuilder.GetILGenerator();

                    MethodInfo publishMethodInfo = typeof(ServiceBusRuntime).GetMethod("Publish", new Type[] { typeof(Type), typeof(string), typeof(object) });
                    MethodInfo runtimeMethodInfo = typeof(WcfListenerServiceImplementationBase).GetProperty("Runtime").GetGetMethod();

                    generator.Emit(OpCodes.Ldarg_0);
                    generator.EmitCall(OpCodes.Call, runtimeMethodInfo, null);
                    generator.Emit(OpCodes.Ldtoken, interfaceType);
                    generator.EmitCall(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Public | BindingFlags.Static), null);
                    generator.Emit(OpCodes.Ldstr, action);
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        generator.Emit(OpCodes.Ldarg, i + 1);
                    }

                    generator.EmitCall(OpCodes.Callvirt, publishMethodInfo, null);

                    generator.Emit(OpCodes.Nop);
                    generator.Emit(OpCodes.Ret);

                    typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
                }
            }
            Type impType = typeBuilder.CreateType();
            return impType;        
        }

        /// <summary>
        /// Dynamically generate a service host
        /// </summary>
        /// <param name="runtime"></param>
        /// <param name="contractType"></param>
        /// <param name="configurationName"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public static ServiceHost CreateHost(ServiceBusRuntime runtime, Type contractType, string configurationName, string address)        
        {
            Type hostType = CreateImplementationType(contractType);            
            if (configurationName == null)
            {
                throw new InvalidOperationException("The endpoint's ConfigurationName was not set");
            }
            object host = Activator.CreateInstance(hostType);
            ((WcfListenerServiceImplementationBase)host).Runtime = runtime;
            ServiceHost serviceHost = new WcfListenerServiceHost(host, contractType.FullName, configurationName, address);
            return serviceHost;
        }
        
    }

    /// <summary>
    /// Base class used by dynamically generated service hosts
    /// </summary>
    public class WcfListenerServiceImplementationBase
    {
        public ServiceBusRuntime Runtime
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Custom service host used by WcfListener
    /// </summary>
    public class WcfListenerServiceHost : ServiceHost
    {
        public WcfListenerServiceHost(object host, string contract, string configurationName, string address)
            : base(host)
        {
            ConfigurationName = configurationName;

            Configuration appConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ServiceModelSectionGroup serviceModel = ServiceModelSectionGroup.GetSectionGroup(appConfig);

            ServiceElement serviceElement = null;
            foreach (ServiceElement e in serviceModel.Services.Services)
            {
                if (e.Name == ConfigurationName)
                {
                    serviceElement = e;
                    break;
                }
            }
            if (serviceElement == null)
            {
                throw new InvalidOperationException("Invalid endpoint configuration name specified");
            }

            if (serviceElement.Endpoints.Count != 1)
            {
                throw new InvalidOperationException("Configuration must contain exactly one endpoint");
            }
            serviceElement.Endpoints[0].Contract = contract;
            serviceElement.Endpoints[0].Address = new Uri(address);
            LoadConfigurationSection(serviceElement);
        }

        public string ConfigurationName
        {
            get;
            private set;
        }

        protected override void ApplyConfiguration()
        {

        }
    }    
}
