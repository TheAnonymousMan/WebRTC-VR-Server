using System.Collections;
using Newtonsoft.Json;
using Unity.WebRTC;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

public class WebSocketSignalingServer : MonoBehaviour
{
    private WebSocketServer _webSocketServer;
    public static WebSocketSignalingServer Singleton { get; private set; }

    private void Awake()
    {
        if (Singleton != null && Singleton != this)
        {
            Destroy(gameObject);
            return;
        }

        Singleton = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _webSocketServer = new WebSocketServer(System.Net.IPAddress.Any, 8080);

        _webSocketServer.AddWebSocketService<SignalingBehavior>("/ws");
        _webSocketServer.Start();

        Debug.Log($"WebSocket server started at ws://{System.Net.IPAddress.Broadcast}:8080/ws");
    }

    public void HandleRawMessage(string rawMessage)
    {
        Debug.Log($"[WebSocketSignalingServer] Received message: {rawMessage}");
    }

    public void Broadcast(WebRtcMessage message)
    {
        string json = JsonConvert.SerializeObject(message);
        Broadcast(json);
    }

    public void Broadcast(string message)
    {
        _webSocketServer.WebSocketServices["/ws"].Sessions.Broadcast(message);
    }

    private void OnApplicationQuit()
    {
        _webSocketServer.Stop();
    }
}

public class SignalingBehavior : WebSocketBehavior
{
    protected override void OnOpen()
    {
        Debug.Log("[SignalingBehavior] Client connected.");
    }

    protected override void OnClose(CloseEventArgs e)
    {
        Debug.Log("[SignalingBehavior] Client disconnected.");
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        var msg = JsonConvert.DeserializeObject<WebRtcMessage>(e.Data);

        Debug.Log($"[SignalingBehavior] Received message: {msg.Type}");

        if (msg.Type == "offer")
        {
            Debug.Log($"[SignalingBehavior] Offer received.");
            MainThreadDispatcher.Enqueue(WebRtcServerManager.Singleton.OnOfferReceived(msg));
        }
        else if (msg.Type == "candidate")
        {
            Debug.Log($"[SignalingBehavior] Ice candidate received.");
            WebRtcServerManager.Singleton.OnIceCandidateReceived(msg);
        }
    }
}