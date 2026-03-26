using UnityEngine;

public class ObstacleController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float destroyY = -10f;
    
    private bool hasHitPlayer = false;


    void Update()
    {
        transform.Translate(Vector3.down * moveSpeed * Time.deltaTime);
        
        if (transform.position.y < destroyY)
        {
            Destroy(gameObject);
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasHitPlayer)
        {
            hasHitPlayer = true;

            Destroy(gameObject);
        }
    }
}
