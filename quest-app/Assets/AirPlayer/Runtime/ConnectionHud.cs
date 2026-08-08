using System.Text;
using AirPlayer.Core.Connection;
using UnityEngine;

namespace AirPlayer.Runtime
{
    /// <summary>
    /// World-space status display: connection state, companion name and
    /// average round-trip latency over the last 10 pings (Loop 0 CA #3).
    /// Uses a legacy TextMesh so no extra package is required.
    /// </summary>
    [RequireComponent(typeof(TextMesh))]
    public sealed class ConnectionHud : MonoBehaviour
    {
        [SerializeField]
        private ConnectionManager connectionManager;

        [SerializeField]
        [Tooltip("HUD refresh interval; no need to rebuild the string every frame.")]
        private float refreshIntervalSeconds = 0.25f;

        private readonly StringBuilder _builder = new StringBuilder(128);
        private TextMesh _textMesh;
        private float _nextRefreshTime;

        private void Awake()
        {
            _textMesh = GetComponent<TextMesh>();
        }

        private void Update()
        {
            if (connectionManager == null || Time.unscaledTime < _nextRefreshTime)
            {
                return;
            }
            _nextRefreshTime = Time.unscaledTime + refreshIntervalSeconds;

            _builder.Length = 0;
            switch (connectionManager.State)
            {
                case ConnectionState.Disconnected:
                    if (connectionManager.IsIncompatible)
                    {
                        _builder.Append("Version incompatible — mettre à jour l'app");
                        _textMesh.color = Color.red;
                    }
                    else
                    {
                        _builder.Append("Recherche du compagnon…");
                        _textMesh.color = Color.yellow;
                    }
                    break;

                case ConnectionState.Connecting:
                    _builder.Append("Déconnecté — connexion à ");
                    _builder.Append(connectionManager.CompanionLabel);
                    _builder.Append('…');
                    _textMesh.color = new Color(1f, 0.6f, 0f);
                    break;

                case ConnectionState.Connected:
                    _builder.Append("Connecté : ");
                    _builder.Append(connectionManager.CompanionLabel);
                    _builder.Append('\n');
                    if (connectionManager.RttSampleCount > 0)
                    {
                        _builder.Append("Latence aller-retour : ");
                        _builder.Append(connectionManager.AverageRttMs.ToString("0.0"));
                        _builder.Append(" ms (moy. ");
                        _builder.Append(connectionManager.RttSampleCount);
                        _builder.Append(" pings)");
                    }
                    else
                    {
                        _builder.Append("Mesure de latence…");
                    }
                    _textMesh.color = Color.green;
                    break;
            }

            _textMesh.text = _builder.ToString();
        }
    }
}
