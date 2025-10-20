using Mafi;
using Mafi.Collections;
using Mafi.Core;
using Mafi.Core.Buildings.Farms;
using Mafi.Core.Buildings.Settlements;
using Mafi.Core.Entities;
using Mafi.Core.Entities.Static;
using Mafi.Core.Entities.Static.Layout;
using Mafi.Core.Entities.Validators;
using Mafi.Core.Terrain;
using Mafi.Localization;
using Mafi.Serialization;
using Mafi.Unity;
using Mafi.Unity.Entities;
using Mafi.Unity.UiStatic.Inspectors.Vehicles;
using RTG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UndergroundTransportMod;
using UnityEngine;

namespace UndergroundTransportMod;

[GlobalDependency(RegistrationMode.AsEverything)]
[GenerateSerializer(false, null, 0)]
public class UTValidator :
    IEntityAdditionValidator<LayoutEntityAddRequest>,
    IEntityAdditionValidator
{
    private static EntitiesManager entitiesManager;
    private LinesFactory linesFactory;
    private LineMb lmb;
    private NewInstanceOf<EntityHighlighter> entityHighlighter;

    public void clearConnectionSigns()
    {
        lmb.Hide();
    }

    public UTValidator(EntitiesManager em, LinesFactory lf, NewInstanceOf<EntityHighlighter> eh)
    {
        entitiesManager = em;

        linesFactory = lf;

        lmb = linesFactory.CreateLinePooled(Vector3.zero, Vector3.zero, 0.1f, Color.red);
        lmb.SetWidth(0.5f);
    }

    string transferPosition (TileTransform transform)
    {
        return $"{transform.Position.ToString()} {transform.Rotation.ToString()}";
    }

    public EntityValidationResult CanAdd(LayoutEntityAddRequest addRequest)
    {
        if (!(addRequest.Proto is UTPrototype))
            return EntityValidationResult.Success;

        Vector3 startLine = addRequest.Transform.Position.ToGroundCenterVector3();

        TileTransform actualTileTransform = new(addRequest.Transform.Position, addRequest.Transform.Transform(Rotation90.Deg0)); // also include Flipped status
        int maxDistance = (addRequest.Proto as UTPrototype).maxDistance;

        Tile3i maxPosition = new Tile3i();

        if (actualTileTransform.Rotation == Rotation90.Deg0)
        {
            maxPosition = actualTileTransform.Position.AddX(-maxDistance);
        }
        else if (actualTileTransform.Rotation == Rotation90.Deg90)
        {
            maxPosition = actualTileTransform.Position.AddY(-maxDistance);
        }
        else if (actualTileTransform.Rotation == Rotation90.Deg180)
        {
            maxPosition = actualTileTransform.Position.AddX(maxDistance);
        }
        else if (actualTileTransform.Rotation == Rotation90.Deg270)
        {
            maxPosition = actualTileTransform.Position.AddY(maxDistance);
        }

        lmb.SetStartPoint(startLine);
        lmb.SetEndPoint(maxPosition.ToGroundCenterVector3());
        lmb.Show();
        return EntityValidationResult.Success;
    }

    public EntityValidatorPriority Priority => EntityValidatorPriority.Default;
}
