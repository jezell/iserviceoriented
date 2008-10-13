using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Reflection;

namespace IServiceOriented.ServiceBus
{
    internal static class WcfUtils
    {
        public static IEnumerable<Type> GetServiceKnownTypes(Type interfaceType)
        {
            ServiceKnownTypeAttribute attribute = interfaceType.GetCustomAttributes(true).OfType<ServiceKnownTypeAttribute>().FirstOrDefault();
            if (attribute != null)
            {
                object obj = Activator.CreateInstance(attribute.DeclaringType);
                Type type = obj.GetType();
                MethodInfo method = type.GetMethod(attribute.MethodName);

                IEnumerable<Type> types = (IEnumerable<Type>)method.Invoke(obj, new object[] { (ICustomAttributeProvider)type });
                return types;
            }
            return new Type[0];

        }
        public static bool UsesMessageContracts(Type interfaceType)
        {
            var infos = GetMessageInformation(interfaceType);
            
            var info = infos.FirstOrDefault();
            if (info != null)
            {
                if (info.MessageType.GetCustomAttributes(true).OfType<MessageContractAttribute>().Count() > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

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

        public const string DefaultNamespace = "http://tempuri.org/";

        public static string GetContractNamespace(Type contractType)
        {
            ServiceContractAttribute attribute = contractType.GetCustomAttributes(true).OfType<ServiceContractAttribute>().FirstOrDefault();
            if (attribute != null)
            {
                return attribute.Namespace ?? DefaultNamespace;
            }
            else
            {
                var cna = contractType.Assembly.GetCustomAttributes(true).OfType<System.Runtime.Serialization.ContractNamespaceAttribute>().FirstOrDefault();
                if (cna != null)
                {
                    return cna.ContractNamespace ?? DefaultNamespace;
                }
            }
            return DefaultNamespace;
        }

        public static string GetReplyAction(Type contractType, MethodInfo info)
        {
            object[] attributes = info.GetCustomAttributes(typeof(OperationContractAttribute), false);
            if (attributes.Length > 0)
            {
                OperationContractAttribute attrib = (OperationContractAttribute)attributes[0];
                if (attrib.IsOneWay) return null;
                if (attrib.ReplyAction == null)
                {
                    return GetContractNamespace(contractType) + info.Name + "Response";
                }
                else
                {
                    return attrib.ReplyAction;
                }
            }
            else
            {
                throw new InvalidOperationException("The method is not a service method");
            }
        }

        public static string GetAction(Type contractType, MethodInfo info)
        {
            object[] attributes = info.GetCustomAttributes(typeof(OperationContractAttribute), false);
            if (attributes.Length > 0)
            {
                OperationContractAttribute attrib = (OperationContractAttribute)attributes[0];
                if (attrib.Action == null)
                {
                    return GetContractNamespace(contractType) + info.Name;
                }
                else
                {
                    return attrib.Action;
                }
            }
            else
            {
                throw new InvalidOperationException("The method is not a service method");
            }
        }

        /// <summary>
        /// Gets a list of the message types that a service contract exposes.
        /// </summary>
        /// <param name="contractType"></param>
        /// <returns></returns>
        public static WcfMessageInformation[] GetMessageInformation(Type contractType)
        {
            List<WcfMessageInformation> list = new List<WcfMessageInformation>();

            foreach (MethodInfo info in contractType.GetMethods())
            {
                if (IsServiceMethod(info))
                {
                    list.Add(new WcfMessageInformation(info.GetParameters()[0].ParameterType, GetAction(contractType, info)));
                }
            }
            return list.ToArray();
        }        

    }

    internal class WcfMessageInformation
    {
        public WcfMessageInformation()
        {
        }

        public WcfMessageInformation(Type messageType, string action)
        {
            MessageType = messageType;
            Action = action;
        }

        public Type MessageType
        {
            get;
            private set;
        }

        public string Action
        {
            get;
            private set;
        }
    }
}
