using UnityEngine;
using Unity.WebRTC;
using System.Collections;

/// <summary>
/// This component captures a Unity Camera feed and creates a WebRTC VideoStreamTrack
/// that can be sent over a WebRTC connection.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraStreamer : MonoBehaviour // Ensures a Camera component is attached to the same GameObject
{
    // The VideoStreamTrack created from the camera's output
    public VideoStreamTrack VideoTrack { get; private set; }
    
    // Reference to the camera to capture from (can be assigned in the Inspector)
    [SerializeField]
    private Camera mainCamera;
    
    /// <summary>
    /// Called when the component is first initialized
    /// </summary>
    private void Start()
    {
        // If no camera is assigned in the inspector, use the Camera component on this GameObject
        if (mainCamera == null)
            mainCamera = GetComponent<Camera>();

        // Create the video track directly from the camera feed
        CreateVideoTrack();
    }
    
    /// <summary>
    /// Captures the camera feed as a WebRTC video track
    /// </summary>
    private void CreateVideoTrack()
    {
        // Use Unity.WebRTC's CaptureStreamTrack extension method to create a track
        // Resolution is set to 1280x720 (HD)
        VideoTrack = mainCamera.CaptureStreamTrack(1280, 720);

        Debug.Log("[CameraStreamer] VideoStreamTrack created using CaptureStreamTrack.");
    }

    /// <summary>
    /// Called when the object is destroyed
    /// Used to clean up the video track to avoid memory leaks
    /// </summary>
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
