using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Random = UnityEngine.Random;
using UnityEngine.Audio;

/// <summary>
/// Tactical FPS Weapon System.
/// Handles raycast hitscan ballistics, spread calculation, dynamic recoil,
/// multiple fire modes (Semi/Burst/Auto), ammo tracking, and procedural hitmarkers.
/// Supports both inanimate targets and reactive EnemyAI combatants.
/// </summary>
public class GunSystem : MonoBehaviour
{
    public enum FireMode
    {
        FullAuto,
        Burst,
        SemiAuto
    }

    [Header("Firing Mode Configuration")]
    public FireMode currentFireMode = FireMode.FullAuto;
    public KeyCode switchModeKey = KeyCode.B;

    [Header("Weapon Timing & Ballistics")]
    public float fireRate = 0.25f;
    public float weaponRange = 100f;
    public int damage = 25;
    public float timeBetweenShooting = 0.1f;
    public float spread = 0.02f;
    public float range = 100f;
    public float reloadTime = 1.5f;
    public float timeBetweenShots = 0.05f;

    [Header("Ammunition")]
    public int magazineSize = 30;
    public int bulletsPerTap = 1;
    public bool allowButtonHold = true;
    private int bulletsLeft;
    private int bulletsShot;

    [Header("Gun References")]
    public Transform gunEnd;
    public ParticleSystem muzzleFlash;
    public ParticleSystem cartridgeEjection;
    public Camera fpsCam;
    [Tooltip("If set (VR), shots are aimed from this transform instead of the camera, e.g. the muzzle.")]
    public Transform aimOverride;
    Transform AimTransform => aimOverride != null ? aimOverride : fpsCam.transform;
    public RaycastHit rayHit;
    public LayerMask whatIsEnemy;

    [Header("Impact VFX Decals")]
    public GameObject metalHitEffect;
    public GameObject sandHitEffect;
    public GameObject stoneHitEffect;
    public GameObject waterLeakEffect;
    public GameObject waterLeakExtinguishEffect;
    public GameObject[] fleshHitEffects;
    public GameObject woodHitEffect;

    [Header("Bullet Trail & Audio")]
    [SerializeField] private TrailRenderer BulletTrail;
    public AudioMixer audioMixer;
    public AudioClip shootingSound;
    public AudioClip hitSound;
    public AudioClip reloadingSound;

    [Header("Camera & UI")]
    public CameraShake camShake;
    public float camShakeMagnitude = 0.1f;
    public float camShakeDuration = 0.1f;
    public TextMeshProUGUI text;

    // Internal State
    private bool shooting;
    private bool readyToShoot;
    private bool reloading;
    private Animator anim;
    private GunAim gunAim;

    // Procedural Hitmarker State
    private float hitmarkerTimer = 0f;
    private const float HITMARKER_DURATION = 0.12f;
    private static Texture2D lineTexture;

    private void Awake()
    {
        bulletsLeft = magazineSize;
        readyToShoot = true;

        anim = GetComponent<Animator>();
        gunAim = GetComponentInParent<GunAim>();
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;

        MyInput();

        // Switch firing mode with key
        if (GameInput.GetKeyDown(switchModeKey))
        {
            CycleFireMode();
        }

        // Update HUD text
        UpdateHUD();
    }

    private void CycleFireMode()
    {
        if (currentFireMode == FireMode.FullAuto)
        {
            currentFireMode = FireMode.Burst;
            allowButtonHold = false;
        }
        else if (currentFireMode == FireMode.Burst)
        {
            currentFireMode = FireMode.SemiAuto;
            allowButtonHold = false;
        }
        else
        {
            currentFireMode = FireMode.FullAuto;
            allowButtonHold = true;
        }
    }

    private void UpdateHUD()
    {
        if (text == null) return;

        string modeLabel = currentFireMode switch
        {
            FireMode.FullAuto => "AUTO",
            FireMode.Burst => "BURST",
            FireMode.SemiAuto => "SEMI",
            _ => "AUTO"
        };

        if (reloading)
        {
            text.text = $"RELOADING... [{modeLabel}]";
        }
        else
        {
            text.text = $"{bulletsLeft} / {magazineSize} [{modeLabel}]";
        }
    }

    private void MyInput()
    {
        if (currentFireMode == FireMode.FullAuto)
        {
            shooting = GameInput.GetKey(KeyCode.Mouse0);
        }
        else
        {
            shooting = GameInput.GetKeyDown(KeyCode.Mouse0);
        }

        // Reload command
        if (GameInput.GetKeyDown(KeyCode.R) && bulletsLeft < magazineSize && !reloading)
        {
            Reload();
        }

        // Shoot trigger
        if (readyToShoot && shooting && !reloading && bulletsLeft > 0)
        {
            bulletsShot = (currentFireMode == FireMode.Burst) ? Mathf.Min(3, bulletsLeft) : bulletsPerTap;
            Shoot();
        }
    }

    private void Shoot()
    {
        readyToShoot = false;

        // Register shot with GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterShotFired();
        }

        // Audio
        if (shootingSound != null)
        {
            float sfxVol = AudioManager.Instance != null ? AudioManager.Instance.GetSfxVolumeLevel() : 1f;
            AudioSource.PlayClipAtPoint(shootingSound, transform.position, sfxVol);
        }

        // Ballistic spread calculation
        float x = Random.Range(-spread, spread);
        float y = Random.Range(-spread, spread);
        float z = Random.Range(-spread, spread);
        Vector3 direction = AimTransform.forward + new Vector3(x, y, z);

        // Hitscan raycast (QueryTriggerInteraction.Collide ensures trigger targets are hit)
        if (Physics.Raycast(AimTransform.position, direction, out rayHit, range, whatIsEnemy, QueryTriggerInteraction.Collide))
        {
            HandleHit(rayHit);

            if (BulletTrail != null && gunEnd != null)
            {
                TrailRenderer trail = Instantiate(BulletTrail, gunEnd.position, Quaternion.identity);
                StartCoroutine(SpawnTrail(trail, rayHit.point));
            }
        }
        else
        {
            // If ray didn't hit an obstacle, cast bullet trail to max distance
            if (BulletTrail != null && gunEnd != null)
            {
                TrailRenderer trail = Instantiate(BulletTrail, gunEnd.position, Quaternion.identity);
                StartCoroutine(SpawnTrail(trail, AimTransform.position + direction * range));
            }
        }

        // Camera recoil
        if (camShake != null)
        {
            StartCoroutine(camShake.Shake(camShakeDuration, camShakeMagnitude));
        }

        // Visual effects
        if (muzzleFlash != null) muzzleFlash.Play();
        if (cartridgeEjection != null) cartridgeEjection.Play();
        if (anim != null) anim.SetTrigger("Fire");

        bulletsLeft--;
        bulletsShot--;

        Invoke(nameof(ResetShot), timeBetweenShooting);

        if (bulletsShot > 0 && bulletsLeft > 0)
        {
            Invoke(nameof(Shoot), timeBetweenShots);
        }
    }

    private void ResetShot()
    {
        readyToShoot = true;
    }

    private void Reload()
    {
        reloading = true;
        if (reloadingSound != null)
        {
            float sfxVol = AudioManager.Instance != null ? AudioManager.Instance.GetSfxVolumeLevel() : 1f;
            AudioSource.PlayClipAtPoint(reloadingSound, transform.position, sfxVol);
        }
        Invoke(nameof(ReloadFinished), reloadTime);
    }

    private void ReloadFinished()
    {
        bulletsLeft = magazineSize;
        reloading = false;
    }

    private void HandleHit(RaycastHit hit)
    {
        bool hitValidTarget = false;


        // 2. Check for modular Target component
        Target targetComp = hit.collider.GetComponentInParent<Target>();
        if (targetComp != null)
        {
            targetComp.TakeDamage(damage, hit.point, hit.normal);
            hitValidTarget = true;
        }
        // 3. Fallback to existing Target layer (layer 12)
        else if (hit.collider.gameObject.layer == 12)
        {
            hit.collider.gameObject.SetActive(false);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TargetHit();
            }
            hitValidTarget = true;
        }

        // 4. Check for Explosive Prop
        ExplosiveProp prop = hit.collider.GetComponentInParent<ExplosiveProp>();
        if (prop != null)
        {
            prop.TakeDamage(damage);
            hitValidTarget = true;
        }

        if (hitValidTarget)
        {
            TriggerHitmarker();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterShotHit();
            }
        }

        // 5. Material-based impact decals & flesh effects
        if (hit.collider.sharedMaterial != null)
        {
            string materialName = hit.collider.sharedMaterial.name;

            switch (materialName)
            {
                case "Metal":
                    SpawnDecal(hit, metalHitEffect);
                    if (hitSound != null)
                    {
                        float sfxVol = AudioManager.Instance != null ? AudioManager.Instance.GetSfxVolumeLevel() : 1f;
                        AudioSource.PlayClipAtPoint(hitSound, transform.position, sfxVol);
                    }
                    break;
                case "Sand":
                    SpawnDecal(hit, sandHitEffect);
                    break;
                case "Stone":
                    SpawnDecal(hit, stoneHitEffect);
                    break;
                case "WaterFilled":
                    SpawnDecal(hit, waterLeakEffect);
                    SpawnDecal(hit, metalHitEffect);
                    break;
                case "Wood":
                    SpawnDecal(hit, woodHitEffect);
                    break;
                case "Meat":
                case "Character":
                    if (fleshHitEffects != null && fleshHitEffects.Length > 0)
                        SpawnDecal(hit, fleshHitEffects[Random.Range(0, fleshHitEffects.Length)]);
                    break;
                case "WaterFilledExtinguish":
                    SpawnDecal(hit, waterLeakExtinguishEffect);
                    SpawnDecal(hit, metalHitEffect);
                    break;
                default:
                    SpawnDecal(hit, metalHitEffect);
                    break;
            }
        }
        else
        {
            SpawnDecal(hit, metalHitEffect);
        }
    }

    private void SpawnDecal(RaycastHit hit, GameObject prefab)
    {
        if (prefab == null) return;
        GameObject spawnedDecal = Instantiate(prefab, hit.point, Quaternion.LookRotation(hit.normal));
        spawnedDecal.transform.SetParent(hit.collider.transform);
    }

    public void TriggerHitmarker()
    {
        hitmarkerTimer = HITMARKER_DURATION;
        if (hitSound != null)
        {
            float sfxVol = AudioManager.Instance != null ? AudioManager.Instance.GetSfxVolumeLevel() : 1f;
            AudioSource.PlayClipAtPoint(hitSound, transform.position, sfxVol);
        }
    }

    private void OnGUI()
    {
        if (hitmarkerTimer > 0f)
        {
            hitmarkerTimer -= Time.deltaTime;
            DrawHitmarker();
        }
    }

    private void DrawHitmarker()
    {
        float x = Screen.width / 2f;
        float y = Screen.height / 2f;
        float size = 8f;
        float gap = 4f;

        Color prevColor = GUI.color;
        GUI.color = Color.white;

        // Draw 4 diagonal cross ticks:
        DrawLine(new Vector2(x - gap, y - gap), new Vector2(x - gap - size, y - gap - size), 2);
        DrawLine(new Vector2(x + gap, y - gap), new Vector2(x + gap + size, y - gap - size), 2);
        DrawLine(new Vector2(x - gap, y + gap), new Vector2(x - gap - size, y + gap + size), 2);
        DrawLine(new Vector2(x + gap, y + gap), new Vector2(x + gap + size, y + gap + size), 2);

        GUI.color = prevColor;
    }

    private static void DrawLine(Vector2 pointA, Vector2 pointB, float width)
    {
        if (lineTexture == null)
        {
            lineTexture = new Texture2D(1, 1);
            lineTexture.SetPixel(0, 0, Color.white);
            lineTexture.Apply();
        }

        Matrix4x4 matrix = GUI.matrix;
        float angle = Mathf.Rad2Deg * Mathf.Atan2(pointB.y - pointA.y, pointB.x - pointA.x);
        float length = (pointB - pointA).magnitude;

        GUIUtility.RotateAroundPivot(angle, pointA);
        GUI.DrawTexture(new Rect(pointA.x, pointA.y - width / 2f, length, width), lineTexture);
        GUI.matrix = matrix;
    }

    private IEnumerator SpawnTrail(TrailRenderer trail, Vector3 hitPoint)
    {
        float time = 0;
        Vector3 startPosition = trail.transform.position;

        while (time < 1)
        {
            trail.transform.position = Vector3.Lerp(startPosition, hitPoint, time);
            time += Time.deltaTime / trail.time;
            yield return null;
        }

        Destroy(trail.gameObject, trail.time);
    }
}
