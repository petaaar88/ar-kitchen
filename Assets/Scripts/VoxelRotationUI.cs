using UnityEngine;
using UnityEngine.UI;

public class VoxelRotationUI : MonoBehaviour
{
    const float RotationStep = 15f;

    [SerializeField] VoxelStateManager stateManager;
    [SerializeField] GameObject rotationPanel;
    [SerializeField] Button rotateLeftButton;
    [SerializeField] Button rotateRightButton;

    void Awake()
    {
        rotationPanel.SetActive(false);

        rotateLeftButton.onClick.AddListener(() =>
            stateManager.Controller?.Rotate(-RotationStep));
        rotateRightButton.onClick.AddListener(() =>
            stateManager.Controller?.Rotate(RotationStep));

        stateManager.OnModeChanged += OnModeChanged;
    }

    void OnDestroy()
    {
        stateManager.OnModeChanged -= OnModeChanged;
    }

    void OnModeChanged(VoxelEditMode mode)
    {
        rotationPanel.SetActive(mode == VoxelEditMode.Rotation);
    }
}
