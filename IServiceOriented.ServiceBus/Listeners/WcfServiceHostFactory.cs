using System;
using System.Threading;
using System.Configuration;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Principal;

using IServiceOriented.ServiceBus.Collections;

namespace IServiceOriented.ServiceBus.Listeners
{
    /// <summary>
    /// Creates WCF service hosts. Used by WcfListener.
    /// </summary>
    internal static class WcfServiceHostFactory
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

        public static void VerifyContract(Type contractType)
        {
            if (contractType == null) throw new ArgumentNullException("contractType");

            MethodInfo[] methods = contractType.GetMethods();
            HashSet<string> set = new HashSet<string>();

            foreach (MethodInfo method in methods)
            {
                if (set.Contains(method.Name))
                {
                    throw new InvalidContractException("Method overloads are not allowed. The method " + method.Name + " is overloaded.");
                }
                else
                {
                    set.Add(method.Name);
                }

                if (method.ReturnType != typeof(void))
                {
                    throw new InvalidContractException(method.Name + " must have no return value instead of " + method.ReturnType);
                }

                if (method.GetParameters().Length != 1)
                {
                    throw new InvalidContractException("Methods must have one parameter");
                }
            }
        }
        /// <summary>
        /// Dynamically generate a service implementation type for use with a ServiceHost
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <returns></returns>
        public static Type CreateImplementationType(Type interfaceType)
        {
            VerifyContract(interfaceType);

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
                if (attributes.Length > 0 )
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

                    if (methodInfo.GetParameters().Length != 1)
                    {
                        continue; // skip methods that don't accept a single parameter
                    }

                    definePublishOverride(interfaceType, typeBuilder, methodInfo, action);
                }
            }
            Type impType = typeBuilder.CreateType();
            return impType;        
        }

        private static void definePublishOverride(Type interfaceType, TypeBuilder typeBuilder, MethodInfo methodInfo, string action)
        {
            Type[] parameters = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(methodInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, methodInfo.ReturnType, parameters);

            ILGenerator generator = methodBuilder.GetILGenerator();

            MethodInfo publishMethodInfo = typeof(WcfListenerServiceImplementationBase).GetMethod("Publish", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(Type), typeof(string), typeof(object) }, null);

            generator.Emit(OpCodes.Ldarg_0);
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

        /// <summary>
        /// Dynamically generate a service host
        /// </summary>
        public static ServiceHost CreateHost(ServiceBusRuntime runtime, Type contractType, Type implementationType, string configurationName, string address)        
        {            
            if (configurationName == null)
            {
                throw new InvalidOperationException("The endpoint's ConfigurationName was not set");
            }
            object host = Activator.CreateInstance(implementationType);
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
        
        protected void Publish(Type contractType, string action, object message)
        {

            Dictionary<string, object> context = new Dictionary<string, object>();

            // Add security context to the message if it is available
            if (System.ServiceModel.OperationContext.Current.ServiceSecurityContext != null)
            {
                WindowsIdentity identity = System.ServiceModel.OperationContext.Current.ServiceSecurityContext.WindowsIdentity;
                if (identity != null && identity.IsAuthenticated)
                {
                    context.Add(MessageDelivery.WindowsIdentityNameKey, identity.Name);
                    context.Add(MessageDelivery.WindowsIdentityImpersonationLevelKey, identity.ImpersonationLevel.ToString());
                }

                IIdentity primaryIdentity = System.ServiceModel.OperationContext.Current.ServiceSecurityContext.PrimaryIdentity;
                if (primaryIdentity != null)
                {
                    context.Add(MessageDelivery.PrimaryIdentityNameKey, primaryIdentity.Name);
                    //context.Add(MessageDelivery.PrimaryIdentityImpersonationLevelKey, primaryIdentity);
                }
            }
            PublishRequest pr = new PublishRequest(contractType, action, message, new MessageDeliveryContext(context));
            Runtime.Publish(pr);
        }

    }
    

    /// <summary>
    /// Custom service host used by WcfListener
    /// </summary>
    public class WcfListenerServiceHost : ServiceHost
    {
        public WcfListenerServiceHost(object host, string contract, string configurationName, string address) : base(host)
        {
            ConfigurationName = configurationName;
            ContractName = contract;
            Address = address;

            LoadConfigurationSection(GetConfiguration(ConfigurationName, ContractName, Address));
        }

        protected static ServiceElement FindServiceElementInConfig(string name)
        {
            Configuration appConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ServiceModelSectionGroup serviceModel = ServiceModelSectionGroup.GetSectionGroup(appConfig);

            ServiceElement serviceElement = null;
            foreach (ServiceElement e in serviceModel.Services.Services)
            {
                if (e.Name == name)
                {
                    serviceElement = e;
                    break;
                }
            }            
            return serviceElement;
        }

        protected static ServiceElement GetConfiguration(string configurationName, string contract, string address)
        {
            // Use standard WCF configuration elements as a template, find the <service> element with a matching name and replace the contract and address with actual values

            ServiceElement serviceElement = FindServiceElementInConfig(configurationName);
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
            return serviceElement;
        }

        public string ConfigurationName
        {
            get;
            private set;
        }

        public string ContractName
        {
            get;
            private set;
        }

        public string Address
        {
            get;
            private set;
        }

        protected override void ApplyConfiguration()
        {
            
        }
    }    
}
