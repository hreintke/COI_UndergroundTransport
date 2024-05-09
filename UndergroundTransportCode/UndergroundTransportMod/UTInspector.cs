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
using Mafi.Unity;
using Mafi.Unity.InputControl.Inspectors.Vehicles;
using UnityEngine;
using Mafi.Core.Buildings.Mine;

namespace UndergroundTransportMod
{
    [GlobalDependency(RegistrationMode.AsEverything)]
    public class UTInspector : EntityInspector<UndergroundTransport, UTWindowView>
    {
        private UTWindowView _windowView;
        private readonly ShortcutsManager _shortcutsManager;
        private LinesFactory _linesFactory;
        public LineOverlayRendererHelper GoalLineRenderer;

        public UTInspector(InspectorContext inspectorContext,
                                     ShortcutsManager shortcutsManager,
                                     LinesFactory linesFactory) : base(inspectorContext)
        {
            _windowView = new UTWindowView(this);
            _shortcutsManager = shortcutsManager;
            _linesFactory = linesFactory;

            GoalLineRenderer = new LineOverlayRendererHelper(linesFactory);
            GoalLineRenderer.SetWidth(0.5f);
            GoalLineRenderer.SetColor(Color.white);
            GoalLineRenderer.HideLine();
        }

        protected override UTWindowView GetView() => this._windowView;

        public override bool InputUpdate(IInputScheduler inputScheduler)
        {
            return false;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            if (SelectedEntity.isConnected)
            {
                HighlightSecondaryEntity(SelectedEntity.connectedUndergroundTransport.Value);
                GoalLineRenderer.SetColor(SelectedEntity.CurrentState == UndergroundTransport.State.ConnectedError ? Color.red : Color.green);
                GoalLineRenderer.ShowLine(SelectedEntity.Position3f, SelectedEntity.connectedUndergroundTransport.Value.Position3f);
            }
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();
            GoalLineRenderer.HideLine();
        }
    }
}
