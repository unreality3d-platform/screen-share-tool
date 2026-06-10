using UnityEngine;
using UnityEngine.UI;

namespace ScreenShareTool
{
    /// <summary>
    /// Manages Share and Stop button state based on room conditions.
    /// Drop this on any GameObject, assign the room and your two buttons,
    /// and it enforces the single-presenter lock without any extra scripting.
    ///
    /// Share button: visible when connected and not currently presenting;
    ///               disabled (not interactable) while anyone is presenting.
    /// Stop button:  visible only while this user is presenting.
    /// </summary>
    [AddComponentMenu("Screen Share/Screen Share Controls")]
    public class ScreenShareControls : MonoBehaviour
    {
        [SerializeField] ScreenShareRoom room;

        [Header("Buttons (optional — assign to manage automatically)")]
        [Tooltip("Button that calls StartPresenting. Disabled while anyone is presenting.")]
        [SerializeField] Button shareButton;

        [Tooltip("Button that calls StopPresenting. Only visible while this user is presenting.")]
        [SerializeField] Button stopButton;

        void Start()
        {
            if (room == null)
            {
                Debug.LogWarning("[ScreenShareControls] No ScreenShareRoom assigned.", this);
                return;
            }

            if (shareButton != null)
                shareButton.onClick.AddListener(room.StartPresenting);

            if (stopButton != null)
                stopButton.onClick.AddListener(room.StopPresenting);

            UpdateButtonState();
        }

        void OnEnable()
        {
            if (room == null) return;
            room.OnConnected.AddListener(UpdateButtonState);
            room.OnDisconnected.AddListener(UpdateButtonState);
            room.OnConnectionFailed.AddListener(OnConnectionFailed);
            room.OnPresentingStarted.AddListener(UpdateButtonState);
            room.OnPresentingStopped.AddListener(UpdateButtonState);
            room.OnPresenterVideoReceived.AddListener(OnRemotePresenterChanged);
            room.OnPresenterVideoRemoved.AddListener(OnRemotePresenterRemoved);
        }

        void OnDisable()
        {
            if (room == null) return;
            room.OnConnected.RemoveListener(UpdateButtonState);
            room.OnDisconnected.RemoveListener(UpdateButtonState);
            room.OnConnectionFailed.RemoveListener(OnConnectionFailed);
            room.OnPresentingStarted.RemoveListener(UpdateButtonState);
            room.OnPresentingStopped.RemoveListener(UpdateButtonState);
            room.OnPresenterVideoReceived.RemoveListener(OnRemotePresenterChanged);
            room.OnPresenterVideoRemoved.RemoveListener(OnRemotePresenterRemoved);
        }

        void OnConnectionFailed(string error) => UpdateButtonState();
        void OnRemotePresenterChanged(Texture2D tex, string identity) => UpdateButtonState();
        void OnRemotePresenterRemoved(string identity) => UpdateButtonState();

        void UpdateButtonState()
        {
            bool connected = room != null && room.IsConnected;
            bool iAmPresenting = room != null && room.IsPresenting;
            bool someonePresenting = room != null && room.IsSomeonePresenting;

            if (shareButton != null)
            {
                shareButton.gameObject.SetActive(connected && !iAmPresenting);
                shareButton.interactable = !someonePresenting;
            }

            if (stopButton != null)
                stopButton.gameObject.SetActive(connected && iAmPresenting);
        }
    }
}