using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Converters;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using UNI_Tools_AR.CreateFinish.FinishFloor;
using UNI_Tools_AR.CreateFinish.FinishWall;

namespace UNI_Tools_AR.CreateFinishWithStair
{
    public class BuilderFinishStair
    {
        public Document document { get; set; }
        private Stairs stair { get; set; }

        private Funcitons func = new Funcitons();

        private Options geometryOptions = 
            new Options() { DetailLevel = ViewDetailLevel.Fine };

        public Solid solid => GetSolid();
        public IList<Face> wallFaces => GetWallFaces();

        public IList<Face> riserWallFaces => GetRiserFace();
        public IList<Face> flankWallFaces => GetFlankFace();

        public IList<Face> floorFaces => GetFloorFaces();

        public IList<Face> treadFaces => GetTreadFace();
        public IList<Face> floorOtherFaces => GetOtherFace();

        public IList<Face> ceilingFaces => GetCeiligFaces();

        public const double minLengthWall = 0.07;

        public BuilderFinishStair(Document document, Stairs stair)
        {
            this.document = document;
            this.stair = stair;
        }

        private Solid GetSolid()
        {
            GeometryElement geometryElements = stair.get_Geometry(geometryOptions);

            IList<Solid> solids = func.OpenGeometryInstances(geometryElements);

            Solid fstSolid = null;
            foreach (Solid solid in solids)
            {
                if (fstSolid is null)
                {
                    fstSolid = solid;
                }
                else
                {
                    fstSolid = BooleanOperationsUtils.ExecuteBooleanOperation(
                        fstSolid, solid, BooleanOperationsType.Union
                    );
                }
            }

            return fstSolid;
        }

        private IList<Face> GetWallFaces()
        {
            IList<Face> wallFaces = new List<Face>();
            foreach (Face face in solid.Faces)
            {
                UV centralUV = new UV(0.5, 0.5);
                XYZ faceNormal = face.ComputeNormal(centralUV);
                if (faceNormal.Z == 0) { wallFaces.Add(face); }
            }
            return wallFaces;
        }

        private IList<Face> GetFloorFaces()
        {
            IList<Face> floorFaces = new List<Face>();
            foreach (Face face in solid.Faces)
            {
                UV centralUV = new UV(0.5, 0.5);
                XYZ faceNormal = face.ComputeNormal(centralUV);
                if (faceNormal.Z > 0) { floorFaces.Add(face); }
            }
            return floorFaces;
        }
        private IList<Face> GetCeiligFaces()
        {
            IList<Face> ceilingFaces = new List<Face>();
            foreach (Face face in solid.Faces)
            {
                UV centralUV = new UV(0.5, 0.5);
                XYZ faceNormal = face.ComputeNormal(centralUV);
                if (faceNormal.Z < 0) { ceilingFaces.Add(face); }
            }
            return ceilingFaces;
        }

        public IList<Face> GetRiserFace()
        {
            Parameter riserHeigthParameter = stair
                .get_Parameter(BuiltInParameter.STAIRS_ACTUAL_RISER_HEIGHT);
            
            double riserHeigth = riserHeigthParameter.AsDouble();

            IList<Face> resultFace = new List<Face>();
            foreach (Face face in wallFaces)
            {
                double verticalFace = Math.Round(func.GetVerticatlHeigthForFace(face), 14);

                if (verticalFace == riserHeigth)
                {
                    resultFace.Add(face);
                }
            }

            return resultFace;
        }

        public IList<Face> GetFlankFace()
        {
            Parameter riserHeigthParameter = stair
                .get_Parameter(BuiltInParameter.STAIRS_ACTUAL_RISER_HEIGHT);

            double riserHeigth = riserHeigthParameter.AsDouble();

            IList<Face> resultFace = new List<Face>();
            foreach (Face face in wallFaces)
            {
                double verticalFace = Math.Round(func.GetVerticatlHeigthForFace(face), 14);

                if (verticalFace != riserHeigth)
                {
                    resultFace.Add(face);
                }
            }

            return resultFace;
        }

        public IList<Face> GetTreadFace()
        {
            Parameter treadDepthParameter = stair
                .get_Parameter(BuiltInParameter.STAIRS_ACTUAL_TREAD_DEPTH);
            double treadDepth = Math.Round(treadDepthParameter.AsDouble(), 13);

            IList<Face> resultFaces = new List<Face>();
            foreach (Face face in floorFaces)
            {
                if (func.isInclineFace(face) & (func.SlopeFace(face) != 0.0))
                {
                    continue;
                }

                IList<Curve> curves = func
                    .GetCurvesFromFace(face)
                    .Where(curve => Math.Round(curve.Length, 13) == treadDepth)
                    .Select(curve => curve)
                    .ToList();

                if (curves.Count == 2) { resultFaces.Add(face); }
            }
            return resultFaces;
        }

        public IList<Face> GetOtherFace()
        {
            Parameter treadDepthParameter = stair
                .get_Parameter(BuiltInParameter.STAIRS_ACTUAL_TREAD_DEPTH);
            double treadDepth = Math.Round(treadDepthParameter.AsDouble(), 13);

            IList<Face> resultFaces = new List<Face>();
            foreach (Face face in floorFaces)
            {
                IList<Curve> curves = func
                    .GetCurvesFromFace(face)
                    .Where(curve => Math.Round(curve.Length, 13) == treadDepth)
                    .Select(curve => curve)
                    .ToList();

                if (curves.Count < 2) { resultFaces.Add(face); }
            }
            return resultFaces;
        }

        public IList<Wall> CreateFinishWalls(IList<Face> verticalFaces, WallType wallType)
        {
            bool isStructural = false;
            bool flip = false;

            ElementId wallTypeId = wallType.Id;
            ElementId levelId = stair
                .get_Parameter(BuiltInParameter.STAIRS_BASE_LEVEL_PARAM)
                .AsElementId();

            Level level = document.GetElement(levelId) as Level;

            Parameter bottomLevelHeigthParameter = level
                .get_Parameter(BuiltInParameter.LEVEL_ELEV);
            double bottomLevelHeigth = bottomLevelHeigthParameter.AsDouble();

            double halfPastWallWith = wallType.Width / 2;

            IList<Wall> finishWalls = new List<Wall>();
            foreach (Face face in verticalFaces)
            {
                if (!(face is PlanarFace)) continue;
                PlanarFace planarFace = face as PlanarFace;

                Wall finishWall = null;

                XYZ faceNormal = planarFace.FaceNormal;
                XYZ directionWall = faceNormal * halfPastWallWith;

                Transform translate = Transform
                    .CreateTranslation(directionWall);

                IList<Curve> curves = func
                    .GetCurvesFromFace(planarFace);
                IList<XYZ> allPointsFromCurves = func
                    .GetAllXYZPointFromCurves(curves);
                
                double minZ = allPointsFromCurves
                    .Select(point => point.Z)
                    .Min();
                double maxZ = allPointsFromCurves
                    .Select(point => point.Z)
                    .Max();

                XYZ minZPoint = allPointsFromCurves
                    .Where(point => point.Z == minZ)
                    .First();
                XYZ maxZPoint = allPointsFromCurves
                    .Where(point => point.Z == maxZ)
                    .First();

                double baseOffsetHeigth = minZ - bottomLevelHeigth;

                IList<Curve> curvesHalfPastWall = curves
                    .Select(curve => curve
                    .CreateTransformed(translate))
                    .ToList();

                Curve bottomCurve = func
                    .GetBottomCurve(curvesHalfPastWall, minZ);

                try
                {
                    finishWall = Wall.Create(
                        document,
                        curvesHalfPastWall,
                        wallTypeId,
                        levelId,
                        isStructural,
                        -directionWall
                    );
                }
                catch (Autodesk.Revit.Exceptions.InvalidOperationException)
                { 
                    continue;
                }

                Parameter refTopLevelParameter = finishWall
                    .get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
                Parameter lengthWallParameter = finishWall
                    .get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
                Parameter wallBaseOffsetParameter = finishWall
                    .get_Parameter(BuiltInParameter.WALL_BASE_OFFSET);
                Parameter wallHeigthOffsetParameter = finishWall
                    .get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
                Parameter groundWallParameter = finishWall
                    .get_Parameter(BuiltInParameter.WALL_ATTR_ROOM_BOUNDING);

                refTopLevelParameter.Set(ElementId.InvalidElementId);
                groundWallParameter.Set(0);
                wallBaseOffsetParameter.Set(baseOffsetHeigth);

                finishWalls.Add(finishWall);
            }
            return finishWalls;
        }

        public IList<Floor> CreateFinishFloor(IList<Face> upSideFaces, FloorType floorType)
        {
            ElementId levelId = stair
                .get_Parameter(BuiltInParameter.STAIRS_BASE_LEVEL_PARAM)
                .AsElementId();
            Level level = document.GetElement(levelId) as Level;

            Parameter levelElev_Par = level
                .get_Parameter(BuiltInParameter.LEVEL_ELEV);
            Parameter depthFloor_Par = floorType
                .get_Parameter(BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM);

            double levelElev = levelElev_Par.AsDouble();
            double depthFloor = depthFloor_Par.AsDouble();

            IList<Floor> finshFloors = new List<Floor>();
            foreach (Face face in upSideFaces)
            {
                Floor finishFloor = null;

                IList<CurveLoop> curveLoops = face.GetEdgesAsCurveLoops();
                IList<Curve> curves = func.GetCurvesFromFace(face);

                double slopeFace = func.SlopeFace(face);
                Line slopeLine = func.GetSlopeLine(face);
                XYZ centerFace = func.GetCenterPointFromFace(face);

                IList<XYZ> allPointsFromCurves = func
                    .GetAllXYZPointFromCurves(curves);

                double minZ = allPointsFromCurves
                    .Select(point => point.Z)
                    .Min();

                XYZ minZPoint = allPointsFromCurves
                    .Where(point => point.Z == minZ)
                    .First();

                if (func.isInclineFace(face) & (slopeFace != 0))
                {
                    IList<Curve> lowerCurves = func.LowerCeilingCurves(curves);

                    CurveLoop lowerCurveLoop = new CurveLoop();
                    foreach (Curve curve in lowerCurves)
                    {
                        lowerCurveLoop.Append(curve);
                    }
                    IList<CurveLoop> newCurveLoops = new List<CurveLoop> { lowerCurveLoop };

                    try
                    {
                        finishFloor = Floor.Create(
                            document,
                            newCurveLoops,
                            floorType.Id,
                            levelId,
                            false,
                            slopeLine,
                            slopeFace
                        );
                    }
                    catch (Autodesk.Revit.Exceptions.ArgumentException)
                    {
                        // skip code emergence Exception
                    }
                }
                else
                {
                    try
                    {
                        finishFloor = Floor.Create(document, curveLoops, floorType.Id, levelId);
                    }
                    catch (Autodesk.Revit.Exceptions.ArgumentException)
                    {
                        // skip code emergence Exception
                    }
                }
                
                if (finishFloor is null) { continue; }

                Parameter floorOffsetParameter = finishFloor
                    .get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);

                floorOffsetParameter.Set(minZ + depthFloor);

                finshFloors.Add(finishFloor);
            }

            return finshFloors;
        }

        public IList<Ceiling> CreateFinishCeiling(IList<Face> bottomSideFaces, CeilingType ceilingType)
        {
            ElementId levelId = stair
                .get_Parameter(BuiltInParameter.STAIRS_BASE_LEVEL_PARAM)
                .AsElementId();
            Level level = document.GetElement(levelId) as Level;

            Parameter levelElev_Par = level
                .get_Parameter(BuiltInParameter.LEVEL_ELEV);
            Parameter depthCeiling_Par = ceilingType
                .get_Parameter(BuiltInParameter.CEILING_THICKNESS);

            double levelElev = levelElev_Par.AsDouble();
            double depthCeiling = depthCeiling_Par.AsDouble();
            //double depthFloor = depthFloor_Par.AsDouble();

            IList<Ceiling> finishCeilings = new List<Ceiling>();
            foreach (Face face in bottomSideFaces)
            {
                Ceiling finishCeiling = null;

                IList<CurveLoop> curveLoops = face.GetEdgesAsCurveLoops();
                IList<Curve> curves = func.GetCurvesFromFace(face);

                double slopeFace = func.SlopeFace(face);
                Line slopeLine = func.GetSlopeLine(face);
                XYZ centerFace = func.GetCenterPointFromFace(face);

                IList<XYZ> allPointsFromCurves = func
                    .GetAllXYZPointFromCurves(curves);

                double minZ = allPointsFromCurves
                    .Select(point => point.Z)
                    .Min();

                XYZ minZPoint = allPointsFromCurves
                    .Where(point => point.Z == minZ)
                    .First();

                if (func.isInclineFace(face) & (slopeFace != 0))
                {
                    IList<Curve> lowerCurves = func.LowerCeilingCurves(curves);

                    CurveLoop lowerCurveLoop = new CurveLoop();
                    foreach (Curve curve in lowerCurves)
                    {
                        lowerCurveLoop.Append(curve);
                    }
                    IList<CurveLoop> newCurveLoops = new List<CurveLoop> { lowerCurveLoop };

                    try
                    {
                        finishCeiling = Ceiling
                            .Create(document, newCurveLoops, ceilingType.Id, levelId, slopeLine, slopeFace);
                    }
                    catch (Autodesk.Revit.Exceptions.ArgumentException)
                    {
                        // skip code emergence Exception
                    }
                }
                else
                {
                    try
                    {
                        finishCeiling = Ceiling
                            .Create(document, curveLoops, ceilingType.Id, levelId);
                    }
                    catch (Autodesk.Revit.Exceptions.ArgumentException)
                    {
                        // skip code emergence Exception
                    }
                }

                if (finishCeiling is null) { continue; }

                Parameter ceilingOffsetParameter = finishCeiling
                    .get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);

                ceilingOffsetParameter.Set(minZ + depthCeiling);

                finishCeilings.Add(finishCeiling);
            }

            return finishCeilings;
        }

        public void CreateFlankFinish()
        {
            WallType wallType = document.GetElement(new ElementId(100913)) as WallType;
            CreateFinishWalls(flankWallFaces, wallType);
        }

        public void CreateRiserFinish()
        {
            WallType wallType = document.GetElement(new ElementId(100913)) as WallType;
            CreateFinishWalls(riserWallFaces, wallType);
        }

        public void CreateOtherFloorFinish()
        {
            FloorType floorType = document.GetElement(new ElementId(1238)) as FloorType;
            CreateFinishFloor(floorOtherFaces, floorType);
        }

        public void CreateTreadFinish()
        {
            FloorType floorType = document.GetElement(new ElementId(1238)) as FloorType;
            CreateFinishFloor(treadFaces, floorType);
        }
    }
}
