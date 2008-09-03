using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Scripting;

using Microsoft.Scripting.Hosting;

namespace IServiceOriented.ServiceBus.Scripting
{
    public class ScriptMessageFilter : MessageFilter
    {
        public ScriptMessageFilter(string languageId, string code)
        {
            LanguageId = languageId;
            Code = code;
            SourceCodeKind = SourceCodeKind.Expression;

            init();
        }

        public ScriptMessageFilter(string languageId, string code, SourceCodeKind sourceCodeKind)
        {
            LanguageId = languageId;
            Code = code;
            SourceCodeKind = sourceCodeKind;

            init();
        }

        ScriptSource _scriptSource;

        void init()
        {
            _scriptSource = ScriptContext.Current.ScriptRuntime.GetEngine(LanguageId).CreateScriptSourceFromString(Code, SourceCodeKind);            
        }

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

        public string Code
        {
            get;
            private set;
        }

        public override bool Include(PublishRequest request)
        {
            ScriptScope scope = ScriptContext.Current.ScriptRuntime.CreateScope();
            scope.SetVariable("request", request);
            return (bool)_scriptSource.Execute(scope);
        }
    }
}
