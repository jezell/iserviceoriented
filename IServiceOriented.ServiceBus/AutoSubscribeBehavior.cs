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
                bool alreadyAdded = (from s in managementService.ListSubscribers() where s.Id == Subscription.Id select s).Count() > 0;
                if(!alreadyAdded) managementService.Subscribe(Subscription);
            });
        }

        void owner_Closed(object sender, EventArgs e)
        {
            if (UnsubscribeOnClosing)
            {
                Service.Use<IServiceBusManagementService>(managementService =>
                {
                    managementService.Unsubscribe(Subscription.Id);
                });
            }
        }

        public SubscriptionEndpoint Subscription
        {
            get;
            set;
        }

        public bool UnsubscribeOnClosing
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
            UnsubscribeOnClosing = true;
        }

        public AutoSubscribe(string name, string configurationName, Type contractType) 
            : this()
        {
            Name = name;
            ConfigurationName = configurationName;
            ContractType = contractType;           
        }

        public AutoSubscribe(Guid subscriptionId, string name, string configurationName, Type contractType)
            : this()
        {
            SubscriptionId = subscriptionId;
            Name = name;
            ConfigurationName = configurationName;
            ContractType = contractType;
        }


        #region IServiceBehavior Members
        

        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, System.Collections.ObjectModel.Collection<ServiceEndpoint> endpoints, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
        {
            // Todo: Add configuration support  
            // Todo: Do custom message filters need to be supported?

            Dispatcher dispatcher;

            if (DispatcherType != null)
            {
                dispatcher = (Dispatcher)Activator.CreateInstance(DispatcherType);
            }
            else
            {
                dispatcher = new WcfDispatcher();
            }

            SubscriptionEndpoint subscription = new SubscriptionEndpoint(SubscriptionId ?? Guid.NewGuid(), Name, ConfigurationName, Address ?? serviceDescription.Endpoints[0].Address.Uri.ToString(), ContractType, dispatcher, WcfDispatcher.CreateMessageFilter(ContractType));
            SubscriptionExtension extension = new SubscriptionExtension(subscription);
            extension.UnsubscribeOnClosing = UnsubscribeOnClosing;
            serviceHostBase.Extensions.Add(extension);
        }


        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            
        }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            if (Address == null)
            {
                // Make sure the endpoint contains an address if we are going to default to the first.
                if (serviceDescription.Endpoints.Count == 0)
                {
                    throw new InvalidOperationException("The service description does not contain any endpoints");
                }
            }
        }

        #endregion


        /// <summary>
        /// Gets or sets the Name used for the subscription.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the ConfigurationName used by the subscription
        /// </summary>
        public string ConfigurationName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the type of the contract to be used by the subscription.
        /// </summary>
        public Type ContractType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the address to be used by the subscription.
        /// </summary>
        /// <remarks> If null, the address of the first endpoint in the service description will be used.</remarks>
        public string Address
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets whether the subscription should be persisted
        /// </summary>
        public bool Persistent
        {
            get;
            set;
        }


        public bool UnsubscribeOnClosing
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the ID used by the subscription.
        /// </summary>
        /// <remarks>If null, a new GUID will be generated.</remarks>
        public Guid? SubscriptionId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the Dispatcher type used by the subscription.
        /// </summary>
        /// <remarks>If null, WcfDispatcher will be used</remarks>
        public Type DispatcherType
        {
            get;
            set;
        }

    }
}
