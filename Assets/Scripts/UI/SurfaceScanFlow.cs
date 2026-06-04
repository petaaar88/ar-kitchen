using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace ArKitchen.UI
{
    /// <summary>
    /// Orchestrates the AR onboarding flow:
    ///   1. Scanning panel until a plane is detected.
    ///   2. Surfaces-found card so the user can confirm placement.
    ///   3. Place-kitchen hint while waiting for a tap on the AR plane.
    ///   4. Hint dismissed once the voxel is placed.
    /// </summary>
    public class SurfaceScanFlow : MonoBehaviour
    {
        [SerializeField] ARPlaneManager planeManager;
        [SerializeField] ScanningPanel scanningPanel;
        [SerializeField] SurfacesFoundPanel surfacesFoundPanel;
        [SerializeField] PlaceKitchenPanel placeKitchenPanel;
        [SerializeField] VoxelPlacer voxelPlacer;

        [Tooltip("Keep the scanning screen up at least this long even if a plane is found sooner.")]
        [SerializeField] float minScanSeconds = 1.5f;

        [Tooltip("Cross-fade duration between panels.")]
        [SerializeField] float crossFadeSeconds = 0.3f;

        float _startTime;
        bool _surfaceFound;
        bool _switched;

        void Awake()
        {
            if (planeManager == null)        planeManager        = FindAnyObjectByType<ARPlaneManager>();
            if (scanningPanel == null)       scanningPanel       = FindAnyObjectByType<ScanningPanel>();
            if (surfacesFoundPanel == null)  surfacesFoundPanel  = FindAnyObjectByType<SurfacesFoundPanel>();
            if (placeKitchenPanel == null)   placeKitchenPanel   = FindAnyObjectByType<PlaceKitchenPanel>();
            if (voxelPlacer == null)         voxelPlacer         = FindAnyObjectByType<VoxelPlacer>();
        }

        void Start()
        {
            _startTime = Time.time;
            _surfaceFound = false;
            _switched = false;

            if (voxelPlacer != null)        voxelPlacer.PlacementEnabled = false;
            if (scanningPanel != null)      scanningPanel.Show();
            if (surfacesFoundPanel != null) surfacesFoundPanel.Hide();
            if (placeKitchenPanel != null)  placeKitchenPanel.Hide();

            if (planeManager != null)
                planeManager.trackablesChanged.AddListener(OnTrackablesChanged);

            if (surfacesFoundPanel != null)
                surfacesFoundPanel.PlacePressed += OnPlacePressed;

            if (voxelPlacer != null)
                voxelPlacer.OnPlaced += OnVoxelPlaced;
        }

        void OnDisable()
        {
            if (planeManager != null)
                planeManager.trackablesChanged.RemoveListener(OnTrackablesChanged);

            if (surfacesFoundPanel != null)
                surfacesFoundPanel.PlacePressed -= OnPlacePressed;

            if (voxelPlacer != null)
                voxelPlacer.OnPlaced -= OnVoxelPlaced;
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
                if (scanningPanel != null)      scanningPanel.FadeOut(crossFadeSeconds);
                if (surfacesFoundPanel != null) surfacesFoundPanel.FadeIn(crossFadeSeconds);
            }
        }

        void OnPlacePressed()
        {
            if (surfacesFoundPanel != null) surfacesFoundPanel.FadeOut(crossFadeSeconds);
            if (placeKitchenPanel != null)  placeKitchenPanel.FadeIn(crossFadeSeconds);
            if (voxelPlacer != null)        voxelPlacer.PlacementEnabled = true;
        }

        void OnVoxelPlaced()
        {
            if (placeKitchenPanel != null) placeKitchenPanel.FadeOut(crossFadeSeconds);
        }
    }
}
