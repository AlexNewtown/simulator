/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utilities;

public class MapManagerData
{
    public float connectionProximity { get; private set; } = 1.0f;

    public List<MapLane> GetTrafficLanes()
    {
        var mapHolder = Object.FindObjectOfType<MapHolder>();
        if (mapHolder == null)
        {
            Debug.LogError("missing MapHolder, please add MapHolder.cs component to map object and set holder transforms");
            return null;
        }
        var trafficLanesHolder = mapHolder.trafficLanesHolder;

        var lanes = new List<MapLane>(trafficLanesHolder.transform.parent.GetComponentsInChildren<MapLane>());
        ProcessLaneData(lanes);

        var trafficLanes = new List<MapLane>(trafficLanesHolder.GetComponentsInChildren<MapLane>());
        foreach (var lane in trafficLanes)
            lane.isTrafficLane = true;

        var laneSections = new List<MapLaneSection>(trafficLanesHolder.transform.GetComponentsInChildren<MapLaneSection>());
        ProcessLaneSections(laneSections);

        var stopLines = new List<MapLine>();
        var allMapLines = new List<MapLine>(trafficLanesHolder.transform.parent.GetComponentsInChildren<MapLine>());
        foreach (var line in allMapLines)
        {
            if (line.lineType == MapData.LineType.STOP)
                stopLines.Add(line);
        }
        ProcessStopLineData(stopLines, lanes);
        return trafficLanes;
    }

    public List<MapIntersection> GetIntersections()
    {
        var mapHolder = Object.FindObjectOfType<MapHolder>();
        if (mapHolder == null)
        {
            Debug.LogError("missing MapHolder, please add MapHolder.cs component to map object and set holder transforms");
            return null;
        }
        var intersectionsHolder = mapHolder.intersectionsHolder;

        var intersections = new List<MapIntersection>(intersectionsHolder.GetComponentsInChildren<MapIntersection>());
        ProcessIntersectionData(intersections);
        return intersections;
    }

    private void ProcessLaneData(List<MapLane> lanes)
    {
        foreach (var lane in lanes) // convert local to world pos
        {
            lane.mapWorldPositions.Clear();
            foreach (var localPos in lane.mapLocalPositions)
                lane.mapWorldPositions.Add(lane.transform.TransformPoint(localPos));
        }

        foreach (var lane in lanes) // set connected lanes
        {
            var lastPt = lane.transform.TransformPoint(lane.mapLocalPositions[lane.mapLocalPositions.Count - 1]);
            foreach (var altLane in lanes)
            {
                var firstPt = altLane.transform.TransformPoint(altLane.mapLocalPositions[0]);
                if ((lastPt - firstPt).magnitude < connectionProximity)
                    lane.nextConnectedLanes.Add(altLane);
            }
        }
    }

    public static float GetTotalLaneDistance(List<MapLane> lanes)
    {
        var totalLaneDist = 0f;

        foreach (var lane in lanes)
            totalLaneDist += Vector3.Distance(lane.mapWorldPositions[0], lane.mapWorldPositions[lane.mapWorldPositions.Count - 1]);  // calc value for npc count

        return totalLaneDist;
    }

    private void ProcessLaneSections(List<MapLaneSection> laneSections)
    {
        foreach (var section in laneSections)
            section.SetLaneData();
    }

    private void ProcessStopLineData(List<MapLine> stopLines, List<MapLane> lanes)
    {
        foreach (var line in stopLines) // convert local to world pos
        {
            line.mapWorldPositions.Clear();
            foreach (var localPos in line.mapLocalPositions)
                line.mapWorldPositions.Add(line.transform.TransformPoint(localPos));
        }

        foreach (var line in stopLines) // set stop lines
        {
            List<Vector2> stopline2D = line.mapWorldPositions.Select(p => new Vector2(p.x, p.z)).ToList();

            foreach (var lane in lanes)
            {
                // check if any points intersect with segment
                List<Vector2> intersects = new List<Vector2>();
                var lanes2D = lane.mapWorldPositions.Select(p => new Vector2(p.x, p.z)).ToList();
                var lane2D = new List<Vector2>();
                lane2D.Add(lanes2D[lanes2D.Count - 1]);
                bool isIntersected = Utility.CurveSegmentsIntersect(stopline2D, lane2D, out intersects);
                bool isClose = Utility.IsPointCloseToLine(stopline2D[0], stopline2D[stopline2D.Count - 1], lanes2D[lanes2D.Count - 1], connectionProximity);
                if (isIntersected || isClose)
                    lane.stopLine = line;
            }
        }
    }

    private void ProcessIntersectionData(List<MapIntersection> intersections)
    {
        intersections.ForEach(intersection => intersection.SetIntersectionData());
    }
}