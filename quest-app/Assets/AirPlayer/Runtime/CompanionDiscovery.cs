using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using AirPlayer.Core.Discovery;
using UnityEngine;

namespace AirPlayer.Runtime
{
    /// <summary>
    /// Browses the network for AirPlayer companions over mDNS on a background
    /// thread and surfaces results on the main thread. On Android a multicast
    /// lock is acquired so the Wi-Fi driver delivers multicast packets
    /// (queries are sent with the QU bit, so responses come back unicast even
    /// when the lock is not granted).
    /// </summary>
    public sealed class CompanionDiscovery : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Seconds between mDNS queries while searching.")]
        private float queryIntervalSeconds = 2.0f;

        [SerializeField]
        [Tooltip("Connect to the first companion found (Loop 0 behaviour).")]
        private bool autoConnectToFirst = true;

        [SerializeField]
        private ConnectionManager connectionManager;

        private readonly ConcurrentQueue<MdnsServiceInfo> _foundQueue = new ConcurrentQueue<MdnsServiceInfo>();
        private readonly List<MdnsServiceInfo> _companions = new List<MdnsServiceInfo>();
        private Thread _thread;
        private volatile bool _running;
        private bool _autoConnected;

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _multicastLock;
#endif

        /// <summary>Companions discovered so far (main-thread only).</summary>
        public IReadOnlyList<MdnsServiceInfo> Companions
        {
            get { return _companions; }
        }

        public event Action<MdnsServiceInfo> CompanionFound;

        private void OnEnable()
        {
            AcquireMulticastLock();
            _running = true;
            _thread = new Thread(DiscoveryLoop) { IsBackground = true, Name = "AirPlayer.Discovery" };
            _thread.Start();
        }

        private void OnDisable()
        {
            _running = false;
            if (_thread != null)
            {
                _thread.Join(1500);
                _thread = null;
            }
            ReleaseMulticastLock();
        }

        private void Update()
        {
            MdnsServiceInfo info;
            while (_foundQueue.TryDequeue(out info))
            {
                if (AlreadyKnown(info))
                {
                    continue;
                }
                _companions.Add(info);
                Debug.Log($"[AirPlayer] Companion discovered: {info}");

                Action<MdnsServiceInfo> handler = CompanionFound;
                if (handler != null)
                {
                    handler(info);
                }

                if (autoConnectToFirst && !_autoConnected && connectionManager != null)
                {
                    _autoConnected = true;
                    connectionManager.ConnectTo(info.Address, info.InstanceName);
                }
            }
        }

        private bool AlreadyKnown(MdnsServiceInfo info)
        {
            for (int i = 0; i < _companions.Count; i++)
            {
                if (_companions[i].Address.Equals(info.Address) && _companions[i].Port == info.Port)
                {
                    return true;
                }
            }
            return false;
        }

        private void DiscoveryLoop()
        {
            MdnsClient client;
            try
            {
                client = new MdnsClient();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AirPlayer] mDNS unavailable ({ex.Message}); use manual IP entry.");
                return;
            }

            using (client)
            {
                double lastQueryTime = double.NegativeInfinity;
                System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();

                while (_running)
                {
                    double now = clock.Elapsed.TotalSeconds;
                    if (now - lastQueryTime >= queryIntervalSeconds)
                    {
                        lastQueryTime = now;
                        try
                        {
                            client.SendQuery();
                        }
                        catch (Exception)
                        {
                            // Transient send failure (Wi-Fi roaming, etc.); retry next interval.
                        }
                    }

                    MdnsServiceInfo info;
                    if (client.TryReceive(250, out info))
                    {
                        _foundQueue.Enqueue(info);
                    }
                }
            }
        }

        private void AcquireMulticastLock()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject wifiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi"))
                {
                    _multicastLock = wifiManager.Call<AndroidJavaObject>("createMulticastLock", "airplayer-mdns");
                    _multicastLock.Call("setReferenceCounted", false);
                    _multicastLock.Call("acquire");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AirPlayer] Multicast lock unavailable: {ex.Message}. Unicast responses should still work.");
            }
#endif
        }

        private void ReleaseMulticastLock()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_multicastLock != null)
            {
                try
                {
                    _multicastLock.Call("release");
                }
                catch (Exception)
                {
                    // Already released or Wi-Fi gone; nothing to do.
                }
                _multicastLock.Dispose();
                _multicastLock = null;
            }
#endif
        }
    }
}
