using UnityEngine;

public class BillboardScript : MonoBehaviour
{
    void Update(){
        transform.rotation = Camera.main.transform.rotation;
    }
}
