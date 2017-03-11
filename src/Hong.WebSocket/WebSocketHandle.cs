using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Hong.Common.Extendsion.Guard;

namespace Hong.WebSocket
{
    public class WebSocketHandle
    {
        readonly WebSocketSessionManger SessionManager;
        readonly ILogger Log;

        public WebSocketHandle(ILoggerFactory loggerFactory, WebSocketSessionManger sessionManager)
        {
            Log = loggerFactory.CreateLogger("WebSocket");
            SessionManager = sessionManager;
        }

        public async Task Process(HttpContext context)
        {
            var socket = await context.WebSockets.AcceptWebSocketAsync();
            if (socket == null || socket.State != WebSocketState.Open)
            {
                return;
            }

            var cookie = context.Request.Cookies["_guid"];
            if (cookie == null || string.IsNullOrEmpty(cookie))
            {
                throw new Exception("沒有Cookie _guid");
            }

            var session = new WebSocketSession(Log)
            {
                Identity = cookie,
                WebSocket = socket
            };

            await SessionHandle(session);
        }


        /// <summary>处理请求
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        async Task SessionHandle(WebSocketSession session)
        {
            if (!SessionManager.AddSession(session))
            {
                return;
            }

            var buffer = new ArraySegment<Byte>(new Byte[4096]);
            WebSocketReceiveResult received = null;

            while (true)
            {
                try
                {
                    received = await session.WebSocket.ReceiveAsync(buffer, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Log.LogError("#WebSocket =>等待消息异常", ex);
                    break;
                }

                switch (received.MessageType)
                {
                    case WebSocketMessageType.Close:
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("#WebSocket =>关闭");
#endif
                        try
                        {
                            SessionManager.RemoveSession(session);
                        }
                        catch { }

                        try
                        {
                            OnClose?.Invoke(received.CloseStatus, received.CloseStatusDescription);
                        }
                        catch (Exception ex)
                        {
                            Log.LogError("#WebSocket =>关闭事件执行失败", ex);
                        }

                        break;

                    case WebSocketMessageType.Text:
                        var data = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count).Trim('\0');
                        session.LastMessageTime = DateTime.Now;
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("#WebSocket =>收到消息:" + data);
#endif
                        try
                        {
                            OnMessage?.Invoke(received.MessageType, data);
                        }
                        catch (Exception ex)
                        {
                            Log.LogError("#WebSocket =>接收文件消息事件执行失败", ex);
                        }

                        break;

                    case WebSocketMessageType.Binary:
                        session.LastMessageTime = DateTime.Now;
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("#WebSocket =>收到二进制数据,长度:" + buffer.Array.Length);
#endif
                        try
                        {
                            OnBinary?.Invoke(received.MessageType, buffer.Array);
                        }
                        catch (Exception ex)
                        {
                            Log.LogError("#WebSocket =>接收二进制消息事件执行失败", ex);
                        }

                        break;
                }

            }
        }

        /// <summary>关闭事件
        /// </summary>
        public EventHandler<string> OnClose;

        /// <summary>收到文本消息事件
        /// </summary>
        public EventHandler<string> OnMessage;

        /// <summary>收到二进制消息事件
        /// </summary>
        public EventHandler<byte[]> OnBinary;

    }
}
