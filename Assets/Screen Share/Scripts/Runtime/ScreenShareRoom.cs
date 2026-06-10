using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
#if UNITY_WEBGL && !UNITY_EDITOR
using LiveKit;
#endif

namespace ScreenShareTool
{
    [Serializable] public class TexturePresenterEvent : UnityEvent<Texture2D, string> { }
    [Serializable] public class PresenterEvent : UnityEvent<string> { }
    [Serializable] public class ConnectionFailedEvent : UnityEvent<string> { }

    /// <summary>
    /// Drop-in screen-share bridge. Put this on one GameObject in the scene, assign a
    /// ScreenShareSettings asset and a room name, and it will connect when the player loads.
    /// A "Share my screen" UI button calls StartPresenting(); a "Stop" button calls StopPresenting().
    /// The screen surface listens to OnPresenterVideoReceived / OnPresenterVideoRemoved.
    ///
    /// Depends on nothing from any host project. The underlying LiveKit SDK is a
    /// browser bridge, so this only does anything in a WebGL build; in the editor it
    /// stays inert so the component still compiles and inspects cleanly.
    /// </summary>
    [AddComponentMenu("Screen Share/Screen Share Room")]
    public class ScreenShareRoom : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("The settings asset holding your LiveKit server URL and token endpoint URL.")]
        [SerializeField] private ScreenShareSettings settings;

        [Tooltip("The room everyone in this space joins. One fixed room per space is the usual setup.")]
        [SerializeField] private string roomName = "main-room";

        [Tooltip("Connect automatically when the scene loads. Leave on for the watch-on-arrival flow.")]
        [SerializeField] private bool connectOnStart = true;

        [Header("Connection events")]
        public UnityEvent OnConnected;
        public UnityEvent OnDisconnected;
        public ConnectionFailedEvent OnConnectionFailed;

        [Header("Presenting events (this user)")]
        public UnityEvent OnPresentingStarted;
        public UnityEvent OnPresentingStopped;

        [Header("Presenter video events (the screen surface listens to these)")]
        public TexturePresenterEvent OnPresenterVideoReceived;
        public PresenterEvent OnPresenterVideoRemoved;

        /// <summary>True once this user is connected to the room.</summary>
        public bool IsConnected { get; private set; }

        /// <summary>True while this user is the one sharing their screen.</summary>
        public bool IsPresenting { get; private set; }

        /// <summary>
        /// True if anyone is presenting right now: either this user, or a remote presenter.
        /// A Share button should stay disabled while this is true.
        /// </summary>
        public bool IsSomeonePresenting => IsPresenting || _remotePresenterActive;

        /// <summary>
        /// The most recently received remote presenter texture. Non-null while a remote share
        /// is active. Used by ScreenShareSurface to recover state when enabling mid-session.
        /// </summary>
        public Texture2D CurrentPresenterTexture { get; private set; }

        /// <summary>The identity of the participant whose texture is in CurrentPresenterTexture.</summary>
        public string CurrentPresenterIdentity { get; private set; }

        /// <summary>
        /// This user's name inside the LiveKit room. An anonymous name is generated if none is set.
        /// A host scene may set its own value before connecting; ignored once connected.
        /// </summary>
        public string ParticipantIdentity
        {
            get
            {
                if (string.IsNullOrEmpty(_identity))
                    _identity = "guest-" + UnityEngine.Random.Range(1000, 9999);
                return _identity;
            }
            set { if (!IsConnected) _identity = value; }
        }

        private string _identity;
        private bool _isConnecting;
        private bool _isStartingPresenting;
        private bool _remotePresenterActive;

#if UNITY_WEBGL && !UNITY_EDITOR
        private string _remotePresenterIdentity;
        private Room _room;
#endif

        private void Start()
        {
            if (connectOnStart)
                Connect();
        }

        public void Connect()
        {
            if (IsConnected || _isConnecting)
                return;

            if (settings == null ||
                string.IsNullOrEmpty(settings.ServerUrl) ||
                string.IsNullOrEmpty(settings.TokenEndpointUrl))
            {
                Debug.LogError("[ScreenShareRoom] Missing settings. Assign a ScreenShareSettings asset with both a server URL and a token endpoint URL.");
                OnConnectionFailed.Invoke("Missing or incomplete settings.");
                return;
            }

            _isConnecting = true;
            StartCoroutine(ConnectRoutine());
        }

        public void Disconnect()
        {
            if (!IsConnected && !_isConnecting)
                return;

            StartCoroutine(DisconnectRoutine());
        }

        public void StartPresenting()
        {
            if (!IsConnected || IsPresenting || _isStartingPresenting)
                return;

            StartCoroutine(StartPresentingRoutine());
        }

        public void StopPresenting()
        {
            if (!IsConnected || !IsPresenting)
                return;

            StartCoroutine(StopPresentingRoutine());
        }

        private IEnumerator ConnectRoutine()
        {
            string token = null;
            string tokenError = null;
            yield return FetchTokenRoutine(t => token = t, e => tokenError = e);

            if (tokenError != null)
            {
                _isConnecting = false;
                Debug.LogError("[ScreenShareRoom] Token request failed: " + tokenError);
                OnConnectionFailed.Invoke(tokenError);
                yield break;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            _room = new Room();

            // Remote presenter: any incoming video track is the screen share.
            // When camera video is ever added, filter by publication.Source instead.
            _room.TrackSubscribed += (track, publication, participant) =>
            {
                if (track.Kind != TrackKind.Video)
                    return;

                var video = track.Attach() as HTMLVideoElement;
                if (video == null)
                    return;

                _remotePresenterActive = true;
                _remotePresenterIdentity = participant.Identity;

                video.VideoReceived += tex =>
                {
                    CurrentPresenterTexture = tex;
                    CurrentPresenterIdentity = participant.Identity;
                    OnPresenterVideoReceived.Invoke(tex, participant.Identity);
                };
            };

            _room.TrackUnsubscribed += (track, publication, participant) =>
            {
                if (track.Kind != TrackKind.Video)
                    return;

                _remotePresenterActive = false;
                string who = _remotePresenterIdentity;
                _remotePresenterIdentity = null;
                CurrentPresenterTexture = null;
                CurrentPresenterIdentity = null;
                OnPresenterVideoRemoved.Invoke(who);
            };

            // Self-view: when this user's local screen share track is published,
            // attach it and surface frames via the same OnPresenterVideoReceived event.
            // Note: LocalVideoTrack.Attach() follows the same pattern as remote tracks.
            // If Attach() doesn't compile on LocalVideoTrack, flag it — the local track
            // API may need a different path on this SDK version.
            _room.LocalTrackPublished += (publication, participant) =>
            {
                if (publication.VideoTrack == null)
                    return;

                var video = publication.VideoTrack.Attach() as HTMLVideoElement;
                if (video == null)
                    return;

                video.VideoReceived += tex =>
                {
                    OnPresenterVideoReceived.Invoke(tex, ParticipantIdentity);
                };
            };

            // Catch the browser's own "Stop sharing" button ending the share outside
            // our control, and catch in-app stop after SetScreenShareEnabled(false) fires.
            // IsPresenting is cleared before the yield in StopPresentingRoutine, so the
            // in-app stop path finds IsPresenting false here and exits early — preventing
            // a double-fire of OnPresentingStopped.
            _room.LocalTrackUnpublished += (publication, participant) =>
            {
                if (publication.Kind != TrackKind.Video)
                    return;
                if (!IsPresenting)
                    return;

                IsPresenting = false;
                OnPresenterVideoRemoved.Invoke(ParticipantIdentity);
                OnPresentingStopped.Invoke();
            };

            var connect = _room.Connect(settings.ServerUrl, token);
            yield return connect;

            if (connect.IsError)
            {
                _isConnecting = false;
                _room = null;
                Debug.LogError("[ScreenShareRoom] LiveKit connection failed.");
                OnConnectionFailed.Invoke("LiveKit connection failed.");
                yield break;
            }

            IsConnected = true;
            _isConnecting = false;
            OnConnected.Invoke();
#else
            _isConnecting = false;
            Debug.LogWarning("[ScreenShareRoom] Screen sharing only runs in a WebGL build; skipping connection in the editor.");
            OnConnectionFailed.Invoke("Screen sharing only runs in a WebGL build.");
            yield break;
#endif
        }

        private IEnumerator FetchTokenRoutine(Action<string> onToken, Action<string> onError)
        {
            var payload = new TokenRequest
            {
                room = roomName,
                identity = ParticipantIdentity,
                canPublish = true
            };

            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));

            using (var request = new UnityWebRequest(settings.TokenEndpointUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    onError(request.error + " (" + request.downloadHandler.text + ")");
                    yield break;
                }

                TokenResponse response;
                try
                {
                    response = JsonUtility.FromJson<TokenResponse>(request.downloadHandler.text);
                }
                catch (Exception e)
                {
                    onError("Could not read token response: " + e.Message);
                    yield break;
                }

                if (response == null || string.IsNullOrEmpty(response.token))
                {
                    onError("Token response did not contain a token.");
                    yield break;
                }

                onToken(response.token);
            }
        }

        private IEnumerator StartPresentingRoutine()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            _isStartingPresenting = true;
            yield return _room.LocalParticipant.SetScreenShareEnabled(true);
            _isStartingPresenting = false;

            // If the user cancelled the browser picker, IsScreenShareEnabled stays false.
            if (!_room.LocalParticipant.IsScreenShareEnabled)
                yield break;

            IsPresenting = true;
            OnPresentingStarted.Invoke();
            // Self-view texture arrives via LocalTrackPublished → VideoReceived above.
#else
            Debug.LogWarning("[ScreenShareRoom] Presenting only works in a WebGL build.");
            yield break;
#endif
        }

        private IEnumerator StopPresentingRoutine()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // Clear IsPresenting before the yield so LocalTrackUnpublished (which fires
            // when SetScreenShareEnabled(false) takes effect) finds it false and exits
            // early, letting this routine fire OnPresenterVideoRemoved/OnPresentingStopped
            // exactly once.
            IsPresenting = false;
            yield return _room.LocalParticipant.SetScreenShareEnabled(false);
            OnPresenterVideoRemoved.Invoke(ParticipantIdentity);
            OnPresentingStopped.Invoke();
#else
            yield break;
#endif
        }

        private IEnumerator DisconnectRoutine()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (_room != null)
            {
                if (IsPresenting)
                {
                    IsPresenting = false;
                    yield return _room.LocalParticipant.SetScreenShareEnabled(false);
                    OnPresenterVideoRemoved.Invoke(ParticipantIdentity);
                    OnPresentingStopped.Invoke();
                }

                _room.Disconnect(true);
                _room = null;
            }
#endif
            bool wasConnected = IsConnected;
            IsConnected = false;
            _isConnecting = false;
            _isStartingPresenting = false;
            _remotePresenterActive = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            _remotePresenterIdentity = null;
#endif
            CurrentPresenterTexture = null;
            CurrentPresenterIdentity = null;

            if (wasConnected)
                OnDisconnected.Invoke();

            yield break;
        }

        private void OnDestroy()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (_room != null)
            {
                _room.Disconnect(true);
                _room = null;
            }
#endif
        }

        [Serializable]
        private class TokenRequest
        {
            public string room;
            public string identity;
            public bool canPublish;
        }

        [Serializable]
        private class TokenResponse
        {
            public string token;
        }
    }
}