using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using System.Runtime.Serialization;

namespace IServiceOriented.ServiceBus
{
    [Serializable]
    [DataContract]
    public abstract class Endpoint
    {
        protected Endpoint(Guid id, string name, string configurationName, string address, Type contractType)
        {
            Id = id;
            Name = name;
            ConfigurationName = configurationName;
            Address = address;
            ContractType = contractType;
        }

        protected Endpoint(Guid id, string name, string configurationName, string address, string contractTypeName)
        {
            Id = id;
            Name = name;
            ConfigurationName = configurationName;
            Address = address;
            ContractTypeName = contractTypeName;
        }

        Guid _endpointId = Guid.NewGuid();
        [DataMember]
        public Guid Id
        {
            get
            {
                return _endpointId;
            }
            private set
            {
                _endpointId = value;
            }
        }
        Type _contractType;                
        public Type ContractType
        {
            get
            {
                return _contractType;
            }
            private set
            {
                _contractType = value;
            }
        }

        [DataMember]
        public string ContractTypeName
        {
            get
            {
                return _contractType.AssemblyQualifiedName;
            }
            private set
            {
                _contractType = Type.GetType(value);
                if (_contractType == null)
                {
                    throw new InvalidOperationException(value + " is an invalid type");
                }
            }
        }

        string _address;
        [DataMember]
        public string Address
        {
            get
            {
                return _address;
            }
            private set
            {
                _address = value;
            }
        }

        string _configurationName;
        [DataMember]
        public string ConfigurationName
        {
            get
            {
                return _configurationName;
            }
            private set
            {
                _configurationName = value;
            }
        }

        string _name;
        [DataMember]
        public string Name
        {
            get
            {
                return _name;
            }
            private set
            {
                _name = value;
            }
        }
    }


    
}
