using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.ServiceModel;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Description;

namespace IServiceOriented.ServiceBus
{
    public class SubscriptionExtension : IExtension<ServiceHostBase>
    {
        public SubscriptionExtension(SubscriptionEndpoint subscription)
        {
            Subscription = subscription;   
        }

        #region IExtension<ServiceHostBase> Members

        public void Attach(ServiceHostBase owner)
        {
            ServiceHost = owner;
            owner.Opened += owner_Opened;    
            owner.Closing+= owner_Closed;
        }
        
        public void Detach(ServiceHostBase owner)
        {
            owner.Opened -= owner_Opened;
            owner.Closing += owner_Closed;
            ServiceHost = null;
        }

        void owner_Opened(object sender, EventArgs e)
        {
            Service.Use<IServiceBusManagementService>(managementService =>
                {
                    managementService.Subscribe(Subscription);
                });
        }

        void owner_Closed(object sender, EventArgs e)
        {
            Service.Use<IServiceBusManagementService>(managementService =>
            {
                managementService.Unsubscribe(Subscription.Id);
            });
        }

        public SubscriptionEndpoint Subscription
        {
            get;
            set;
        }

        ServiceHostBase ServiceHost
        {
            get;
            set;
        }


        #endregion
    }

    public class AutoSubscribe : Attribute,  IServiceBehavior
    {
        public AutoSubscribe()
        {
        }

        public AutoSubscribe(string name, string configurationName, Type contractType)
        {
            Name = name;
            ConfigurationName = configurationName;
            ContractType = contractType;
        }

        #region IServiceBehavior Members

        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, System.Collections.ObjectModel.Collection<ServiceEndpoint> endpoints, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
        {
            // Todo: Add configuration support
            // Todo: Add message filter automatically
            // Todo: Figure out how to handle service hosts with multiple endpoionts

            SubscriptionEndpoint subscription = new SubscriptionEndpoint(Guid.NewGuid(), Name, ConfigurationName, serviceDescription.Endpoints[0].Address.Uri.ToString(), ContractType, new WcfDispatcher() { ApplyCredentials = true }, null);
            serviceHostBase.Extensions.Add(new SubscriptionExtension(subscription));            
        }


        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            
        }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            
        }

        #endregion

        public string Name
        {
            get;
            set;
        }

        public string ConfigurationName
        {
            get;
            set;
        }

        public Type ContractType
        {
            get;
            set;
        }

    }
}
