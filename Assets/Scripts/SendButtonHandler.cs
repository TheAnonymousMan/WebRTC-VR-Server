using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SendButtonHandler : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    private InputAction sendMessageAction;

    private void OnEnable()
    {
        var actionMap = inputActions.FindActionMap("Messenger", true);
        sendMessageAction = actionMap.FindAction("SendMessage", true);

        // Subscribe to the input action
        sendMessageAction.performed += OnSendMessage;
        sendMessageAction.Enable();
    }
    
    private void OnDisable()
    {
        // Unsubscribe and disable the action
        if (sendMessageAction != null)
        {
            sendMessageAction.performed -= OnSendMessage;
            sendMessageAction.Disable();
        }
    }
    private void OnSendMessage(InputAction.CallbackContext context)
    {
        SendMessageButtonClicked(); // Call your existing method
    }


    public void SendMessageButtonClicked()
    {
        Debug.Log("Send message button clicked");
        WebRtcServerManager.Singleton.SendMessageBuffered("Test Message Sent from Server!");
    }
}
