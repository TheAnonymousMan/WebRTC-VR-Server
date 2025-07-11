using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays the local IP address on a UI TextMeshPro component in the Unity scene.
/// </summary>
public class IPAddressDisplay : MonoBehaviour
{
    // Reference to the TextMeshPro UI element where the IP address will be displayed
    [SerializeField] private TMP_Text ipAddressText;

    /// <summary>
    /// Called when the script instance is being loaded
    /// </summary>
    private void Start()
    {
        // Set the text of the UI to the local IP address when the game starts
        ipAddressText.text = GetLocalIPAddress();
    }

    /// <summary>
    /// Returns the first available local IPv4 address (ignoring VPNs and virtual adapters)
    /// </summary>
    private string GetLocalIPAddress()
    {
        // Loop through all network interfaces on the device
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            // Skip any interface that is not currently active
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;

            string name = ni.Name.ToLower();
            string desc = ni.Description.ToLower();

            // Ignore known VPN or virtual adapters like NordLynx or WireGuard
            if (name.Contains("nordlynx") || desc.Contains("nordlynx") || name.Contains("wireguard") ||
                desc.Contains("wireguard"))
                continue;
                
            // Look through the list of unicast IP addresses on the interface
            foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
            {
                // Look through the list of unicast IP addresses on the interface
                if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    // Return the first valid IPv4 address found
                    return ip.Address.ToString();
                }
            }
        }

        // If no suitable IP address found, return the loopback address as a fallback
        return "127.0.0.1";
    }
}
