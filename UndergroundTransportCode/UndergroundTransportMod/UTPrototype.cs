using Mafi.Core.Entities.Static.Layout;
using Mafi.Core.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Mafi.Core.Prototypes.Proto;

namespace UndergroundTransportMod
{
    public partial class PrototypeIDs
    {
        public partial class LocalEntities
        {
            public static readonly UTPrototype.ID LooseUndergroundTransportID = new UTPrototype.ID("LooseUndergroundTransport");
            public static readonly UTPrototype.ID FlatUndergroundTransportID = new UTPrototype.ID("FlatUndergroundTransport");
            public static readonly UTPrototype.ID PipeUndergroundTransportID = new UTPrototype.ID("PipeUndergroundTransport");
        }
    }

    public class UTPrototype : LayoutEntityProto, IProto
    {
        public UTPrototype(UTPrototype.ID id, Str strings, EntityLayout layout, EntityCosts costs, Gfx graphics)
             : base(id, strings, layout, costs, graphics)
        {
        }

        public enum TransportType
        {
            Unkown,
            Fluid,
            Loose,
            Unit
        }

        public override Type EntityType => typeof(UndergroundTransport);

        public int maxDistance;
        public int maxHeightDifference;
        public bool singleProduct;
        public TransportType transportType;
    }
}
