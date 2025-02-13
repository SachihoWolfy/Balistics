using System.Collections;
using UnityEngine;

public class BallisticJumper : MonoBehaviour
{
    public Transform target;
    public float launchAngle = 45f;   // Launch angle in degrees
    public float jumpDelay = 1f;      // Delay before jumping
    public float maxLaunchSpeed = 50f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        StartCoroutine(PrepareJump());
    }

    IEnumerator PrepareJump()
    {
        if (target == null)
        {
            Debug.LogError("No target assigned!");
            yield break;
        }

        FaceTarget(target);
        yield return new WaitForSeconds(jumpDelay);

        if (!JumpToTarget(target))
        {
            Debug.LogWarning("Jump calculation failed!");
        }
    }

    void FaceTarget(Transform target)
    {
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0; // Only rotate on Y-axis
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = targetRotation;
        }
    }

    bool JumpToTarget(Transform target)
    {
        Vector3 start = transform.position;
        Vector3 end = target.position;
        Vector3 displacement = end - start;

        float gravity = -Physics.gravity.y;
        float yOffset = displacement.y;
        Vector3 horizontalDisplacement = new Vector3(displacement.x, 0, displacement.z);
        float horizontalDistance = horizontalDisplacement.magnitude;

        float angleRad = launchAngle * Mathf.Deg2Rad;

        // Debug output
        Debug.Log($"Jump Debug: Distance={horizontalDistance}, Height={yOffset}");

        // Ensure a valid denominator
        float tanAngle = Mathf.Tan(angleRad);
        float cosAngleSq = Mathf.Pow(Mathf.Cos(angleRad), 2);

        float denominator = 2 * (yOffset - tanAngle * horizontalDistance) * cosAngleSq;

        if (denominator <= 0)
        {
            Debug.LogWarning("Invalid denominator! Switching to alternative calculation.");

            // Alternative: Use a simplified projectile motion formula
            float timeToTarget = horizontalDistance / (Mathf.Cos(angleRad) * maxLaunchSpeed);
            float requiredVerticalSpeed = (yOffset + 0.5f * gravity * timeToTarget * timeToTarget) / timeToTarget;

            if (float.IsNaN(requiredVerticalSpeed) || float.IsInfinity(requiredVerticalSpeed))
            {
                Debug.LogError("Alternative calculation failed! Jump is impossible.");
                return false;
            }

            Vector3 velocityAlt = horizontalDisplacement.normalized * maxLaunchSpeed;
            velocityAlt.y = requiredVerticalSpeed;
            rb.velocity = velocityAlt;
            return true;
        }

        // Normal ballistic calculation
        float initialSpeedSquared = (gravity * horizontalDistance * horizontalDistance) / denominator;
        if (initialSpeedSquared <= 0)
        {
            Debug.LogWarning("Invalid speed calculation!");
            return false;
        }

        float initialSpeed = Mathf.Sqrt(initialSpeedSquared);
        if (initialSpeed > maxLaunchSpeed)
        {
            Debug.LogWarning($"Speed {initialSpeed} too high! Clamping to {maxLaunchSpeed}.");
            initialSpeed = maxLaunchSpeed;
        }

        Debug.Log($"Calculated Jump Speed: {initialSpeed}");

        // Compute velocity
        Vector3 velocity = horizontalDisplacement.normalized * (initialSpeed * Mathf.Cos(angleRad));
        velocity.y = initialSpeed * Mathf.Sin(angleRad);

        // Apply velocity
        rb.isKinematic = false;
        rb.velocity = velocity;

        return true;
    }
}
