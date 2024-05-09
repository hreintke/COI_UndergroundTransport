using Mafi.Core;
using Mafi;
using Mafi.Core.Buildings.Towers;
using Mafi.Core.Entities;
using Mafi.Core.Entities.Static.Layout;
using Mafi.Core.Maintenance;
using Mafi.Core.Population;
using Mafi.Core.Ports;
using Mafi.Core.Ports.Io;
using Mafi.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Mafi.Base.Assets.Base.Tutorials;
using Mafi.Core.Input;
using Mafi.Core.Terrain.Designation;
using Mafi.Core.Terrain;
using static Mafi.Base.Assets.Base.Buildings;

namespace UndergroundTransportMod
{
    [GenerateSerializer(false, null, 0)]
    public class UndergroundTransport : LayoutEntity, IEntityWithSimUpdate, IEntityWithPorts
    {
        public UndergroundTransport(EntityId id, UTPrototype proto, TileTransform transform, EntityContext context) : base(id, proto, transform, context)
        {
            _proto = proto;
        }

        private UTPrototype _proto;

        public new UTPrototype Prototype
        {
            get
            {
                return _proto;
            }
            protected set
            {
                _proto = value;
                base.Prototype = value;
            }
        }

        public override bool CanBePaused => true;

        void IEntityWithSimUpdate.SimUpdate()
        {
        }
        public Quantity ReceiveAsMuchAsFromPort(ProductQuantity pq, IoPortToken sourcePort)
        {
            return pq.Quantity;
        }



    }
}

