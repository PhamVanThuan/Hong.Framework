using ProtoBuf;
using System.Collections.Generic;

namespace Hong.DAO.QueryCache
{
    /// <summary>缓存项
    /// </summary>
    [Cache.CacheSet(Cache.ExpirationMode.Absolute, 1200, 1200)]
    [ProtoContract]
    public class CacheItem
    {
        /// <summary>查询结果
        /// </summary>
        [ProtoMember(1)]
        public List<int> IDs = null;

        /// <summary>缓存版本
        /// </summary>
        [ProtoMember(2)]
        public int Version = 0;
    }
}
