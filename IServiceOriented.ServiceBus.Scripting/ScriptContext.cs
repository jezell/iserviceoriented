using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Scripting.Hosting;

namespace IServiceOriented.ServiceBus.Scripting
{
    public class ScriptContext
    {
        public ScriptContext()
        {
            ScriptRuntime = ScriptRuntime.Create();
        }

        static ScriptContext()
        {
            Current = new ScriptContext();
        }

        public ScriptRuntime ScriptRuntime
        {
            get;
            private set;
        }

        public static ScriptContext Current
        {
            get;
            private set;
        }
        
    }
}
