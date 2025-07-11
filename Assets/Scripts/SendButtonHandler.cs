using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles the input action for sending a message via a button press or input event.
/// Integrates with Unity's new Input System.
/// </summary>
public class SendButtonHandler : MonoBehaviour
{
    // Reference to the InputActionAsset containing the "Messenger" action map
    [SerializeField] private InputActionAsset inputActions;
    
    // The specific input action for sending a message
    private InputAction sendMessageAction;

    /// <summary>
    /// Called when the object becomes enabled and active
    /// Subscribes to the input action events.
    /// </summary>
    private void OnEnable()
    {
        // Get the "Messenger" action map (throws if not found)
        var actionMap = inputActions.FindActionMap("Messenger", true);

        // Find the "SendMessage" action within that map (throws if not found)
        sendMessageAction = actionMap.FindAction("SendMessage", true);

        // Subscribe to the action's performed event
        sendMessageAction.performed += OnSendMessage;

        // Enable the action so it starts listening for input
        sendMessageAction.Enable();
    }
    
    /// <summary>
    /// Called when the object is disabled
    /// Unsubscribes and disables the input action to avoid memory leaks or duplicate events.
    /// </summary>
    private void OnDisable()
    {
        // Unsubscribe and disable the action
        if (sendMessageAction != null)
        {
            // Unsubscribe from the action's performed event
            sendMessageAction.performed -= OnSendMessage;

            // Disable the input action            
            sendMessageAction.Disable();
        }
    }

    /// <summary>
    /// Callback method invoked when the input action is performed (e.g., button press)
    /// </summary>
    private void OnSendMessage(InputAction.CallbackContext context)
    {
        SendMessageButtonClicked(); // Call your existing method
    }

    /// <summary>
    /// Method that gets triggered when the send message input is detected or button is clicked
    /// </summary>
    public void SendMessageButtonClicked()
    {
        Debug.Log("Send message button clicked");

        // Sends a test message via WebRtcServerManager
        WebRtcServerManager.Singleton.SendMessageBuffered("Test Message Sent from Server!");
    }
}
