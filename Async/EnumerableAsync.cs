using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs.Collections.Async
{
    public delegate Task YieldCallbackAsync<TDelegate>(TDelegate yield);
    public delegate Task YieldStructDelegateAsync<TStruct>(Func<TStruct, Task> yield);
    
}
