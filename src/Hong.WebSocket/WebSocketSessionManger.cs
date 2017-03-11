using System.Collections.Concurrent;
using System.Threading.Tasks;
using static Hong.Common.Extendsion.Guard;
using Hong.Common.Extendsion;

namespace Hong.WebSocket
{
    public class WebSocketSessionManger
    {
        static ConcurrentDictionary<string, WebSocketSession> _session = new ConcurrentDictionary<string, WebSocketSession>();

        public WebSocketSession GetSession(string identity)
        {
            NotNullOrEmpty(identity, nameof(identity));

            WebSocketSession session = null;
            if (_session.TryGetValue(identity, out session))
            {
                return session;
            }

            return null;
        }

        public bool RemoveSession(WebSocketSession session)
        {
            NotNull(session, nameof(session));
            return _session.TryRemove(session.Identity, out session);
        }

        public bool AddSession(WebSocketSession session)
        {
            NotNull(session, nameof(session));
            return _session.TryAdd(session.Identity, session);
        }

        public System.Collections.Generic.ICollection<WebSocketSession> Values
        {
            get { return _session.Values; }
        }

        /// <summary>当前请求对应的session
        /// </summary>
        public WebSocketSession CurrentRequestSession
        {
            get
            {
                var guid = Cookie.Get("_guid");
                if (string.IsNullOrEmpty(guid))
                {
                    return null;
                }

                return GetSession(guid);
            }
        }

        /// <summary>消息广播
        /// </summary>
        /// <param name="message">消息</param>
        public void Broadcast(string message)
        {
            var task = new Task(() =>
            {
                foreach (var session in _session.Values)
                {
                    new Task(async () => await session.Send(message), TaskCreationOptions.AttachedToParent).Start();
                }
            });

            task.Start();
            task.Wait();
        }

        /// <summary>关闭所有连接
        /// </summary>
        public void CloseAllSession()
        {
            var task = new Task(() =>
            {
                foreach (var session in _session.Values)
                {
                    new Task(async () => await session.Close(), TaskCreationOptions.AttachedToParent).Start();
                }
            });

            task.Start();
            task.Wait();
        }
    }
}
