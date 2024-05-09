using Mafi.Base;
using Mafi.Core.Mods;
using Mafi.Core.Research;
using Mafi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndergroundTransportMod
{
    public partial class PrototypeIDs
    {
        public partial class Research
        {
            public static readonly ResearchNodeProto.ID UnlockUndergroundTransport = Ids.Research.CreateId("UnlockUndergroundTransport");
        }
    }

    public class UTResearch : IModData
    {
        public void RegisterData(ProtoRegistrator registrator)
        {
            registrator.ResearchNodeProtoBuilder
                .Start("UndergroundTransport", PrototypeIDs.Research.UnlockUndergroundTransport)
                .Description("Underground Transport")
                .AddLayoutEntityToUnlock(PrototypeIDs.LocalEntities.FlatUndergroundTransportID)
                .AddLayoutEntityToUnlock(PrototypeIDs.LocalEntities.LooseUndergroundTransportID)
                .AddLayoutEntityToUnlock(PrototypeIDs.LocalEntities.PipeUndergroundTransportID)
                .SetGridPosition(registrator.PrototypesDb.GetOrThrow<ResearchNodeProto>(Ids.Research.ConveyorBeltsT3).GridPosition + new Vector2i(4, 0))
                .SetCosts(new ResearchCostsTpl(5))
                .AddParents(registrator.PrototypesDb.GetOrThrow<ResearchNodeProto>(Ids.Research.PipeTransportsT3),
                            registrator.PrototypesDb.GetOrThrow<ResearchNodeProto>(Ids.Research.ConveyorBeltsT3))
                .BuildAndAdd();
        }
    }
}
