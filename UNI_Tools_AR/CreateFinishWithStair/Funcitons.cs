using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UNI_Tools_AR.CreateFinishWithStair
{
    internal class Funcitons
    {
        private double radAndgleSideFromFace(Face face)
        /* */
        {
            UV centralUV = new UV(0.5, 0.5);
            XYZ faceNormal = face.ComputeNormal(centralUV);
            return faceNormal.AngleTo(XYZ.BasisZ);
        }

        public IList<Parameter> AllParametersInElement(Element element)
        {
            IList<Parameter> parameters = new List<Parameter>();
            foreach (Parameter parameter in element.Parameters) { parameters.Add(parameter); }
            return parameters;
        } 

        public string GetStrValueForParameterName(Element element, string name)
        {
            Parameter parameter = element.LookupParameter(name);

            if (!(parameter is null))
            {
                string value = "";

                switch (parameter.StorageType)
                {
                    case StorageType.String:
                        value = parameter.AsString();
                        break;

                    case StorageType.Integer:
                        value = $"{parameter.AsInteger()}"; 
                        break;

                    case StorageType.ElementId:
                        value = parameter.AsValueString();
                        break;

                    case StorageType.Double:
                        double valueAsDouble = parameter.AsDouble();
                        
                        double convertValue = UnitConverter(valueAsDouble, false, parameter.GetUnitTypeId());

                        value = $"{convertValue}";
                        break;
                }
                if (value != string.Empty)
                {
                    return value;
                }
            }
            return "Без значения";
        }

        public bool isVerticalFace(Face face)
        /* */
        {
            return Math.Sin(radAndgleSideFromFace(face)) == 1;
        }

        public bool isHorisontalFace(Face face)
        /* */
        {
            return Math.Sin(radAndgleSideFromFace(face)) == 0;
        }

        public bool isInclineFace(Face face)
        /* */
        {
            double sinResult = Math.Sin(radAndgleSideFromFace(face));
            return (0 < sinResult) & (sinResult < 1);
        }

        public bool isUpSideFace(Room room, Face face)
        /* */
        {
            XYZ centralPointFace = GetCenterPointFromFace(face);
            Level levelRoom = room.Level;

            Parameter heigthLevelParameter = levelRoom
                .get_Parameter(BuiltInParameter.LEVEL_ELEV);
            Parameter countLevelRoomParameter = levelRoom
                .get_Parameter(BuiltInParameter.LEVEL_ROOM_COMPUTATION_HEIGHT);

            double globalHeigthCountRoom =
                heigthLevelParameter.AsDouble() + countLevelRoomParameter.AsDouble();

            return centralPointFace.Z > globalHeigthCountRoom;
        }

        public bool isBottomSideFace(Room room, Face face)
        /* */
        {
            XYZ centralPointFace = GetCenterPointFromFace(face);
            Level levelRoom = room.Level;

            Parameter heigthLevelParameter = levelRoom
                .get_Parameter(BuiltInParameter.LEVEL_ELEV);
            Parameter countLevelRoomParameter = levelRoom
                .get_Parameter(BuiltInParameter.LEVEL_ROOM_COMPUTATION_HEIGHT);
            double globalHeigthCountRoom =
                heigthLevelParameter.AsDouble() + countLevelRoomParameter.AsDouble();

            return centralPointFace.Z < globalHeigthCountRoom;
        }

        public XYZ GetCenterPointFromFace(Face face)
        /* */
        {
            BoundingBoxUV boundingBoxUV = face.GetBoundingBox();
            UV midUV = new UV(
                (boundingBoxUV.Min.U + boundingBoxUV.Max.U) / 2,
                (boundingBoxUV.Min.V + boundingBoxUV.Max.V) / 2
            );
            XYZ midXYZ = face.Evaluate(midUV);
            return midXYZ;
        }
        
        public double SlopeFace(Face face)
        {
            XYZ faceNormal = face.ComputeNormal(new UV(0.5, 0.5));

            double horisontalProjectionLength = Math.Sqrt(Math.Pow(faceNormal.X, 2) + Math.Pow(faceNormal.Y, 2));

            double verticalProjectionLength = faceNormal.Z;

            if (horisontalProjectionLength == 0) { return 0; }
            return (horisontalProjectionLength / verticalProjectionLength);
        }

        public IList<Curve> GetCurveFromCurveLoop(CurveLoop curveLoop)
        {
            IList<Curve> curves = new List<Curve>();
            IEnumerator<Curve> enumCurves = curveLoop.GetEnumerator();
            while (enumCurves.MoveNext()) { curves.Add(enumCurves.Current); }
            enumCurves.Reset();
            return curves;
        }

        public IList<Curve> GetCurvesFromFace(Face face)
        {
            return (from curveLoop in face.GetEdgesAsCurveLoops()
                    from curve in GetCurveFromCurveLoop(curveLoop)
                    select curve).ToList();
        }

        public IList<Curve> SetLineFromHalfPastWidth(XYZ directionWallType, IList<Curve> curves)
        {
            IList<Curve> newCurves = new List<Curve>();
            foreach (Curve curve in curves)
            {
                if (curve is Line)
                {
                    XYZ startPoint = curve.GetEndPoint(0) - directionWallType;
                    XYZ endPoint = curve.GetEndPoint(1) - directionWallType;

                    try
                    {
                        newCurves.Add(Line.CreateBound(startPoint, endPoint));
                    }
                    catch (Autodesk.Revit.Exceptions.ArgumentsInconsistentException)
                    {
                        continue;
                    }
                }
            }

            return newCurves;
        }

        public Curve GetBottomCurve(IList<Curve> curves, double minZ)
        {
            Curve bottomCurve = null;

            foreach (Curve curve in curves)
            {
                Line line = curve as Line;

                XYZ startPoint = line.GetEndPoint(0);
                XYZ endPoint = line.GetEndPoint(1);
                XYZ directionPoint = line.Direction;

                double startPointZ = startPoint.Z;
                double endPointZ = endPoint.Z;

                if ((directionPoint.Z == 0) || (directionPoint.X != 0 && directionPoint.Y != 0))
                    if ((startPointZ == minZ || endPointZ == minZ))
                    {
                        if (startPointZ == endPointZ)
                        {
                            bottomCurve = curve;
                        }
                        else
                        {
                            XYZ newEndPoint = new XYZ(endPoint.X, endPoint.Y, startPointZ);
                            try
                            {
                                Line newLine = Line.CreateBound(startPoint, newEndPoint);
                                bottomCurve = newLine;
                            }
                            catch (Autodesk.Revit.Exceptions.ArgumentsInconsistentException)
                            {
                                continue;
                            }
                        }
                    }
            }
            return bottomCurve;
        }

        public bool SegmentIsModelLine(Document document, BoundarySegment segment)
        {
            if (segment != null)
                if (segment.ElementId != ElementId.InvalidElementId)
                {
                    Element element = document.GetElement(segment.ElementId);
                    if (element != null)
                    {
                        if (element is ModelLine)
                        {
                            return true;
                        }
                    }
                }
            return false;
        }

        public HermiteSpline GetBottomHermiteSplineFromRuledFace(IList<Curve> curves, double minZ)
        {
            var filterHermiteSpline = from curve in curves
                                      where curve is HermiteSpline
                                      select (HermiteSpline)curve;
            foreach (Curve curve in curves)
            {
                if (curve is HermiteSpline)
                {
                    HermiteSpline hermiteSpline = (HermiteSpline)curve;
                    IList<XYZ> controlPoints = (from point in hermiteSpline.ControlPoints
                                                where point.Z == minZ
                                                select point).ToList();
                    if (controlPoints.Count() > 0)
                    {
                        return hermiteSpline;
                    }
                }
            }
            return null;
        }

        public Arc GetBottomArcFromCylindricalFace(IList<Curve> curves)
        {
            double minZ = curves
                .Where(curve => curve is Arc)
                .Select(curve => (curve as Arc).Center.Z).Min();

            Arc arcMinZ = curves
                .Where(curve => curve is Arc)
                .Where(curve => (curve as Arc).Center.Z == minZ)
                .Select(curve => curve as Arc).First();

            return arcMinZ;
        }

        public Arc SetArcFromHalfPastWidth(Room room, double halfPashWidthWall, XYZ centerFace, Arc arc)
        {
            XYZ center = new XYZ(arc.Center.X, arc.Center.Y, centerFace.Z);
            double radius = arc.Radius;
            double startAngle = arc.GetEndParameter(0);
            double endAngle = arc.GetEndParameter(1);
            XYZ xAxis = arc.XDirection;
            XYZ yAxis = arc.YDirection;

            XYZ xyzDirecton = (center - centerFace).Normalize() * halfPashWidthWall;

            if (!(room.IsPointInRoom(xyzDirecton + centerFace))) { radius += halfPashWidthWall; }
            else { radius -= halfPashWidthWall; }

            return Arc.Create(center, radius, startAngle, endAngle, xAxis, yAxis);
        }

        public IList<Line> GetLinesFromHermiteSpline(HermiteSpline hermiteSpline)
        {
            IList<Line> lines = new List<Line>();

            XYZ startPoint = null;
            XYZ endPoint = null;

            foreach (XYZ point in hermiteSpline.ControlPoints)
            {
                if (startPoint is null)
                {
                    startPoint = point;
                }
                else
                {
                    endPoint = point;

                    if (startPoint.DistanceTo(endPoint) > 0.001)
                    {
                        Line line = Line.CreateBound(startPoint, endPoint);
                        startPoint = point;
                        lines.Add(line);
                    }
                }
            }

            return lines;
        }

        public Line CreateLineFromSpline(XYZ startPoint, XYZ endPoint, double halfPastWallWith)
        {
            double currentZCoord = endPoint.Z;

            startPoint = new XYZ(startPoint.X, startPoint.Y, currentZCoord);

            XYZ normalPoint = -(startPoint - endPoint).Normalize() * halfPastWallWith;

            XYZ offsetPointStart = new XYZ(-normalPoint.Y, normalPoint.X, normalPoint.Z) + startPoint;
            XYZ offsetPointEnd = new XYZ(-normalPoint.Y, normalPoint.X, normalPoint.Z) + endPoint;

            try
            {
                Line line = Line.CreateBound(offsetPointStart, offsetPointEnd);
                return line;
            }
            catch (Autodesk.Revit.Exceptions.ArgumentsInconsistentException)
            {
                return null;
            }
        }

        public IList<Line> SetSplineFromHalfPastWidth(HermiteSpline hermiteSpline, double halfPastWallWith)
        {
            XYZ startPointSpline = hermiteSpline.GetEndPoint(0);
            XYZ endPointSpline = hermiteSpline.GetEndPoint(1);

            XYZ startPoint = null;
            XYZ endPoint = null;

            IList<Line> lines = new List<Line>();

            foreach (XYZ point in hermiteSpline.ControlPoints)
            {
                if (hermiteSpline.Distance(point) > 0.001)
                {
                    continue;
                }
                else if (startPoint is null)
                {
                    startPoint = point;

                    Line line = CreateLineFromSpline(startPointSpline, startPoint, halfPastWallWith);

                    if (line != null) { lines.Add(line); }
                }
                else
                {
                    endPoint = point;

                    Line line = CreateLineFromSpline(startPoint, endPoint, halfPastWallWith);

                    if (line != null) { lines.Add(line); }

                    startPoint = endPoint;
                }
            }
            Line endLine = CreateLineFromSpline(endPoint, endPointSpline, halfPastWallWith);

            if (endLine != null) { lines.Add(endLine); }

            return lines;
        }

        public HermiteSpline GetBottomHermiteSpline(IList<Curve> curves, double minZ)
        {
            minZ = Math.Round(minZ, 12);

            HermiteSpline bottomHermiteSpline = null;
            foreach (Curve curve in curves)
            {
                if (curve is HermiteSpline)
                {
                    HermiteSpline hermiteSpline = curve as HermiteSpline;

                    IList<XYZ> allPoints = (from point in hermiteSpline.ControlPoints
                                            select point).ToList();

                    allPoints.Add(hermiteSpline.GetEndPoint(0));
                    allPoints.Add(hermiteSpline.GetEndPoint(1));

                    IList<bool> isZCoord = (from xyz in hermiteSpline.ControlPoints
                                            select Math.Round(xyz.Z, 12) == minZ).ToList();

                    if (isZCoord.Contains(true))
                    {
                        bottomHermiteSpline = hermiteSpline;
                        break;
                    }
                }
            }
            return bottomHermiteSpline;
        }

        public XYZ GetMidPointArc(Arc arc)
        {
            double startAngle = arc.GetEndParameter(0);
            double endAngle = arc.GetEndParameter(1);
            XYZ center = arc.Center;
            double radius = arc.Radius;
            double midAngle = (startAngle + endAngle) / 2;
            double mid_x = center.X + radius * Math.Cos(midAngle);
            double mid_y = center.X + radius * Math.Sin(midAngle);
            XYZ midPoint = new XYZ(mid_x, mid_y, center.Z);
            return midPoint;
        }

        public IList<XYZ> GetAllXYZPointFromCurves(IList<Curve> curves)
        {
            List<XYZ> pointsXYZ = new List<XYZ>();
            foreach (Curve curve in curves)
            {
                if (curve is HermiteSpline)
                {
                    HermiteSpline hermiteSpline = (HermiteSpline)curve;
                    IList<XYZ> controlPoints = hermiteSpline.ControlPoints;
                    foreach (XYZ controlPoint in controlPoints) { pointsXYZ.Add(controlPoint); }
                }
                else if (curve is Arc)
                {
                    Arc arc = (Arc)curve;
                    XYZ startPoint = arc.GetEndPoint(0);
                    XYZ endPoint = arc.GetEndPoint(1);
                    XYZ midPoint = GetMidPointArc(arc);
                    IList<XYZ> arcPoints = new List<XYZ> { startPoint, endPoint, midPoint };
                    foreach (XYZ point in arcPoints) { pointsXYZ.Add(point); }
                }
                else
                {
                    pointsXYZ.Add(curve.GetEndPoint(0));
                    pointsXYZ.Add(curve.GetEndPoint(1));
                }
            }
            return pointsXYZ;
        }

        public XYZ SetZ(XYZ xyz, double z)
        {
            return new XYZ(xyz.X, xyz.Y, z);
        }

        public IList<Curve> LowerCeilingCurves(IList<Curve> curves)
        {
            IList<XYZ> points = GetAllXYZPointFromCurves(curves);
            double minZPoint = points.Select(p => p.Z).Min<double>();

            IList<Curve> lowerCurves = new List<Curve>();
            foreach (Curve curve in curves)
            {
                if (curve is Line)
                {
                    Line line = (Line)curve;
                    XYZ startPoint = SetZ(line.GetEndPoint(0), minZPoint);
                    XYZ endPoint = SetZ(line.GetEndPoint(1), minZPoint);
                    Line newLine = Line.CreateBound(startPoint, endPoint);
                    lowerCurves.Add(newLine);
                }
                else if (curve is Arc)
                {
                    Arc arc = (Arc)curve;
                    double startAngle = arc.GetEndParameter(0);
                    double endAngle = arc.GetEndParameter(1);
                    XYZ center = arc.Center;
                    double radius = arc.Radius;
                    XYZ xAxis = arc.XDirection;
                    XYZ yAxis = arc.YDirection;

                    XYZ newCenter = SetZ(center, minZPoint);
                    Arc newArc = Arc.Create(
                        newCenter, radius, startAngle, endAngle, xAxis, yAxis);
                    lowerCurves.Add(newArc);
                }
                else if (curve is HermiteSpline)
                {
                    HermiteSpline hermiteSpline = (HermiteSpline)curve;

                    IList<XYZ> xyzHermiteSpline = new List<XYZ>();
                    foreach (XYZ controlPoint in hermiteSpline.ControlPoints)
                    {
                        xyzHermiteSpline.Add(SetZ(controlPoint, minZPoint));
                    }
                    HermiteSpline newHermiteSpline =
                        HermiteSpline.Create(xyzHermiteSpline, hermiteSpline.IsPeriodic);
                    lowerCurves.Add(newHermiteSpline);
                }
            }
            return lowerCurves;
        }

        public Line GetSlopeLine(Face face)
        {
            XYZ startPoint = GetCenterPointFromFace(face);
            XYZ faceNormal = face.ComputeNormal(new UV(0.5, 0.5));
            XYZ horizontalPorjection = new XYZ(faceNormal.X, faceNormal.Y, 0).Normalize() * 5;
            if (horizontalPorjection.IsZeroLength()) { return null; }
            XYZ endPoint = startPoint - faceNormal;
            Line slopeLine = Line.CreateBound(SetZ(startPoint, endPoint.Z), endPoint);
            return slopeLine;
        }

        public Room CheckRoom(Room room)
        /* */
        {
            Location locationRoom = room.Location;
            double areaRoom = room.Area;
            double areaParameter = room.Perimeter;

            if ((locationRoom is null) & (areaRoom == 0) & (areaParameter == 0))
            {
                string nameRoom = room.get_Parameter(BuiltInParameter.ROOM_NAME).AsString();
                string nameTitle = "Предупреждение";
                string infoMessage = $"Имя {nameRoom} Id{room.Id.IntegerValue} " +
                    "- данное помещение не размещенно или размещено неккоректно";
                TaskDialog.Show(nameTitle, infoMessage);
                return null;
            }
            return room;
        }

        public IList<Curve> FilterNotVerticalCurve(IList<Curve> curves)
        {
            IList<Curve> filteredCurves = new List<Curve>();
            foreach (Curve curve in curves)
            {
                if (curve is Line)
                {
                    Line line = (Line)curve;
                    XYZ direction = line.Direction;
                    double angle = direction.AngleTo(new XYZ(1, 1, 0));
                    if (Math.Sin(angle) != 1)
                    {
                        filteredCurves.Add(curve);
                    }
                }
            }
            return filteredCurves;
        }

        public double GetVerticalOffsetForFace(Level level, Face face)
        {
            IList<Curve> curves = new List<Curve>();
            foreach (CurveLoop curveLoop in face.GetEdgesAsCurveLoops())
            {
                foreach (Curve curve in GetCurveFromCurveLoop(curveLoop))
                {
                    curves.Add(curve);
                }
            }

            double levelHeigth = level.get_Parameter(BuiltInParameter.LEVEL_ELEV).AsDouble();
            double minZ = GetAllXYZPointFromCurves(curves).Select(p => p.Z).Min<double>();

            return minZ - levelHeigth;
        }

        public double GetVerticatlHeigthForFace(Face face)
        {
            IList<Curve> curves = new List<Curve>();
            foreach (CurveLoop curveLoop in face.GetEdgesAsCurveLoops())
            {
                foreach (Curve curve in GetCurveFromCurveLoop(curveLoop))
                {
                    curves.Add(curve);
                }
            }
            double minZ = GetAllXYZPointFromCurves(curves).Select(p => p.Z).Min<double>();
            double maxZ = GetAllXYZPointFromCurves(curves).Select(p => p.Z).Max<double>();
            double heigthFace = maxZ - minZ;
            return heigthFace;
        }

        public IList<Solid> OpenGeometryInstances(GeometryElement geometryElement)
        {
            IList<Solid> resultSolids = new List<Solid>();

            foreach (var gElement in geometryElement)
            {
                if (gElement is Solid)
                {
                    resultSolids.Add(gElement as Solid);
                }
                else if (gElement is GeometryInstance)
                {
                    GeometryElement instanceGeometry = (gElement as GeometryInstance)
                        .GetInstanceGeometry();

                    IList<Solid> instanceSolids = OpenGeometryInstances(instanceGeometry);

                    foreach (Solid solid in instanceSolids) { resultSolids.Add(solid); }
                }
            }
            return resultSolids;
        }

        public void CreateDirectShape(Document document, IList<GeometryObject> gElements)
        {
            DirectShape directShape = DirectShape.CreateElement(document, new ElementId(-2000011));
            if (directShape.IsValidShape(gElements))
            {
                directShape.SetShape(gElements);
            }
        }

        public IList<WallType> GetAllWallType(Document document)
        {
            FilteredElementCollector collector = new FilteredElementCollector(document);

            IList<WallType> allWallTypes = 
                collector.OfClass(typeof(WallType))
                .Select(wallType => wallType as WallType)
                .ToList();

            return allWallTypes;
        }

        public IList<FloorType> GetAllFloorType(Document document)
        {
            FilteredElementCollector collector = new FilteredElementCollector(document);

            IList<FloorType> allFloorTypes = 
                collector.OfClass(typeof(FloorType))
                .Select(floorType => floorType as FloorType)
                .ToList();
            return allFloorTypes;
        }

        public IList<CeilingType> GetAllCeilingType(Document document)
        {
            FilteredElementCollector collector = new FilteredElementCollector(document);

            IList<CeilingType> allCeilingTypes =
                collector.OfClass(typeof(CeilingType))
                .Select(ceilingType => ceilingType as CeilingType)
                .ToList();

            return allCeilingTypes;
        }

        public IList<Element> GetAllElementsTypeFromGroundRoom(Document document, IList<Room> rooms)
        {
            SpatialElementBoundaryOptions spatialOptions = new SpatialElementBoundaryOptions()
            {
                StoreFreeBoundaryFaces = true,
                SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish,
            };

            HashSet<ElementId> groundElementIds = (
                from room in rooms
                from segments in room.GetBoundarySegments(spatialOptions)
                from segment in segments
                where segment.ElementId != ElementId.InvalidElementId
                where document.GetElement(segment.ElementId).GetTypeId() != ElementId.InvalidElementId
                select document.GetElement(segment.ElementId).GetTypeId()
            ).ToHashSet();

            return groundElementIds.Select(elementId => document.GetElement(elementId)).ToList();
        }

        public Dictionary<string, Level> GetAllLevelsCollection(Document document)
        {
            Dictionary<string, Level> levels = new Dictionary<string, Level>();

            FilteredElementCollector collector = new FilteredElementCollector(document);
            foreach (Element element in collector.OfClass(typeof(Level)).ToElements())
            {
                Level level = (Level)element;
                string levelName = level.get_Parameter(BuiltInParameter.DATUM_TEXT).AsString();
                levels[levelName] = level;
            }
            return levels;
        }

        public IList<Room> GetRoomSelection(UIDocument uiDocument, Document document)
        {
            IList<Room> selectionRooms = new List<Room>();

            foreach (ElementId elementId in uiDocument.Selection.GetElementIds())
            {
                Element element = document.GetElement(elementId);
                if (element is Room)
                {
                    selectionRooms.Add((Room)element);
                }
            }
            return selectionRooms;
        }

        public IList<Room> GetAllRoomsInProject(Document document)
        {
            FilteredElementCollector collector = new FilteredElementCollector(document);

            IList<Room> allRoomsInProject = collector
                .OfClass(typeof(SpatialElement)).ToElements()
                .Where(room => room is Room)
                .Where(room => room.Location != null)
                .Where(room => room.get_Parameter(BuiltInParameter.ROOM_AREA).AsDouble() != 0)
                .Where(room => room.get_Parameter(BuiltInParameter.ROOM_PERIMETER).AsDouble() != 0)
                .Select(room => room as Room)
                .ToList();

            return allRoomsInProject;
        }

        public IList<Room> GetAllRoomsInActiveView(Document document)
        {
            Autodesk.Revit.DB.View viewId = document.ActiveView;
            FilteredElementCollector collector = new FilteredElementCollector(document, viewId.Id);

            IList<Room> allRoomsInActiveView = collector
                .OfClass(typeof(SpatialElement)).ToElements()
                .Where(room => room is Room)
                .Where(room => room.Location != null)
                .Where(room => room.get_Parameter(BuiltInParameter.ROOM_AREA).AsDouble() != 0)
                .Where(room => room.get_Parameter(BuiltInParameter.ROOM_PERIMETER).AsDouble() != 0)
                .Select(room => room as Room)
                .ToList();

            return allRoomsInActiveView;
        }

        public IList<Room> GetAllRoomsInLevel(Document document, Level level)
        {
            FilteredElementCollector collector = new FilteredElementCollector(document);

            IList<Room> allRoomsInLevel = collector
                .OfClass(typeof(SpatialElement)).ToElements()
                .Where(room => room is Room)
                .Where(room => room.Location != null)
                .Where(room => room.LevelId == level.Id)
                .Where(room => room.get_Parameter(BuiltInParameter.ROOM_AREA).AsDouble() != 0)
                .Where(room => room.get_Parameter(BuiltInParameter.ROOM_PERIMETER).AsDouble() != 0)
                .Select(room => room as Room)
                .ToList();

            return allRoomsInLevel;
        }

        public double UnitConverter(double value, bool toInternal, ForgeTypeId forgeTypeId)
        {
            Func<double, ForgeTypeId, double> method;

            if (toInternal)
            {
                method = UnitUtils.ConvertToInternalUnits;
            }
            else
            {
                method = UnitUtils.ConvertFromInternalUnits;
            }
            return method(value, forgeTypeId);
        }

        public void SetMiterJoinWall(Wall wall)
        {
            LocationCurve locationCurve = (LocationCurve)wall.Location;
            JoinType miterJoinType = JoinType.Miter;
            locationCurve.set_JoinType(0, miterJoinType);
            locationCurve.set_JoinType(1, miterJoinType);
        }

        //public void SetWarningResolver(Transaction transaction)
        //{
        //    FailureHandlingOptions failOptions = transaction.GetFailureHandlingOptions();
        //    failOptions.SetFailuresPreprocessor(new WarningResolver());
        //    transaction.SetFailureHandlingOptions(failOptions);
        //}

        public bool EqualPoints(XYZ point1, XYZ point2)
        {
            return
                point1.X == point2.X &&
                point1.Y == point2.Y &&
                point1.Z == point2.Z;
        }

        public XYZ GetIntersectionPoint(IntersectionResultArray intersectionResultArray)
        {
            XYZ resultPoint = null;
            foreach (IntersectionResult intersectionResult in intersectionResultArray)
            {
                resultPoint = intersectionResult.XYZPoint;
            }
            return resultPoint;
        }

        public bool IsMeCheckoutElement(
            Autodesk.Revit.DB.Document doc, Autodesk.Revit.ApplicationServices.Application app, IList<ElementId> elementIds)
        {
            foreach (ElementId elementId in elementIds)
            {
                WorksharingTooltipInfo worksharingInfo = WorksharingUtils.GetWorksharingTooltipInfo(doc, elementId);
                if ((worksharingInfo.Owner != "") & (worksharingInfo.Owner != app.Username))
                {
                    TaskDialog.Show("Ошибка", $"Елементы заняты пользователем {worksharingInfo.Owner}" +
                        $" попросите его освободить элементы");
                    return false;
                }
            }
            return true;
        }
    }
}
