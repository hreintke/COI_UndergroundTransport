using Mafi;
using Mafi.Collections;
using Mafi.Collections.ImmutableCollections;
using Mafi.Core;
using Mafi.Core.Bridges;
using Mafi.Core.Buildings.Farms;
using Mafi.Core.Buildings.Mine;
using Mafi.Core.Buildings.Towers;
using Mafi.Core.Entities;
using Mafi.Core.Entities.Static.Layout;
using Mafi.Core.Factory.Transports;
using Mafi.Core.Factory.Zippers;
using Mafi.Core.Input;
using Mafi.Core.Maintenance;
using Mafi.Core.Population;
using Mafi.Core.Ports;
using Mafi.Core.Ports.Io;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;
using Mafi.Core.Simulation;
using Mafi.Core.Terrain;
using Mafi.Core.Terrain.Designation;
using Mafi.Core.Trains;
using Mafi.Serialization;
using Mafi.Unity.Ui.Statistics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using static Mafi.Base.Assets.Base.Buildings;
using static Mafi.Base.Assets.Base.Tutorials;
using static Mafi.Base.Assets.Core;

namespace UndergroundTransportMod;

[GenerateSerializer(false, null, 0)]
public class UndergroundTransport : LayoutEntity, IEntityWithSimUpdate, IEntityWithPorts
{
    private readonly int serializeVersion;

    public enum State
    {
        None,
        Paused,
        Idle,
        Working
    }

    public enum ConnectionState
    {
        Unknown,
        Connected,
        ConnectedIn,
        ConnectedOut,
        ConnectedError,
        NotConnected
    }

    public enum Direction
    {
        In,
        Out,
        Unspecified
    }

    public UndergroundTransport(EntityId id, UTPrototype proto, TileTransform transform, EntityContext context,
        EntitiesManager entitiesManager,
        TerrainManager terrainManager,
        TerrainOccupancyManager terrainOccupancyManager,
        SimLoopEvents simLoopEvents,
        ProductsManager productsManager) 
        : base(id, proto, transform, context)
    {
        _proto = proto;
        _entitiesManager = entitiesManager;
        _terrainManager = terrainManager;
        _terrainOccupancyManager = terrainOccupancyManager;
        _simLoopEvents = simLoopEvents;
        _productsManager = productsManager;
        productBuffer = new Queueue<BuffProduct>();
        ht.Add((CenterTile.Tile2i,UnityEngine.Color.blue));
    }
    private UTPrototype _proto;
    private static EntitiesManager _entitiesManager;
    private TerrainManager _terrainManager;
    private TerrainOccupancyManager _terrainOccupancyManager;
    private SimLoopEvents _simLoopEvents;
    private ProductsManager _productsManager;

    public Quantity transportCapacity;
    public int transportLength;
    public Duration transportDuration;
    public Quantity currentInuse;

    private Tile3i portPostion => Ports[0].Position;

    public Dict<ProductProto, Quantity> productCount = new Dict<ProductProto, Quantity>();

    private Queueue<BuffProduct> productBuffer = new Queueue<BuffProduct>();

    public Option<UndergroundTransport> connectedUndergroundTransport = new Option<UndergroundTransport>();

    public Lyst<(Tile2i,UnityEngine.Color)> ht = new();

    public bool isConnected => connectedUndergroundTransport.HasValue;

    public TileTransform connectTransform 
    {
        get
        {
            TileTransform tt = this.Transform.RotateBy(Rotation90.Deg180);
            
            return new TileTransform();
        } 
    }

    public Lyst<ProductQuantity> getBufferLyst()
    {
        Lyst<ProductQuantity> t = new Lyst<ProductQuantity>();
        foreach(var pc in productCount)
        {
            t.Add(new ProductQuantity(pc.Key,pc.Value));
        }
        return t;
    }

    public void updatePortDirections()
    {
        ImmutableArrayBuilder<IoPort> immutableArrayBuilder = new ImmutableArrayBuilder<IoPort>(this.Ports.Length);
        for (int index = 0; index < this.Ports.Length; ++index)
        {
            IoPort port = this.Ports[index];
            IoPort ioPort = new IoPort(this.Context.PortIdFactory.GetNextId(), (IEntityWithPorts)this, new PortSpec(port.Spec.Name, 
                port.Spec.Type == IoPortType.Output ? IoPortType.Input : IoPortType.Output, port.Spec.Shape, port.Spec.CanOnlyConnectToTransports), port.Position, port.Direction, (int)port.PortIndex);
            this.Context.IoPortsManager.DisconnectAndRemove(port);
            immutableArrayBuilder[index] = ioPort;
        }
        this.Ports = immutableArrayBuilder.GetImmutableArrayAndClear();
        foreach (IoPort port in this.Ports)
            this.Context.IoPortsManager.AddPortAndTryConnect(port);
    }



    private State updateState()
    {
        if ((!base.IsEnabled))
        {
            return State.Paused;
        }
        if ((!(currentConnectionState == ConnectionState.ConnectedIn)) && (!(currentConnectionState == ConnectionState.ConnectedOut))) 
        {
            return State.Idle;
        }
        return State.Working;
    }

    public State CurrentState { get; private set; }
    public ConnectionState currentConnectionState { get; private set; }
    private ConnectionState updateConnectionState()
    {
        if (connectedUndergroundTransport.HasValue)
        {
            switch (currentDirection)
            {
                case Direction.In:
                    {
                        if (connectedUndergroundTransport.Value.currentDirection == Direction.Out)
                        {
                            return ConnectionState.ConnectedIn;
                        }
                        else if (connectedUndergroundTransport.Value.currentDirection == Direction.In)
                        {
                            return ConnectionState.ConnectedError;
                        }
                        else
                        {
                            return ConnectionState.Connected;
                        }
                    }

                case Direction.Out:
                    {
                        if (connectedUndergroundTransport.Value.currentDirection == Direction.In)
                        {
                            return ConnectionState.ConnectedOut;
                        }
                        else if (connectedUndergroundTransport.Value.currentDirection == Direction.Out)
                        {
                            return ConnectionState.ConnectedError;
                        }
                        else
                        {
                            return  ConnectionState.Connected;
                        }
                    }
            }
            return ConnectionState.Connected;
        }
        else
        {
            return ConnectionState.NotConnected;
        }
    }

    public void calulateTransportCapacity()
    {
        if (isConnected)
        {
            transportCapacity = (10 * transportLength).Quantity();
        }
    }

    public void connect(UndergroundTransport ut)
    {
        if (isConnected)
        {
            disconnect();
        }
        transportLength = 2 + (this.Transform.Position.Tile2i.X - ut.Transform.Position.X).Abs() + (this.Transform.Position.Tile2i.Y - ut.Transform.Position.Y).Abs()
                            + (this.Transform.Position.Z - ut.Transform.Position.Z).Abs();

        transportDuration = new Duration((int)((float)transportLength / 0.4f));
        transportCapacity = (10 * transportLength).Quantity();

        connectedUndergroundTransport = ut;
    }

    public void setDurationCapacity(TransportProto tp)
    {

    }

    public void disconnect()
    {
        if (connectedUndergroundTransport.HasValue)
        {
            UndergroundTransport connection = connectedUndergroundTransport.Value;
            connectedUndergroundTransport = Option<UndergroundTransport>.None;
            connection.disconnect();
        }
    }

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

    public string statusText()
    {
        Quantity productBufferQuantity = Quantity.Zero;
        foreach (var pb in productBuffer)
        {
            productBufferQuantity += pb.ProductQuantity.Quantity;
        }

        Quantity pcQuantity = Quantity.Zero;
        foreach (var pc in productCount)
        {
            pcQuantity += pc.Value;
        }

        string c = isConnected ? "Connected Direction " + currentDirection : "Not Connected";
        string buf = $" maxBuffer {transportCapacity} In use {currentInuse} pbuffer {productBufferQuantity} pCount {pcQuantity}";
        String cap = $" cap = {transportLength} {transportCapacity} {transportDuration}";
        string cp = connectedUndergroundTransport.HasValue ? connectedUndergroundTransport.Value.Transform.Position.ToString() : " ";

        string r = Transform.IsReflected ? "Flipped" : "Normal";

        // return Transform.Position.ToString() + " " + Transform.Transform(Rotation90.Deg0) + " " +r + " " + isBridgeTransport().HasValue + getOffsetTile(2).ToString() ;
      
        return $"{groundTile} {Transform.Transform(Rotation90.Deg0)} {Ports[0].Position}"; 
    }

    public Direction currentDirection = Direction.Unspecified;
    public override bool CanBePaused => true;

    void IEntityWithSimUpdate.SimUpdate()
    {
        currentConnectionState = updateConnectionState();
        if ((CurrentState = updateState()) == State.Working)
        {
            trySendProducts();
        }
    }

    public Quantity sendToOuputPort(ProductQuantity quantity)
    {
        IoPortData ioPortData = ConnectedOutputPorts[0];
        return ioPortData.SendAsMuchAs(quantity);
    }

    public void trySendProducts()
    {
        // Direction.In is controlling the transport
        if (!isConnected
             || connectedUndergroundTransport.Value.CurrentState != State.Working
             || connectedUndergroundTransport.Value.currentConnectionState != UndergroundTransport.ConnectionState.ConnectedOut
             || currentConnectionState != UndergroundTransport.ConnectionState.ConnectedIn)
        {
            return;
        }

        bool connectedFull = false;
        bool lastSend = true;
        while (productBuffer.IsNotEmpty && !connectedFull && lastSend)
        {
            BuffProduct zp = productBuffer.Peek();
            if (_simLoopEvents.CurrentStep - zp.EnqueuedAtStep >= transportDuration)
            {
                productBuffer.Dequeue();
                Quantity rq = Quantity.Zero;

                rq = connectedUndergroundTransport.Value.sendToOuputPort(zp.ProductQuantity);
                
                productCount.AddQuantity(zp.ProductQuantity.Product, -(zp.ProductQuantity.Quantity - rq));
                currentInuse = currentInuse - (zp.ProductQuantity.Quantity - rq);
                if (rq != Quantity.Zero)
                {
                    connectedFull = true;
                    productBuffer.EnqueueFirst(new BuffProduct(new ProductQuantity(zp.ProductQuantity.Product, rq), zp.EnqueuedAtStep));
                }
                
            }
            else
            {
                lastSend = false;
            }
        }
    }

    public Quantity ReceiveAsMuchAsFromPort(ProductQuantity pq, IoPortToken sourcePort)
    {
        if  (!(CurrentState == State.Working)
            || !isConnected 
            || currentConnectionState != ConnectionState.ConnectedIn
            || connectedUndergroundTransport.Value.currentConnectionState != ConnectionState.ConnectedOut)
        {
            return (pq.Quantity);
        }
        else
        {
            return receiveProduct(pq);
        }
    }

    public Quantity receiveProduct(ProductQuantity pq)
    {

        if ((!_proto.singleProduct) || (productCount.Count == 0) || (productCount.ContainsKey(pq.Product)))
        {
            Quantity receivedQuantity = pq.Quantity.Min(transportCapacity - currentInuse);
            if (receivedQuantity != Quantity.Zero)
            {
                productCount.AddQuantity(pq.Product, receivedQuantity);
                currentInuse = currentInuse + receivedQuantity;
                productBuffer.Enqueue(new BuffProduct(new ProductQuantity(pq.Product, receivedQuantity), _simLoopEvents.CurrentStep));
            }
            return (pq.Quantity - receivedQuantity);
        }
        else
        {
            return pq.Quantity;
        }
    }

    protected override void OnPortConnectionChanged(IoPort ourPort)
    {
        // ourPort.GetMaxThroughputPerTick
        base.OnPortConnectionChanged(ourPort);
        if (!ourPort.IsConnected)
        {
            currentDirection = Direction.Unspecified;
        }
        else
        if (ourPort.IsConnectedAsOutput)
        {
            currentDirection = Direction.Out;
        }
        else
        if (ourPort.IsConnectedAsInput)
        { 
            currentDirection = Direction.In;
        }
    }

    private void logData(string s)
    {
        string p = productCount.Count == 0 ? "empty" : productCount.First().Key.ToString() + " " + productCount.First().Value.ToString();
    }

    public bool checkPossibleConnect(Tile3i position, bool connectIfAllowed)
    {

        TileTransform actualTileTransform = new TileTransform(Transform.Position, Transform.Transform(Rotation90.Deg0)); // also include Flipped status

        if ((actualTileTransform.Position.Z - position.Z) >= Prototype.maxHeightDifference)
        {
            return false;
        }

        bool withinDistance = false;
        if (actualTileTransform.Rotation == Rotation90.Deg0)
        {
            if ((actualTileTransform.Position.X - position.X) <= Prototype.maxDistance)
            {
                withinDistance = true;
            }
        }
        else if (actualTileTransform.Rotation == Rotation90.Deg90)
        {
            if ((actualTileTransform.Position.Y - position.Y) <= Prototype.maxDistance)
            {
                withinDistance = true;
            }
        }
        else if (actualTileTransform.Rotation == Rotation90.Deg180)
        {
            if ((position.X - actualTileTransform.Position.X) <= Prototype.maxDistance)
            {
                withinDistance = true;
            }
        }

        else if (actualTileTransform.Rotation == Rotation90.Deg270)
        {
            if ((position.Y - actualTileTransform.Position.Y) <= Prototype.maxDistance)
            {
                withinDistance = true;
            }
        }

        if (!withinDistance)
        {
            return false;
        }

        Tile2iIndex checkPosition = _terrainManager.GetTileIndex(position.Tile2i);

        EntityId entityId = _terrainOccupancyManager.GetLowestOccupyingEntity(checkPosition);
        UndergroundTransport utc;

        if ((entityId == EntityId.Invalid) || !(_entitiesManager.TryGetEntity<UndergroundTransport>(entityId, out utc)))
        {
            return false;
        }

        if (utc.Transform.Transform(Rotation90.Deg0) != this.Transform.Transform(Rotation90.Deg0).Rotated180)
        {
            return false;
        }

        if (utc.isConnected || (utc.Prototype.transportType != Prototype.transportType))
        {
            return false;
        }

        if (connectIfAllowed)
        {
            utc.connect(this);
            this.connect(utc);
        }
        return true;
    }

    public Tile3i getMaxOffsetTile()
    {
        return getOffsetTile(Prototype.maxDistance);
    }

    public Tile3i getOffsetTile(int offset)
    {
        TileTransform actualTileTransform = new TileTransform(Transform.Position, Transform.Transform(Rotation90.Deg0)); // also include Flipped status
        
        Tile3i offsetTile = new Tile3i();
        if (actualTileTransform.Rotation == Rotation90.Deg0)
        {
            offsetTile = actualTileTransform.Position.AddX(-offset);
        }
        else if (actualTileTransform.Rotation == Rotation90.Deg90)
        {
            offsetTile = actualTileTransform.Position.AddY(-offset);
        }
        else if (actualTileTransform.Rotation == Rotation90.Deg180)
        {
            offsetTile = actualTileTransform.Position.AddX(offset);
        }

        else if (actualTileTransform.Rotation == Rotation90.Deg270)
        {
            offsetTile = actualTileTransform.Position.AddY(offset);
        }
        return offsetTile;
    }

    public Option<BridgeSegment> isBridgeTransport()
    {
        //LogWrite.Info($"Check bridge {Transform.Position} {portPostion} {Transform.Rotation} {Transform.IsReflected} {Transform.Transform90RotFlip.Rotation90}");
        BridgeSegment bridgeSegment;
        if (_entitiesManager.TryGetEntity<BridgeSegment>(_terrainOccupancyManager.GetLowestOccupyingEntity(_terrainManager.GetTileIndex(getOffsetTile(2).Tile2i)), out bridgeSegment))
            {
            LogWrite.Info($"iBT {getOffsetTile(2).Tile2i} {Transform.IsReflected} {Transform.Transform(Rotation90.Deg0)} {Transform.Rotation} {bridgeSegment.Transform.IsReflected} {bridgeSegment.Transform.Transform(Rotation90.Deg0)} {bridgeSegment.Transform.Rotation})");
        }
        else
        {
            LogWrite.Info($"iBT No BE {getOffsetTile(2).Tile2i} {groundTile} {Transform.IsReflected} {Transform.Transform(Rotation90.Deg0)} {Transform.Rotation}");
        }
        
        if (_entitiesManager.TryGetEntity<BridgeSegment>(_terrainOccupancyManager.GetLowestOccupyingEntity(_terrainManager.GetTileIndex(getOffsetTile(2).Tile2i)), out bridgeSegment)
        && (bridgeSegment.Prototype.IsEntrance)
        && ((bridgeSegment.Transform.Position.Z) == this.Transform.Position.Z)
//        && (Transform.IsReflected ? 
//                this.Transform.Transform90RotFlip.Rotation90 == bridgeSegment.Transform.Transform90RotFlip.Rotation90
//                : this.Transform.Transform90RotFlip.Rotation90.Rotated180 == bridgeSegment.Transform.Transform90RotFlip.Rotation90)
        && this.Transform.Transform(Rotation90.Deg0).Rotated180 == bridgeSegment.Transform.Transform(Rotation90.Deg0)
        )
        {
            return bridgeSegment;
        }
        else
        {
            return Option.None;
        }
    }

    public Tile2i[] getTransportTiles(BridgeSegment bs)
    {
        Tile2i mid = getStartTile(bs);
        int z = 4;
        int width = 0;
        Rotation90 rotation = bs.Transform.Transform(Rotation90.Deg0);
        bool includeMidPoint;

        if (bs.Prototype.BridgeProto.Id.ToString() == "Bridge_CableStayed")
        {
            width = 12;
            includeMidPoint = true;
        }
        else if (bs.Prototype.BridgeProto.Id.ToString() == "Bridge_Truss2Lane")
        {
            width = 6;
            includeMidPoint = false;
        }
        else
        {
            LogWrite.Info($"Unknown BridgeProto {bs.Prototype.BridgeProto.Id.ToString()}");
            return new Tile2i[0];
        }
        LogWrite.Info($"GT {bs.Prototype.Id} {bs.GetType().Name} {bs.Prototype.BridgeProto.Id} {bs.Prototype.BridgeProto.SlimId}");
        int midX = mid.X;
        int midY = mid.Y;
        int lowerHalf = width / 2;
        int upperHalf = width - 1 - lowerHalf;

        int farMidX = midX - (rotation.Dx * z);// + (rotation == Rotation90.Deg0 ? 1 : 0);// + rotation.Dx;
        int farMidY = midY - (rotation.Dy * z);//  + (rotation == Rotation90.Deg90 ? 1 : 0);// + rotation.Dy;

        LogWrite.Info($"GFE Dx {rotation.Dx} Dy {rotation.Dy}");

        // "width axis" step direction (perpendicular to extrusion direction)
        var (wdx, wdy) = rotation.Is90Or270Deg ? (1, 0) : (0, 1) ;

        var points = new Lyst<Tile2i>();

        // far endpoint 1 (lower side)
        points.Add(new Tile2i(farMidX - lowerHalf * wdx, farMidY - lowerHalf * wdy));
        //points.Add((farMidX, farMidY));
    
        if (includeMidPoint)
        {
            // input midpoint, already projected by z
            points.Add(new Tile2i(farMidX, farMidY));

            // if width is even, add the other cell adjacent to true center
            if (width % 2 == 0)
            {
                points.Add(new Tile2i(farMidX - wdx, farMidY - wdy));
            }
        }

        // far endpoint 2 (upper side)
        points.Add(new Tile2i(farMidX + upperHalf * wdx, farMidY + upperHalf * wdy));
        return points.ToArray();
    }

    Tile2i getStartTile(BridgeSegment bs)
    {
        var addrel = new RelTile2i(0, 0);
        if (bs.Transform.Transform(Rotation90.Deg0) == Rotation90.Deg270)
        {
            addrel = new RelTile2i(0, -1);
        }
        if (bs.Transform.Transform(Rotation90.Deg0) == Rotation90.Deg180)
        {
            addrel = new RelTile2i(-1, 0);
        }
        return bs.GetStartPosition().Tile2i + addrel;
    }

    public Tile2i portTile => Ports[0].Position.Tile2i;

    public Tile2i groundTile => (portTile != CenterTile.Tile2i) ? CenterTile.Tile2i : CenterTile.Tile2i - new RelTile2i(Transform.Transform(Rotation90.Deg0).Dx, Transform.Transform(Rotation90.Deg0).Dy);


    public bool findConnectedEntrance()

    {
        ht.Add((CenterTile.Tile2i, UnityEngine.Color.blue));
        ht.Add((groundTile, UnityEngine.Color.red));

        // First check on Bridge connection.
        Option<BridgeSegment> bridgeSegment = isBridgeTransport();
        if (bridgeSegment.HasValue)
        {
            LogWrite.Info($"BridgeSegment {bridgeSegment.Value.Transform.Transform(Rotation90.Deg0)}");
            Tile2i[] thisTransportTiles = getTransportTiles(bridgeSegment.Value);
            bool c = true;
            int btIndex = 0;
            bool onTile = false;

            foreach (var r in thisTransportTiles)
            {
                if (groundTile == r)
                {
                    onTile = true;
                    break;
                }
                btIndex++;
                LogWrite.Info($"TT {r}");
                ht.Add((r, c ? UnityEngine.Color.white : UnityEngine.Color.pink));
                c = false;
                //break;
            }
            if (!onTile)
            {
                LogWrite.Info($"UT not on tile");
                return false;
            }
            // Need to check whether the transport is on a TransportTile
            Tile2i[] otherTransportTiles ;
            foreach (var bs in bridgeSegment.Value.GetAllConnectedSegments())
            {
                if ((!bs.Prototype.IsEntrance) || bs == bridgeSegment.Value)
                {
                    continue;
                }
                LogWrite.Info($"Other Bidge segment {bs.Transform.Transform(Rotation90.Deg0)}");
                otherTransportTiles = getTransportTiles(bs);
                c = true;
                foreach (var r in otherTransportTiles)
                {
                    LogWrite.Info($"OT {r}");
                    ht.Add((r, c ?  UnityEngine.Color.blue : UnityEngine.Color.green ));
                    c = false;
                    //break;
                }
                Tile2iIndex checkBridgePosition = _terrainManager.GetTileIndex
                    (((bs.Transform.Transform(Rotation90.Deg0) ==  bridgeSegment.Value.Transform.Transform(Rotation90.Deg0).RotatedPlus90) 
                        || (bs.Transform.Transform(Rotation90.Deg0) == bridgeSegment.Value.Transform.Transform(Rotation90.Deg0)))
                    ? otherTransportTiles[otherTransportTiles.Length -1 - btIndex] : otherTransportTiles[btIndex]);

                var cp = ((bs.Transform.Transform(Rotation90.Deg0) == bridgeSegment.Value.Transform.Transform(Rotation90.Deg0).RotatedPlus90) ? otherTransportTiles[otherTransportTiles.Length - 1 - btIndex] : otherTransportTiles[btIndex]);


                LogWrite.Info($"Check {cp}");
                EntityId entityId = _terrainOccupancyManager.GetLowestOccupyingEntity(checkBridgePosition);
                UndergroundTransport utc;

                if ((entityId == EntityId.Invalid) || !(_entitiesManager.TryGetEntity<UndergroundTransport>(entityId, out utc)))
                {
                    return false;
                }
                if (utc.isConnected || (utc.Prototype.transportType != Prototype.transportType))
                {
                    return false;
                }

                LogWrite.Info($"Connecting");
                utc.connect(this);
                this.connect(utc);
                 return true;
            }
            return false;
        }

        Tile3i checkPosition = new Tile3i();

        for (int i = 2; i <= _proto.maxDistance; i++)
        {
            checkPosition = getOffsetTile(i);

            if (checkPossibleConnect(checkPosition, true))
            {
                return true;
            }
        }
        return false;
    }

    protected override void OnAddedToWorld(EntityAddReason reason)
    {
        
        base.OnAddedToWorld(reason);
        findConnectedEntrance();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (connectedUndergroundTransport.HasValue)
        {
            connectedUndergroundTransport.Value.disconnect();
            disconnect();
        }
        while (productBuffer.IsNotEmpty)
        {
            BuffProduct zp = productBuffer.Dequeue();
            _productsManager.ProductCleared(zp.ProductQuantity);
        }
    }

    private static readonly Action<object, BlobWriter> s_serializeDataDelayedAction;

    private static readonly Action<object, BlobReader> s_deserializeDataDelayedAction;

    public static void Serialize(UndergroundTransport value, BlobWriter writer)
    {
        if (writer.TryStartClassSerialization(value))
        {
            writer.EnqueueDataSerialization(value, s_serializeDataDelayedAction);
        }
    }

    protected override void SerializeData(BlobWriter writer)
    {
        base.SerializeData(writer);
        writer.WriteInt(serializeVersion);
        writer.WriteGeneric(_proto);
        EntitiesManager.Serialize(_entitiesManager, writer);
        TerrainManager.Serialize(_terrainManager, writer);
        TerrainOccupancyManager.Serialize(_terrainOccupancyManager, writer);
        ProductsManager.Serialize(_productsManager, writer);
        SimLoopEvents.Serialize(_simLoopEvents, writer);

        writer.WriteInt(transportLength);
        Duration.Serialize(transportDuration, writer);
        Quantity.Serialize(transportCapacity, writer);
        Dict<ProductProto, Quantity>.Serialize(productCount, writer);
        Queueue<BuffProduct>.Serialize(productBuffer, writer);
        Option<UndergroundTransport>.Serialize(connectedUndergroundTransport, writer);
        writer.WriteInt((int)currentDirection);
    }

    public static UndergroundTransport Deserialize(BlobReader reader)
    {
        if (reader.TryStartClassDeserialization(out UndergroundTransport obj, (Func<BlobReader, Type, UndergroundTransport>)null))
        {
            reader.EnqueueDataDeserialization(obj, s_deserializeDataDelayedAction);
        }
        return obj;
    }

    protected override void DeserializeData(BlobReader reader)
    {
        base.DeserializeData(reader);
        int sVersion = reader.ReadInt();
        reader.SetField(this, "_proto", reader.ReadGenericAs<UTPrototype>());
        _entitiesManager =  EntitiesManager.Deserialize(reader);
        _terrainManager = TerrainManager.Deserialize(reader);
        _terrainOccupancyManager =  TerrainOccupancyManager.Deserialize(reader);
        _productsManager = ProductsManager.Deserialize(reader);
        _simLoopEvents = SimLoopEvents.Deserialize(reader);
        transportLength = reader.ReadInt(); 
        transportDuration = Duration.Deserialize(reader);
        transportCapacity = Quantity.Deserialize(reader);
        productCount =  Dict<ProductProto, Quantity>.Deserialize(reader);
        if (reader.LoadedSaveVersion < 334)
        {
            var pb = Queueue<ZipBuffProduct>.Deserialize(reader);
            productBuffer = new Queueue<BuffProduct>();
            productCount = new Dict<ProductProto, Quantity>();
        }
        else
        {
            productBuffer = Queueue<BuffProduct>.Deserialize(reader);
        }
        
        connectedUndergroundTransport = Option<UndergroundTransport>.Deserialize(reader);
        currentDirection = (Direction)reader.ReadInt();
        reader.RegisterInitAfterLoad<UndergroundTransport>(this, "initSelf", InitPriority.Normal);
    }




    [InitAfterLoad(InitPriority.Normal)]
    private void initSelf(int saveVersion, DependencyResolver resolver)
    {
        currentInuse = Quantity.Zero;
        foreach(var pc in productCount)
        {
            currentInuse += pc.Value;
        }
        updateConnectionState();
        calulateTransportCapacity();
        ht = new();
        ht.Add((CenterTile.Tile2i, UnityEngine.Color.blue));
      }

    static UndergroundTransport()
    {
        s_serializeDataDelayedAction = delegate (object obj, BlobWriter writer)
        {
            ((UndergroundTransport)obj).SerializeData(writer);
        };
        s_deserializeDataDelayedAction = delegate (object obj, BlobReader reader)
        {
            ((UndergroundTransport)obj).DeserializeData(reader);
        };
    }

}

