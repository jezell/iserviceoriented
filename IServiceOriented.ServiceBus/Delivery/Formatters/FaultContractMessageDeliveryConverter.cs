using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Description;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Reflection;

namespace IServiceOriented.ServiceBus.Delivery.Formatters
{
    internal class FaultContractMessageDeliveryConverter : MessageDeliveryConverter
    {
        public FaultContractMessageDeliveryConverter(params Type[] contractTypes) : this( (from c in contractTypes select ContractDescription.GetContract(c)).ToArray())
        {
        }

        public FaultContractMessageDeliveryConverter(params ContractDescription[] contracts)
        {
            foreach (ContractDescription contract in contracts)
            {
                foreach (OperationDescription operation in contract.Operations)
                {
                    foreach (FaultDescription fault in operation.Faults)
                    {
                        cacheSerializer(operation, fault);
                    }
                }
            }
        }


        void cacheSerializer(OperationDescription operation, FaultDescription fault)
        {
            // todo: should this include operation known types? If so, what happens if two ops have different known types but same fault?
            _faultTypes.Add(fault.Action, fault.DetailType);
        }

        Dictionary<string, Type> _faultTypes = new Dictionary<string, Type>();

        protected override object GetMessageObject(System.ServiceModel.Channels.Message message)
        {
            MessageFault messageFault = MessageFault.CreateFault(message, Int32.MaxValue);

            Type faultType;

            if (message.Headers.Action == null)
            {
                // todo: check into null action header on unknown fault messages
                throw new NotImplementedException("empty action");
            }
            else if (_faultTypes.TryGetValue(message.Headers.Action, out faultType))
            {
                MethodInfo m = messageFault.GetType().GetMethod("GetDetail", new Type[] { }).MakeGenericMethod(faultType);
                
                return Activator.CreateInstance(typeof(FaultException<>).MakeGenericType(faultType), m.Invoke(messageFault, null), messageFault.Reason, messageFault.Code);
            }
            else
            {
                throw new InvalidOperationException("Unknown action in fault");
            }

            //return _serializers[message.Headers.Action]. .ReadObject(message.GetReaderAtBodyContents());
        }

        protected override System.ServiceModel.Channels.Message ToMessageCore(MessageDelivery delivery)
        {
            
            return Message.CreateMessage(MessageVersion.Default, ((FaultException)(delivery.Message)).CreateMessageFault(), delivery.Action);
        }

        public override IEnumerable<string> SupportedActions
        {
            get 
            {
                return _faultTypes.Keys;
            }
        }
    }
}
