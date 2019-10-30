using System;

namespace Integration.Common.Interface
{
    public interface IBinaryMessageSerializer
    {
        byte[] SerializePayload<T>(T entity);
        T DeserializePayload<T>(byte[] data);
        byte[] SerializePayload(object entity, Type entityType);
        object DeserializePayload(byte[] data, Type entityType);
    }
}
