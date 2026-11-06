#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEditor.XR.Management;

/// <summary>Toggle OpenXR initialisation for Play mode. Off by default so desktop play is unaffected.</summary>
public static class VRMenu
{
    const string MenuPath = "Tools/VR/Start OpenXR On Play";

    static XRGeneralSettings Settings =>
        XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);

    [MenuItem(MenuPath)]
    static void Toggle()
    {
        var s = Settings;
        if (s == null) { Debug.LogError("No XR General Settings for Standalone."); return; }
        s.InitManagerOnStart = !s.InitManagerOnStart;
        EditorUtility.SetDirty(s);
        AssetDatabase.SaveAssets();
    }

    [MenuItem(MenuPath, true)]
    static bool ToggleValidate()
    {
        var s = Settings;
        Menu.SetChecked(MenuPath, s != null && s.InitManagerOnStart);
        return s != null;
    }
}
#endif
