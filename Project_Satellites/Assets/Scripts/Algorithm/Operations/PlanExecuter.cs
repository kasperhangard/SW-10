﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;
using System.Linq;

public static class PlanExecuter
{
    public static void ExecutePlan(INode myNode, PlanRequest request)
    {
        if (request.DestinationID != myNode.ID)
        {
            return;
        }
        else
        {
            myNode.State = Node.NodeState.EXECUTING;
            
            if (myNode.executingPlan)
            {
                myNode.State = Node.NodeState.PASSIVE;
                return; // Ignore Execute command if already executing which stops the execute communication loop
            }
            else
            {
                myNode.executingPlan = true;
            }

            ForwardRequest(myNode, request);

            //Set my targetposition to the position I was assigned in the plan
            myNode.TargetPosition = request.Plan.Entries.Find(entry => entry.NodeID == myNode.ID).Position;

            // <--- TODO: Maybe not necessary to do discovery after execution,
            // we can just assume everything is according to the plan and update networkmap based on that. --->
            // Entries in active plan that are also in the new plan
            List<ConstellationPlanEntry> activeEntries = new List<ConstellationPlanEntry>();
            // Entries in new plan
            List<ConstellationPlanEntry> newEntries = request.Plan.Entries;
            // IDs of entries in the new plan
            IEnumerable<uint?> newEntryIDs = request.Plan.Entries.Select(entry => entry.NodeID);
            // Fill out activeEntries
            foreach (ConstellationPlanEntry entry in myNode.ActivePlan.Entries) {
                if (newEntryIDs.Contains(entry.NodeID)) {
                    activeEntries.Add(entry);
                }
            }

            // Order by ID pre-zip
            activeEntries = activeEntries.OrderBy(entry => entry.NodeID).ToList();
            newEntries = newEntries.OrderBy(entry => entry.NodeID).ToList();
            // Zip active and new entries together on NodeID including Position of them both
            IEnumerable<Tuple<uint?, Vector3, Vector3>> entriesZipped = Enumerable.Zip(activeEntries, newEntries, (ae, ne) => new Tuple<uint?, Vector3, Vector3>(ae.NodeID, ae.Position, ne.Position));
            // Distance nodes have to travel based on active and new plan
            IEnumerable<Tuple<uint?, float>> travelDistanceByID = entriesZipped.Select(entry => new Tuple<uint?, float>(entry.Item1, Vector3.Distance(entry.Item2, entry.Item3)));
            // Find max travel distance and ID of node that has to travel that
            float maxTravelDistance = travelDistanceByID.Max(x => x.Item2);
            uint? maxTravelID = travelDistanceByID.Single(x => x.Item2 == maxTravelDistance).Item1;

            // If the found ID is this node's, then discovery should be started when the node is at its new location.
            if (maxTravelID == myNode.ID)
            {
                DiscoveryIfNewNeighboursAfterExecuting(myNode);
            }

            // <--- TODO: Maybe not necessary to do discovery after execution,
            // we can just assume everything is according to the plan and update networkmap based on that. --->

            myNode.ActivePlan = request.Plan;

            myNode.Router.ClearNetworkMap();
            myNode.Router.UpdateNetworkMap(request.Plan);

            Thread.Sleep(Constants.ONE_SECOND_IN_MILLISECONDS / Constants.TIME_SCALE);
            myNode.State = Node.NodeState.PASSIVE;

            // <--- TODO: Maybe set passive and executingplan = false when target position is reached instead of right away? --->
        }
    }

    private static async void DiscoveryIfNewNeighboursAfterExecuting(INode myNode)
    {
        while (Vector3.Distance(myNode.Position, myNode.TargetPosition) > 0.01f)
        {
            await Task.Delay(100 / Constants.TIME_SCALE);
        }

        // If ReachableNodes contains any that are not in networkmap neighbours -> Any new neighbours
        if (myNode.CommsModule.Discover().Except(myNode.Router.NetworkMap.GetEntryByID(myNode.ID).Neighbours).Count() > 0)
        {
            Discovery.StartDiscovery(myNode, true);
        }
    }

    private static void ForwardRequest(INode myNode, PlanRequest request) {
        PlanRequest newRequest = request.DeepCopy();
        uint? nextSeq = myNode.Router.NextSequential(myNode, newRequest.Dir);

        if (nextSeq == null) {
            newRequest.Dir = newRequest.Dir == Router.CommDir.CW ? Router.CommDir.CCW : Router.CommDir.CW;
            nextSeq = myNode.Router.NextSequential(myNode, newRequest.Dir);
        }

        if (nextSeq != null) {
            newRequest.SourceID = myNode.ID;
            newRequest.DestinationID = nextSeq;
            uint? nextHop = myNode.Router.NextHop(myNode.ID, nextSeq);
            myNode.CommsModule.Send(nextHop, newRequest);
        }
    }
}

