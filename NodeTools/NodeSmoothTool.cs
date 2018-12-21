using System;
using UnityEngine;
using ColossalFramework;
using System.Collections.Generic;
using ColossalFramework.UI;

namespace NodeTools
{
    public class NodeSelectionTool : ToolBase
    {
        public static NodeSelectionTool instance;

        ushort m_selection1;
        ushort m_selection2;
        ushort m_hover;
        public List<ushort> m_nodes = new List<ushort>();
        List<ushort> m_segments = new List<ushort>();

        // quality of life functions because I am lazy and I hate typing Singleton<NetManager>.instance.m_nodes.m_buffer[nodeName] whenever I want to get a node
        public NetManager Manager {
            get { return Singleton<NetManager>.instance; }
        }
        public NetNode GetNode(ushort id){
            return Manager.m_nodes.m_buffer[id];
        }
        public NetSegment GetSegment(ushort id)
        {
            return Manager.m_segments.m_buffer[id];
        }
        public void Reset()
        {
            m_selection1 = m_selection2 = m_hover = 0;
            m_nodes.Clear();
            m_segments.Clear();
        }
        public void ThrowError(string message){
            ExceptionPanel panel = UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel");
            panel.SetMessage("Segment Slope Smoother", message, false);
        }
        public void Smooth()
        {
            // Validate nodes
            // This process finds the connections of every selected node.
            // If there is only one connection, a node is an endpoint.
            // If there are more than two connections for any given node, the path is invalid.
            // If there are more than two endpoints, the path is invalid.
            List<ushort> unsortedNodes = m_nodes;
            if(m_nodes.Count < 3){
                ThrowError("Validation error: Not enough nodes! You need at least three nodes to complete this process.");
                return;
            }
            List<List<ushort>> fragmentXZ = new List<List<ushort>>();
            List<ushort> endpoints = new List<ushort>();
            for (int i = 0; i < unsortedNodes.Count; i++){
                NetNode node = GetNode(unsortedNodes[i]);
                List<ushort> connections = new List<ushort>();
                connections.Add(unsortedNodes[i]);
                for (int j = 0; j < unsortedNodes.Count; j++){
                    if (i == j) continue;

                    if(node.IsConnectedTo(unsortedNodes[j])){
                        connections.Add(unsortedNodes[j]);
                    }

                    if(connections.Count > 3){ // the node itself, then both connections
                        ThrowError("Validation error: Too many connections! Each node should be only connected by segments to at most two other nodes.");
                        return;
                    }
                }

                if(connections.Count == 2){ // the node itself, and one connection
                    endpoints.Add(unsortedNodes[i]);
                }

                if(connections.Count == 1){
                    ThrowError("Validation error: No connections! Each node needs at least one other segment connection.");
                    return;
                }
                fragmentXZ.Add(connections);
            }

            // Sort nodes
            // We now have a two-dimensional list that resembles the following:
            // fragmentXZ = [[2, 1, 3], [1, 2], [3, 2]]
            // The first element of each list is the node's specific ushort id, and the next two elements are its connections.
            // Now all we have to do is connect the dots. 
            // Let's take the last point on the far right as an example. Since node with id 3 is connected to the node with id 2, then we'll jump to node 2. 
            // Node 2 is connected to node 3, but we just came from there, so we'll jump to node 1 instead.
            // Node 1 is an endpoint (has less than three elements) so we're done. Yay!
            List<ushort> sortedNodes = new List<ushort>();
            bool incomplete = true;
            int index = -1;

            // Find starting endpoint and immediately jump to and add the second node. The other endpoint will be where we end.
            for (int i = 0; i < fragmentXZ.Count; i++){
                if(fragmentXZ[i].Count == 2){
                    index = fragmentXZ.FindIndex(e => e[0] == fragmentXZ[i][1]);
                    sortedNodes.Add(fragmentXZ[i][0]);
                    sortedNodes.Add(fragmentXZ[i][1]);
                    if(index == -1){
                        ThrowError("Sort error: Invalid path! Endpoint is connected to undefined node.");
                        return;
                    }
                    break;
                }
            }
            while(incomplete){
                for (int i = 0; i < fragmentXZ.Count; i++){
                    for (int j = 1; j <= 2; j++){
                        if ((fragmentXZ[i][0] == fragmentXZ[index][j] && !sortedNodes.Contains(fragmentXZ[index][j]))){
                            sortedNodes.Add(fragmentXZ[i][0]);
                            if(fragmentXZ[i].Count == 2){
                                incomplete = false;
                                break;
                            }
                            index = i;
                        }
                    }
                }
            }
            m_nodes = sortedNodes;

            // Dynamically add segments for mathematical calculation
            // And now, all we have to do is 
            for (var i = 0; i < m_nodes.Count-1; i++){
                for (var j = 0; j < GetNode(m_nodes[i]).CountSegments(); j++){
                    ushort segmentID = GetNode(m_nodes[i]).GetSegment(j);
                    NetSegment testedSegment = GetSegment(segmentID);
                    if((testedSegment.m_startNode == m_nodes[i] || testedSegment.m_endNode == m_nodes[i]) && (testedSegment.m_startNode == m_nodes[i+1] || testedSegment.m_endNode == m_nodes[i+1])){
                        m_segments.Add(segmentID);
                        break;
                    }
                }
            }

            /*
             * Slope Calculation
             * 
             * This is a little more complicated than it would seem, as not all segments are the same length and most importantly there is no real way to figure out curves.
             * Therefore, I chose not to calculate the smoothed height of each segment not by its position in 3D space 
             * (which would be heckin difficult and would involve some extremely painful math), but instead calculated y as a function of length.
             * The height at a given point in the series of nodes = y = N(l_n]) = ((yf-y0)/l)(l_n) + y0, where:
             * l_n is the length up to the point
             * yf is the final position of the final node
             * y0 is the initial position
             * l is the total length
             * 
             * (yf-y0)/l is the calculated slope from solving for the linear equation. 
             * Since y is a function of l_n the y-intercept is, by logic, the initial position, y0
             * 
             * The equation follows the general form of a linear equation: y = mx + b
             * 
             * Edit: I have later discovered that NetSegment.m_averageLength refers to the combined 3D length of the segment, not the linear length that I require above. (Why wouldn't it?) The above formula still approximates lengths in 3D space, however, when iterated several times. Hooray :}
             * If I were somehow able to get the angle of elevation (angular slope) of the segment, I could calculate the linear length of the segments, but for now I can only approximate it.
             * 
             * Edit 2: I'm dumb. Just use basic geometry. Pythagoras is rolling in his grave.
             */

            float totalLength = 0;
            List<float> segmentLinearLengthsXZ = new List<float>();
            for (int i = 0; i < m_segments.Count; i++)
            {
                NetSegment calcSegment = GetSegment(m_segments[i]);
                float linearDistanceXZ = (float) Math.Sqrt(Math.Pow(GetNode(calcSegment.m_startNode).m_position.y - GetNode(calcSegment.m_endNode).m_position.y, 2) + Math.Pow(calcSegment.m_averageLength, 2));
                totalLength += linearDistanceXZ;
                segmentLinearLengthsXZ.Add(linearDistanceXZ);

            }
            float incrementLength = 0;
            NetNode startNode = GetNode(m_nodes[0]);
            NetNode endNode = GetNode(m_nodes[m_nodes.Count-1]);
            for (int i = 0; i < m_nodes.Count; i++)
            {
                float Nln = ((endNode.m_position.y - startNode.m_position.y) / totalLength) * incrementLength + startNode.m_position.y;
                QuickMove(m_nodes[i], Nln);
                if (i != m_nodes.Count - 1) incrementLength += segmentLinearLengthsXZ[i];
            }

            Reset();
        }

        public void QuickMove(ushort node, float height)
        {
            Vector3 currentPosition = GetNode(node).m_position;
            Manager.MoveNode(node, new Vector3(currentPosition.x, height, currentPosition.z));
            Manager.UpdateNode(node); // not sure if this is necessary but I put it here as a test before and it *seemed* to work
        }

        protected override void OnToolUpdate()
        {
            base.OnToolUpdate();

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastInput input = new RaycastInput(ray, Camera.main.farClipPlane);
            input.m_ignoreNodeFlags = NetNode.Flags.None;
            input.m_ignoreSegmentFlags = NetSegment.Flags.None;

            input.m_ignoreParkFlags = DistrictPark.Flags.All;
            input.m_ignorePropFlags = PropInstance.Flags.All;
            input.m_ignoreTreeFlags = TreeInstance.Flags.All;
            input.m_ignoreCitizenFlags = CitizenInstance.Flags.All;
            input.m_ignoreVehicleFlags = Vehicle.Flags.Created;
            input.m_ignoreBuildingFlags = Building.Flags.All;
            input.m_ignoreDisasterFlags = DisasterData.Flags.All;
            input.m_ignoreTransportFlags = TransportLine.Flags.All;
            input.m_ignoreParkedVehicleFlags = VehicleParked.Flags.All;
            input.m_ignoreTerrain = true;
            RayCast(input, out RaycastOutput output);
            m_hover = output.m_netNode;
            if(m_hover == 0 && output.m_netSegment != 0){
                Bounds bounds = GetNode(GetSegment(output.m_netSegment).m_startNode).m_bounds;
                if(bounds.IntersectRay(ray)){
                    m_hover = GetSegment(output.m_netSegment).m_startNode;
                    output.m_netSegment = 0;
                }

                bounds = GetNode(GetSegment(output.m_netSegment).m_endNode).m_bounds;
                if (bounds.IntersectRay(ray))
                {
                    m_hover = GetSegment(output.m_netSegment).m_endNode;
                }
            }

            if (m_hover != 0 && !m_nodes.Contains(m_hover))
            {
                if (Input.GetMouseButtonUp(0))
                {
                    m_nodes.Add(m_hover);
 
                }
                if (Input.GetMouseButtonUp(1))
                {
                    m_nodes.Remove(m_hover);
                }
            }
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            base.RenderOverlay(cameraInfo);
            if(enabled == true){
                if (m_hover != 0)
                {
                    NetNode hoveredNode = GetNode(m_hover);

                    // kinda stole this color from Move It!
                    // thanks to SamsamTS because they're a UI god
                    RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, m_nodes.Contains(m_hover) ? new Color(1f, 0f, 0f, 1f) : new Color(0f, 0f, 0f, 1f), hoveredNode.m_position, 15f, hoveredNode.m_position.y - 1f, hoveredNode.m_position.y + 1f, true, true);
                }
                for (var i = 0; i < m_nodes.Count; i++){
                    if(m_nodes[i] != 0){
                        NetNode selectNode = GetNode(m_nodes[i]);
                        RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, new Color(0f, 0f, 0f, 0.8f), selectNode.m_position, 15f, selectNode.m_position.y - 1f, selectNode.m_position.y + 1f, true, true);
                    }
                }
            }
        }
    }
}
