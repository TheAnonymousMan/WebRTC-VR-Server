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
        // Get camera reference
        if (mainCamera == null)
            mainCamera = GetComponent<Camera>();

        // Create the video track directly from the camera
        CreateVideoTrack();
    }

    private void CreateVideoTrack()
    {
        // Capture the camera stream as a VideoStreamTrack
        // Width/height define the resolution of the captured stream
        VideoTrack = mainCamera.CaptureStreamTrack(1280, 720);

        Debug.Log("[CameraStreamer] VideoStreamTrack created using CaptureStreamTrack.");
    }

    private void OnDestroy()
    {
        // Clean up
        if (VideoTrack != null)
        {
            VideoTrack.Dispose();
            VideoTrack = null;
        }
    }
}