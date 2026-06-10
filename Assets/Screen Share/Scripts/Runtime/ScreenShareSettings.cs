using UnityEngine;

namespace ScreenShareTool
{
    /// <summary>
    /// Holds the two server addresses the operator fills in once, before publishing:
    /// the LiveKit server URL and the token endpoint URL. Create one via
    /// Assets > Create > Screen Share > Settings and assign it to a ScreenShareRoom.
    /// </summary>
    [CreateAssetMenu(fileName = "ScreenShareSettings", menuName = "Screen Share/Settings")]
    public class ScreenShareSettings : ScriptableObject
    {
        [Tooltip("Your LiveKit server URL, e.g. wss://yourproject.livekit.cloud")]
        [SerializeField] private string serverUrl;

        [Tooltip("Your token endpoint URL, e.g. https://us-central1-yourproject.cloudfunctions.net/createLiveKitToken")]
        [SerializeField] private string tokenEndpointUrl;

        public string ServerUrl => serverUrl;
        public string TokenEndpointUrl => tokenEndpointUrl;
    }
}
