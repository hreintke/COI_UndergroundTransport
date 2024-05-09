using Mafi.Base;
using Mafi.Collections.ImmutableCollections;
using Mafi.Core.Entities.Static.Layout;
using Mafi.Core.Mods;
using Mafi.Core.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RTG.Object2ObjectSnap;

namespace UndergroundTransportMod
{
    public class UTRegistrator : IModData
    {
        public void RegisterDataOld(ProtoRegistrator registrator)
        {
            Proto.Str ps = Proto.CreateStr(PrototypeIDs.LocalEntities.LooseUndergroundTransportID, "Loose Underground Entry", "A building to start/end an underground Transport");

            EntityLayout el = registrator.LayoutParser.ParseLayoutOrThrow(
                "   [1][1]A~+"
                );

            EntityCostsTpl ecTpl = new EntityCostsTpl.Builder().CP3(100);
            EntityCosts ec = ecTpl.MapToEntityCosts(registrator);

            LayoutEntityProto.Gfx lg =
                 new LayoutEntityProto.Gfx("Assets/Prefabs/LooseEntrance9.prefab",
                customIconPath: "Assets/Icons/LooseEntrance2.png",

                categories: new ImmutableArray<ToolbarCategoryProto>?(registrator.GetCategoriesProtos(Ids.ToolbarCategories.Landmarks)))
                ;

            UTPrototype bp =
                new UTPrototype(
                    PrototypeIDs.LocalEntities.LooseUndergroundTransportID, ps, el, ec, lg);
            bp.maxDistance = 40;
            bp.maxHeightDifference = 20;
            bp.singleProduct = false;
            bp.transportType = UTPrototype.TransportType.Loose;
            registrator.PrototypesDb.Add(bp);
        }

        public void RegisterData(ProtoRegistrator registrator)
        {
            LogWrite.Info("Registrating Undergound Entrances");
            registerEntrance(registrator,
                PrototypeIDs.LocalEntities.LooseUndergroundTransportID,
                "Loose Underground Entry",
                "A building to start/end a loose underground Transport",
                "Assets/UndergroundTransportMod/Prefabs/LooseEntrance38.prefab",
                "Assets/UndergroundTransportMod/Icons/LooseIcon.png",
                50,
                20,
                new EntityCostsTpl.Builder().CP3(50),
                UTPrototype.TransportType.Loose
                );

            registerEntrance(registrator,
                PrototypeIDs.LocalEntities.FlatUndergroundTransportID,
                "Flat Underground Entry",
                "A building to start/end a flat underground Transport",
                "Assets/UndergroundTransportMod/Prefabs/FlatEntrance38.prefab",
                "Assets/UndergroundTransportMod/Icons/FlatIcon.png",
                50,
                20,
                new EntityCostsTpl.Builder().CP3(50),
                UTPrototype.TransportType.Unit
                );

            registerEntrance(registrator,
                PrototypeIDs.LocalEntities.PipeUndergroundTransportID,
                "Pipe Underground Entry",
                "A building to start/end a pipe underground Transport",
                "Assets/UndergroundTransportMod/Prefabs/PipeEntrance38.prefab",
                "Assets/UndergroundTransportMod/Icons/PipeIcon1.png",
                50,
                20,
                new EntityCostsTpl.Builder().CP3(50),
                UTPrototype.TransportType.Fluid
                );
        }

        public void registerEntrance(ProtoRegistrator registrator,
                        UTPrototype.ID id,
                        String shortDescr,
                        String longDescr,
                        String gfx, 
                        String icon, 
                        int maxD,
                        int maxH,
                        EntityCostsTpl ecTpl, 
                        UTPrototype.TransportType tt)
        {
            String x;
            switch (tt)
            {
                case UTPrototype.TransportType.Loose:
                    {
                        x = "~";
                        break;
                    }
                case UTPrototype.TransportType.Unit :
                    {
                        x = "#";
                        break;
                    }
                case UTPrototype.TransportType.Fluid:
                    {
                        x = "@";
                        break;
                    }
                default :
                    {
                        x = " ";
                        break;
                    }
            }

            String layout = "   [1][1]A" + x + "+";
            EntityLayout el = registrator.LayoutParser.ParseLayoutOrThrow(layout);
            EntityCosts ec = ecTpl.MapToEntityCosts(registrator);
            LayoutEntityProto.Gfx lg =
                new LayoutEntityProto.Gfx(gfx, 
                    customIconPath: icon,
                    categories: new ImmutableArray<ToolbarCategoryProto>?(registrator.GetCategoriesProtos(Ids.ToolbarCategories.Transports)));

            Proto.Str ps = Proto.CreateStr(id, shortDescr, longDescr);

            UTPrototype bp =
                new UTPrototype(
                    id, ps, el, ec, lg);
            bp.maxDistance = maxD;
            bp.maxHeightDifference = maxH;
            bp.singleProduct = (tt == UTPrototype.TransportType.Fluid) ? true : false;
            bp.transportType = tt;
            registrator.PrototypesDb.Add(bp);
        }
    }
}
