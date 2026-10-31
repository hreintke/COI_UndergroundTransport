using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndergroundTransportMod;

using Mafi.Core;
using Mafi;

using Mafi.Serialization;

public struct BuffProduct
{
    public readonly ProductQuantity ProductQuantity;

    public readonly SimStep EnqueuedAtStep;

    public BuffProduct(ProductQuantity productQuantity, SimStep enqueuedAtStep)
    {
        ProductQuantity = productQuantity;
        EnqueuedAtStep = enqueuedAtStep;
    }

    public static void Serialize(BuffProduct value, BlobWriter writer)
    {
        ProductQuantity.Serialize(value.ProductQuantity, writer);
        SimStep.Serialize(value.EnqueuedAtStep, writer);
    }

    public static BuffProduct Deserialize(BlobReader reader)
    {
        ProductQuantity productQuantity = ProductQuantity.Deserialize(reader);
        SimStep enqueuedAtStep = SimStep.Deserialize(reader);
        return new BuffProduct(productQuantity, enqueuedAtStep);
    }
}

