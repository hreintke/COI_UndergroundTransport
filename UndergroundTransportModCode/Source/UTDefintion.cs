using Mafi.Core;
using Mafi;
using Mafi.Core.Buildings.Towers;
using Mafi.Core.Entities;
using Mafi.Core.Entities.Static.Layout;
using Mafi.Core.Maintenance;
using Mafi.Core.Population;
using Mafi.Core.Ports;
using Mafi.Core.Ports.Io;
using Mafi.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Mafi.Base.Assets.Base.Tutorials;
using Mafi.Core.Input;
using Mafi.Core.Terrain.Designation;
using Mafi.Core.Terrain;
using static Mafi.Base.Assets.Base.Buildings;
using Mafi.Core.Simulation;
using Mafi.Collections;
using Mafi.Core.Factory.Zippers;
using Mafi.Core.Products;
using static Mafi.Base.Assets.Core;
using System.Diagnostics.Eventing.Reader;
using Mafi.Core.Buildings.Farms;
using Mafi.Core.Buildings.Mine;
using System.Runtime.CompilerServices;
using Mafi.Core.Factory.Transports;
using UnityEngine.UIElements;

namespace UndergroundTransportMod;

[GenerateSerializer(false, null, 0)]
public class UndergroundTransport : LayoutEntity, IEntityWithSimUpdate, IEntityWithPorts
{
    private readonly int serializeVersion;

    public enum State
    {
        None,
        Paused,
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
        productBuffer = new Queueue<ZipBuffProduct>();
        LogWrite.Info($"New UT {this.Transform.Position.ToString()} {this.Transform.Rotation.ToString()}");
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

    public Dict<ProductProto, Quantity> productCount = new Dict<ProductProto, Quantity>();

    private Queueue<ZipBuffProduct> productBuffer = new Queueue<ZipBuffProduct>();

    public Option<UndergroundTransport> connectedUndergroundTransport = new Option<UndergroundTransport>();

    public bool isConnected => connectedUndergroundTransport.HasValue;

    public TileTransform connectTransform 
    {
        get
        {
            TileTransform tt = this.Transform.RotateBy(Rotation90.Deg180);
            
            return new TileTransform();
        } 
    }

    public State CurrentState { get; private set; }
    private State updateState()
    {
        if ((!base.IsEnabled))
        {
            return State.Paused;
        }

        if (connectedUndergroundTransport.HasValue)
        {
            switch (currentDirection)
            {
                case Direction.In:
                    {
                        if (connectedUndergroundTransport.Value.currentDirection == Direction.Out)
                        {
                            return State.ConnectedIn;
                        }
                        else if (connectedUndergroundTransport.Value.currentDirection == Direction.In)
                        {
                            return State.ConnectedError;
                        }
                        else
                        {
                            return State.Connected;
                        }
                    }

                case Direction.Out:
                    {
                        if (connectedUndergroundTransport.Value.currentDirection == Direction.In)
                        {
                            return State.ConnectedOut;
                        }
                        else if (connectedUndergroundTransport.Value.currentDirection == Direction.Out)
                        {
                            return State.ConnectedError;
                        }
                        else
                        {
                            return State.Connected;
                        }
                    }
            }
            return State.Connected;
        }
        else
        {
            return State.NotConnected;
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
        transportCapacity = (4 * transportLength).Quantity();

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
        string c = isConnected ? "Connected Direction " + currentDirection : "Not Connected";
        string buf = $" maxBuffer {transportCapacity} In use {currentInuse}";
        String cap = $" cap = {transportLength} {transportCapacity} {transportDuration}";
        string cp = connectedUndergroundTransport.HasValue ? connectedUndergroundTransport.Value.Transform.Position.ToString() : " ";

        string r = Transform.IsReflected ? "Flipped" : "Normal";

        return Transform.Position.ToString() + " " + Transform.Transform(Rotation90.Deg0) + " "  + c + buf;
    }

    public Direction currentDirection = Direction.Unspecified;
    public override bool CanBePaused => true;

    void IEntityWithSimUpdate.SimUpdate()
    {
        CurrentState = updateState();
        trySendProducts();
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
             || connectedUndergroundTransport.Value.CurrentState != State.ConnectedOut
             || CurrentState != State.ConnectedIn)
        {
            return;
        }

        bool connectedFull = false;
        bool lastSend = true;
        while (productBuffer.IsNotEmpty && !connectedFull && lastSend)
        {
            ZipBuffProduct zp = productBuffer.Peek();
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
                    productBuffer.EnqueueFirst(new ZipBuffProduct(new ProductQuantity(zp.ProductQuantity.Product, rq), zp.EnqueuedAtStep));
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
        if  (!isConnected 
            || CurrentState != State.ConnectedIn
            || connectedUndergroundTransport.Value.CurrentState != State.ConnectedOut)
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
                productBuffer.Enqueue(new ZipBuffProduct(new ProductQuantity(pq.Product, receivedQuantity), _simLoopEvents.CurrentStep));
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

        if ((actualTileTransform.Position.Z - position.Z) > Prototype.maxHeightDifference)
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

    public bool findConnectedEntrance()
    {
 //       LogWrite.Info($"__ find connected {this.Transform.Position.Tile2i.ToString()} {this.Transform.Transform(Rotation90.Deg0).ToString()}");
        TileTransform actualTileTransform = new TileTransform(Transform.Position, Transform.Transform(Rotation90.Deg0)); // also include Flipped status

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
            ZipBuffProduct zp = productBuffer.Dequeue();
            _productsManager.ClearProduct(zp.ProductQuantity);
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
        Queueue<ZipBuffProduct>.Serialize(productBuffer, writer);
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
        LogWrite.Info("Deserializing");
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
        productBuffer = Queueue<ZipBuffProduct>.Deserialize(reader);
        connectedUndergroundTransport = Option<UndergroundTransport>.Deserialize(reader);
        currentDirection = (Direction)reader.ReadInt();
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

