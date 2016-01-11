﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        // from: https://github.com/Rychard/CityWebServer/ & https://github.com/Alakaiser/Cities-Skylines-Stat-Button/blob/master/City%20Statistics%20Button/StatButton.cs
        private void btnRename_Click(UIComponent component, UIMouseEventParameter eventParam) {

            // SimulationManager.instance.SimulationPaused = true;
            // Debug.developerConsoleVisible = true;
            try {
                DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, "Started");
                DistrictManager districtManager = Singleton<DistrictManager>.instance;
                Dictionary<int, string> districtNames = new Dictionary<int, string>();
                District[] districts = districtManager.m_districts.m_buffer;
                int i = 0;
                foreach (District d in districts) {
                    string districtName;
                    if (i == 0)
                        districtName = SimulationManager.instance.m_metaData.m_CityName; // "Luizjana"; // city name: https://github.com/skwasjer/CSAutosave/blob/master/Autosave.cs
                    else
                        districtName = districtManager.GetDistrictName(i);
                    if (d.IsValid() && d.IsAlive() && districtName != null)
                        districtNames[i] = districtManager.GetDistrictName(i);
                    i++;
                }
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "District list build");
                Dictionary<int, int> districtBuildingCount = new Dictionary<int, int>();
                BuildingManager buildingManager = Singleton<BuildingManager>.instance;
                ushort j = 0;
                foreach (Building building in buildingManager.m_buildings.m_buffer) {
                    if (building.m_flags != Building.Flags.None) {
                        string typeName = null;
                        switch (building.Info.GetService()) {
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
                                    default: typeName = "Sklep"; break;
                                }
                                break;
                            case ItemClass.Service.Industrial:
                                switch (building.Info.GetSubService()) {
                                    case ItemClass.SubService.IndustrialGeneric: typeName = "Fabryka"; break;
                                    case ItemClass.SubService.IndustrialFarming: typeName = "Gospodarstwo"; break;
                                    case ItemClass.SubService.IndustrialForestry: typeName = "Leśnictwo"; break;
                                };
                                break;
                        }
                    if (typeName != null) {
                        int districtId = (int)districtManager.GetDistrict(building.m_position);
                            // multi key dictionary: http://stackoverflow.com/questions/1171812/multi-key-dictionary-in-c
                            if (districtNames.ContainsKey(districtId)) {

                            if (districtBuildingCount.ContainsKey(districtId))
                                districtBuildingCount[districtId]++;
                            else
                                districtBuildingCount[districtId] = 1;
                            int lastCount = districtBuildingCount[districtId];

                            string districtName = districtNames[districtId];
                            string newName = string.Format("{0}, {1} {2}", typeName, districtName, lastCount);

                            var res = buildingManager.SetBuildingName(j, newName);
                            while (res.MoveNext()) { } // CitizenManager.instance.StartCoroutine(CitizenManager.instance.SetCitizenName(id, name));
                        }
                        else
                            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "District name not found for districtId = " + districtId);
                    }
                }
                j++;
            }
                // send message
                MessageManager.instance.QueueMessage(new Message("Nowe numery domów! Znowu trzeba zmieniać pieczątki :("));
        }
            catch (Exception ex) {
                DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, ex.Message);
            }
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
