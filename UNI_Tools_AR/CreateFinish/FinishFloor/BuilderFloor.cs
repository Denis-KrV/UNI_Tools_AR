using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UNI_Tools_AR.CreateFinish.FinishWall;

namespace UNI_Tools_AR.CreateFinish.FinishFloor
{
    internal class BuilderFloor : BuilderFinish
    {
        public BuilderFloor(Document document, Room room)
        {
            this.document = document;
            this.room = room;
        }

        private IList<Wall> GetGroundWallsWithRoom()
        {
            HashSet<int> wallIds = new HashSet<int>();

            foreach (BoundarySegment boundarySegment in curvesForBoundarySegments)
            {
                ElementId elementId = boundarySegment.ElementId;

                if (elementId == ElementId.InvalidElementId) { continue; }

                Element element = document.GetElement(elementId);

                if (element is Wall) { wallIds.Add(elementId.IntegerValue); }
            }

            IList<Wall> walls = wallIds
                .Select(wallId => document.GetElement(new ElementId(wallId)) as Wall)
                .ToList();

            return walls;
        }

        private IList<Floor> CreateDoorsFloor(Wall wall, Curve segmentCurve, FloorType floorType, Level level)
        {
            Curve locationCurve = (wall.Location as LocationCurve).Curve;

            XYZ startPointLocation = locationCurve.GetEndPoint(0);
            XYZ endPointLocation = locationCurve.GetEndPoint(1);

            XYZ startPointSegment = segmentCurve.GetEndPoint(0);
            XYZ endPointSegment = segmentCurve.GetEndPoint(1);

            double zCoord = startPointSegment.Z + 0.01;

            locationCurve = Line.CreateBound(
                new XYZ(startPointLocation.X, startPointLocation.Y, zCoord),
                new XYZ(endPointLocation.X, endPointLocation.Y, zCoord)
            );
            segmentCurve = Line.CreateBound(
                new XYZ(startPointSegment.X, startPointSegment.Y, zCoord),
                new XYZ(endPointSegment.X, endPointSegment.Y, zCoord)
            );

            Options gОptions = new Options();
            gОptions.DetailLevel = ViewDetailLevel.Coarse;
            GeometryElement geometryElement = wall.get_Geometry(gОptions);

            Solid solid = geometryElement
                .Where(gElement => gElement is Solid)
                .Select(gElement => gElement as Solid)
                .First();

            SolidCurveIntersectionOptions solidIntersectOptions = 
                new SolidCurveIntersectionOptions();
            SolidCurveIntersection solidCurveIntersection = solid
                .IntersectWithCurve(segmentCurve, solidIntersectOptions);

            IList<Curve> curvesFromSolid = solidCurveIntersection
                .Select(intersectResult => intersectResult)
                .ToList();

            IList<XYZ> intersectXYZs = new List<XYZ>();

            foreach (Curve curve in curvesFromSolid)
            {
                XYZ fstPoint = curve.GetEndPoint(0);
                XYZ scdPoint = curve.GetEndPoint(1);

                intersectXYZs.Add(fstPoint);
                intersectXYZs.Add(scdPoint);
            }

            IList<XYZ> orderXYZs = intersectXYZs
                .OrderBy(xyz => xyz.X + xyz.Y + xyz.Z)
                .Select(xyz => xyz)
                .ToList();

            XYZ fstXYZ = null;
            XYZ sndXYZ = null;

            List<List<XYZ>> doorVoidXYZs = new List<List<XYZ>>();
            foreach (XYZ xyz in orderXYZs)
            {
                if (fstXYZ == null)
                {
                    fstXYZ = xyz;
                }
                else
                {
                    sndXYZ = xyz;

                    try
                    {
                        Line checkline = Line.CreateBound(fstXYZ, sndXYZ);

                        IList<Curve> equalLine = curvesFromSolid
                            .Where(curve => curve.Intersect(checkline) == SetComparisonResult.Equal)
                            .Select(curve => curve)
                            .ToList();

                        if (equalLine.Count == 0)
                        {
                            XYZ fstProjectPoint = locationCurve.Project(fstXYZ).XYZPoint;
                            XYZ sndProjectPoint = locationCurve.Project(sndXYZ).XYZPoint;

                            List<XYZ> doorVoidXYZ = new List<XYZ> 
                            { 
                                fstXYZ, 
                                sndXYZ, 
                                sndProjectPoint, 
                                fstProjectPoint 
                            };

                            doorVoidXYZs.Add(doorVoidXYZ);
                        }
                    }
                    catch (Autodesk.Revit.Exceptions.ArgumentsInconsistentException)
                    {
                        // skip code emergence Exception
                    }

                    fstXYZ = xyz;
                }
            }

            IList<Floor> resutlDoorFloors = new List<Floor>();

            foreach (List<XYZ> doorVoidXYZ in doorVoidXYZs)
            {
                IList<CurveLoop> doorVoidLoops = new List<CurveLoop>();
                CurveLoop doorVoid = new CurveLoop();

                Line line = null;

                XYZ fstXYZVoid = null;
                XYZ sndXYZVoid = null;

                foreach (XYZ xyz in doorVoidXYZ)
                {
                    if (fstXYZVoid is null)
                    {
                        fstXYZVoid = xyz;
                    }
                    else
                    {
                        sndXYZVoid = xyz;
                        if (!func.EqualPoints(fstXYZVoid, sndXYZVoid))
                        {
                            line = Line.CreateBound(fstXYZVoid, sndXYZVoid);
                            doorVoid.Append(line);
                        }
                        fstXYZVoid = xyz;
                    }
                }

                if (!func.EqualPoints(fstXYZVoid, doorVoidXYZ.First()))
                {
                    line = Line.CreateBound(fstXYZVoid, doorVoidXYZ.First());
                    doorVoid.Append(line);
                }

                doorVoidLoops.Add(doorVoid);

                Floor floor = Floor.Create(document, doorVoidLoops, floorType.Id, level.Id);
                resutlDoorFloors.Add(floor);
            }
            return resutlDoorFloors;
        }

        public IList<Floor> CreateFinishFloor(FloorType floorType, double offsetValue = 0, bool hasGround = true)
        /* */
        {
            Level level = document.GetElement(room.LevelId) as Level;

            Parameter levelElev_Par = level.get_Parameter(BuiltInParameter.LEVEL_ELEV);
            Parameter depthFloor_Par = floorType.get_Parameter(BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM);

            double levelElev = levelElev_Par.AsDouble();

            double depthFloor = depthFloor_Par.AsDouble();

            double convertOffsetValue = func.UnitConverter(offsetValue, true, depthFloor_Par.GetUnitTypeId());
            
            offsetValue = convertOffsetValue;

            offsetValue += depthFloor;

            IList<Floor> newFinishFloors = new List<Floor>();

            foreach (Face face in floorFaces)
            {
                IList<CurveLoop> curveLoops= face.GetEdgesAsCurveLoops();
                IList<Curve> curves = func.GetCurvesFromFace(face);

                IList<Floor> newFloorForCurrentFace = new List<Floor>();

                double slopeFace = func.SlopeFace(face);
                Line slopeLine = func.GetSlopeLine(face);
                XYZ centerFace = func.GetCenterPointFromFace(face);

                Floor newFloor = null;
                if (func.isInclineFace(face) & (func.SlopeFace(face) != 0.0))
                {
                    IList<Curve> lowerCurves = func.LowerCeilingCurves(curves);

                    CurveLoop lowerCurveLoop = new CurveLoop();
                    foreach (Curve curve in lowerCurves)
                    {
                        lowerCurveLoop.Append(curve);
                    }
                    IList<CurveLoop> newCurveLoops = new List<CurveLoop> { lowerCurveLoop };

                    //if (!BoundaryValidation.IsValidHorizontalBoundary(newCurveLoops)) { continue; }

                    try
                    {
                        newFloor = Floor.Create(document, newCurveLoops, floorType.Id, room.LevelId, false, slopeLine, slopeFace);
                        newFloorForCurrentFace.Add(newFloor);
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
                        bool isopen = curveLoops.First().IsOpen();
                        newFloor = Floor.Create(document, curveLoops, floorType.Id, room.LevelId);
                        newFloorForCurrentFace.Add(newFloor);
                    }
                    catch (Autodesk.Revit.Exceptions.ArgumentException)
                    {
                        // skip code emergence Exception
                    }
                }


                foreach (Wall wall in GetGroundWallsWithRoom())
                {
                    IList<Element> wallInserts = wall
                        .FindInserts(true, false, false, false)
                        .Select(insertElementId => document.GetElement(insertElementId))
                        .Where(insertElement => !(insertElement is null))
                        .Where(insertElement => insertElement.Category.Id.IntegerValue == -2000023)
                        .ToList();

                    if (wallInserts.Count == 0) { continue; }

                    Solid wallSolid = wall.get_Geometry(geometryOptions)
                        .Where(geometryElement => geometryElement is Solid)
                        .Select(geometryElement => geometryElement as Solid)
                        .First();

                    SolidCurveIntersectionOptions solidCurveIntersection = new SolidCurveIntersectionOptions();

                    foreach (Curve curve in curves)
                    {
                        SolidCurveIntersection intersectResult = wallSolid
                            .IntersectWithCurve(curve, solidCurveIntersection);

                        if (intersectResult.Count() == 0) { continue; }

                        foreach (Floor doorFloor in CreateDoorsFloor(wall, curve, floorType, level))
                        {
                            newFloorForCurrentFace.Add(doorFloor);
                        }
                    }
                }

                foreach (Floor finishFloor in newFloorForCurrentFace)
                {
                    double valueHeigthAbovLevel = offsetValue;
                    valueHeigthAbovLevel += centerFace.Z - levelElev;

                    finishFloor
                        .get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM)
                        .Set(valueHeigthAbovLevel);

                    newFinishFloors.Add(finishFloor);
                }
            }

            return newFinishFloors;
        }
    }
}
