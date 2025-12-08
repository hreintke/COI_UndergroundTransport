using HarmonyLib;
using Mafi;
using Mafi.Collections;
using Mafi.Collections.ImmutableCollections;
using Mafi.Core;
using Mafi.Core.Entities.Dynamic;
using Mafi.Core.Entities.Static;
using Mafi.Core.Factory.Transports;
using Mafi.Core.GameLoop;
using Mafi.Core.Map;
using Mafi.Core.PathFinding;
using Mafi.Core.PathFinding.Goals;
using Mafi.Core.Products;
using Mafi.Core.Roads;
using Mafi.Core.Terrain.Designation;
using Mafi.Core.Vehicles;
using Mafi.Core.Vehicles.Jobs;
using Mafi.Core.Vehicles.Trucks;
using Mafi.PathFinding;
using Mafi.Serialization;
using Mafi.Unity;
using Mafi.Unity.InstancedRendering;
using Mafi.Unity.Ui.Controllers.LayoutEntityPlacing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

using UnityEngine;
using static RTG.ObjectAlign;

namespace UndergroundTransportMod;


[GlobalDependency(RegistrationMode.AsSelf)]
[HarmonyPatch]
public class ModPatches
{

    private readonly Harmony harmony;
    private static UTValidator utValidator;
    static private GameLoopEvents gameLoopEvents;
    ModPatches(UTValidator uv, GameLoopEvents gle)
    {
        harmony = new Harmony("UndergroundTransport");

        if (Harmony.HasAnyPatches("UndergroundTransport"))
        {
            LogWrite.Info($"Allready applied , removing UT harmony patches");
            harmony.UnpatchAll("UndergroundTransport");
        }

        harmony.PatchAll();
        LogWrite.Info("Underground harmony patches applied");

        utValidator = uv;
        gameLoopEvents = gle;
        gameLoopEvents.Terminate.AddNonSaveable<ModPatches>(this, terminateEvent);
    }

    void terminateEvent()
    {
        LogWrite.Info("Remove UT Harmony patches");
        harmony.UnpatchAll("UndergroundTransport");
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(StaticEntityMassPlacer), nameof(StaticEntityMassPlacer.Deactivate))]
    static void placerDeactivate()
    {
        utValidator.clearConnectionSigns();    
    }
}

