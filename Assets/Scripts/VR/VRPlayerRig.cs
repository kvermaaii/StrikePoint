using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

/// <summary>
/// Turns the existing first-person player into a VR player at runtime. Nothing in the scenes is edited:
/// when a headset (or the XR Interaction Simulator) is present, this builds an XR rig around the existing Player,
/// and when none is present it removes itself and the game plays exactly as before.
///
/// What it does when VR is active
///   - Moves the existing Camera under an XR Origin and drives it from the headset (TrackedPoseDriver).
///   - Disables mouse look / camera follow / camera shake / the cursor-driven GunAim, which fight a tracked head.
///   - Mounts the blaster (Camera/GunAim) on the RIGHT controller and the grapple gun (Camera/GunPosition) on the LEFT,
///     and makes both aim from the hand instead of the head.
///   - Keeps the head over the player capsule (no room-scale walking; the capsule does the moving) and faces the
///     movement direction where you look.
///   - Adds snap turning, and converts the screen-space HUD/menus to world-space panels that follow the head.
///   - Provides the controller button mapping used by <see cref="GameInput"/>.
///
/// Controls (Quest/Touch naming; other controllers map the same positions)
///   Left stick  move                     Right stick     snap turn
///   Right A     jump                     Right B         reload
///   Right trig  fire                     Left trigger    grapple (hold)
///   Left stick click  sprint             Right stick click  slide
///   Left grip   door button / interact   Left X          switch fire mode
///   Left Y      leaderboard              Left menu       pause
/// </summary>
public class VRPlayerRig : MonoBehaviour
{
    public static bool Active { get; private set; }
    static VRPlayerRig instance;

    [Header("Comfort")]
    [Tooltip("Degrees per mouse unit when testing without a headset.")]
    public float simMouseSensitivity = 2f;
    float simPitch;
    [Tooltip("Eye height relative to the Player pivot (feet are at -1, so 0.6 = 1.6 m above the floor).")]
    public float eyeHeightLocal = 0.6f;
    public float snapTurnAngle = 30f;
    [Range(0.3f, 0.95f)] public float snapTurnThreshold = 0.7f;
    public float stickDeadzone = 0.15f;

    [Header("Weapons (offsets from the controller)")]
    public Vector3 blasterOffset = new Vector3(0f, -0.02f, -0.30f);
    public Vector3 grappleOffset = new Vector3(0f, -0.02f, -0.05f);

    [Header("HUD / menus")]
    public float hudDistance = 1.5f;
    public float hudScale = 0.0008f;          // 1920 px * 0.0008 = 1.54 m wide
    public float hudFollowSpeed = 3f;

    // ---- bootstrap ---------------------------------------------------------------------------------------------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += (s, m) => Boot();
        Boot();
    }

    static void Boot()
    {
        if (FindAnyObjectByType<VRPlayerRig>() != null) return;
        var pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null) { pm.gameObject.AddComponent<VRPlayerRig>(); return; }   // waits briefly for XR, builds the rig, or removes itself
        // a scene without a player (the main menu): give it a camera rig and a pointer so the menu is usable
        if (Camera.main != null && FindAnyObjectByType<Canvas>() != null)
            new GameObject("VR Menu Rig").AddComponent<VRPlayerRig>().menuMode = true;
    }

    static bool XRReady() => XRSettings.isDeviceActive || InputSystem.GetDevice<XRHMD>() != null;

    // ---- state -------------------------------------------------------------------------------------------------
    Camera cam; Transform origin, offset, leftHand, rightHand, orientation;
    readonly List<InputAction> actions = new List<InputAction>();
    InputAction move, turn, fire, grappleAction, jump, reload, interact, sprint, slide, switchMode, leaderboard, pause;
    readonly List<Canvas> hudCanvases = new List<Canvas>();
    bool turnArmed = true;
    [HideInInspector] public bool menuMode;      // no Player in this scene: camera + controllers + UI pointer only

    IEnumerator Start()
    {
        float waited = 0f;
        while (!XRReady() && waited < 4f) { waited += Time.unscaledDeltaTime; yield return null; }
        if (!XRReady()) { Destroy(menuMode ? gameObject : (Object)this); yield break; }     // no headset: stay a normal desktop game
        yield return null;                                  // give the XR devices one frame to register their controls
        Build();
    }

    void OnDestroy()
    {
        if (instance == this) { instance = null; Active = false; }
        foreach (var a in actions) { a.Disable(); a.Dispose(); }
        actions.Clear();
    }

    // ---- building the rig --------------------------------------------------------------------------------------
    InputAction Make(string name, InputActionType type, params string[] bindings)
    {
        var a = new InputAction(name, type);
        foreach (var b in bindings) a.AddBinding(b);
        a.Enable(); actions.Add(a); return a;
    }

    void Build()
    {
        var gun = FindAnyObjectByType<GunSystem>();
        cam = menuMode ? Camera.main : gun != null && gun.fpsCam != null ? gun.fpsCam : GetComponentInChildren<Camera>(true);
        if (cam == null) { Debug.LogWarning("[VR] No player camera found; staying in desktop mode."); Destroy(menuMode ? gameObject : (Object)this); return; }
        if (menuMode) transform.SetPositionAndRotation(cam.transform.position, Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f));
        orientation = transform.Find("Orientation");
        Transform blaster = cam.transform.Find("GunAim"), grappleParent = cam.transform.Find("GunPosition");
        var grapplingGun = FindAnyObjectByType<GrapplingGun>();

        // 1. things that fight a tracked head
        var look = GetComponent<PlayerLookV2>(); if (look != null) look.enabled = false;
        var follow = cam.transform.parent != null ? cam.transform.parent.GetComponent<MoveCamera>() : null; if (follow != null) follow.enabled = false;
        var shake = cam.GetComponent<CameraShake>(); if (shake != null) shake.enabled = false;
        if (gun != null) gun.camShake = null;                               // a shaking camera would fight the headset pose
        var aimScript = blaster != null ? blaster.GetComponent<GunAim>() : null; if (aimScript != null) aimScript.enabled = false;   // re-aims at the mouse cursor

        // 2. XR Origin -> Camera Offset -> Camera, with the floor at the bottom of the capsule
        origin = new GameObject("VR Origin").transform; origin.SetParent(transform, false); origin.localPosition = new Vector3(0f, -1f, 0f);
        offset = new GameObject("Camera Offset").transform; offset.SetParent(origin, false);
        cam.transform.SetParent(offset, false); cam.transform.localPosition = Vector3.zero; cam.transform.localRotation = Quaternion.identity;
        cam.nearClipPlane = 0.05f;
        var xr = origin.gameObject.AddComponent<XROrigin>();
        xr.Origin = origin.gameObject; xr.CameraFloorOffsetObject = offset.gameObject; xr.Camera = cam;
        xr.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
        AddPoseDriver(cam.gameObject, "<XRHMD>/centerEyePosition", "<XRHMD>/centerEyeRotation", "<XRHMD>/trackingState");

        // 3. controllers
        leftHand = MakeHand("LeftHand Controller", "LeftHand"); rightHand = MakeHand("RightHand Controller", "RightHand");

        // 4. weapons: blaster on the right hand, grapple gun on the left; both aim from the hand
        if (blaster != null) { blaster.SetParent(rightHand, false); blaster.localPosition = blasterOffset; blaster.localRotation = Quaternion.identity; }
        if (grappleParent != null) { grappleParent.SetParent(leftHand, false); grappleParent.localPosition = Vector3.zero; grappleParent.localRotation = Quaternion.identity; }
        if (grapplingGun != null)
        {
            grapplingGun.transform.localPosition = grappleOffset; grapplingGun.transform.localRotation = Quaternion.identity;
            grapplingGun.camera = grapplingGun.transform;                    // the rope fires along the grapple gun, not the head
        }
        if (gun != null && gun.gunEnd != null) gun.aimOverride = gun.gunEnd; // bullets leave the muzzle, along the barrel

        // 5. controller buttons
        move = Make("Move", InputActionType.Value, "<XRController>{LeftHand}/{Primary2DAxis}");
        turn = Make("Turn", InputActionType.Value, "<XRController>{RightHand}/{Primary2DAxis}");
        fire = Make("Fire", InputActionType.Button, "<XRController>{RightHand}/{TriggerButton}");
        grappleAction = Make("Grapple", InputActionType.Button, "<XRController>{LeftHand}/{TriggerButton}");
        jump = Make("Jump", InputActionType.Button, "<XRController>{RightHand}/{PrimaryButton}");
        reload = Make("Reload", InputActionType.Button, "<XRController>{RightHand}/{SecondaryButton}");
        slide = Make("Slide", InputActionType.Button, "<XRController>{RightHand}/{Primary2DAxisClick}");
        sprint = Make("Sprint", InputActionType.Button, "<XRController>{LeftHand}/{Primary2DAxisClick}");
        interact = Make("Interact", InputActionType.Button, "<XRController>{LeftHand}/{GripButton}", "<XRController>{RightHand}/{GripButton}");
        switchMode = Make("SwitchMode", InputActionType.Button, "<XRController>{LeftHand}/{PrimaryButton}");
        leaderboard = Make("Leaderboard", InputActionType.Button, "<XRController>{LeftHand}/{SecondaryButton}");
        pause = Make("Pause", InputActionType.Button, "<XRController>{LeftHand}/{Menu}", "<XRController>{RightHand}/{Menu}");

        // 6. HUD / menus -> world-space panels
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            if (c.isRootCanvas && c.renderMode != RenderMode.WorldSpace) hudCanvases.Add(c);
        for (int i = 0; i < hudCanvases.Count; i++)
        {
            var c = hudCanvases[i]; c.renderMode = RenderMode.WorldSpace; c.worldCamera = cam;
            var rt = (RectTransform)c.transform; rt.sizeDelta = new Vector2(1920f, 1080f); rt.localScale = Vector3.one * hudScale;
        }

        instance = this; Active = true;
        if (stereoPreview && !XRSettings.isDeviceActive) SetStereoPreview(true);
        Debug.Log($"[VR] VR rig active ({(XRSettings.isDeviceActive ? "headset: " + XRSettings.loadedDeviceName : "simulated devices")}). Canvases converted: {hudCanvases.Count}.");
    }

    Transform MakeHand(string name, string usage)
    {
        var go = new GameObject(name); go.transform.SetParent(offset, false);
        // prefer the 'aim' (pointer) pose so the weapon points where the controller points; fall back to the grip pose
        string prefix = $"<XRController>{{{usage}}}/";
        bool aim = HasControl(usage, "pointerPosition");
        AddPoseDriver(go, prefix + (aim ? "pointerPosition" : "devicePosition"), prefix + (aim ? "pointerRotation" : "deviceRotation"), prefix + "trackingState");
        return go.transform;
    }

    static bool HasControl(string usage, string control)
    {
        foreach (var d in InputSystem.devices)
            if (d is XRController c) foreach (var u in c.usages) if (u == usage) return c.TryGetChildControl(control) != null;
        return false;
    }

    static void AddPoseDriver(GameObject go, string position, string rotation, string state)
    {
        var tpd = go.AddComponent<TrackedPoseDriver>(); tpd.enabled = false;        // configure first, then enable
        tpd.positionInput = new InputActionProperty(new InputAction("Position", InputActionType.Value, position, expectedControlType: "Vector3"));
        tpd.rotationInput = new InputActionProperty(new InputAction("Rotation", InputActionType.Value, rotation, expectedControlType: "Quaternion"));
        tpd.trackingStateInput = new InputActionProperty(new InputAction("Tracking State", InputActionType.Value, state, expectedControlType: "Integer"));
        tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        tpd.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        tpd.ignoreTrackingState = true;
        tpd.enabled = true;
    }

    // ---- per frame ---------------------------------------------------------------------------------------------
    void Update()
    {
        if (!Active || cam == null) return;
        // movement is relative to where you look
        if (orientation != null) orientation.rotation = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);
        if (!XRSettings.isDeviceActive && Input.GetKeyDown(KeyCode.F2)) SetStereoPreview(!stereoPreview);
        // no headset (XR simulator): plain mouse look so the view can be turned without learning the simulator's keys.
        // Skipped while the simulator itself has the mouse (right button held) to avoid double rotation.
        if (!XRSettings.isDeviceActive && !menuMode && Time.timeScale > 0f && !Input.GetMouseButton(1))
        {
            float mx = Input.GetAxisRaw("Mouse X") * simMouseSensitivity, my = Input.GetAxisRaw("Mouse Y") * simMouseSensitivity;
            if (mx != 0f) origin.RotateAround(cam.transform.position, Vector3.up, mx);
            if (my != 0f) { simPitch = Mathf.Clamp(simPitch - my, -80f, 80f); offset.localRotation = Quaternion.Euler(simPitch, 0f, 0f); }
        }
        UpdateUIPointer();
        // snap turn: flick the right stick, release to re-arm
        float x = turn.ReadValue<Vector2>().x;
        if (turnArmed && Mathf.Abs(x) > snapTurnThreshold) { origin.RotateAround(cam.transform.position, Vector3.up, Mathf.Sign(x) * snapTurnAngle); turnArmed = false; }
        else if (!turnArmed && Mathf.Abs(x) < 0.3f) turnArmed = true;
    }

    // ---- menu pointer: the right controller's ray clicks world-space UI ---------------------------------------
    // (on a flat screen with the simulator the mouse still works through the normal EventSystem)
    PointerEventData ped; GameObject hovered, pressed, dragging; RaycastResult pressResult; Canvas pressCanvas;
    readonly List<RaycastResult> hits = new List<RaycastResult>();
    LineRenderer laser; bool wasDown; Vector2 pressPos;

    bool MenuOpen()
    {
        if (menuMode || Time.timeScale == 0f) return true;
        return false;
    }

    void UpdateUIPointer()
    {
        var es = EventSystem.current;
        bool open = MenuOpen() && es != null && rightHand != null;
        if (laser == null && rightHand != null)
        {
            laser = rightHand.gameObject.AddComponent<LineRenderer>();
            laser.positionCount = 2; laser.startWidth = 0.004f; laser.endWidth = 0.002f; laser.useWorldSpace = true;
            laser.material = new Material(Shader.Find("Sprites/Default")); laser.startColor = laser.endColor = new Color(0.3f, 0.9f, 1f, 0.9f);
        }
        if (laser != null) laser.enabled = open;
        if (!open) { ReleasePointer(es, false); return; }
        if (ped == null) ped = new PointerEventData(es);

        // find the canvas plane the ray hits first (while dragging, stay on the canvas that was pressed)
        var ray = new Ray(rightHand.position, rightHand.forward);
        Canvas best = null; float bestD = float.MaxValue; Vector3 bestPoint = Vector3.zero;
        foreach (var c in hudCanvases)
        {
            if (c == null || !c.gameObject.activeInHierarchy || !c.enabled || c.renderMode != RenderMode.WorldSpace) continue;
            if (pressCanvas != null && c != pressCanvas) continue;
            var rt = (RectTransform)c.transform;
            if (!new Plane(rt.forward, rt.position).Raycast(ray, out float d) || d <= 0f || d >= bestD) continue;
            Vector3 pt = ray.GetPoint(d);
            if (pressCanvas == null && !rt.rect.Contains(rt.InverseTransformPoint(pt))) continue;
            best = c; bestD = d; bestPoint = pt;
        }
        Vector3 end = best != null ? bestPoint : ray.GetPoint(3f);
        if (laser != null) { laser.SetPosition(0, ray.origin); laser.SetPosition(1, end); }

        bool down = fire != null && fire.IsPressed();
        RaycastResult top = default; GameObject target = null;
        if (best != null)
        {
            Vector2 sp = cam.WorldToScreenPoint(bestPoint);
            ped.delta = sp - ped.position; ped.position = sp;
            hits.Clear(); es.RaycastAll(ped, hits);
            foreach (var h in hits) if (h.gameObject != null) { top = h; target = h.gameObject; break; }
            ped.pointerCurrentRaycast = top;
        }

        // hover
        var hoverTarget = target != null ? ExecuteEvents.GetEventHandler<IPointerEnterHandler>(target) : null;
        if (hoverTarget != hovered)
        {
            if (hovered != null) ExecuteEvents.Execute(hovered, ped, ExecuteEvents.pointerExitHandler);
            hovered = hoverTarget; ped.pointerEnter = hovered;
            if (hovered != null) ExecuteEvents.Execute(hovered, ped, ExecuteEvents.pointerEnterHandler);
        }

        if (down && !wasDown && target != null)
        {
            pressCanvas = best; pressResult = top; pressPos = ped.position;
            ped.pressPosition = ped.position; ped.pointerPressRaycast = top; ped.eligibleForClick = true;
            pressed = ExecuteEvents.ExecuteHierarchy(target, ped, ExecuteEvents.pointerDownHandler);
            if (pressed == null) pressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(target);
            ped.pointerPress = pressed; ped.rawPointerPress = target;
            ped.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(target);
            if (ped.pointerDrag != null) ExecuteEvents.Execute(ped.pointerDrag, ped, ExecuteEvents.initializePotentialDrag);
            if (pressed != null && pressed.GetComponent<UnityEngine.UI.Selectable>() != null) es.SetSelectedGameObject(pressed);
        }
        else if (down && pressCanvas != null)
        {
            if (ped.pointerDrag != null)
            {
                if (dragging == null && (ped.position - pressPos).sqrMagnitude > 4f)
                { dragging = ped.pointerDrag; ped.dragging = true; ExecuteEvents.Execute(dragging, ped, ExecuteEvents.beginDragHandler); }
                if (dragging != null) ExecuteEvents.Execute(dragging, ped, ExecuteEvents.dragHandler);
            }
        }
        else if (!down && wasDown) ReleasePointer(es, true);
        wasDown = down;
    }

    void ReleasePointer(EventSystem es, bool allowClick)
    {
        if (ped != null && pressed != null)
        {
            ExecuteEvents.Execute(pressed, ped, ExecuteEvents.pointerUpHandler);
            var under = ped.pointerCurrentRaycast.gameObject != null ? ExecuteEvents.GetEventHandler<IPointerClickHandler>(ped.pointerCurrentRaycast.gameObject) : null;
            if (allowClick && dragging == null && under == pressed) ExecuteEvents.Execute(pressed, ped, ExecuteEvents.pointerClickHandler);
        }
        if (dragging != null) { ExecuteEvents.Execute(dragging, ped, ExecuteEvents.endDragHandler); }
        if (ped != null) { ped.dragging = false; ped.pointerPress = null; ped.pointerDrag = null; }
        pressed = null; dragging = null; pressCanvas = null; wasDown = false;
        if (hovered != null && ped != null && !allowClick) { ExecuteEvents.Execute(hovered, ped, ExecuteEvents.pointerExitHandler); hovered = null; }
    }

    // ---- stereo preview (flat screen only) -----------------------------------------------------------------
    [Tooltip("Without a headset, render left/right eye views side by side in the Game view. F2 toggles at runtime.")]
    public bool stereoPreview = false;
    public float eyeSeparation = 0.064f;
    Camera rightEye;

    void SetStereoPreview(bool on)
    {
        stereoPreview = on;
        if (on && rightEye == null)
        {
            var go = new GameObject("Right Eye (preview)"); go.transform.SetParent(cam.transform, false);
            go.transform.localPosition = new Vector3(eyeSeparation, 0f, 0f);
            rightEye = go.AddComponent<Camera>(); rightEye.CopyFrom(cam);
            rightEye.depth = cam.depth; rightEye.tag = "Untagged";
        }
        cam.rect = on ? new Rect(0f, 0f, 0.5f, 1f) : new Rect(0f, 0f, 1f, 1f);
        if (rightEye != null) { rightEye.rect = new Rect(0.5f, 0f, 0.5f, 1f); rightEye.enabled = on; }
    }

    [Tooltip("Show a small 'VR ACTIVE' label in the Game view (flat screen only; a headset won't show it).")]
    public bool showStatusLabel = true;

    void OnGUI()
    {
        if (!Active || !showStatusLabel) return;
        string mode = XRSettings.isDeviceActive ? "headset: " + XRSettings.loadedDeviceName : "XR SIMULATOR (no headset)";
        GUI.Label(new Rect(10, 10, 600, 24), "VR ACTIVE - " + mode + (XRSettings.isDeviceActive ? "" : "   [F2: stereo preview " + (stereoPreview ? "ON" : "off") + "]"));
    }

    void LateUpdate()
    {
        if (!Active || cam == null) return;
        // keep the head over the capsule: the body moves, you don't walk away from it
        Vector3 head = transform.InverseTransformPoint(cam.transform.position);
        // and lock eye height (relative to the Player pivot, feet are at y=-1) so it doesn't depend on the tracking origin
        origin.localPosition += new Vector3(-head.x, eyeHeightLocal - head.y, -head.z);

        // HUD / menu panels: hover in front of you and lazily follow your gaze
        Vector3 fwd = cam.transform.forward; fwd.y = 0f; if (fwd.sqrMagnitude < 0.01f) fwd = transform.forward; fwd.Normalize();
        for (int i = 0; i < hudCanvases.Count; i++)
        {
            var c = hudCanvases[i]; if (c == null) continue;
            var rt = (RectTransform)c.transform;
            Vector3 target = cam.transform.position + fwd * (hudDistance - i * 0.01f) + Vector3.down * 0.05f;
            rt.position = Vector3.Lerp(rt.position, target, 1f - Mathf.Exp(-hudFollowSpeed * Time.unscaledDeltaTime));
            Vector3 away = rt.position - cam.transform.position; if (away.sqrMagnitude > 0.0001f) rt.rotation = Quaternion.LookRotation(away, Vector3.up);
        }
    }

    // ---- button table used by GameInput -------------------------------------------------------------------------
    InputAction ActionFor(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Space: return jump;
            case KeyCode.LeftShift: return sprint;
            case KeyCode.LeftControl: return slide;
            case KeyCode.Mouse0: return fire;
            case KeyCode.Mouse1: return grappleAction;
            case KeyCode.R: return reload;
            case KeyCode.B: return switchMode;
            case KeyCode.F: return interact;
            case KeyCode.Tab: return leaderboard;
            case KeyCode.Escape: return pause;
            default: return null;
        }
    }

    public static bool Held(KeyCode k) { var a = instance?.ActionFor(k); return a != null && a.IsPressed(); }
    public static bool PressedThisFrame(KeyCode k) { var a = instance?.ActionFor(k); return a != null && a.WasPressedThisFrame(); }
    public static bool ReleasedThisFrame(KeyCode k) { var a = instance?.ActionFor(k); return a != null && a.WasReleasedThisFrame(); }

    public static float GetMoveAxis(string axisName)
    {
        if (instance == null || instance.move == null) return 0f;
        Vector2 v = instance.move.ReadValue<Vector2>();
        float dz = instance.stickDeadzone; if (v.magnitude < dz) return 0f;
        return axisName == "Horizontal" ? v.x : axisName == "Vertical" ? v.y : 0f;
    }
}
