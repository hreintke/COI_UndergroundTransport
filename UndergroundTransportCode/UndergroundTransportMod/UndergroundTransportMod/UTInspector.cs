using Mafi.Core.Buildings.Towers;
using Mafi.Core.Input;
using Mafi.Core.Terrain;
using Mafi.Unity.InputControl.AreaTool;
using Mafi.Unity.InputControl.Inspectors;
using Mafi.Unity.InputControl;
using Mafi.Unity.Mine;
using Mafi.Unity.Terrain;
using Mafi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndergroundTransportMod
{
    [GlobalDependency(RegistrationMode.AsEverything)]
    public class UTInspector : EntityInspector<UndergroundTransport, UTWindowView>
    {
        private UTWindowView _windowView;
        private readonly ShortcutsManager _shortcutsManager;

        public UTInspector(InspectorContext inspectorContext,
                                     ShortcutsManager shortcutsManager) : base(inspectorContext)
        {
            _windowView = new UTWindowView(this);
            _shortcutsManager = shortcutsManager;
        }

        protected override UTWindowView GetView() => this._windowView;

        public override bool InputUpdate(IInputScheduler inputScheduler)
        {
            return false;
        }
    }
}
