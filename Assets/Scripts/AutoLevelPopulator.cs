using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Automatically populates course levels with Static Bullseye Targets,
/// Dynamic Moving Targets, and Explosive Barrels across the full course.
/// Target hits award 50 points each.
/// </summary>
public class AutoLevelPopulator : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnSceneLoaded()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        string name = currentScene.name;

        if (name.Contains("Menu") || name.Contains("Tech Demo"))
        {
            return;
        }

        GameObject existingRoot = GameObject.Find("Auto_Combat_Populations");
        if (existingRoot != null)
        {
            return;
        }

        PopulateScene(name);
    }

    public static void PopulateScene(string sceneName)
    {
        // Avoid duplicate population
        GameObject existingRoot = GameObject.Find("Auto_Combat_Populations");
        if (existingRoot != null)
        {
            return;
        }

        GameObject root = new GameObject("Auto_Combat_Populations");

        // 1. Upgrade existing static barrels in the scene to have Target component
        UpgradeExistingTargets(root);

        // 2. Spawn customized targets & barrels across each level
        if (sceneName.Contains("Easy") || sceneName.Contains("Kolay"))
        {
            PopulateEasyLevel(root);
        }
        else if (sceneName.Contains("Mid"))
        {
            PopulateMidLevel(root);
        }
        else if (sceneName.Contains("Hard"))
        {
            PopulateHardLevel(root);
        }
        else
        {
            PopulateGenericCourse(root);
        }

        Debug.Log($"[AutoLevelPopulator] Successfully populated {sceneName} with Targets and Barrels!");
    }

    private static void UpgradeExistingTargets(GameObject parent)
    {
        BlinkEffect[] blinks = Object.FindObjectsByType<BlinkEffect>();
        foreach (var b in blinks)
        {
            if (b.GetComponent<Target>() == null)
            {
                b.gameObject.AddComponent<Target>();
            }
        }
    }

    private static void PopulateEasyLevel(GameObject root)
    {
        // --- ZONE 1: Starting Range (Directly in front of spawn at Z: -44) ---
        SpawnStaticTarget(root, new Vector3(7.8f, 17f, -30f), "Target_Alpha_1");
        SpawnStaticTarget(root, new Vector3(4.5f, 17.5f, -15f), "Target_Alpha_2");
        SpawnStaticTarget(root, new Vector3(11.0f, 17.5f, -15f), "Target_Alpha_3");
        SpawnMovingTarget(root, new Vector3(7.8f, 18.5f, 5f), Vector3.right, 4.5f, 2.2f);
        SpawnExplosiveBarrel(root, new Vector3(10f, 16f, -8f));

        // --- ZONE 2: Mid-Course Descent ---
        SpawnStaticTarget(root, new Vector3(7.8f, 11f, 80f), "Target_Bravo_1");
        SpawnStaticTarget(root, new Vector3(4f, -7f, 125f), "Target_Bravo_2");
        SpawnMovingTarget(root, new Vector3(12f, -8f, 150f), Vector3.up, 3.5f, 1.8f);
        SpawnExplosiveBarrel(root, new Vector3(7.8f, -12f, 205f));
        SpawnStaticTarget(root, new Vector3(5f, -11f, 215f), "Target_Bravo_3");

        // --- ZONE 3: Final Approach ---
        SpawnMovingTarget(root, new Vector3(7.8f, -18f, 280f), Vector3.right, 5f, 2.5f);
        SpawnStaticTarget(root, new Vector3(3f, -21f, 320f), "Target_Charlie_1");
        SpawnStaticTarget(root, new Vector3(12f, -21f, 335f), "Target_Charlie_2");
        SpawnExplosiveBarrel(root, new Vector3(9f, -22f, 355f));
        SpawnStaticTarget(root, new Vector3(7.8f, -21f, 375f), "Target_Charlie_Master");
    }

    private static void PopulateMidLevel(GameObject root)
    {
        // --- 1. STARTING ROOM (Visible directly from spawn at 1.49, 1.05, 1.5) ---
        SpawnStaticTarget(root, new Vector3(1.5f, 2.8f, 11f), "Mid_Start_CenterTarget");
        SpawnStaticTarget(root, new Vector3(4.5f, 3.0f, 15f), "Mid_Start_RightTarget");
        // Left Target positioned safely inside corridor, completely free of pillar geometry
        SpawnStaticTarget(root, new Vector3(0.8f, 2.8f, 14f), "Mid_Start_LeftTarget");
        SpawnMovingTarget(root, new Vector3(1.5f, 4.0f, 22f), Vector3.up, 2.0f, 2.0f);
        SpawnExplosiveBarrel(root, new Vector3(4.0f, 0.65f, 12f));

        // --- 2. ROOM 0 & TRANSITION CORRIDOR (Ascent to Room 1) ---
        SpawnStaticTarget(root, new Vector3(-5f, 3.5f, 38f), "Mid_Room0_Target1");
        SpawnStaticTarget(root, new Vector3(-10f, 4.0f, 55f), "Mid_Corridor_Target1");
        SpawnMovingTarget(root, new Vector3(-14f, 5.5f, 75f), Vector3.up, 3.5f, 2.0f);

        // --- 3. ROOM 1 (Checkpoint 1 at -17.4, 4.4, 96.2) ---
        SpawnStaticTarget(root, new Vector3(-17.4f, 6.5f, 115f), "Mid_Room1_Target1");
        SpawnStaticTarget(root, new Vector3(-12f, 7.0f, 135f), "Mid_Room1_Target2");
        SpawnMovingTarget(root, new Vector3(-15f, 8.5f, 155f), Vector3.right, 4.0f, 2.2f);
        SpawnExplosiveBarrel(root, new Vector3(-14f, 4.5f, 125f));

        // --- 4. ROOM 2 (Checkpoint 2 at -15.2, 11.2, 199.8) ---
        SpawnStaticTarget(root, new Vector3(-17f, 13.5f, 205f), "Mid_Room2_Target1");
        SpawnStaticTarget(root, new Vector3(-13f, 13.5f, 215f), "Mid_Room2_Target2");
        SpawnMovingTarget(root, new Vector3(-15.2f, 14.5f, 218f), Vector3.up, 3.0f, 2.5f);
        SpawnExplosiveBarrel(root, new Vector3(-12f, 11.5f, 212f));

        // --- 5. SECTOR 2 (course now ends at the finish gate on Platform 13, x ~108, z ~171) ---
        SpawnStaticTarget(root, new Vector3(103.6f, 13.5f, 180f), "Mid_Sec2_Target1");
        SpawnMovingTarget(root, new Vector3(110f, 14.0f, 195f), Vector3.right, 4.0f, 2.0f);
    }

    private static void PopulateHardLevel(GameObject root)
    {
        // --- 1. STARTING SECTION (Spawn at 1.0, 2.5, 0.25) ---
        SpawnStaticTarget(root, new Vector3(1.0f, 4.5f, 15f), "Hard_Start_CenterTarget");
        SpawnStaticTarget(root, new Vector3(4.5f, 5.0f, 25f), "Hard_Start_RightTarget");
        SpawnMovingTarget(root, new Vector3(1.0f, 6.5f, 35f), Vector3.right, 5.0f, 2.5f);
        SpawnExplosiveBarrel(root, new Vector3(3.5f, 2.5f, 18f));

        // --- 2. DESCENT & FLOATING PILLARS (Z: 60 to 110) ---
        SpawnStaticTarget(root, new Vector3(2.5f, -3.0f, 75f), "Hard_Drop_Target1");
        SpawnMovingTarget(root, new Vector3(-1.0f, -4.0f, 105f), Vector3.up, 4.5f, 2.2f);

        // --- 3. SECTION 1 PLATFORM (Checkpoint at 2.07, -6.73, 139.4) ---
        SpawnStaticTarget(root, new Vector3(2.07f, -5.0f, 130f), "Hard_Sec1_Target1");
        SpawnExplosiveBarrel(root, new Vector3(4.5f, -6.5f, 135f));

        // --- 4. MID-AIR GRAPPLING CANYON (Suspended drones snipable during grapple) ---
        SpawnMovingTarget(root, new Vector3(65f, -10.0f, 139.4f), Vector3.up, 5.0f, 2.5f);
        SpawnMovingTarget(root, new Vector3(135f, -14.0f, 139.4f), Vector3.forward, 5.0f, 2.5f);

        // --- 5. SECTION 2 LANDING ARENA (Checkpoint at 195.1, -19.24, 139.4) ---
        SpawnStaticTarget(root, new Vector3(191.0f, -17.0f, 140f), "Hard_Sec2_Target1");
        SpawnStaticTarget(root, new Vector3(198.5f, -17.0f, 143f), "Hard_Sec2_Target2");
        SpawnMovingTarget(root, new Vector3(195.1f, -15.5f, 145f), Vector3.right, 4.0f, 2.8f);
        SpawnExplosiveBarrel(root, new Vector3(193.0f, -18.5f, 142f));

        // --- 6. HIGH TOWER & EXTENDED PLATFORMS ---
        SpawnStaticTarget(root, new Vector3(219.2f, 44.5f, 10f), "Hard_Tower_Target1");
        SpawnMovingTarget(root, new Vector3(225f, 47.0f, -20f), Vector3.up, 4.0f, 2.0f);
        SpawnStaticTarget(root, new Vector3(76.5f, 10.5f, -80f), "Hard_Final_Target1");
    }

    private static void PopulateGenericCourse(GameObject root)
    {
        PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
        Vector3 start = (pm != null) ? pm.transform.position : Vector3.zero;

        SpawnStaticTarget(root, start + new Vector3(0f, 2f, 15f), "Target_1");
        SpawnStaticTarget(root, start + new Vector3(4f, 2.5f, 25f), "Target_2");
        SpawnMovingTarget(root, start + new Vector3(-3f, 3.5f, 35f), Vector3.right, 4f, 2f);
        SpawnExplosiveBarrel(root, start + new Vector3(2f, 1f, 20f));
    }

    public static GameObject SpawnStaticTarget(GameObject parent, Vector3 position, string name = "StaticTarget_Bullseye")
    {
        // Outer Red Ring
        GameObject targetObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        targetObj.name = name;
        targetObj.layer = 12;
        targetObj.transform.position = position;
        targetObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        targetObj.transform.localScale = new Vector3(1.5f, 0.08f, 1.5f);
        if (parent != null) targetObj.transform.SetParent(parent.transform);

        Collider mainCol = targetObj.GetComponent<Collider>();
        if (mainCol != null)
        {
            mainCol.isTrigger = true; // Non-blocking trigger ensures player can never be trapped
        }

        Renderer ren = targetObj.GetComponent<Renderer>();
        if (ren != null) ren.material.color = Color.red;

        // Inner White Ring
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "InnerRing";
        ring.layer = 12;
        ring.transform.SetParent(targetObj.transform);
        ring.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        ring.transform.localRotation = Quaternion.identity;
        ring.transform.localScale = new Vector3(0.68f, 0.5f, 0.68f);
        Collider ringCol = ring.GetComponent<Collider>();
        if (ringCol != null) Object.Destroy(ringCol);
        Renderer ringRen = ring.GetComponent<Renderer>();
        if (ringRen != null) ringRen.material.color = Color.white;

        // Center Bullseye Dot
        GameObject center = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        center.name = "CenterDot";
        center.layer = 12;
        center.transform.SetParent(targetObj.transform);
        center.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        center.transform.localRotation = Quaternion.identity;
        center.transform.localScale = new Vector3(0.35f, 0.6f, 0.35f);
        Collider centerCol = center.GetComponent<Collider>();
        if (centerCol != null) Object.Destroy(centerCol);
        Renderer centerRen = center.GetComponent<Renderer>();
        if (centerRen != null) centerRen.material.color = Color.red;

        targetObj.AddComponent<Target>();

        return targetObj;
    }

    public static GameObject SpawnMovingTarget(GameObject parent, Vector3 position, Vector3 axis, float distance, float speed)
    {
        // Outer Red Ring
        GameObject targetObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        targetObj.name = "MovingTarget_Drone";
        targetObj.layer = 12;
        targetObj.transform.position = position;
        targetObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        targetObj.transform.localScale = new Vector3(1.6f, 0.1f, 1.6f);
        if (parent != null) targetObj.transform.SetParent(parent.transform);

        Collider mainCol = targetObj.GetComponent<Collider>();
        if (mainCol != null)
        {
            mainCol.isTrigger = true; // Non-blocking trigger ensures player can never be trapped
        }

        Renderer ren = targetObj.GetComponent<Renderer>();
        if (ren != null) ren.material.color = Color.red;

        // Inner White Ring
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "InnerRing";
        ring.layer = 12;
        ring.transform.SetParent(targetObj.transform);
        ring.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        ring.transform.localRotation = Quaternion.identity;
        ring.transform.localScale = new Vector3(0.68f, 0.5f, 0.68f);
        Collider ringCol = ring.GetComponent<Collider>();
        if (ringCol != null) Object.Destroy(ringCol);
        Renderer ringRen = ring.GetComponent<Renderer>();
        if (ringRen != null) ringRen.material.color = Color.white;

        // Center Bullseye Dot
        GameObject center = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        center.name = "CenterDot";
        center.layer = 12;
        center.transform.SetParent(targetObj.transform);
        center.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        center.transform.localRotation = Quaternion.identity;
        center.transform.localScale = new Vector3(0.35f, 0.6f, 0.35f);
        Collider centerCol = center.GetComponent<Collider>();
        if (centerCol != null) Object.Destroy(centerCol);
        Renderer centerRen = center.GetComponent<Renderer>();
        if (centerRen != null) centerRen.material.color = Color.red;

        // Target Script with motion
        Target targetComp = targetObj.AddComponent<Target>();
        SetTargetMoving(targetComp, axis, distance, speed);

        return targetObj;
    }

    public static GameObject SpawnExplosiveBarrel(GameObject parent, Vector3 position)
    {
        GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.name = "ExplosiveBarrel";
        barrel.layer = 12;
        barrel.transform.position = position;
        barrel.transform.localScale = new Vector3(1.2f, 1.3f, 1.2f);
        if (parent != null) barrel.transform.SetParent(parent.transform);

        Renderer ren = barrel.GetComponent<Renderer>();
        if (ren != null) ren.material.color = new Color(0.9f, 0.15f, 0.1f);

        // Yellow Hazard Stripe
        GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stripe.name = "HazardStripe";
        stripe.layer = 12;
        stripe.transform.SetParent(barrel.transform);
        stripe.transform.localPosition = Vector3.zero;
        stripe.transform.localScale = new Vector3(1.02f, 0.35f, 1.02f);
        Collider stripeCol = stripe.GetComponent<Collider>();
        if (stripeCol != null) Object.Destroy(stripeCol);
        Renderer stripeRen = stripe.GetComponent<Renderer>();
        if (stripeRen != null) stripeRen.material.color = new Color(1f, 0.85f, 0.1f);

        barrel.AddComponent<ExplosiveProp>();

        return barrel;
    }

    private static void SetTargetMoving(Target target, Vector3 axis, float distance, float speed)
    {
        var type = typeof(Target);
        var moveField = type.GetField("isMovingTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var axisField = type.GetField("moveAxis", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var distField = type.GetField("moveDistance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var spdField = type.GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (moveField != null) moveField.SetValue(target, true);
        if (axisField != null) axisField.SetValue(target, axis);
        if (distField != null) distField.SetValue(target, distance);
        if (spdField != null) spdField.SetValue(target, speed);
    }
}
