using System;

namespace Comvita.Common.Actor.Interfaces
{
    public interface ICsvContent
    {
        Tuple<string, string> ToCsv();
    }

    public interface IXmlContent
    {
        Tuple<string, string> ToXml();
    }
}