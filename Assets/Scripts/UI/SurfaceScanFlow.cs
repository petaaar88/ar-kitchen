using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace ArKitchen.UI
{
    /// <summary>
    /// Orchestrates the AR onboarding flow: show the Scanning panel first,
    /// then swap to the Surfaces-found card once AR Foundation detects a plane
    /// (and a short minimum scan time has elapsed so the animation is seen).
    /// References are auto-found if left unassigned.
    /// </summary>
    public class SurfaceScanFlow : MonoBehaviour
    {
        [SerializeField] ARPlaneManager planeManager;
        [SerializeField] ScanningPanel scanningPanel;
        [SerializeField] SurfacesFoundPanel surfacesFoundPanel;

        [Tooltip("Keep the scanning screen up at least this long even if a plane is found sooner.")]
        [SerializeField] float minScanSeconds = 1.5f;

        [Tooltip("Cross-fade duration when swapping Scanning -> Surfaces found.")]
        [SerializeField] float crossFadeSeconds = 0.3f;

        float _startTime;
        bool _surfaceFound;
        bool _switched;

        void Awake()
        {
            if (planeManager == null) planeManager = FindAnyObjectByType<ARPlaneManager>();
            if (scanningPanel == null) scanningPanel = FindAnyObjectByType<ScanningPanel>();
            if (surfacesFoundPanel == null) surfacesFoundPanel = FindAnyObjectByType<SurfacesFoundPanel>();
        }

        // Runs after every Awake/OnEnable (incl. UIDocument building its tree),
        // so the panels' root visual elements are ready to be shown/hidden.
        void Start()
        {
            _startTime = Time.time;
            _surfaceFound = false;
            _switched = false;

            if (scanningPanel != null) scanningPanel.Show();
            if (surfacesFoundPanel != null) surfacesFoundPanel.Hide();

            if (planeManager != null)
                planeManager.trackablesChanged.AddListener(OnTrackablesChanged);
        }

        void OnDisable()
        {
            if (planeManager != null)
                planeManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
        }

        void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
        {
            if (args.added != null && args.added.Count > 0)
                _surfaceFound = true;
        }

        void Update()
        {
            if (_switched) return;
            if (_surfaceFound && Time.time - _startTime >= minScanSeconds)
            {
                _switched = true;
                if (scanningPanel != null) scanningPanel.FadeOut(crossFadeSeconds);
                if (surfacesFoundPanel != null) surfacesFoundPanel.FadeIn(crossFadeSeconds);
            }
        }
    }
}
