using System.IO;

namespace Client
{
    internal interface ILoadable
    {
        void LoadFromBytes(MemoryStream data);
    }
}