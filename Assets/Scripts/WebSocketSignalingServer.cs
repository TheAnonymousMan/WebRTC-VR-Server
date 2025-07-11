using System.Collections;
using Newtonsoft.Json;
using Unity.WebRTC;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

/// <summary>
/// Unity MonoBehaviour that starts and manages a WebSocket server for WebRTC signaling.
/// Uses WebSocketSharp to host signaling services within the Unity application.
/// </summary>
public class WebSocketSignalingServer : MonoBehaviour
{
    // WebSocketSharp server instance
    private WebSocketServer _webSocketServer;

    // Singleton instance
    public static WebSocketSignalingServer Singleton { get; private set; }

    /// <summary>
    /// Ensures only one instance exists and persists across scenes.
    /// </summary>
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

    /// <summary>
    /// Starts the WebSocket server and registers the signaling behavior
    /// </summary>
    private void Start()
    {
        // Start WebSocket server on all IP addresses (0.0.0.0) on port 8080
        _webSocketServer = new WebSocketServer(System.Net.IPAddress.Any, 8080);

        // Add a WebSocket service endpoint for signaling at /ws
        _webSocketServer.AddWebSocketService<SignalingBehavior>("/ws");

        // Start the server
        _webSocketServer.Start();

        Debug.Log($"WebSocket server started at ws://{System.Net.IPAddress.Broadcast}:8080/ws");
    }

    /// <summary>
    /// Optional: Handles a raw message received if needed (currently just logs)
    /// </summary>
    public void HandleRawMessage(string rawMessage)
    {
        Debug.Log($"[WebSocketSignalingServer] Received message: {rawMessage}");
    }

    /// <summary>
    /// Serializes and broadcasts a WebRtcMessage to all connected clients
    /// </summary>
    public void Broadcast(WebRtcMessage message)
    {
        string json = JsonConvert.SerializeObject(message);
        Broadcast(json);
    }

    /// <summary>
    /// Broadcasts a raw JSON string to all connected clients
    /// </summary>
    public void Broadcast(string message)
    {
        _webSocketServer.WebSocketServices["/ws"].Sessions.Broadcast(message);
    }

    /// <summary>
    /// Cleans up the WebSocket server when the application quits
    /// </summary>
    private void OnApplicationQuit()
    {
        _webSocketServer.Stop();
    }
}

/// <summary>
/// Handles WebSocket communication with a connected signaling client.
/// Each client connection uses an instance of this class.
/// </summary>
public class SignalingBehavior : WebSocketBehavior
{
    /// <summary>
    /// Called when a client connects
    /// </summary>
    protected override void OnOpen()
    {
        Debug.Log("[SignalingBehavior] Client connected.");
    }

    /// <summary>
    /// Called when a client disconnects
    /// </summary>
    protected override void OnClose(CloseEventArgs e)
    {
        Debug.Log("[SignalingBehavior] Client disconnected.");
    }

    /// <summary>
    /// Called when a message is received from the client
    /// </summary>
    protected override void OnMessage(MessageEventArgs e)
    {
        // Deserialize the incoming JSON string to a WebRtcMessage
        var msg = JsonConvert.DeserializeObject<WebRtcMessage>(e.Data);

        Debug.Log($"[SignalingBehavior] Received message: {msg.Type}");

        // Handle offer (async coroutine via MainThreadDispatcher)
        if (msg.Type == "offer")
        {
            Debug.Log($"[SignalingBehavior] Offer received.");
            MainThreadDispatcher.Enqueue(WebRtcServerManager.Singleton.OnOfferReceived(msg));
        }
        // Handle ICE candidate
        else if (msg.Type == "candidate")
        {
            Debug.Log($"[SignalingBehavior] Ice candidate received.");
            WebRtcServerManager.Singleton.OnIceCandidateReceived(msg);
        }

        // Other message types (answer, data, etc.) can be handled here if needed
    }
}
