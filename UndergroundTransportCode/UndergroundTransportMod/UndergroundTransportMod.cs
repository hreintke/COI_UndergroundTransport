using Mafi;
using Mafi.Collections;
using Mafi.Core.Game;
using Mafi.Core.Mods;
using System;
using Mafi.Core.Prototypes;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mafi.Base;
using Mafi.Core.Factory.Transports;
using Mafi.Serialization;

namespace UndergroundTransportMod
{
    public sealed class UndergroundTransportMod : IMod
    {
        public string Name => "UndergroundTransportMod";

        public int Version => 2;
        public static Version ModVersion = new Version(0, 0, 4);
        public bool IsUiOnly => false;

        public Option<IConfig> ModConfig { get; }

        public void ChangeConfigs(Lyst<IConfig> configs)
        {
        }

        public void Initialize(DependencyResolver resolver, bool gameWasLoaded)
        {
            LogWrite.Info("Initializing ");
        }

        public void RegisterDependencies(DependencyResolverBuilder depBuilder, ProtosDb protosDb, bool gameWasLoaded)
        {
            LogWrite.Info("Register Dependencies ");
        }

        public void RegisterPrototypes(ProtoRegistrator registrator)
        {
            LogWrite.Info("Registrating Prototypes");
            registrator.RegisterData<UTRegistrator>();
            registrator.RegisterData<UTResearch>();
        }

        public void EarlyInit(DependencyResolver resolver)
        {
            LogWrite.Info($"EarlyInit");
        }
    }
}
