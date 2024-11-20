using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    [SerializeField] float speed = 1f;
    private void Update()
    {
        var axisX = Input.GetAxis("Horizontal");
        var axisY = Input.GetAxis("Vertical");

        transform.position += new Vector3(axisX,  0, axisY).normalized * Time.deltaTime * speed;
    }
}
