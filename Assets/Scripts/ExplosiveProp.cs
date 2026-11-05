using System.Collections;
using UnityEngine;

/// <summary>
/// Destructible explosive barrel or prop for interactive combat environments.
/// When destroyed, triggers an area-of-effect explosion damaging nearby targets.
/// </summary>
public class ExplosiveProp : MonoBehaviour
{
    [Header("Explosive Settings")]
    [SerializeField] private float health = 25f;
    [SerializeField] private float explosionRadius = 6f;
    [SerializeField] private float explosionDamage = 120f;
    [SerializeField] private float explosionForce = 700f;

    [Header("Feedback")]
    [SerializeField] private GameObject explosionVfx;
    [SerializeField] private AudioClip explosionSound;

    private bool exploded = false;

    public void TakeDamage(float damage)
    {
        if (exploded) return;

        health -= damage;
        if (health <= 0f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        exploded = true;

        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1f);
        }

        if (explosionVfx != null)
        {
            Instantiate(explosionVfx, transform.position, Quaternion.identity);
        }

        // Damage nearby targets and rigidbodies
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider col in colliders)
        {
            if (col.gameObject == gameObject) continue;

            // Damage Target component
            Target target = col.GetComponentInParent<Target>();
            if (target != null)
            {
                target.TakeDamage(explosionDamage, col.transform.position, Vector3.up);
            }
            else if (col.gameObject.layer == 12)
            {
                col.gameObject.SetActive(false);
                if (GameManager.Instance != null) GameManager.Instance.TargetHit();
            }

            // Chain react with other explosive props
            ExplosiveProp otherProp = col.GetComponentInParent<ExplosiveProp>();
            if (otherProp != null && otherProp != this)
            {
                otherProp.TakeDamage(explosionDamage);
            }

            // Apply physical blast force
            Rigidbody rb = col.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
        }

        gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}

