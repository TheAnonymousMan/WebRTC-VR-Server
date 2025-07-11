using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Unity.WebRTC;
using UnityEngine;

/// <summary>
/// Manages the WebRTC server-side connection including signaling, ICE candidates, data channels,
/// and video track streaming. Acts as the main controller for a Unity-hosted WebRTC peer.
/// </summary>
public class WebRtcServerManager : MonoBehaviour
{
    // Singleton instance
    public static WebRtcServerManager Singleton;

    // The remote peer connection
    private RTCPeerConnection _remoteConnection;

    // The data channel received from the client
    private RTCDataChannel _receiveChannel;

    // Whether the receive data channel is open
    private bool _isReceiveChannelReady;

    // Queue for messages that arrive before the data channel is ready
    private readonly Queue<string> _queuedMessages = new();

    // ICE candidates received before the remote description is set
    private readonly List<RTCIceCandidate> _pendingCandidates = new();

    private bool _remoteDescSet = false;

    // Reference to the camera stream track sender
    [SerializeField] private CameraStreamer cameraStreamer;

    /// <summary>
    /// Setup Singleton instance and persist across scenes
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
    /// Called on start - initializes WebRTC connection and starts the update loop
    /// </summary>
    private void Start()
    {
        Connect();
        
        // Start Unity.WebRTC coroutine to keep peer connection updated
        StartCoroutine(WebRTC.Update());
    }

    /// <summary>
    /// Establishes a new RTCPeerConnection and hooks up event handlers
    /// </summary>
    private void Connect()
    {
        RTCConfiguration configuration = new RTCConfiguration
        {
            iceServers = new[]
            {
                new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } }
            }
        };

        _remoteConnection = new RTCPeerConnection(ref configuration);

        // Assign callback for receiving a data channel from remote peer
        _remoteConnection.OnDataChannel = ReceiveChannelCallback;

        // Send ICE candidates to the client as they are gathered
        _remoteConnection.OnIceCandidate = candidate =>
        {
            if (candidate != null)
            {
                string json = JsonConvert.SerializeObject(new WebRtcMessage
                {
                    Type = "candidate",
                    Candidate = candidate.Candidate,
                    SdpMid = candidate.SdpMid,
                    SdpMLineIndex = candidate.SdpMLineIndex ?? 0
                });

                WebSocketSignalingServer.Singleton.Broadcast(json);
            }
        };

        _remoteConnection.OnIceConnectionChange = state =>
        {
            Debug.Log("[WebRtcServerManager] ICE State: " + state);
        };
    }

    /// <summary>
    /// Handles an ICE candidate received from the client
    /// </summary>
    public void OnIceCandidateReceived(WebRtcMessage msg)
    {
        var candidate = new RTCIceCandidate(new RTCIceCandidateInit
        {
            candidate = msg.Candidate,
            sdpMid = msg.SdpMid,
            sdpMLineIndex = msg.SdpMLineIndex
        });

        if (_remoteDescSet)
        {
            _remoteConnection.AddIceCandidate(candidate);
        }
        else
        {
            _pendingCandidates.Add(candidate);
            Debug.Log("[WebRtcServerManager] Candidate buffered.");
        }
    }

    /// <summary>
    /// Called when an SDP offer is received from the client
    /// </summary>
    public IEnumerator OnOfferReceived(WebRtcMessage msg)
    {
        Debug.Log("[WebRtcServerManager] Offer received.");

        var desc = new RTCSessionDescription
        {
            type = RTCSdpType.Offer,
            sdp = msg.Sdp
        };

        yield return HandleOffer(desc);
    }

    /// <summary>
    /// Handles setting remote description, sending answer, and adding video track
    /// </summary>
    private IEnumerator HandleOffer(RTCSessionDescription desc)
    {
        Debug.Log("[WebRtcServerManager] Setting remote description...");
        var op = _remoteConnection.SetRemoteDescription(ref desc);
        yield return op;

        _remoteDescSet = true;

        // Apply any candidates received before remote description was ready
        foreach (var candidate in _pendingCandidates)
        {
            _remoteConnection.AddIceCandidate(candidate);
        }
        _pendingCandidates.Clear();
        
        // Prepare to send video (TrackKind.Video)
        _remoteConnection.AddTransceiver(TrackKind.Video);

        // Add local camera video track if available
        if (cameraStreamer != null && cameraStreamer.VideoTrack != null)
        {
            Debug.Log($"[WebRtcServerManager] cameraStreamer: {cameraStreamer != null}, VideoTrack: {cameraStreamer?.VideoTrack}");
            _remoteConnection.AddTrack(cameraStreamer.VideoTrack);
        }
        else
        {
            Debug.LogWarning("[WebRtcServerManager] Video track is null or cameraStreamer not assigned.");
        }

        // Create and send SDP answer
        var answerOp = _remoteConnection.CreateAnswer();
        yield return answerOp;
        var answer = answerOp.Desc;

        yield return _remoteConnection.SetLocalDescription(ref answer);

        Debug.Log("[WebRtcServerManager] Answer created.");
        Debug.Log($"[WebRtcServerManager] Answer SDP:\n{answer.sdp}");

        WebRtcMessage answerMsg = new WebRtcMessage
        {
            Type = "answer",
            Sdp = answer.sdp
        };

        string json = JsonConvert.SerializeObject(answerMsg);
        WebSocketSignalingServer.Singleton.Broadcast(json);
    }

    /// <summary>
    /// Callback when a data channel is opened by the client
    /// </summary>
    private void ReceiveChannelCallback(RTCDataChannel channel)
    {
        Debug.Log($"[WebRtcServerManager] Receive channel created: {channel.Label}");
        _receiveChannel = channel;
        _receiveChannel.OnOpen = HandleReceiveChannelStatusChange;
        _receiveChannel.OnClose = HandleReceiveChannelStatusChange;
        _receiveChannel.OnMessage = HandleReceiveMessage;
    }

    /// <summary>
    /// Updates the ready status of the receive channel and flushes queued messages
    /// </summary>
    private void HandleReceiveChannelStatusChange()
    {
        Debug.Log($"[WebRtcServerManager] DataChannel state: {_receiveChannel.ReadyState}");
        _isReceiveChannelReady = _receiveChannel.ReadyState == RTCDataChannelState.Open;

        if (_isReceiveChannelReady)
        {
            // Send queued messages now that the channel is open
            while (_queuedMessages.Count > 0)
            {
                string msg = _queuedMessages.Dequeue();
                _receiveChannel.Send(msg);
                Debug.Log($"[WebRtcServerManager] Flushed message: {msg}");
            }
        }
        else
        {
            Debug.Log("[WebRtcServerManager] Send channel closed or not ready.");
            _isReceiveChannelReady = false;
        }
    }

    /// <summary>
    /// Sends a message to the connected client via data channel, or queues it if not ready
    /// </summary>
    public void SendMessageBuffered(string message)
    {
        _isReceiveChannelReady = _receiveChannel.ReadyState == RTCDataChannelState.Open;

        if (_isReceiveChannelReady)
        {
            _receiveChannel.Send(message);
            Debug.Log($"[WebRtcServerManager] Sent message: {message}");
        }
        else
        {
            Debug.Log("[WebRtcServerManager] Channel not ready. Queuing message.");
            _queuedMessages.Enqueue(message);
        }
    }

    /// <summary>
    /// Callback for when a message is received via data channel
    /// </summary>
    private void HandleReceiveMessage(byte[] bytes)
    {
        var message = Encoding.UTF8.GetString(bytes);
        Debug.Log($"[WebRtcServerManager] Message received: {message}");
    }

    /// <summary>
    /// (Optional/Unused) Stub to add video track to the peer connection
    /// </summary>
    private void AddLocalVideoTrack(VideoStreamTrack track)
    {
        if (_remoteConnection != null)
        {
            // (Placeholder: track could be added here if needed)
            Debug.Log("[WebRtcServerManager] Local video track added to Remote Connection.");
        }
        else
        {
            Debug.LogWarning("[WebRtcServerManager] Remote Connection is null. Cannot add video track.");
        }
    }

    /// <summary>
    /// Cleanup WebRTC resources
    /// </summary>
    private void OnDestroy()
    {
        _receiveChannel?.Close();
        _remoteConnection?.Close();
    }
}
