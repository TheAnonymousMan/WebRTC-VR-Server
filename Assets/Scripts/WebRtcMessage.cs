using Newtonsoft.Json;

/// <summary>
/// Represents a WebRTC signaling message used to exchange SDP offers, answers, ICE candidates,
/// or custom data between peers.
/// This is serialized and deserialized as JSON for signaling over WebSocket or other channels.
/// </summary>
public class WebRtcMessage
{
    // Type of message: "offer", "answer", "candidate", "data", etc.
    [JsonProperty("type")] 
    public string Type;

    // Session Description Protocol (SDP) data for offer/answer
    [JsonProperty("sdp")] 
    public string Sdp;

    // ICE candidate string used for network traversal
    [JsonProperty("candidate")] 
    public string Candidate;

    // The media stream identification tag associated with the ICE candidate
    [JsonProperty("sdpMid")] 
    public string SdpMid;

    // The index of the media description in SDP this candidate is associated with
    [JsonProperty("sdpMLineIndex")] 
    public int SdpMLineIndex;

    // Optional custom data (e.g., chat messages or control messages)
    [JsonProperty("data")] 
    public string Data;
}
