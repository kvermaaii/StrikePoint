using System.Collections;
using UnityEngine;

/// <summary>
/// Reactive target system for the tactical shooting range.
/// Supports health, hit flash reactions, oscillating movement, and score bonuses.
/// Designed for modular student project evaluation.
/// </summary>
public class Target : MonoBehaviour
{
    [Header("Target Properties")]
    [Tooltip("Maximum health before target is destroyed")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Tooltip("Time bonus (in seconds) subtracted from course run time")]
    [SerializeField] private float timeBonusSeconds = 3f;

    [Header("Movement (Optional)")]
    [Tooltip("Enable if target should move/patrol")]
    [SerializeField] private bool isMovingTarget = false;
    [Tooltip("Axis of oscillation (e.g. 1,0,0 for horizontal)")]
    [SerializeField] private Vector3 moveAxis = Vector3.right;
    [Tooltip("Distance target travels from start point")]
    [SerializeField] private float moveDistance = 3f;
    [Tooltip("Speed of oscillation")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Visual & Audio Feedback")]
    [SerializeField] private GameObject destructionVfx;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip destroySound;
    [SerializeField] private Color hitFlashColor = Color.red;

    private Vector3 initialPosition;
    private Renderer[] meshRenderers;
    private Color[] originalColors;
    private bool isDestroyed = false;

    private void Awake()
    {
        currentHealth = maxHealth;
        initialPosition = transform.position;
        meshRenderers = GetComponentsInChildren<Renderer>();

        if (meshRenderers != null && meshRenderers.Length > 0)
        {
            originalColors = new Color[meshRenderers.Length];
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                if (meshRenderers[i].material != null && meshRenderers[i].material.HasProperty("_Color"))
                {
                    originalColors[i] = meshRenderers[i].material.color;
                }
            }
        }
    }

    private void Update()
    {
        if (isMovingTarget && !isDestroyed)
        {
            float offset = Mathf.Sin(Time.time * moveSpeed) * moveDistance;
            transform.position = initialPosition + (moveAxis.normalized * offset);
        }
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (isDestroyed) return;

        currentHealth -= damage;
        StartCoroutine(HitFlashRoutine());

        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position, 0.75f);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private IEnumerator HitFlashRoutine()
    {
        if (meshRenderers == null) yield break;

        for (int i = 0; i < meshRenderers.Length; i++)
        {
            if (meshRenderers[i] != null && meshRenderers[i].material != null && meshRenderers[i].material.HasProperty("_Color"))
            {
                meshRenderers[i].material.color = hitFlashColor;
            }
        }

        yield return new WaitForSeconds(0.08f);

        for (int i = 0; i < meshRenderers.Length; i++)
        {
            if (meshRenderers[i] != null && originalColors != null && i < originalColors.Length)
            {
                if (meshRenderers[i].material != null && meshRenderers[i].material.HasProperty("_Color"))
                {
                    meshRenderers[i].material.color = originalColors[i];
                }
            }
        }
    }

    private void Die()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        if (destroySound != null)
        {
            AudioSource.PlayClipAtPoint(destroySound, transform.position, 1f);
        }

        if (destructionVfx != null)
        {
            Instantiate(destructionVfx, transform.position, Quaternion.identity);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TargetHit();
        }

        gameObject.SetActive(false);
    }

    public float GetTimeBonusSeconds()
    {
        return timeBonusSeconds;
    }

    public void ResetTarget()
    {
        isDestroyed = false;
        currentHealth = maxHealth;
        transform.position = initialPosition;
        gameObject.SetActive(true);
    }
}
