using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasController : MonoBehaviour,ITickUpdate
{
    //public void OnAfterTick(float dt, bool skip) 
    //{ 
    //    gameObject.SetActive(false);
    //}


    public void TickUpdate(float dt, bool skip)
    {
        gameObject.SetActive(false);
    }
    public void LateTickUpdate(float dt, bool skip)
    {
        gameObject.SetActive(true);
    }

}
