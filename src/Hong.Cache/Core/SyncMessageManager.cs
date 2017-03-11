using Hong.Common.Tools;
using Hong.MQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using static Hong.Common.Extendsion.Guard;

namespace Hong.Cache.Core
{
    public class SyncMessageManager
    {
        public SyncMessageManager(IConfiguration config, ILoggerFactory loggerFactory = null)
        {
            NotNull(config, nameof(config));

            var factory = new MQFactory(config);
            Publisher = factory.CreatePublish(loggerFactory);
            Subscriber = factory.CreateISubscribe(loggerFactory);

            RegisteSubscriber();
        }

        /// <summary>消息发布者
        /// </summary>
        IPublish Publisher { get; set; }

        /// <summary>消息认阅者
        /// </summary>
        ISubscribe Subscriber { get; set; }

        /// <summary>自已的KEY
        /// </summary>
        public string OwnerIdentify { get; private set; } = Guid.NewGuid().ToString();

        /// <summary>注册订阅
        /// </summary>
        /// <returns></returns>
        public void RegisteSubscriber()
        {
            Subscriber.Start(true, Event);
        }

        #region 通知消息

        public void NotifyUpdate(string key, string region) => Notify(SyncMessageAction.UPDATED, key, region);

        public void NotifyRemove(string key, string region) => Notify(SyncMessageAction.REMOVE, key, region);

        public void NotifyClear(string region) => Notify(SyncMessageAction.CLEAR, null, region);

        public void Notify(SyncMessageAction action, string key, string region)
        {
            var msg = new SyncMessage()
            {
                Action = action,
                Key = key,
                Region = region,
                OwnerIdentity = OwnerIdentify
            };

            Publisher.SendMsg(System.Text.Encoding.UTF8.GetString(msg.Serialize()));
        }

        #endregion

        #region 消息事件

        private void Event(string exchange, string routeKey, string msg)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(msg);
            var syncMessage = bytes.Deserialize<SyncMessage>();

            if (syncMessage.OwnerIdentity == this.OwnerIdentify)
            {
                return;
            }

            switch (syncMessage.Action)
            {
                case SyncMessageAction.UPDATED:
                    OnUpdate(this, syncMessage);
                    break;

                case SyncMessageAction.REMOVE:
                    OnRemove(this, syncMessage);
                    break;

                case SyncMessageAction.CLEAR:
                    OnClear(this, syncMessage);
                    break;
            }
        }

        /// <summary>更新缓存事件
        /// </summary>
        public EventHandler<SyncMessage> OnUpdate;

        /// <summary>删除缓存回调
        /// </summary>
        public EventHandler<SyncMessage> OnRemove;

        /// <summary>清除所有缓存回调
        /// </summary>
        public EventHandler<SyncMessage> OnClear;

        #endregion
    }
}
