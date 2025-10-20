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
        public string Name => typeof(UndergroundTransportMod).Assembly.GetName().Name;

        public int Version => (typeof(UndergroundTransportMod).Assembly.GetName().Version.Major * 100) +
                                (typeof(UndergroundTransportMod).Assembly.GetName().Version.Minor * 10) +
                                (typeof(UndergroundTransportMod).Assembly.GetName().Version.Build);

        public static Version ModVersion => typeof(UndergroundTransportMod).Assembly.GetName().Version;
        public bool IsUiOnly => false;

        public Option<IConfig> ModConfig { get; }

        public void ChangeConfigs(Lyst<IConfig> configs)
        {
        }

        public void Initialize(DependencyResolver resolver, bool gameWasLoaded)
        {
            LogWrite.Info($"Initializing Version = {Version}");
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
