using ProtoBuf;

namespace Hong.DAO.QueryCache
{
    /// <summary>SQL Key缓存项
    /// </summary>
    [Cache.CacheSet(Cache.ExpirationMode.None, 0, 0)]
    [ProtoContract]
    public class SQLKeyItem
    {
        /// <summary>SQL 语句的Key
        /// </summary>
        [ProtoMember(1)]
        public string SQLKey;

        /// <summary>缓存版本
        /// </summary>
        [ProtoMember(2)]
        public int Version;
    }
}
