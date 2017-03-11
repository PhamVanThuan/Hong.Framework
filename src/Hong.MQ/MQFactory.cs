using Hong.MQ.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Hong.MQ
{
    public class MQFactory
    {
        ConfigurationManager configurationManager = null;

        public MQFactory(IConfiguration config = null)
        {
            if (config == null)
            {
                if (!File.Exists("web.json"))
                {
                    throw new FileNotFoundException("没有找到配置文件web.json");
                }

                var c = new ConfigurationBuilder()
                .AddJsonFile("web.json")
                .Build();

                const string _rabbitMQSection = "rabbitMQ";

                config = c.GetSection(_rabbitMQSection);
                if (config == null)
                {
                    throw new System.Exception("请在配置文件添加'rabbitMQ'区域配置");
                }
            }

            configurationManager = new ConfigurationManager(config);
        }

        public IPublish CreatePublish(ILoggerFactory loggerFactory = null)
        {
            return configurationManager.CreatePublish(loggerFactory);
        }

        public ISubscribe CreateISubscribe(ILoggerFactory loggerFactory = null)
        {
            return configurationManager.CreateSubscribe(loggerFactory);
        }
    }
}
