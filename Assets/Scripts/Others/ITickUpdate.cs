using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITickUpdate
{
    public virtual void EarlyTickUpdate(float dt) { }    // before TickUpdate -> capture result of 1 tick
    public virtual void TickUpdate(float dt) { }        // change object states
    public virtual void LateTickUpdate(float dt) { }    // after TickUpdate -> reset buffer
}
