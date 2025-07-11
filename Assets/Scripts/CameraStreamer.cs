using UnityEngine;
using Unity.WebRTC;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class CameraStreamer : MonoBehaviour
{
    public VideoStreamTrack VideoTrack { get; private set; }

    [SerializeField]
    private Camera mainCamera;

    private void Start()
    {
        // If no camera is assigned in the inspector, use the Camera component on this GameObject
        if (mainCamera == null)
            mainCamera = GetComponent<Camera>();

        // Create the video track directly from the camera feed
        CreateVideoTrack();
    }

    private void CreateVideoTrack()
    {
        // Use Unity.WebRTC's CaptureStreamTrack extension method to create a track
        // Resolution is set to 1280x720 (HD)
        VideoTrack = mainCamera.CaptureStreamTrack(1280, 720);

        Debug.Log("[CameraStreamer] VideoStreamTrack created using CaptureStreamTrack.");
    }

    private void OnDestroy()
    {
        // Dispose the VideoStreamTrack if it exists
        if (VideoTrack != null)
        {
            VideoTrack.Dispose();
            VideoTrack = null;
        }
    }
}
