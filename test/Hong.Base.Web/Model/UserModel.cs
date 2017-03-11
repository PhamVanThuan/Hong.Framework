using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hong.DAO.Core;
using Hong.Cache;
using Hong.Cache.Core;
using ProtoBuf;

namespace Hong.Base.API.Model
{
    [CacheSet(ExpirationMode.None,0,0)]
    [DataTable("t_users")]
    [ProtoContract]
    public class UserModel : DataModel, IDataVersion
    {
        [DBField("name", 255)]
        [ProtoMember(1)]
        public string Name { get; set; } = string.Empty;

        [DBField("age", 3)]
        [ProtoMember(2)]
        public int Age { get; set; } = 0;

        [DBField("password", 255)]
        [ProtoMember(3)]
        public string password { get; set; } = string.Empty;

        [DBField("create_time")]
        [ProtoMember(4)]
        public DateTime CreateTime { get; set; }= DateTime.Now;

        [DBField("description", 255)]
        [ProtoMember(5)]
        public string description { get; set; } = string.Empty;

        [DBField("version", 11)]
        [ProtoMember(6)]
        public override long Version
        {
            get;set;
        }

        [DBField("id", 0)]
        [ProtoMember(7)]
        public override long ID { get; set; }

        [ProtoMember(8)]
        public override IDictionary<string, object> DynamicPropertys { get; set; } = new Dictionary<string, object>();
    }
}
