﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Threading;
using Vector3 = System.Numerics.Vector3;
using System.Collections;

public abstract class INode: MonoBehaviour
{
    public abstract Node.NodeState State { get; set; }
    public abstract ConstellationPlan ActivePlan { get; set; }
    public abstract ConstellationPlan GeneratingPlan { get; set; }
    public abstract uint? Id { get; set; }
    public abstract Vector3 Position { get; set; }
    public abstract Vector3 TargetPosition { get; set; }
    public abstract Router Router { get; set; }
    public abstract bool Active { get; set; }
    public abstract bool AutoChecksAllowed { get; set; }
    public abstract int SleepCount { get; set; }
    public ICommunicate CommsModule { get; set; }
    public bool ExecutingPlan;
    public string LastDiscoveryId;
    public Vector3 PlaneNormalDir { get; set; }
    public bool ResettingTimers { get; set; }

    public abstract void GenerateRouter();
    public abstract void Communicate(Request message);
    public abstract IEnumerator ResetTimers();
    public int ReachableNodeCount;
}
