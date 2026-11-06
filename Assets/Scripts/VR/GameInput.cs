using UnityEngine;

/// <summary>
/// Single entry point for gameplay input.
/// Keyboard and mouse always work. When the VR rig is active (<see cref="VRPlayerRig"/>), the VR controllers are
/// added on top: a button counts as pressed if EITHER the keyboard/mouse key OR its mapped VR control is pressed,
/// and the movement axes use the left thumbstick. Mouse-look axes read 0 in VR because the headset controls the view.
///
/// Gameplay scripts call GameInput.* where they used to call Input.* so the VR mapping lives in one place
/// (see <see cref="VRPlayerRig.Held"/> for the KeyCode -> VR control table).
/// </summary>
public static class GameInput
{
    static bool VR => VRPlayerRig.Active;

    public static float GetAxis(string axisName)
    {
        if (VR)
        {
            if (axisName == "Mouse X" || axisName == "Mouse Y") return 0f;          // the headset controls the view
            float v = VRPlayerRig.GetMoveAxis(axisName);
            if (Mathf.Abs(v) > 0.001f) return v;
        }
        return Input.GetAxis(axisName);
    }

    public static float GetAxisRaw(string axisName)
    {
        if (VR)
        {
            if (axisName == "Mouse X" || axisName == "Mouse Y") return 0f;
            float v = VRPlayerRig.GetMoveAxis(axisName);
            if (Mathf.Abs(v) > 0.001f) return Mathf.Sign(v);
        }
        return Input.GetAxisRaw(axisName);
    }

    public static bool GetKey(KeyCode key) => Input.GetKey(key) || (VR && VRPlayerRig.Held(key));
    public static bool GetKeyDown(KeyCode key) => Input.GetKeyDown(key) || (VR && VRPlayerRig.PressedThisFrame(key));
    public static bool GetKeyUp(KeyCode key) => Input.GetKeyUp(key) || (VR && VRPlayerRig.ReleasedThisFrame(key));

    public static bool GetMouseButtonDown(int button) => GetKeyDown(MouseKey(button));
    public static bool GetMouseButtonUp(int button) => GetKeyUp(MouseKey(button));
    public static bool GetMouseButton(int button) => GetKey(MouseKey(button));

    static KeyCode MouseKey(int button) => button == 0 ? KeyCode.Mouse0 : button == 1 ? KeyCode.Mouse1 : KeyCode.Mouse2;
}
