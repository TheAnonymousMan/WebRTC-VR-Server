using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Unity.WebRTC;
using UnityEngine;

public class WebRtcServerManager : MonoBehaviour
{
    public static WebRtcServerManager Singleton;

    private RTCPeerConnection _remoteConnection;
    private RTCDataChannel _receiveChannel;

    private bool _isReceiveChannelReady;
    private readonly Queue<string> _queuedMessages = new();

    private readonly List<RTCIceCandidate> _pendingCandidates = new();
    private bool _remoteDescSet = false;

    [SerializeField] private CameraStreamer cameraStreamer;

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
        Connect();
        
        // Continuously update WebRTC
        StartCoroutine(WebRTC.Update());
    }

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
        _remoteConnection.OnDataChannel = ReceiveChannelCallback;
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
        
        _remoteConnection.OnIceConnectionChange = state => { Debug.Log("[WebRtcServerManager] ICE State: " + state); };
    }

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

    private IEnumerator HandleOffer(RTCSessionDescription desc)
    {
        Debug.Log("[WebRtcServerManager] Setting remote description...");
        var op = _remoteConnection.SetRemoteDescription(ref desc);
        yield return op;

        _remoteDescSet = true;

        // 🔥 Flush buffered candidates
        foreach (var candidate in _pendingCandidates)
        {
            _remoteConnection.AddIceCandidate(candidate);
        }

        _pendingCandidates.Clear();
        
        _remoteConnection.AddTransceiver(TrackKind.Video);
        
        if (cameraStreamer != null && cameraStreamer.VideoTrack != null)
        {
            Debug.Log($"[WebRtcServerManager] cameraStreamer: {cameraStreamer != null}, VideoTrack: {cameraStreamer?.VideoTrack}");
            _remoteConnection.AddTrack(cameraStreamer.VideoTrack);
        }
        else
        {
            Debug.LogWarning("[WebRtcServerManager] Video track is null or cameraStreamer not assigned.");
        }

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

    private void HandleReceiveChannelStatusChange()
    {
        Debug.Log($"[WebRtcServerManager] DataChannel state: {_receiveChannel.ReadyState}");
        _isReceiveChannelReady = _receiveChannel.ReadyState == RTCDataChannelState.Open;

        if (_isReceiveChannelReady)
        {
            // Flush queued messages
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

    private void ReceiveChannelCallback(RTCDataChannel channel)
    {
        Debug.Log($"[WebRtcServerManager] Receive channel created: {channel.Label}");
        _receiveChannel = channel;
        _receiveChannel.OnOpen = HandleReceiveChannelStatusChange;
        _receiveChannel.OnClose = HandleReceiveChannelStatusChange;
        _receiveChannel.OnMessage = HandleReceiveMessage;
    }

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

    private void HandleReceiveMessage(byte[] bytes)
    {
        var message = Encoding.UTF8.GetString(bytes);
        Debug.Log($"[WebRtcServerManager] Message received: {message}");
    }

    private void AddLocalVideoTrack(VideoStreamTrack track)
    {
        if (_remoteConnection != null)
        {
            
            Debug.Log("[WebRtcServerManager] Local video track added to Remote Connection.");
        }
        else
        {
            Debug.LogWarning("[WebRtcServerManager] Remote Connection is null. Cannot add video track.");
        }
    }

    private void OnDestroy()
    {
        _receiveChannel?.Close();
        _remoteConnection?.Close();
    }
}