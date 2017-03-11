using Hong.WebSocket;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace WEB_NETCORE.Controllers
{
    public class SocketController : Controller
    {
        private WebSocketHandle SocketHandle { get; set; }

        public SocketController(WebSocketHandle webSocketHandle)
        {
            SocketHandle = webSocketHandle;
            webSocketHandle.OnMessage += OnMessage;
            webSocketHandle.OnClose += OnClose;
            webSocketHandle.OnBinary += OnBinary;
        }

        public async void Accept()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                await SocketHandle.Process(HttpContext);
            }
        }

        private void OnMessage(object sender,string message)
        {

        }

        private void OnBinary(object sender, byte[] message)
        {

        }

        private void OnClose(object sender, string message)
        {

        }
    }
}
