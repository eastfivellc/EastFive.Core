using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Serialization
{
    public interface IBind<TFrom>
    {
        TResult Bind<TResult>(TFrom value, Type type,
            Func<object, TResult> onBound,
            Func<TResult> onFailedToBind);
    }
}
