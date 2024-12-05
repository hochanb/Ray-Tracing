using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITickUpdate
{
    public virtual void EarlyTickUpdate(float dt, bool skip) { }    // before TickUpdate -> capture result of 1 tick
    public virtual void TickUpdate(float dt, bool skip) { }        // change object states
    public virtual void LateTickUpdate(float dt, bool skip) { }    // after TickUpdate -> reset buffer
}
