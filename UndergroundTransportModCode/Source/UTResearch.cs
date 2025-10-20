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
                .Start("UndergroundTransport", PrototypeIDs.Research.UnlockUndergroundTransport, 60, "UndergroundTransport")
                .Description("Underground Transport")
                .AddLayoutEntityToUnlock(PrototypeIDs.LocalEntities.FlatUndergroundTransportID)
                .AddLayoutEntityToUnlock(PrototypeIDs.LocalEntities.LooseUndergroundTransportID)
                .AddLayoutEntityToUnlock(PrototypeIDs.LocalEntities.PipeUndergroundTransportID)
                .SetGridPosition(registrator.PrototypesDb.GetOrThrow<ResearchNodeProto>(Ids.Research.ConveyorBeltsT2).GridPosition + new Vector2i(12, 0))
                .AddParents( registrator.PrototypesDb.GetOrThrow<ResearchNodeProto>(Ids.Research.ConveyorBeltsT2))
                .BuildAndAdd();
        }
    }
}
