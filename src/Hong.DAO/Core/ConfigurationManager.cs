using Hong.Common.Extendsion;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Hong.DAO.Core
{
    public class DataConfigurationManager
    {
        static DataConfiguration configuration = null;
        static object _locker = new object();

        public static DataConfiguration Configuration()
        {
            if (configuration != null)
            {
                return configuration;
            }

            lock (_locker)
            {
                if (configuration != null)
                {
                    return configuration;
                }

                var config = new ConfigurationBuilder()
                    .AddJsonFile("web.json")
                    .Build();

                configuration = new DataConfiguration(config);
            }

            return configuration;
        }
    }

    public class DataConfiguration
    {
        const string database = "database";
        const string databaseName = "name";
        const string connection = "connection";

        Dictionary<string, string> creator = new Dictionary<string, string>()
        {
            { "mysql","MySql.Data.MySqlClient.MySqlConnection,MySql.Data"},
            {"sqlite","Microsoft.Data.Sqlite.SqliteConnection,Microsoft.Data.Sqlite" },
            { "mssql","System.Data.SqlClient.SqlConnection,System.Data.SqlClient"}
        };

        public DataConfiguration(IConfiguration config)
        {
            var section = config.GetSection(database);

            DataBase = section.GetValue<string>(databaseName);
            ConnestionString = section.GetValue<string>(connection);

            if (DataBase == "mysql")
            {
                KeywordFrontChar = KeywordEndChar = "`";
            }
            else if (DataBase == "orcal")
            {
                KeywordFrontChar = KeywordEndChar = "`";
                ParamerNameStr = ":v";
            }

            string loadClass = null;
            if (!creator.TryGetValue(DataBase, out loadClass))
            {
                throw new Exception("未知数据库");
            }
            connCreator = Reflection.CreateInstance<DbConnection, string>(Type.GetType(loadClass));
        }

        /// <summary>数据库
        /// </summary>
        public string DataBase = string.Empty;

        /// <summary>连接字符串
        /// </summary>
        public string ConnestionString = string.Empty;

        /// <summary>关键词前置字符
        /// </summary>
        public string KeywordFrontChar = "[";

        /// <summary>关键词后置字符
        /// </summary>
        public string KeywordEndChar = "]";

        /// <summary>变量名前置字符,变量按顺序从"0"依次排下去
        /// </summary>
        public string ParamerNameStr = "@v";

        Func<string, DbConnection> connCreator;

        /// <summary>创建连接
        /// </summary>
        /// <returns></returns>
        public DbConnection CreateConnection()
        {
            return connCreator(ConnestionString);
        }
    }
}
