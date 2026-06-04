using UnityEngine;
using UnityEngine.UI;

public class KitchenUI : MonoBehaviour
{
    [SerializeField] VoxelStateManager stateManager;

    [Header("Panels")]
    [SerializeField] GameObject editPanel;
    [SerializeField] GameObject subButtonPanel;

    [Header("Buttons")]
    [SerializeField] Button editButton;
    [SerializeField] Button doneButton;
    [SerializeField] Button scaleButton;
    [SerializeField] Button placementButton;
    [SerializeField] Button rotationButton;
    [SerializeField] Button fillButton;
    [SerializeField] Button colorButton;

    void Awake()
    {
        editPanel.SetActive(false);
        subButtonPanel.SetActive(false);
        doneButton.gameObject.SetActive(false);

        if (editButton != null) editButton.onClick.AddListener(stateManager.EnterEdit);
        doneButton.onClick.AddListener(stateManager.ExitEdit);
        scaleButton.onClick.AddListener(() => stateManager.SetMode(VoxelEditMode.Scale));
        placementButton.onClick.AddListener(() => stateManager.SetMode(VoxelEditMode.Placement));
        rotationButton.onClick.AddListener(() => stateManager.SetMode(VoxelEditMode.Rotation));
        fillButton.onClick.AddListener(() => stateManager.SetMode(VoxelEditMode.FillKitchen));
        colorButton.onClick.AddListener(() => stateManager.SetMode(VoxelEditMode.Color));

        stateManager.OnVoxelPlaced  += OnVoxelPlaced;
        stateManager.OnEditingChanged += OnEditingChanged;
    }

    void OnDestroy()
    {
        stateManager.OnVoxelPlaced   -= OnVoxelPlaced;
        stateManager.OnEditingChanged -= OnEditingChanged;
    }

    void OnVoxelPlaced()
    {
        editPanel.SetActive(true);
    }

    void OnEditingChanged(bool editing)
    {
        if (editButton != null) editButton.gameObject.SetActive(!editing);
        doneButton.gameObject.SetActive(editing);
        subButtonPanel.SetActive(editing);
    }
}
