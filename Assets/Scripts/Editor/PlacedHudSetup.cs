using UnityEditor;

/// <summary>
/// Kept for the old menu item. The placed HUD is now part of the full
/// UI Toolkit setup, together with the edit shell and onboarding documents.
/// </summary>
public static class PlacedHudSetup
{
    [MenuItem("Tools/AR Kitchen/Setup Placed HUD")]
    public static void SetupPlacedHud()
    {
        UISceneSetup.SetupUI();
    }
}
