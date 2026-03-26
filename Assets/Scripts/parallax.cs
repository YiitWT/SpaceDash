using UnityEngine;

public class parallax : MonoBehaviour
{
    [Header("Parallax Settings")]
    public float speed = 0.5f;
    
    [Header("Direction")]
    public bool scrollVertical = true;
    public bool scrollHorizontal = false;
    
    private Material mat;
    private Vector2 offset;
    
    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            mat = renderer.material;
        }
        else
        {
            Debug.LogError("No Renderer component found on " + gameObject.name);
        }
    }

    void Update()
    {
        if (mat == null) return;
        
        float verticalOffset = scrollVertical ? Time.time * speed : 0f;
        float horizontalOffset = scrollHorizontal ? Time.time * speed : 0f;
        
        offset = new Vector2(horizontalOffset, verticalOffset);
        
        mat.mainTextureOffset = offset;
    }
    
    void OnDestroy()
    {
        if (mat != null)
        {
            mat.mainTextureOffset = Vector2.zero;
        }
    }
}