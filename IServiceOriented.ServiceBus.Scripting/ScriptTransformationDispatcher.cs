using System;
using System.Collections.Generic;
using System.Text;

using System.Runtime.Serialization;
using Microsoft.Scripting;
using IServiceOriented.ServiceBus.Dispatchers;

namespace IServiceOriented.ServiceBus.Scripting
{
    [Serializable]
    [DataContract]
    public class ScriptTransformationDispatcher : TransformationDispatcher
    {
        public ScriptTransformationDispatcher(string languageId, string code)
        {
            Script = new Script(languageId, code);
        }

        public ScriptTransformationDispatcher(string languageId, string code, SourceCodeKind sourceCodeKind)
        {
            Script = new Script(languageId, code, sourceCodeKind);         
        }
        
        [DataMember]
        public Script Script
        {
            get;
            set;
        }

        protected override PublishRequest Transform(PublishRequest request)        
        {
            return (PublishRequest)Script.ExecuteWithVariables(new Dictionary<string, object>() { { "request", request } });            
        }
    }
}
