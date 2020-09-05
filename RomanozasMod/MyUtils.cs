using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
// using System.Threading.Tasks;
using ColossalFramework;
using ColossalFramework.Plugins;
using UnityEngine;

namespace RomanozasMod
{
    public class Street
    {

        List<MyBuilding> _buildings = new List<MyBuilding>();
        Vector3? _beginPosition = null;

        public Vector3 BeginPosition {
            get {
                if (_beginPosition == null)
                    _beginPosition = getBeginPosition();
                return _beginPosition.Value;
            }
        }

        private Vector3 getBeginPosition() {
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
                
                for (int s = 0; s < endNode.CountSegments(); s++) {
                    ushort newSegmentId = endNode.GetSegment(s);
                    if (segmentId != newSegmentId && netManager.IsSameName(segmentId, newSegmentId) && !visitedSegments.Contains(newSegmentId)) {
                        segmentId = newSegmentId;
                        nextSegmentFound = true;
                        
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
                            
                            resNode = netManager.m_nodes.m_buffer[netManager.m_segments.m_buffer[segmentId].GetOtherNode(segment.m_startNode)];
                            break;
                        }
                    }
                }
                if (!nextSegmentFound) {
                    break;
                }
            }
            while (true);
            return resNode.Value.m_position;
        }

        public List<MyBuilding> Buildings {
            get {
                //if (_buildings == null)
                //    _buildings = getBuildings();
                return _buildings;
            }
        }

        //private List<MyBuilding> getBuildings() {
        //    ushort[] closeSegments = new ushort[16];
        //    List<MyBuilding> buildings = new List<MyBuilding>();
        //    BuildingManager buildingManager = Singleton<BuildingManager>.instance;
        //    NetManager netManager = Singleton<NetManager>.instance;
        //    DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Buildings Count: " + buildingManager.m_buildingCount);
        //    DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Buildings Length: " + buildingManager.m_buildings.m_buffer.Length);
        //    ushort i = 0;
        //    foreach (Building building in buildingManager.m_buildings.m_buffer) {
        //        if ((building.m_flags & Building.Flags.Created) == Building.Flags.Created && (building.m_flags & Building.Flags.Completed) == Building.Flags.Completed) {
        //            int closeSegmentCount;
        //            netManager.GetClosestSegments(building.m_position, closeSegments, out closeSegmentCount);
        //            for (int sc = 0; sc < closeSegmentCount; sc++) {
        //                ushort segmentId = closeSegments[sc];
        //                NetInfo info = netManager.m_segments.m_buffer[(int)segmentId].Info;
        //                if (info.m_class.m_service == ItemClass.Service.Road) {
        //                    string segmentName = netManager.GetSegmentName(segmentId);
        //                    if (!string.IsNullOrEmpty(segmentName)) {
        //                        if (segmentName == Name)
        //                            buildings.Add(new MyBuilding(i, building));
        //                        break;
        //                    }
        //                }
        //            }
        //        }
        //        i++;
        //    }
        //    return buildings;
        //}

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
        public string FullNewName;

        InstanceID instanceId = new InstanceID();
        int spNumber = 0, loNumber = 0, unNumber = 0;

        public void SetStreetNumber(string streetName, int number) {
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            DistrictManager districtManager = Singleton<DistrictManager>.instance;

            string fullOldName = buildingManager.GetBuildingName(BuildingIndex, instanceId);
            string oldName;
            if(!string.IsNullOrEmpty(fullOldName)) {
                int colonPos = fullOldName.IndexOf(',');
                if(colonPos < 0)
                    colonPos = fullOldName.Length;
                oldName = fullOldName.Substring(0, colonPos);
            }
            else
                oldName = null;

            string newName = oldName;
            //if (oldName != null) {
            //    string[] fullNameParts = oldName.Split(',');
            //    if (fullNameParts.Length > 0)
            //        typeName = fullNameParts[0];
            //};

            Building building = buildingManager.m_buildings.m_buffer[BuildingIndex];
            // bool customized = fullOldName != null && (fullOldName.Contains(" im. ") || (fullOldName.Contains("\"")));
            BuildingAI buildingAI = building.Info.m_buildingAI;

            if ((string.IsNullOrEmpty(newName) /*||*/ /* zawsze renumeruj szkoły */ /*buildingAI is SchoolAI*/) /*&& !customized*/) {

                switch (building.Info.GetService()) {
                    case ItemClass.Service.Beautification: newName = "Park"; break;
                    case ItemClass.Service.PoliceDepartment: newName = "Posterunek"; break;
                    case ItemClass.Service.FireDepartment: newName = "Remiza"; break;
                    case ItemClass.Service.HealthCare:
                        if (buildingAI is CemeteryAI) {
                            if ((buildingAI as CemeteryAI).m_graveCount > 0)
                                newName = "Cmentarz";
                            else
                                newName = "Krematorium";
                            // DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, string.Format("Burial rate {0}, corpse capacity {1}, grave count {2}", (buildingAI as CemeteryAI).m_burialRate, (buildingAI as CemeteryAI).m_corpseCapacity, (buildingAI as CemeteryAI).m_graveCount));
                        }
                        else
                            newName = "Przychodnia";
                        break;
                    case ItemClass.Service.Education:
                        if (buildingAI is SchoolAI) {
                            SchoolAI ai = buildingAI as SchoolAI;
                            int max = new int[] { ai.m_workPlaceCount0, ai.m_workPlaceCount1, ai.m_workPlaceCount2, ai.m_workPlaceCount3 }.Max();
                            if (max == ai.m_workPlaceCount0) {
                               // if (!customized)
                                    newName = $"SP {++spNumber}";
                            }
                            else
                                if (max == ai.m_workPlaceCount1) {
                              //  if (!customized)
                                    newName = $"{LoadingExtension.toRoman(++loNumber)} LO";
                            }
                            else
                                if (max == ai.m_workPlaceCount2)
                                newName = $"{LoadingExtension.toRoman(++unNumber)} Uniwersytet";
                            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, string.Format("Student count {0}, m_workPlaceCount0 {1}, m_workPlaceCount1 {2}", (buildingAI as SchoolAI).m_studentCount, (buildingAI as SchoolAI).m_workPlaceCount0, (buildingAI as SchoolAI).m_workPlaceCount1));
                            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "buildingAI type: " + buildingAI.GetType().ToString());
                            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "subservice: " + building.Info.GetSubService().ToString());
                            ////DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "customized: " + customized);
                            ////DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "typeName: " + typeName);
                        };
                        break;
                    case ItemClass.Service.Residential:
                        switch (building.Info.GetSubService()) {
                            case ItemClass.SubService.ResidentialLow: newName = "Dom"; break;
                            case ItemClass.SubService.ResidentialHigh: newName = "Kamienica"; break;
                        }
                        break;
                    case ItemClass.Service.Office:
                        newName = "Biuro"; break;
                    case ItemClass.Service.Commercial:
                        switch (building.Info.GetSubService()) {
                            case ItemClass.SubService.CommercialLeisure: newName = "Restauracja"; break;
                            case ItemClass.SubService.CommercialTourist: newName = "Hotel"; break;
                            case ItemClass.SubService.CommercialLow: newName = "Sklep"; break;
                            case ItemClass.SubService.CommercialHigh: newName = "Dom Handlowy"; break;
                            default: newName = "Sklep"; break;
                        }
                        break;
                    case ItemClass.Service.Industrial:
                        switch (building.Info.GetSubService()) {
                            case ItemClass.SubService.IndustrialGeneric: newName = "Fabryka"; break;
                            case ItemClass.SubService.IndustrialFarming: newName = "Gospodarstwo"; break;
                            case ItemClass.SubService.IndustrialForestry: newName = "Leśnictwo"; break;
                            case ItemClass.SubService.IndustrialOre: newName = "Huta"; break;
                            case ItemClass.SubService.IndustrialOil: newName = "Rafineria"; break;
                        };
                        break;
                    case ItemClass.Service.Garbage:
                        newName = "Wysypisko";
                        break;
                }
            }

            //if(fullOldName.IsNullOrWhiteSpace())
            //    fullOldName = typeName;

            if (!string.IsNullOrEmpty(newName)) {
                int districtId = (int)districtManager.GetDistrict(building.m_position);
                string districtName;
                if (districtId == 0)
                    districtName = Singleton<SimulationManager>.instance.m_metaData.m_CityName;
                else
                    districtName = districtManager.GetDistrictName(districtId);

                //string fullNewName;
                if (districtId == 0)
                    FullNewName = $"{newName}, {streetName} {number}";
                else
                    FullNewName = $"{newName}, {streetName} {number} ({districtName})";

                if (fullOldName != FullNewName) {
                    buildingManager.StartCoroutine(buildingManager.SetBuildingName(BuildingIndex, FullNewName));
                    //var res = buildingManager.SetBuildingName(BuildingIndex, newName);
                    //while (res.MoveNext()) { } // CitizenManager.instance.StartCoroutine(CitizenManager.instance.SetCitizenName(id, name));
                }
            }
        }
    }

    public static class MyUtils
    {
        internal static void FillBuildings(List<Street> streets) {

            ushort[] closeSegments = new ushort[16];
            List<MyBuilding> buildings = new List<MyBuilding>();
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            NetManager netManager = Singleton<NetManager>.instance;
            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Buildings Count: " + buildingManager.m_buildingCount);
            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Buildings Length: " + buildingManager.m_buildings.m_buffer.Length);
            ushort i = 0;
            foreach (Building building in buildingManager.m_buildings.m_buffer) {
                if ((building.m_flags & Building.Flags.Created) == Building.Flags.Created && (building.m_flags & Building.Flags.Completed) == Building.Flags.Completed) {
                    int closeSegmentCount;
                    netManager.GetClosestSegments(building.m_position, closeSegments, out closeSegmentCount);
                    for (int sc = 0; sc < closeSegmentCount; sc++) {
                        ushort segmentId = closeSegments[sc];
                        NetInfo info = netManager.m_segments.m_buffer[(int)segmentId].Info;
                        if (info.m_class.m_service == ItemClass.Service.Road) {
                            string segmentName = netManager.GetSegmentName(segmentId);
                            if (!string.IsNullOrEmpty(segmentName)) {
                                addBuilding(streets, segmentName, i, building);
                                //if (segmentName == Name)
                                //    buildings.Add(new MyBuilding(i, building));
                                break;
                            }
                        }
                    }
                }
                i++;
            }
            //return buildings;
        }

        private static void addBuilding(List<Street> streets, string streetName, ushort i, Building building) {
            Street street = streets.Find((s) => s.Name == streetName);
            if (street != null)
                street.Buildings.Add(new MyBuilding(i, building));
        }

        internal static List<Street> GetStreets() {
            List<Street> streets = new List<Street>();
            NetManager netManager = Singleton<NetManager>.instance;
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Segment count: " + netManager.m_segmentCount);
            ushort i = 0;
            foreach (NetSegment netSegment in netManager.m_segments.m_buffer) {
                if (((netSegment.m_flags & NetSegment.Flags.Created) != NetSegment.Flags.None) && ((netSegment.m_flags & NetSegment.Flags.Deleted) == NetSegment.Flags.None)) {
                    NetInfo info = netSegment.Info;
                    if (info.m_class.m_service == ItemClass.Service.Road) {
                        string streetName = netManager.GetSegmentName(i);
                        if (!string.IsNullOrEmpty(streetName))
                            addSegmentToStreet(streets, streetName, i);
                    }
                }
                i++;
            }
            return streets;
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

        internal static void SaveStreets(List<Street> streets)
        {
            List<string> lines = new List<string>();
            lines.Add("Streets:");
            lines.Add("");

            //foreach (Street s in streets)
            //    lines.Add(s.Name);

            //lines.Add(Environment.NewLine);
            lines.AddRange(streets.Select(s => s.Name).OrderBy(s => s));

            lines.Add("");
            lines.Add("Schools:");
            lines.Add("");

            lines.AddRange(streets
                .SelectMany(s => s.Buildings)
                .Where(b => b.Building.Info.GetService() == ItemClass.Service.Education)
                .Select(b => b.FullNewName)
                .OrderBy(s => s));

            File.WriteAllLines("D:\\city.txt", lines.ToArray());
        }
    }

}
