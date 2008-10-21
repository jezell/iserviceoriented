using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.ServiceModel.Description;

namespace IServiceOriented.ServiceBus
{
    [Serializable]
    [DataContract]
    public class ActionMessageFilter : MessageFilter
    {
        public ActionMessageFilter(string[] actions)
        {
            _actions = new HashSet<string>(actions);
        }

        [DataMember(Name="Actions")]
        HashSet<string> _actions;

        public ReadOnlyCollection<string> Actions
        {
            get
            {
                return new ReadOnlyCollection<string>(_actions.ToList());
            }
        }

        public override bool Include(PublishRequest request)
        {
            return _actions.Contains(request.Action);
        }

        public static ActionMessageFilter Create(ContractDescription description)
        {
            List<string> actions = new List<string>();

            foreach (OperationDescription od in description.Operations)
            {
                foreach (MessageDescription md in od.Messages)
                {
                   actions.Add(md.Action);
                }

                foreach (FaultDescription fd in od.Faults)
                {
                   actions.Add(fd.Action);                   
                }
            }

            return new ActionMessageFilter(actions.Distinct().ToArray());
        }
    }
}
