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

namespace UndergroundTransportMod
{
    public class MDRegistrator : IModData
    {
        public void RegisterData(ProtoRegistrator registrator)
        {
            Proto.Str ps = Proto.CreateStr(PrototypeIDs.LocalEntities.UndergroundTransportID, "MineDumpControl", "A building to control a mining and dumping in an area");

            EntityLayout el = registrator.LayoutParser.ParseLayoutOrThrow(
                "   [2][2][2]   ",
                "   [2][2][2]A@+",
                "   [2][2][2]   "
                );


            EntityCostsTpl ecTpl = new EntityCostsTpl.Builder().CP3(100);
            EntityCosts ec = ecTpl.MapToEntityCosts(registrator);

            LayoutEntityProto.Gfx lg =
                 new LayoutEntityProto.Gfx("Empty",
                customIconPath: "Empty",

                categories: new ImmutableArray<ToolbarCategoryProto>?(registrator.GetCategoriesProtos(Ids.ToolbarCategories.Landmarks)))
                ;

            UTPrototype bp =
                new UTPrototype(
                    PrototypeIDs.LocalEntities.UndergroundTransportID, ps, el, ec, lg);
            registrator.PrototypesDb.Add(bp);
        }
    }
}
