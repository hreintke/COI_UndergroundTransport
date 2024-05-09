using Mafi.Core.Products;
using Mafi.Unity.InputControl.Inspectors;
using Mafi.Unity.UiFramework.Components;
using Mafi.Unity.UserInterface.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndergroundTransportMod
{
    public class UTWindowView : StaticEntityInspectorBase<UndergroundTransport>
    {
        private readonly UTInspector _inspector;

        public UTWindowView(UTInspector inspector) : base(inspector)
        {
            _inspector = inspector;
        }

        protected override UndergroundTransport Entity => _inspector.SelectedEntity;

    }
}