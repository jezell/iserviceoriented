using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.ServiceLocation;

namespace IServiceOriented.ServiceBus
{
    public class SimpleServiceLocator : IServiceLocator
    {
        public SimpleServiceLocator()
        {
        }

        #region IServiceLocator Members

        public IEnumerable<TService> GetAllInstances<TService>()
        {
            List<TService> services = new List<TService>();
            foreach (RegisteredService rs in _registeredServices)
            {
                if (typeof(TService).IsAssignableFrom(rs.Type))
                {
                    services.Add((TService)rs.Instance);
                }
            }
            return services;
        }

        public IEnumerable<object> GetAllInstances(Type serviceType)
        {
            List<object> services = new List<object>();
            foreach (RegisteredService rs in _registeredServices)
            {
                if (serviceType.IsAssignableFrom(rs.Type))
                {
                    services.Add(rs.Instance);
                }
            }
            return services;
        }

        public TService GetInstance<TService>(string key)
        {
            return (TService)GetInstance(typeof(TService), key);
        }

        public TService GetInstance<TService>()
        {
            return (TService)GetInstance(typeof(TService));
        }

        public object GetInstance(Type serviceType, string key)
        {
            foreach (RegisteredService rs in _registeredServices)
            {
                if (key == rs.Key && serviceType.IsAssignableFrom(rs.Type))
                {
                    return rs.Instance;
                }
            }

            throw new ActivationException("The key has not been registered with the specified service type: " + serviceType);
        }

        public object GetInstance(Type serviceType)
        {
            foreach (RegisteredService rs in _registeredServices)
            {
                if (serviceType.IsAssignableFrom(rs.Type))
                {
                    return rs.Instance;
                }
            }

            throw new ActivationException("The key has not been registered with the specified service type: "+serviceType);
        }

        #endregion

        #region IServiceProvider Members

        public object GetService(Type serviceType)
        {
            return GetInstance(serviceType);
        }

        #endregion


        List<RegisteredService> _registeredServices = new List<RegisteredService>();

        public void RegisterService(object instance)
        {
            RegisterService(instance, null);
        }

        public void RegisterService(object instance, string key)
        {
            if (instance == null) throw new ArgumentNullException("instance");
            _registeredServices.Add(new RegisteredService(instance.GetType(), key, instance));
        }
        public void RegisterService(Type serviceType, string key)
        {
            _registeredServices.Add(new RegisteredService(serviceType, key, Activator.CreateInstance(serviceType)));
        }

        public void RegisterService(Type serviceType)
        {
            RegisterService(serviceType, null);
        }

        class RegisteredService
        {
            public RegisteredService()
            {
            }
            public RegisteredService(Type type, string key, object instance)
            {
                Type = type;
                Key = key;
                Instance = instance;
            }
            public Type Type;
            public Object Instance;
            public string Key;
        }

        public static SimpleServiceLocator With(params object[] services)
        {
            SimpleServiceLocator loc = new SimpleServiceLocator();
            foreach (object o in services)
            {
                loc.RegisterService(o);
            }
            return loc;
        }
    }

    
}
