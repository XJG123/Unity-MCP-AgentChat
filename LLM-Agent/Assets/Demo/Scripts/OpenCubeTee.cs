using UnityEngine;

public class OpenCubeTee : MonoBehaviour
{
    public static OpenCubeTee Instance;

    void Awake()
    {
        Instance = this;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public  GameObject CubeTe;
    public  void OpenCubeTees()
    {
        CubeTe.SetActive(true);
    }
}
