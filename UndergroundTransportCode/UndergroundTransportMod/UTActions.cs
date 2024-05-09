using Mafi.Collections.ImmutableCollections;
using Mafi.Core.Prototypes;
using Mafi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndergroundTransportMod
{
    [GlobalDependency(RegistrationMode.AsSelf)]
    public class UTActions
    {
        private readonly ProtosDb _protosDb;
        private readonly UnlockedProtosDb _unlockedProtosDb;

        public UTActions(
            ProtosDb protosDb,
            UnlockedProtosDb unlockedProtosDb
        )
        {
            // This unlocks the custom entity at startup
            // Only use during debugging
//            unlockedProtosDb.Unlock(ImmutableArray.Create((IProto)protosDb.Get(PrototypeIDs.LocalEntities.LooseUndergroundTransportID).Value));
//            unlockedProtosDb.Unlock(ImmutableArray.Create((IProto)protosDb.Get(PrototypeIDs.LocalEntities.FlatUndergroundTransportID).Value));
//            unlockedProtosDb.Unlock(ImmutableArray.Create((IProto)protosDb.Get(PrototypeIDs.LocalEntities.PipeUndergroundTransportID).Value));
        }
    }
}
