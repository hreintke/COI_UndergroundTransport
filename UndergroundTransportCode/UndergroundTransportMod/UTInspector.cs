using Mafi;
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
                case UndergroundTransport.State.None:
                    this.Status.As(Tr.EntityStatus__Idle, DisplayState.Neutral);
                    break;
                case UndergroundTransport.State.ConnectedIn:
                    this.Status.As(new LocStrFormatted("ConnectedIn"), DisplayState.Positive);
                        break;
                case UndergroundTransport.State.ConnectedOut:
                    this.Status.As(new LocStrFormatted("ConnectedOut"), DisplayState.Positive);
                    break;
                case UndergroundTransport.State.Paused:
                    this.Status.AsPaused();
                    break;
                case UndergroundTransport.State.NotConnected:
                    this.Status.As(new LocStrFormatted("Not Connected"), DisplayState.Neutral);
                    break;
                case UndergroundTransport.State.Connected:
                    this.Status.As(new LocStrFormatted("Connected"), DisplayState.Warning);
                    break;
                case UndergroundTransport.State.ConnectedError:
                    this.Status.As(new LocStrFormatted("Connected Error"), DisplayState.Warning);
                        break;
            }
        }));



        ButtonText connectButton = new ButtonText(new LocStrFormatted("Connect"), () => { if (!this.Entity.isConnected) { this.Entity.findConnectedEntrance(); } });
        ButtonText disconnectButton = new ButtonText(new LocStrFormatted("Disconnect"),() => { this.Entity.disconnect(); });

        ProductBufferUi productBuffer = new ProductBufferUi();

        Panel UTPanel = this.AddPanel();
        UTPanel.Add(statusLabel);
        Row buttonRow = new Row(5);

        buttonRow.Gap(5);
        buttonRow.Margin(4);
        buttonRow.Add(connectButton);
        buttonRow.Add(disconnectButton);
        
        UTPanel.Add(buttonRow);
        UTPanel.Add(productBuffer);

        this.HeaderPanel.Body.Add(UTPanel);

        this.Observe<Quantity>((Func<Quantity>)(() => this.Entity.currentInuse))
            .Do((Action<Quantity>)(pc =>
                {
                    statusLabel.Text(Entity.statusText());
                }));
        this.Observe<Quantity>((Func<Quantity>)(() => this.Entity.currentInuse)).Do(q => { productBuffer.Values(Entity.currentInuse, Entity.transportCapacity); });

    }
    TextField statusLabel = new TextField();
    protected override void OnActivated()
    {
        base.OnActivated();

        statusLabel.Text(Entity.statusText());
        if (Entity.isConnected)
        {
            HighlightSecondaryEntity(Entity.connectedUndergroundTransport.Value);
            goalLineRenderer.SetColor(Entity.CurrentState == UndergroundTransport.State.ConnectedError ? Color.red : Color.green);
            goalLineRenderer.ShowLine(Entity.Position3f, Entity.connectedUndergroundTransport.Value.Position3f);
        }
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();
        goalLineRenderer.HideLine();
    }


}
