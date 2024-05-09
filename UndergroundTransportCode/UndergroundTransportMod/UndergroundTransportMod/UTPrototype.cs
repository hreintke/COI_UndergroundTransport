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
            public static readonly UTPrototype.ID UndergroundTransportID = new UTPrototype.ID("UndergroundTransport");
        }
    }

    public class UTPrototype : LayoutEntityProto, IProto
    {
        public UTPrototype(ID id, Str strings, EntityLayout layout, EntityCosts costs, Gfx graphics)
             : base(id, strings, layout, costs, graphics)
        {
        }

        public override Type EntityType => typeof(UndergroundTransport);
    }
}
