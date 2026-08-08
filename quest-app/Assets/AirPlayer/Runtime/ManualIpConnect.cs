using System.Net;
using UnityEngine;

namespace AirPlayer.Runtime
{
    /// <summary>
    /// Manual IP fallback for networks that block mDNS. Wire the public
    /// methods to world-space keypad buttons (poke interaction), or type the
    /// IP in the inspector field for quick testing.
    /// </summary>
    public sealed class ManualIpConnect : MonoBehaviour
    {
        [SerializeField]
        private ConnectionManager connectionManager;

        [SerializeField]
        [Tooltip("Current IP address text, editable in the inspector for testing.")]
        private string ipText = "192.168.1.";

        [SerializeField]
        private TextMesh display;

        public string IpText
        {
            get { return ipText; }
        }

        private void Start()
        {
            RefreshDisplay();
        }

        /// <summary>Appends one character ('0'..'9' or '.'). Hook to keypad buttons.</summary>
        public void Append(string character)
        {
            if (string.IsNullOrEmpty(character) || ipText.Length >= 15)
            {
                return;
            }
            char c = character[0];
            if ((c < '0' || c > '9') && c != '.')
            {
                return;
            }
            ipText += c;
            RefreshDisplay();
        }

        public void Backspace()
        {
            if (ipText.Length > 0)
            {
                ipText = ipText.Substring(0, ipText.Length - 1);
                RefreshDisplay();
            }
        }

        public void Clear()
        {
            ipText = "";
            RefreshDisplay();
        }

        /// <summary>Validates the address and hands it to the connection manager.</summary>
        public void Connect()
        {
            IPAddress address;
            if (!IPAddress.TryParse(ipText, out address))
            {
                Debug.LogWarning($"[AirPlayer] '{ipText}' is not a valid IP address.");
                return;
            }
            if (connectionManager == null)
            {
                Debug.LogError("[AirPlayer] ManualIpConnect has no ConnectionManager assigned.");
                return;
            }
            connectionManager.ConnectTo(address, $"IP manuelle {ipText}");
        }

        private void RefreshDisplay()
        {
            if (display != null)
            {
                display.text = ipText.Length > 0 ? ipText : "_";
            }
        }
    }
}
