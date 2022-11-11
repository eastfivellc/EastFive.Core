using System;
using System.IO;

namespace EastFive.Serialization.Text
{
    public interface IMapText
    {
        bool DoesParse(string scope);

        TResource[] Parse<TResource>(Stream csvData,
            IFilterText[] textFilters, 
            params Stream[] csvDataJoins);
    }
}
