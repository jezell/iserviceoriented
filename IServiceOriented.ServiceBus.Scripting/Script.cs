using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using Microsoft;

namespace IServiceOriented.ServiceBus.Scripting
{
    [DataContract]
    public class Script
    {
        public Script(string languageId, string code)
        {
            LanguageId = languageId;
            Code = code;
            SourceCodeKind = SourceCodeKind.Expression;
        }

        public Script(string languageId, string code, SourceCodeKind sourceCodeKind)
        {
            LanguageId = languageId;
            Code = code;
            SourceCodeKind = sourceCodeKind;            
        }

        object _scriptLock = new object();

        [NonSerialized]
        ScriptSource _scriptSource;

        public void Check()
        {
            scriptSourceInit();
        }

        void scriptSourceInit()
        {
            lock (_scriptLock)
            {
                if (_scriptSource == null)
                {
                    _scriptSource = ScriptContext.Current.ScriptRuntime.GetEngine(LanguageId).CreateScriptSourceFromString(Code, SourceCodeKind);
                }
            }            
        }

        protected ScriptSource ScriptSource
        {
            get
            {
                if (_scriptSource == null)
                {
                    scriptSourceInit();
                }
                return _scriptSource;
            }
        }

        [DataMember]
        public string LanguageId
        {
            get;
            private set;
        }

        public SourceCodeKind SourceCodeKind
        {
            get;
            private set;
        }

        // For serialization support
        [DataMember(Name = "SourceCodeKind")]
        private string SourceCodeKindName
        {
            get
            {
                return SourceCodeKind.ToString();
            }
            set
            {
                if (value == null)
                {
                    SourceCodeKind = SourceCodeKind.Expression;
                }
                else
                {
                    SourceCodeKind = (SourceCodeKind)Enum.Parse(typeof(SourceCodeKind), value);
                }
            }
        }

        [DataMember]
        public string Code
        {
            get;
            private set;
        }

        public object ExecuteWithVariables(IDictionary<string, object> variables)
        {
            ScriptScope scope = ScriptContext.Current.ScriptRuntime.CreateScope();
            ScriptEngine engine = ScriptContext.Current.ScriptRuntime.GetEngine(LanguageId);

            foreach (string key in variables.Keys)
            {
                scope.SetVariable(key, variables[key]);
            }
            if (SourceCodeKind == SourceCodeKind.Expression)
            {
                return ScriptSource.Execute(scope);
            }
            else
            {
                ScriptSource.Execute(scope);
                var handle = scope.GetVariableHandle("Execute");
                if (handle == null) throw new InvalidOperationException("Script does not define an Execute method");        
                Func<Object> func = engine.Operations.Unwrap<Microsoft.Func<object>>(handle);                
                return func.Invoke();
            }
        }
    }
}
