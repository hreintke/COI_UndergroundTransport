using Mafi;
using Mafi.Core;
using Mafi.Core.Products;
using Mafi.Core.Syncers;
using Mafi.Unity;
using Mafi.Unity.InputControl.Inspectors;
using Mafi.Unity.UiFramework;
using Mafi.Unity.UiFramework.Components;
using Mafi.Unity.UserInterface;
using Mafi.Unity.UserInterface.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UndergroundTransportMod
{
    public class UTWindowView : StaticEntityInspectorBase<UndergroundTransport>
    {
        private readonly UTInspector _inspector;
        private QuantityBar productBufferBar;

        public UTWindowView(UTInspector inspector) : base(inspector)
        {
            _inspector = inspector;
        }

        protected override UndergroundTransport Entity => _inspector.SelectedEntity;

        private Txt statusLabel;

        Btn AddMDButton(StackContainer parent, Action action, string title)
        {
            return Builder.NewBtnGeneral(title, parent)
                .SetButtonStyle(Style.Global.GeneralBtn).SetText(title)
                .OnClick(action)
                .AppendTo(parent, new Vector2(80, 25), ContainerPosition.MiddleOrCenter, Offset.Top(5f));
        }

        protected override void AddCustomItems(StackContainer itemContainer)
        {
            UpdaterBuilder updaterBuilder = UpdaterBuilder.Start();
            base.AddCustomItems(itemContainer);

            StatusPanel statusInfo = AddStatusInfoPanel();
            updaterBuilder.Observe<UndergroundTransport.State>((Func<UndergroundTransport.State>)(() => this.Entity.CurrentState)).Do((Action<UndergroundTransport.State>)(state =>
            {
                switch (state)
                {
                    case UndergroundTransport.State.None:
                        statusInfo.SetStatus(Tr.EntityStatus__Idle, StatusPanel.State.Warning);
                        break;
                    case UndergroundTransport.State.ConnectedIn:
                        statusInfo.SetStatus("Connected In", StatusPanel.State.Ok);
                        break;
                    case UndergroundTransport.State.ConnectedOut:
                        statusInfo.SetStatus("Connected Out", StatusPanel.State.Ok);
                        break;
                    case UndergroundTransport.State.Paused:
                        statusInfo.SetStatusPaused();
                        break;
                    case UndergroundTransport.State.NotConnected:
                        statusInfo.SetStatus("Not Connected", StatusPanel.State.Warning);
                        break;
                    case UndergroundTransport.State.Connected:
                        statusInfo.SetStatus("Connected", StatusPanel.State.Warning);
                        break;
                    case UndergroundTransport.State.ConnectedError:
                        statusInfo.SetStatus("Connected Error", StatusPanel.State.Critical);
                        break;
                }
            }));

            AddSectionTitle(itemContainer, "Control actions");

            StackContainer buttonStack = Builder.NewStackContainer("buttons")
                .SetStackingDirection(StackContainer.Direction.LeftToRight)
                .SetItemSpacing(5f)
                .SetHeight(40);

            AddMDButton(buttonStack, () => { if (!this.Entity.isConnected) { this.Entity.findConnectedEntrance(); } }, "Connect");
            AddMDButton(buttonStack, () => { this.Entity.disconnect(); }, "Disconnect");

            buttonStack.AppendTo(ItemsContainer);

            AddSectionTitle(itemContainer, "Status");

            statusLabel = Builder
                .NewTxt("")
                .SetTextStyle(Builder.Style.Global.TextControls)
                .SetText("Default text");
            statusLabel.AppendTo(itemContainer);

            updaterBuilder.Observe<EntityId>((Func<EntityId>)(() => this.Entity.Id))
                .Do((Action<EntityId>)(pc =>
                {
                    statusLabel.SetText(Entity.statusText());
                }));
            updaterBuilder.Observe<Quantity>((Func<Quantity>)(() => this.Entity.currentInuse))
                .Do((Action<Quantity>)(pc =>
                {
                    statusLabel.SetText(Entity.statusText());
                }));
            updaterBuilder.Observe<UndergroundTransport.State>((Func<UndergroundTransport.State>)(() => this.Entity.CurrentState))
                .Do((Action<UndergroundTransport.State>)(st =>
                {
                    statusLabel.SetText(Entity.statusText());
                }));

            AddSectionTitle(itemContainer, "Product Buffer");
            productBufferBar = new QuantityBar(Builder);
            productBufferBar.SetHeight(30);
            productBufferBar.AppendTo(ItemsContainer, null, new Offset(20, 0, 20, 0));

            updaterBuilder.Observe<Quantity>((Func<Quantity>)(() => this.Entity.currentInuse)).Do(q => { productBufferBar.UpdateValues(Entity.transportCapacity, Entity.currentInuse); });

            this.AddUpdater(updaterBuilder.Build());

        }
    }
}