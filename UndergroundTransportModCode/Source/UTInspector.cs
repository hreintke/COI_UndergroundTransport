using Mafi;
using Mafi.Collections;
using Mafi.Core;
using Mafi.Core.Syncers;
using Mafi.Localization;
using Mafi.Unity;
using Mafi.Unity.Ui;
using Mafi.Unity.Ui.Inspectors;
using Mafi.Unity.Ui.Library;
using Mafi.Unity.Ui.Library.Inspectors;
using Mafi.Unity.UiStatic.Inspectors.Vehicles;
using Mafi.Unity.UiToolkit.Component;
using Mafi.Unity.UiToolkit.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UndergroundTransportMod;

public class UTInspector : BaseInspector<UndergroundTransport>
{

    private LinesFactory linesFactory;
    private LineOverlayRendererHelper goalLineRenderer;
    private UiContext uiContext;

    public UTInspector(UiContext uic,
                       LinesFactory lf) : base(uic)

    {
        uiContext = uic;
        linesFactory = lf;

        goalLineRenderer = new LineOverlayRendererHelper(linesFactory);
        goalLineRenderer.SetWidth(0.5f);
        goalLineRenderer.SetColor(Color.white);
        goalLineRenderer.HideLine();

        this.Observe<UndergroundTransport.State>((Func<UndergroundTransport.State>)(() => this.Entity.CurrentState)).Do((Action<UndergroundTransport.State>)(state =>
        {
            switch (state)
            {
                default:
                    this.Status.As(Tr.EntityStatus__Idle, DisplayState.Neutral);
                    break;
                case UndergroundTransport.State.None:
                    this.Status.As(Tr.EntityStatus__Idle, DisplayState.Neutral);
                    break;
                case UndergroundTransport.State.Working:
                    this.Status.AsWorking();
                    break;
                case UndergroundTransport.State.Paused:
                    this.Status.AsPaused();
                    break;
            }
        }));

        this.Observe<UndergroundTransport.ConnectionState>((Func<UndergroundTransport.ConnectionState>)(() => this.Entity.currentConnectionState)).Do((Action<UndergroundTransport.ConnectionState>)(state =>
        {
            connectionStatus.Value(state.ToString().AsLoc());
        }));



        ButtonText connectButton = new ButtonText(new LocStrFormatted("Connect"), () => { if (!this.Entity.isConnected) { this.Entity.findConnectedEntrance(); } });
        ButtonText disconnectButton = new ButtonText(new LocStrFormatted("Disconnect"),() => { this.Entity.disconnect(); });

        BufferWithMultipleProductsUi productBuffer = new BufferWithMultipleProductsUi();

        Panel UTPanel = new Panel().MarginLeftRight(10.px());
        Row buttonRow = new Row(5);

        buttonRow.Gap(5);
        buttonRow.Margin(4);
        buttonRow.Add(connectButton);
        buttonRow.Add(disconnectButton);
        buttonRow.Add(connectionStatus);
        
        UTPanel.Add(buttonRow);
        UTPanel.Add(productBuffer);


        this.HeaderPanel.Body.Add(UTPanel);

        this.Observe<Quantity>((Func<Quantity>)(() => this.Entity.currentInuse))
            .Do((Action<Quantity>)(pc =>
                {
                    statusLabel.Text(Entity.statusText());
                }));

        this.ObserveIndexable<ProductQuantity>(() => Entity.getBufferLyst())
            .Observe<Quantity>(() => Entity.transportCapacity).Do((Action<Lyst<ProductQuantity>, Quantity>)((cargo, capacity) =>
            {
                productBuffer.SetProducts(cargo, capacity);
            }));

    }
    TextField statusLabel = new TextField();
    TextField connectionStatus = new TextField().Value("Unkown".AsLoc());
    protected override void OnActivated()
    {
        base.OnActivated();

        connectionStatus.Value(Entity.currentConnectionState.ToString().AsLoc());
        statusLabel.Text(Entity.statusText());
        if (Entity.isConnected)
        {
            HighlightSecondaryEntity(Entity.connectedUndergroundTransport.Value);
            goalLineRenderer.SetColor(Entity.currentConnectionState == UndergroundTransport.ConnectionState.ConnectedError ? Color.red : Color.green);
            goalLineRenderer.ShowLine(Entity.Position3f, Entity.connectedUndergroundTransport.Value.Position3f);
        }
        else
        {
            goalLineRenderer.SetColor(Color.blue);
            Tile3i maxOffset = Entity.getMaxOffsetTile();
            Tile3f maxOffset3f = new Tile3f(maxOffset.X, maxOffset.Y, maxOffset.Z);
            goalLineRenderer.ShowLine(Entity.Position3f, maxOffset3f);
        }
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();
        goalLineRenderer.HideLine();
    }


}
