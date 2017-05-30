using System;
using System.Collections.Generic;
// using System.Linq;
using System.Text;
// using System.Threading.Tasks;
using ColossalFramework;
using ColossalFramework.Plugins;
using UnityEngine;

namespace RomanozasMod
{
    public class Street
    {

        List<MyBuilding> _buildings = null;
        Vector3? _beginPosition = null;
        // int[] _segments;

        public Vector3 BeginPosition {
            get {
                if (_beginPosition == null)
                    _beginPosition = getBeginPosition();
                return _beginPosition.Value;
            }
        }

        private Vector3 getBeginPosition() {
            // return new Vector3();
            NetManager netManager = Singleton<NetManager>.instance;
            NetNode? resNode = null;
            ushort segmentId = netSegments[0];
            List<ushort> visitedSegments = new List<ushort>();
            do {
                visitedSegments.Add(segmentId);
                NetSegment segment = netManager.m_segments.m_buffer[segmentId];
                NetNode endNode = netManager.m_nodes.m_buffer[segment.m_endNode];
                if (resNode == null)
                    resNode = endNode;
                bool nextSegmentFound = false;
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "CountSegments: " + endNode.CountSegments());
                for (int s = 0; s < endNode.CountSegments(); s++) {
                    ushort newSegmentId = endNode.GetSegment(s);
                    if (segmentId != newSegmentId && netManager.IsSameName(segmentId, newSegmentId) && !visitedSegments.Contains(newSegmentId)) {
                        segmentId = newSegmentId;
                        nextSegmentFound = true;
                        DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "NextSegmentFound: " + segmentId + ", breaking for");
                        resNode = netManager.m_nodes.m_buffer[netManager.m_segments.m_buffer[segmentId].GetOtherNode(segment.m_endNode)];
                        break;
                    }
                }
                if (!nextSegmentFound) {
                    NetNode startNode = netManager.m_nodes.m_buffer[segment.m_startNode];
                    for (int s = 0; s < startNode.CountSegments(); s++) {
                        ushort newSegmentId = startNode.GetSegment(s);
                        if (segmentId != newSegmentId && netManager.IsSameName(segmentId, newSegmentId) && !visitedSegments.Contains(newSegmentId)) {
                            segmentId = newSegmentId;
                            nextSegmentFound = true;
                            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "NextSegmentFound: " + segmentId + ", breaking for");
                            resNode = netManager.m_nodes.m_buffer[netManager.m_segments.m_buffer[segmentId].GetOtherNode(segment.m_startNode)];
                            break;
                        }
                    }
                }
                if (!nextSegmentFound) {
                    DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "NextSegmentNotFound, breaking while");
                    break;
                }
                //if (segmentId == netSegments[0]) // circle, not necessary
                //    break;
            }
            while (true);
            return resNode.Value.m_position;
        }

        public List<MyBuilding> Buildings {
            get {
                if (_buildings == null)
                    _buildings = getBuildings();
                return _buildings;
            }
        }

        private List<MyBuilding> getBuildings() {
            ushort[] closeSegments = new ushort[16];
            List<MyBuilding> buildings = new List<MyBuilding>();
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            NetManager netManager = Singleton<NetManager>.instance;
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Buildings Count: " + buildingManager.m_buildingCount);
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Buildings Length: " + buildingManager.m_buildings.m_buffer.Length);
            // InstanceID instanceId = new InstanceID();
            ushort i = 0;
            foreach (Building building in buildingManager.m_buildings.m_buffer) {
                // for (ushort i = 0; i < buildingManager.m_buildingCount; i++) {
                // Building building = buildingManager.m_buildings.m_buffer[i];

                if ((building.m_flags & Building.Flags.Created) == Building.Flags.Created && (building.m_flags & Building.Flags.Completed) == Building.Flags.Completed) {
                    //string oldName = buildingManager.GetBuildingName(i, instanceId);
                    // DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Old name: " + oldName);
                    int closeSegmentCount;
                    netManager.GetClosestSegments(building.m_position, closeSegments, out closeSegmentCount);
                    // DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "closeSegmentCount: " + closeSegmentCount);
                    for (int sc = 0; sc < closeSegmentCount; sc++) {
                        ushort segmentId = closeSegments[sc];
                        // DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "segmentId: " + segmentId);
                        NetInfo info = netManager.m_segments.m_buffer[(int)segmentId].Info;
                        // DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "isRoad: " + (info.m_class.m_service == ItemClass.Service.Road));
                        if (info.m_class.m_service == ItemClass.Service.Road) {
                            string segmentName = netManager.GetSegmentName(segmentId);
                            if (!string.IsNullOrEmpty(segmentName)) {
                                if (segmentName == Name)
                                    buildings.Add(new MyBuilding(i, building));
                                break;
                            }
                        }
                    }
                }
                i++;
            }
            return buildings;
        }

        public string Name;

        List<ushort> netSegments = new List<ushort>();

        internal void AddSegment(ushort netSegment) {
            netSegments.Add(netSegment);
        }
    }

    public class MyBuilding
    {
        public MyBuilding(ushort buildingIndex, Building building) {
            BuildingIndex = buildingIndex;
            Building = building;
        }

        public Vector3 Position {
            get {
                return Building.m_position;
            }
        }

        public Building Building;
        public ushort BuildingIndex;

        public void SetStreetNumber(string streetName, int number) {
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            var res = buildingManager.SetBuildingName(BuildingIndex, $"{streetName} {number}");
            while (res.MoveNext()) { }
        }
    }

    public static class MyUtils
    {

        internal static Street[] GetStreets() {
            List<Street> streets = new List<Street>();
            NetManager netManager = Singleton<NetManager>.instance;
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Segment count: " + netManager.m_segmentCount);
            ushort i = 0;
            foreach (NetSegment netSegment in netManager.m_segments.m_buffer) {
                // for (ushort i = 0; i < netManager.m_segmentCount; i++) {
                // NetSegment netSegment = netManager.m_segments.m_buffer[i];
                if (((netSegment.m_flags & NetSegment.Flags.Created) != NetSegment.Flags.None) && ((netSegment.m_flags & NetSegment.Flags.Deleted) == NetSegment.Flags.None)) {
                    NetInfo info = netSegment.Info;
                    // string streetName = null;
                    if (info.m_class.m_service == ItemClass.Service.Road) {
                        string streetName = netManager.GetSegmentName(i);
                        if (!string.IsNullOrEmpty(streetName))
                            addSegmentToStreet(streets, streetName, i);
                    }
                }
                //if(streetName != null)
                //    addSegmentToStreet(streets, streetName, i);
                i++;
            }

            return streets.ToArray();
        }

        private static void addSegmentToStreet(List<Street> streets, string streetName, ushort netSegment) {
            Street street = streets.Find((s) => s.Name == streetName);
            if (street == null) {
                street = new Street();
                street.Name = streetName;
                streets.Add(street);
            }
            street.AddSegment(netSegment);
        }
    }

}
