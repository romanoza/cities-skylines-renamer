using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace RomanozasMod
{
    public class LoadingExtension : LoadingExtensionBase
    {
        // from: https://github.com/Alakaiser/Cities-Skylines-Stat-Button/blob/master/City%20Statistics%20Button/StatButton.cs
        public override void OnLevelLoaded(LoadMode mode) {

            var uiView = UIView.GetAView();
            var btnRename = (UIButton)uiView.AddUIComponent(typeof(UIButton));

            btnRename.width = 140;
            btnRename.height = 30;

            btnRename.normalBgSprite = "ButtonMenu";
            btnRename.hoveredBgSprite = "ButtonMenuHovered";
            btnRename.focusedBgSprite = "ButtonMenuFocused";
            btnRename.pressedBgSprite = "ButtonMenuPressed";
            btnRename.textColor = new Color32(186, 217, 238, 0);
            btnRename.disabledTextColor = new Color32(7, 7, 7, 255);
            btnRename.hoveredTextColor = new Color32(7, 132, 255, 255);
            btnRename.focusedTextColor = new Color32(255, 255, 255, 255);
            btnRename.pressedTextColor = new Color32(30, 30, 44, 255);

            btnRename.transformPosition = new Vector3(1.2f, -0.93f);
            btnRename.BringToFront();

            btnRename.text = "Rename Buildings!";
            btnRename.playAudioEvents = true;

            btnRename.eventClick += btnRename_Click;
        }

        ushort[] _closeSegments = new ushort[16];

        // from: https://github.com/Rychard/CityWebServer/ & https://github.com/Alakaiser/Cities-Skylines-Stat-Button/blob/master/City%20Statistics%20Button/StatButton.cs
        private void btnRename_Click(UIComponent component, UIMouseEventParameter eventParam) {
            // SimulationManager.instance.SimulationPaused = true;
            // Debug.developerConsoleVisible = true;
            rename2();
        }

        void rename2() {
            try {
                DateTime start = DateTime.Now;
                List<Street> streets = MyUtils.GetStreets();
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "GetStreets() " + (DateTime.Now - start));
                MyUtils.FillBuildings(streets);
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "FillBuildings() " + (DateTime.Now - start));
                foreach (Street street in streets) {
                    int buildingNo = 1;
                    street.Buildings.Sort((b1, b2) =>
                    {
                        float m1 = Vector3.SqrMagnitude(b1.Position - street.BeginPosition);
                        float m2 = Vector3.SqrMagnitude(b2.Position - street.BeginPosition);
                        return m1 < m2 ? -1 : (m1 == m2 ? 0 : 1);
                    });
                    foreach (MyBuilding building in street.Buildings/*.OrderBy(b => Vector3.SqrMagnitude(b.Position - street.BeginPosition))*/) {
                        building.SetStreetNumber(street.Name, buildingNo);
                        buildingNo++;
                    }
                    DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, $"SetStreetNumber({street.Name}) " + (DateTime.Now - start));
                }
                MyUtils.SaveStreets(streets);
                MessageManager.instance.QueueMessage(new Message("Ojoj! Nowe numery domów! Znowu trzeba zmieniać pieczątki :("));
            }
            catch(Exception ex) {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Error, ex.ToString());
            }
        }

        void rename() {
            try {
                DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, "Started");
                DistrictManager districtManager = Singleton<DistrictManager>.instance;

                Dictionary<int, string> districtNames = new Dictionary<int, string>();
                District[] districts = districtManager.m_districts.m_buffer;
                int i = 0;
                foreach (District d in districts) {
                    string districtName;
                    if (i == 0) {
                        districtName = Singleton<SimulationManager>.instance.m_metaData.m_CityName;  // SimulationManager.instance.m_metaData.m_CityName; // "Luizjana"; // city name: https://github.com/skwasjer/CSAutosave/blob/master/Autosave.cs
                        DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Nazwa distr 0 = " + districtName);
                    }
                    else
                        districtName = districtManager.GetDistrictName(i);
                    if (d.IsValid() && d.IsAlive() && districtName != null)
                        districtNames[i] = districtName.Replace("\"", string.Empty); // usuń z nazw dzielnic cudzysłowia
                    i++;
                }

                NetManager netManager = Singleton<NetManager>.instance;
                NetSegment[] segments = netManager.m_segments.m_buffer;

                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "District list build");
                Dictionary<int, int> districtBuildingCount = new Dictionary<int, int>();
                BuildingManager buildingManager = Singleton<BuildingManager>.instance;
                ushort j = 0;
                InstanceID instanceId = new InstanceID();
                int spNumber = 0, loNumber = 0, unNumber = 0;
                foreach (Building building in buildingManager.m_buildings.m_buffer) {
                    if ((building.m_flags & Building.Flags.Created) == Building.Flags.Created && (building.m_flags & Building.Flags.Completed) == Building.Flags.Completed) {

                        // string fullName = buildingManager.GetBuildingName(j, instanceId);
                        string oldName = buildingManager.GetBuildingName(j, instanceId);
                        string typeName = null;
                        //if (oldName != null) {
                        //    string[] fullNameParts = oldName.Split(',');
                        //    if (fullNameParts.Length > 0)
                        //        typeName = fullNameParts[0];
                        //};

                        bool customized = oldName != null && (oldName.Contains(" im. ") || (oldName.Contains("\"")));
                        BuildingAI buildingAI = building.Info.m_buildingAI;

                        if ((string.IsNullOrEmpty(typeName) || /* zawsze renumeruj szkoły */ buildingAI is SchoolAI) && !customized) {

                            switch (building.Info.GetService()) {
                                case ItemClass.Service.Beautification: typeName = "Park"; break;
                                case ItemClass.Service.PoliceDepartment: typeName = "Posterunek"; break;
                                case ItemClass.Service.FireDepartment: typeName = "Remiza"; break;
                                case ItemClass.Service.HealthCare:
                                    if (buildingAI is CemeteryAI) {
                                        if ((buildingAI as CemeteryAI).m_graveCount > 0)
                                            typeName = "Cmentarz";
                                        else
                                            typeName = "Krematorium";
                                        // DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, string.Format("Burial rate {0}, corpse capacity {1}, grave count {2}", (buildingAI as CemeteryAI).m_burialRate, (buildingAI as CemeteryAI).m_corpseCapacity, (buildingAI as CemeteryAI).m_graveCount));
                                    }
                                    else
                                        typeName = "Przychodnia";
                                    break;
                                case ItemClass.Service.Education:
                                    if (buildingAI is SchoolAI) {
                                        SchoolAI ai = buildingAI as SchoolAI;
                                        int max = new int[] { ai.m_workPlaceCount0, ai.m_workPlaceCount1, ai.m_workPlaceCount2, ai.m_workPlaceCount3 }.Max();
                                        if (max == ai.m_workPlaceCount0) {
                                            if (!customized)
                                                typeName = $"SP {++spNumber}";
                                        }
                                        else
                                            if (max == ai.m_workPlaceCount1) {
                                            if (!customized)
                                                typeName = $"{toRoman(++loNumber)} LO";
                                        }
                                        else
                                            if (max == ai.m_workPlaceCount2)
                                            typeName = $"{toRoman(++unNumber)} Uniwersytet";
                                        //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, string.Format("Student count {0}, m_workPlaceCount0 {1}, m_workPlaceCount1 {2}", (buildingAI as SchoolAI).m_studentCount, (buildingAI as SchoolAI).m_workPlaceCount0, (buildingAI as SchoolAI).m_workPlaceCount1));
                                        //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "buildingAI type: " + buildingAI.GetType().ToString());
                                        //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "subservice: " + building.Info.GetSubService().ToString());
                                        ////DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "customized: " + customized);
                                        ////DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "typeName: " + typeName);
                                    };
                                    break;
                                case ItemClass.Service.Residential:
                                    switch (building.Info.GetSubService()) {
                                        case ItemClass.SubService.ResidentialLow: typeName = "Dom"; break;
                                        case ItemClass.SubService.ResidentialHigh: typeName = "Kamienica"; break;
                                    }
                                    break;
                                case ItemClass.Service.Office:
                                    typeName = "Biuro"; break;
                                case ItemClass.Service.Commercial:
                                    switch (building.Info.GetSubService()) {
                                        case ItemClass.SubService.CommercialLeisure: typeName = "Restauracja"; break;
                                        case ItemClass.SubService.CommercialTourist: typeName = "Hotel"; break;
                                        case ItemClass.SubService.CommercialLow: typeName = "Sklep"; break;
                                        case ItemClass.SubService.CommercialHigh: typeName = "Dom Handlowy"; break;
                                        default: typeName = "Sklep"; break;
                                    }
                                    break;
                                case ItemClass.Service.Industrial:
                                    switch (building.Info.GetSubService()) {
                                        case ItemClass.SubService.IndustrialGeneric: typeName = "Fabryka"; break;
                                        case ItemClass.SubService.IndustrialFarming: typeName = "Gospodarstwo"; break;
                                        case ItemClass.SubService.IndustrialForestry: typeName = "Leśnictwo"; break;
                                        case ItemClass.SubService.IndustrialOre: typeName = "Huta"; break;
                                        case ItemClass.SubService.IndustrialOil: typeName = "Rafineria"; break;
                                    };
                                    break;
                            }
                        }

                        string segmentName = null;

                        if (typeName != null) {
                            int districtId = (int)districtManager.GetDistrict(building.m_position);
                            ushort segmentId;
                            int closeSegmentCount;

                            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Looking for segments");
                            netManager.GetClosestSegments(building.m_position, _closeSegments, out closeSegmentCount);

                            for (int sc = 0; sc < closeSegmentCount; sc++) {
                                segmentId = _closeSegments[sc];
                                NetInfo info = Singleton<NetManager>.instance.m_segments.m_buffer[(int)segmentId].Info;

                                if (info.m_class.m_service == ItemClass.Service.Road) {
                                    segmentName = netManager.GetSegmentName(segmentId);
                                    if (!string.IsNullOrEmpty(segmentName))
                                        break;
                                }
                            }

                            //segmentId = building.FindSegment(ItemClass.Service.Road, ItemClass.SubService.None, ItemClass.Layer.Default);
                            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "segmentId: " + segmentId);
                            //segmentName = netManager.GetSegmentName(segmentId);
                            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "segmentName: " + segmentName);

                            // multi key dictionary: http://stackoverflow.com/questions/1171812/multi-key-dictionary-in-c
                            if (districtNames.ContainsKey(districtId)) {

                                if (districtBuildingCount.ContainsKey(districtId))
                                    districtBuildingCount[districtId]++;
                                else
                                    districtBuildingCount[districtId] = 1;
                                int lastCount = districtBuildingCount[districtId];
                                // int lastCount = 10;

                                //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "lastCount: " + lastCount);
                                //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "flags: " + building.m_flags.ToString());

                                string districtName = districtNames[districtId];
                                string streetName = segmentName;

                                string newName;
                                if (!string.IsNullOrEmpty(streetName))
                                    newName = string.Format("{0}, {1}, {2} {3}", typeName, streetName, districtName, lastCount);
                                else
                                    newName = string.Format("{0}, {1} {2}", typeName, districtName, lastCount);

                                if (!customized && oldName != newName) {
                                    var res = buildingManager.SetBuildingName(j, newName);
                                    while (res.MoveNext()) { } // CitizenManager.instance.StartCoroutine(CitizenManager.instance.SetCitizenName(id, name));
                                }
                            }
                            else
                                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "District name not found for districtId = " + districtId);
                        }
                    }
                    j++;
                    //yield return null;
                }
                // send message
                MessageManager.instance.QueueMessage(new Message("Ojoj! Nowe numery domów! Znowu trzeba zmieniać pieczątki :("));
            }
            catch (Exception ex) {
                DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, ex.Message);
            }
        }

        public static string toRoman(int number, bool upperCase = true) {
            if (number < 0)
                throw new ArgumentOutOfRangeException("number", number, "Liczba musi być większa od zera.");
            string[] romans = new string[] { "I", "IV", "V", "IX", "X", "XL", "L", "XC", "C", "CD", "D", "CM", "M" };
            // string[] romansLower = new string[] {"i", "iv", "v", "ix", "x", "xl", "l", "xc", "c", "cd", "d", "cm", "m"};

            int[] numbers = new int[] { 1, 4, 5, 9, 10, 40, 50, 90, 100, 400, 500, 900, 1000 };
            int j = 12;
            string result = "";
            // string[] romans = upperCase? romansUpper: romansLower; // za romansUpper i romansLower powstawiac konstruktory tablic - new string[] {}
            while (number != 0) {
                if (number >= numbers[j]) {
                    number -= numbers[j];
                    result += romans[j];
                }
                else
                    j--;
            }
            if (!upperCase)
                result = result.ToLower();
            return result;
        }

        private Dictionary<int, int> GetBuildingBreakdownByDistrict() {
            var districtManager = Singleton<DistrictManager>.instance;

            Dictionary<int, int> districtBuildings = new Dictionary<int, int>();
            BuildingManager instance = Singleton<BuildingManager>.instance;
            foreach (Building building in instance.m_buildings.m_buffer) {
                if (building.m_flags == Building.Flags.None) { continue; }
                var districtID = (int)districtManager.GetDistrict(building.m_position);
                if (districtBuildings.ContainsKey(districtID)) {
                    districtBuildings[districtID]++;
                }
                else {
                    districtBuildings.Add(districtID, 1);
                }
            }
            return districtBuildings;
        }

    }

    public static class CitizenExtensions
    {
        public static String GetName(this Citizen citizen) {
            return Singleton<CitizenManager>.instance.GetCitizenName(citizen.m_instance);
        }
    }

    public static class DistrictExtensions
    {
        public static Boolean IsValid(this District district) {
            return (district.m_flags != District.Flags.None);
        }

        public static Boolean IsAlive(this District district) {
            // Get the flags on the district, to ensure we don't access garbage memory if it doesn't have a flag for District.Flags.Created
            Boolean alive = ((district.m_flags & District.Flags.Created) == District.Flags.Created);
            return alive;
        }

        //public static PopulationGroup[] GetPopulation(this District district) {
        //    PopulationGroup[] ageGroups =
        //    {
        //        new PopulationGroup("Children", district.GetChildrenCount()),
        //        new PopulationGroup("Teen", district.GetTeenCount()),
        //        new PopulationGroup("YoungAdult", district.GetYoungAdultCount()),
        //        new PopulationGroup("Adult", district.GetAdultCount()),
        //        new PopulationGroup("Senior", district.GetSeniorCount())
        //    };
        //    return ageGroups;
        //}

        public static int GetChildrenCount(this District district) {
            return (int)district.m_childData.m_finalCount;
        }

        public static int GetTeenCount(this District district) {
            return (int)district.m_teenData.m_finalCount;
        }

        public static int GetYoungAdultCount(this District district) {
            return (int)district.m_youngData.m_finalCount;
        }

        public static int GetAdultCount(this District district) {
            return (int)district.m_adultData.m_finalCount;
        }

        public static int GetSeniorCount(this District district) {
            return (int)district.m_seniorData.m_finalCount;
        }
    }

    public class DistrictInfo
    {
        public int DistrictID { get; set; }

        public String DistrictName { get; set; }

        //public PopulationGroup[] PopulationData { get; set; }

        public int TotalPopulationCount { get; set; }

        public int TotalBuildingCount { get; set; }

        public int TotalVehicleCount { get; set; }

        public int CurrentHouseholds { get; set; }

        public int AvailableHouseholds { get; set; }

        public int CurrentJobs { get; set; }

        public int AvailableJobs { get; set; }

        public int WeeklyTouristVisits { get; set; }

        public int AverageLandValue { get; set; }

        public Double Pollution { get; set; }

        //public PolicyInfo[] Policies { get; set; }

        public static IEnumerable<int> GetDistricts() {
            var districtManager = Singleton<DistrictManager>.instance;

            // This is the value used in Assembly-CSharp, so I presume that's the maximum number of districts allowed.
            const int count = 128;

            var districts = districtManager.m_districts.m_buffer;

            for (int i = 0; i < count; i++) {

                DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, "district  " + i);
                DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, "district isalive " + districts[i].IsAlive());
                string name = districtManager.GetDistrictName(i);
                DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, "district name " + name);

                // if (!districts[i].IsAlive()) { continue; }
                if (!districts[i].IsValid()) { continue; }
                yield return i;
            }
        }

        public static DistrictInfo GetDistrictInfo(int districtID) {
            var districtManager = Singleton<DistrictManager>.instance;
            var district = GetDistrict(districtID);

            if (!district.IsValid()) { return null; }

            String districtName = String.Empty;

            if (districtID == 0) {
                // The district with ID 0 is always the global district.
                // It receives an auto-generated name by default, but the game always displays the city name instead.
                districtName = "City";
            }
            else {
                districtName = districtManager.GetDistrictName(districtID);
            }

            var pollution = Math.Round((district.m_groundData.m_finalPollution / (Double)byte.MaxValue), 2);

            var model = new DistrictInfo
            {
                DistrictID = districtID,
                DistrictName = districtName,
                TotalPopulationCount = (int)district.m_populationData.m_finalCount,
                //      PopulationData = GetPopulationGroups(districtID),
                CurrentHouseholds = (int)district.m_residentialData.m_finalAliveCount,
                AvailableHouseholds = (int)district.m_residentialData.m_finalHomeOrWorkCount,
                CurrentJobs = (int)district.m_commercialData.m_finalAliveCount + (int)district.m_industrialData.m_finalAliveCount + (int)district.m_officeData.m_finalAliveCount + (int)district.m_playerData.m_finalAliveCount,
                AvailableJobs = (int)district.m_commercialData.m_finalHomeOrWorkCount + (int)district.m_industrialData.m_finalHomeOrWorkCount + (int)district.m_officeData.m_finalHomeOrWorkCount + (int)district.m_playerData.m_finalHomeOrWorkCount,
                AverageLandValue = district.GetLandValue(),
                Pollution = pollution,
                WeeklyTouristVisits = (int)district.m_tourist1Data.m_averageCount + (int)district.m_tourist2Data.m_averageCount + (int)district.m_tourist3Data.m_averageCount,
                //    Policies = GetPolicies().ToArray(),
            };
            return model;
        }

        private static District GetDistrict(int? districtID = null) {
            if (districtID == null) { districtID = 0; }
            var districtManager = Singleton<DistrictManager>.instance;
            var district = districtManager.m_districts.m_buffer[districtID.Value];
            return district;
        }

        //private static PopulationGroup[] GetPopulationGroups(int? districtID = null) {
        //    var district = GetDistrict(districtID);
        //    return district.GetPopulation();
        //}

        //private static IEnumerable<PolicyInfo> GetPolicies() {
        //    var policies = EnumHelper.GetValues<DistrictPolicies.Policies>();
        //    var districtManager = Singleton<DistrictManager>.instance;

        //    foreach (var policy in policies) {
        //        String policyName = Enum.GetName(typeof(DistrictPolicies.Policies), policy);
        //        Boolean isEnabled = districtManager.IsCityPolicySet(policy);
        //        yield return new PolicyInfo
        //        {
        //            Name = policyName,
        //            Enabled = isEnabled
        //        };
        //    }
        //}
    }
}
