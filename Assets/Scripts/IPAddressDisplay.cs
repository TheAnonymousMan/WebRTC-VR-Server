using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using TMPro;
using UnityEngine;

public class IPAddressDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text ipAddressText;

    private void Start()
    {
        ipAddressText.text = GetLocalIPAddress();
    }

    private string GetLocalIPAddress()
    {
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;

            string name = ni.Name.ToLower();
            string desc = ni.Description.ToLower();

            // Ignore NordLynx / WireGuard / VPNs
            if (name.Contains("nordlynx") || desc.Contains("nordlynx") || name.Contains("wireguard") ||
                desc.Contains("wireguard"))
                continue;

            foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
            {
                if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.Address.ToString(); // Return first valid IPv4
                }
            }
        }

        return "127.0.0.1"; // Fallback if no suitable address found
    }
}