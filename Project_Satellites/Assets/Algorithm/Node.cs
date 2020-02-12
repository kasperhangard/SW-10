﻿using System;
using System.Collections.Generic;
using System.Linq;

public class Node : INode
{
    public int ID { get; set; }
    public List<INode> ReachableNodes { get; set; } // Future Work: Make part of the algorithm that reachable nodes are calculated based on position and a communication distance
    public Position Position { get; set; }
    public Position TargetPosition { get; set; }

    public Node(int ID)
    {
        this.ID = ID;
    }

    public void Communicate(Constants.Commands command)
    {
        if (command != Constants.Commands.Execute)
        {
            throw new Exception("Wrong command"); // Only accept Execute command
        }

        TargetPosition = intermediateTargetPosition;

        if (executingPlan)
        {
            return; // Ignore Execute command if already executing which stops the execute communication loop
        }
        else
        {
            executingPlan = true;
        }

        router.NextHop(this).Communicate(Constants.Commands.Execute);
    }

    public void Communicate(Constants.Commands command, ConstellationPlan plan)
    {
        if (command != Constants.Commands.Generate)
        {
            throw new Exception("Wrong command"); // Only accept Generate command
        }

        if (router == null)
        {
            router = new Router(plan);
        }
        else
        {
            router.UpdateNetworkMap(plan);
        }

        executingPlan = false;

        Dictionary<int, float> fieldDeltaVPairs = new Dictionary<int, float>();

        for (int i = 0; i < plan.entries.Count; i++)
        {
            float requiredDeltaV = Position.Distance(Position, plan.entries[i].Position);
            fieldDeltaVPairs.Add(i, requiredDeltaV);
        }

        if (plan.entries.Any(x => x.Node.ID == ID) == false)
        {
            foreach (KeyValuePair<int, float> pair in fieldDeltaVPairs.OrderBy(x => x.Value))
            {
                if (plan.ReduceBy("DeltaV", pair.Key, pair.Value))
                {
                    plan.entries[pair.Key].Node = this;
                    plan.entries[pair.Key].Fields["DeltaV"].Value = pair.Value;
                    intermediateTargetPosition = plan.entries[pair.Key].Position;
                    plan.lastEditedBy = ID;
                    justChangedPlan = true;
                    break;
                }
            }
        }

        if (plan.lastEditedBy == ID && justChangedPlan == false)
        {
            justChangedPlan = false;
            Communicate(Constants.Commands.Execute);
        }
        else
        {
            justChangedPlan = false;
            router.NextHop(this).Communicate(Constants.Commands.Generate, plan);
        }
    }

    private bool executingPlan;
    private bool justChangedPlan;
    private Position intermediateTargetPosition;
    private IRouter router;
}
