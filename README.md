# Unity WebRTC Server VR App (Therapy Experience)

This Unity project hosts the WebRTC server logic inside a VR environment. It is designed for Google Cardboard-based smartphones and provides immersive therapy experiences via video streaming.

## Features
- Google Cardboard VR support
- Embedded signaling + media server
- Sends 3D or 360° views over WebRTC
- Works on Android smartphones

## Running
1. Open the project in Unity.
2. Build the app for Android.
3. Install the APK on a smartphone and launch it.
4. The app starts a server and waits for WebRTC client connections.

## Dependencies
- Unity WebRTC
- Google Cardboard XR Plugin
- WebSocketSharp
- Newtonsoft.Json

## Notes
- Test with a VR-compatible Android smartphone.
- Connect a client to receive the video stream.
