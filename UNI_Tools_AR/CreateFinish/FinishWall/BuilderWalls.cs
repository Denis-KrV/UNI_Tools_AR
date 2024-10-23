using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;



namespace UNI_Tools_AR.CreateFinish.FinishWall
{
    internal class BuilderWalls
    {
        private Autodesk.Revit.DB.Document document { get; set; }
        private IList<FinishWallItem> finishWallItems { get; }

        private Funcitons func = new Funcitons();

        private IList<Room> rooms;
        private IList<BuilderWall> builders => (
            from room in rooms
            select new BuilderWall(document, room, finishWallItems)).ToList();

        public BuilderWalls(Document document, IList<Room> rooms, IList<FinishWallItem> finishWallItems)
        {
            this.document = document;
            this.rooms = rooms;
            this.finishWallItems = finishWallItems;
        }

        public IList<Wall> MakeWallAutomate(double offset, bool isMiterJoinType = false)
        {
            IList<Wall> newWalls = new List<Wall>();
            foreach (BuilderWall builder in builders)
            {
                int roomId = builder.room.Id.IntegerValue;

                string transactionMessage = $"Для помещения с id:{roomId} создана отделка стен.";

                using (Transaction transaction = new Transaction(document, transactionMessage))
                {
                    transaction.Start();
                    func.SetWarningResolver(transaction, document);

                    newWalls = builder.CreateFinishWall(offset);

                    transaction.Commit();
                }
            }
            return newWalls;
        }

        public IList<Wall> MakeWallForRoomHeigth(double offset)
        {
            IList<Wall> newWalls = new List<Wall>();
            foreach (BuilderWall builder in builders)
            {
                int roomId = builder.room.Id.IntegerValue;

                string nameTransaction = $"Для помещения с id:{roomId} создана отделка стен.";

                using (Transaction transaction = new Transaction(document, $"Для помещения с id:{roomId} создана отделка стен."))
                {
                    transaction.Start();
                    func.SetWarningResolver(transaction, document);

                    newWalls = builder.CreateFinishWall(offset, 0, true);
                    transaction.Commit();
                }
            }
            return newWalls;
        }

        public IList<Wall> MakeWallForHeigth(double offset, double heigth)
        {
            IList<Wall> newWalls = new List<Wall>();
            foreach (BuilderWall builder in builders)
            {
                int roomId = builder.room.Id.IntegerValue;

                using (Transaction transaction = new Transaction(document, $"Для помещения с id:{roomId} создана отделка стен."))
                {
                    transaction.Start();
                    func.SetWarningResolver(transaction, document);
                    newWalls = builder.CreateFinishWall(offset, heigth, true);
                    transaction.Commit();
                }
            }
            return newWalls;
        }
    }

    class BuilderWall : BuilderFinish
    {
        private IList<FinishWallItem> finishWallItems { get; }

        public BuilderWall(Document document, Room room, IList<FinishWallItem> finishWallItems)
        {
            this.document = document;
            this.room = room;
            this.finishWallItems = finishWallItems;
        }

        public BoundarySegment GetSegmentSubsetFromFace(Face face)
        {
            IList<Curve> curves = func.GetCurvesFromFace(face);

            BoundarySegment segmentSubset = null;

            foreach (BoundarySegment boundarySegment in curvesForBoundarySegments)
            {
                Curve curveBoundarySegment = boundarySegment.GetCurve();

                Curve equalCurve = null;
                foreach (Curve curve in curves)
                {
                    if (curve.Intersect(curveBoundarySegment) == SetComparisonResult.Equal)
                    {
                        equalCurve = curve;
                        break;
                    }
                }
                if (equalCurve != null)
                {
                    segmentSubset = boundarySegment;
                    break;
                }
            }
            return segmentSubset;
        }

        public FinishWallItem GetFinishWallItemFromSegment(BoundarySegment boundarySegment)
        {
            FinishWallItem resultWallItem;
            if (!(boundarySegment is null))
            {
                ElementId elementIdSegment = boundarySegment.ElementId;

                if (!(elementIdSegment == ElementId.InvalidElementId))
                {
                    Element elementSegment = document.GetElement(elementIdSegment);
                    ElementId typeIdElementSegment = elementSegment.GetTypeId();

                    if (!(typeIdElementSegment == ElementId.InvalidElementId))
                    {
                        resultWallItem = finishWallItems
                            .Where(finishItem => finishItem.baseElement.elementType != null)
                            .Where(finishItem => finishItem.baseElement.elementType.Id.IntegerValue == typeIdElementSegment.IntegerValue)
                            .Select(finishItem => finishItem)
                            .First();

                        return resultWallItem;
                    }
                }
            }
            resultWallItem = finishWallItems
                .Where(finishItem => finishItem.baseElement.elementType is null)
                .Select(finishItem => finishItem)
                .First();

            return resultWallItem;
        }

        public bool SetParametersWall(
            Wall newWall, double offset, BoundarySegment segmentSubset, bool isGround, double baseOffsetHeigth, double topHeigthWall)
        {
            Parameter refTopLevelParameter = newWall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
            Parameter lengthWallParameter = newWall.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
            Parameter wallBaseOffsetParameter = newWall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET);
            Parameter wallHeigthOffsetParameter = newWall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
            Parameter panelWallParameter = newWall.get_Parameter(BuiltInParameter.AUTO_PANEL_WALL);

            refTopLevelParameter.Set(ElementId.InvalidElementId);

            double valueLengthWall = lengthWallParameter.AsDouble();
            double convertOffset = func.UnitConverter(offset, true, wallBaseOffsetParameter.GetUnitTypeId());

            func.CloseJoinWall(newWall);

            if ((valueLengthWall <= minLengthWall) || (topHeigthWall <= (convertOffset * 1.2)))
            {
                document.Delete(newWall.Id);
                return false;
            }
            else
            {
                wallBaseOffsetParameter.Set(baseOffsetHeigth + convertOffset);
                wallHeigthOffsetParameter.Set(topHeigthWall - baseOffsetHeigth - convertOffset);

                newWall.get_Parameter(BuiltInParameter.WALL_KEY_REF_PARAM).Set(3);
            }

            newWall.get_Parameter(BuiltInParameter.WALL_ATTR_ROOM_BOUNDING).Set(isGround ? 1 : 0);

            if (segmentSubset != null)
                if ((segmentSubset.ElementId != ElementId.InvalidElementId) && (panelWallParameter is null))
                {
                    Element joinElement = document.GetElement(segmentSubset.ElementId);
                    try
                    {
                        JoinGeometryUtils.JoinGeometry(document, newWall, joinElement);
                    }
                    catch (Autodesk.Revit.Exceptions.ArgumentException) { }
                }
            return true;
        }


        public FinishWallItem GetWallTypeFromBoundarySegment(BoundarySegment boundarySegment)
        {
            FinishWallItem finishWallItem = null;

            IList<FinishWallItem> filterFinishWallItems = null;

            if (boundarySegment != null)
            {
                ElementId elementId = boundarySegment.ElementId;

                if (elementId != ElementId.InvalidElementId)
                {
                    Element element = document.GetElement(elementId);
                    ElementId elementTypeId = element.GetTypeId();

                    if (elementTypeId != ElementId.InvalidElementId)
                    {
                        filterFinishWallItems = finishWallItems
                            .Where(wallItem => wallItem.baseElement.elementType != null)
                            .Where(wallItem => wallItem.baseElement.elementType.Id.IntegerValue == elementTypeId.IntegerValue)
                            .Where(wallItem => wallItem.finishWall != null)
                            .Where(wallItem => wallItem.finishWall.wallType != null)
                            .ToList();

                        if (filterFinishWallItems.Count > 0)
                        {
                            finishWallItem = filterFinishWallItems.First();
                        }

                        return finishWallItem;
                    }
                }
            }

            filterFinishWallItems = finishWallItems
                .Where(wallItem => wallItem.baseElement.elementType is null)
                .Where(wallItem => wallItem.finishWall != null)
                .ToList();

            if (filterFinishWallItems.Count > 0)
            {
                finishWallItem = filterFinishWallItems.First();
            }

            return finishWallItem;
        }

        public Curve CutAndLengthenCurve(Curve curve, XYZ intersectPoint)
        {
            XYZ startPoint = curve.GetEndPoint(0);
            XYZ endPoint = curve.GetEndPoint(1);

            double startIntersectDistance = startPoint.DistanceTo(intersectPoint);
            double endIntersectDistance = endPoint.DistanceTo(intersectPoint);

            if (startIntersectDistance > endIntersectDistance)
            {
                return Line.CreateBound(startPoint, intersectPoint);
            }
            else if (startIntersectDistance < endIntersectDistance)
            {
                return Line.CreateBound(intersectPoint, endPoint);
            }
            else
            {
                return curve;
            }
        }

        public void JoinFinishWalls(IList<Wall> finishWall)
        {
            IList<Arc> arcs = finishWall
                .Where(wall => (wall.Location as LocationCurve).Curve is Arc)
                .Select(wall => (wall.Location as LocationCurve).Curve as Arc)
                .ToList();

            double heigthLine;
            if (arcs.Count > 0)
            {
                heigthLine = arcs.First().Center.Z;
            }
            else
            {
                heigthLine = (room.Location as LocationPoint).Point.Z;
            }

            Wall fstWall = null;
            Wall sndWall = null;

            foreach (Wall wall in finishWall)
            {
                if (fstWall == null)
                {
                    fstWall = wall;
                }
                else
                {
                    sndWall = wall;

                    LocationCurve fstLocationCurve = fstWall.Location as LocationCurve;
                    LocationCurve sndLocationCurve = sndWall.Location as LocationCurve;

                    Curve fstCurve = fstLocationCurve.Curve.Clone();
                    Curve sndCurve = sndLocationCurve.Curve.Clone();

                    Curve newFstCurve;
                    Curve newSndCurve;

                    if (fstCurve is Line)
                    {
                        XYZ startPoint = fstCurve.GetEndPoint(0);
                        XYZ endPoint = fstCurve.GetEndPoint(1);

                        XYZ newStartPoint = new XYZ(startPoint.X, startPoint.Y, heigthLine);
                        XYZ newEndPoint = new XYZ(endPoint.X, endPoint.Y, heigthLine);

                        Line newLine = Line.CreateBound(newStartPoint, newEndPoint);

                        newFstCurve = newLine as Curve;
                    }
                    else if (fstCurve is Arc)
                    {
                        Arc fstArc = fstCurve as Arc;

                        XYZ centerArc = fstArc.Center;

                        if (centerArc.Z != heigthLine)
                        {
                            XYZ center = new XYZ(centerArc.X, centerArc.Y, heigthLine);
                            double radius = fstArc.Radius;
                            double startAngle = fstArc.GetEndParameter(0);
                            double endAngle = fstArc.GetEndParameter(1);
                            XYZ xAxis = fstArc.XDirection;
                            XYZ yAxis = fstArc.YDirection;

                            Arc newArc = Arc.Create(center, radius, startAngle, endAngle, xAxis, yAxis);

                            newFstCurve = newArc;
                        }
                        else
                        {
                            newFstCurve = fstArc;
                        }
                    }
                    else
                    {
                        newFstCurve = fstCurve;
                    }

                    if (sndCurve is Line)
                    {
                        XYZ startPoint = sndCurve.GetEndPoint(0);
                        XYZ endPoint = sndCurve.GetEndPoint(1);

                        XYZ newStartPoint = new XYZ(startPoint.X, startPoint.Y, heigthLine);
                        XYZ newEndPoint = new XYZ(endPoint.X, endPoint.Y, heigthLine);

                        Line newLine = Line.CreateBound(newStartPoint, newEndPoint);

                        newSndCurve = newLine as Curve;
                    }
                    else if (sndCurve is Arc)
                    {
                        Arc sndArc = sndCurve as Arc;

                        XYZ centerArc = sndArc.Center;

                        if (centerArc.Z != heigthLine)
                        {
                            XYZ center = new XYZ(centerArc.X, centerArc.Y, heigthLine);
                            double radius = sndArc.Radius;
                            double startAngle = sndArc.GetEndParameter(0);
                            double endAngle = sndArc.GetEndParameter(1);
                            XYZ xAxis = sndArc.XDirection;
                            XYZ yAxis = sndArc.YDirection;

                            Arc newArc = Arc.Create(center, radius, startAngle, endAngle, xAxis, yAxis);

                            newSndCurve = newArc;
                        }
                        else
                        {
                            newSndCurve = sndArc;
                        }
                    }
                    else
                    {
                        newSndCurve = sndCurve;
                    }

                    IntersectionResultArray intersectionResultArray;
                    if (newFstCurve.Intersect(newSndCurve, out intersectionResultArray) == SetComparisonResult.Overlap)
                    {
                        IList<IntersectionResult> intersectionResults = new List<IntersectionResult>();

                        foreach (IntersectionResult intersectionResult in intersectionResultArray)
                        {
                            intersectionResults.Add(intersectionResult);
                        }

                        if (intersectionResults.Count == 1)
                        {
                            XYZ intersectPoint = intersectionResults.First().XYZPoint;

                            if (!(fstCurve is Arc))
                            {
                                double zCoord = fstCurve.GetEndPoint(0).Z;
                                XYZ fstIntersectPoint = new XYZ(intersectPoint.X, intersectPoint.Y, zCoord);
                                fstCurve = CutAndLengthenCurve(fstCurve, fstIntersectPoint);
                            }
                            if (!(sndCurve is Arc))
                            {
                                double zCoord = sndCurve.GetEndPoint(0).Z;
                                XYZ sndIntersectPoint = new XYZ(intersectPoint.X, intersectPoint.Y, zCoord);
                                sndCurve = CutAndLengthenCurve(sndCurve, sndIntersectPoint);
                            }

                            fstLocationCurve.Curve = fstCurve;
                            sndLocationCurve.Curve = sndCurve;
                        }
                    }

                    fstWall = wall;
                }
            }
        }

        public IList<Line> GetJoinWallCurves(IList<SegmentItem> segments, double halfPastWallWith)
        {
            IList<SegmentItem> segmentItems = segments
                .Select(segment => segment)
                .ToList();

            segmentItems.Add(segments.First());

            SegmentItem fstSegment = null;
            SegmentItem sndSegment = null;

            IList<XYZ> offsetPoints = new List<XYZ>();
            foreach (SegmentItem segmentItem in segmentItems)
            {
                if (fstSegment is null)
                {
                    fstSegment = segmentItem;
                    continue;
                }

                sndSegment = segmentItem;

                Curve fstCurve = fstSegment.segmentCurve;
                Curve sndCurve = sndSegment.segmentCurve;
                
                XYZ fstFaceNormal = fstSegment.face.FaceNormal.Normalize() * halfPastWallWith;
                XYZ sndFaceNormal = sndSegment.face.FaceNormal.Normalize() * halfPastWallWith;

                Curve intersectCurve = func.GetIntersecitonCurveFromTwoFaces(fstSegment.face, sndSegment.face);

                if (intersectCurve is null) { continue; }

                //XYZ intersectPoint = intersectCurve.GetEndPoint();

                //XYZ centerPointFacesNormal = ((fstFaceNormal + sndFaceNormal) / 2).Normalize();

                //double fstAngle = fstFaceNormal.AngleTo(sndFaceNormal) / 2;

                //double lengthSndSide = Math.Abs(halfPastWallWith * Math.Tan(fstAngle));

                //double lengthCentralSide = Math.Pow(Math.Pow(halfPastWallWith, 2) + Math.Pow(lengthSndSide, 2), 0.5);

                //XYZ offsetXYZ = intersectPoint - centerPointFacesNormal * lengthCentralSide;

                //offsetPoints.Add(offsetXYZ);

                fstSegment = segmentItem;
            }
            //offsetPoints.Add(offsetPoints.First());

            IList<Line> lines = new List<Line>();

            //XYZ fstXYZ = null;
            //XYZ sndXYZ = null;

            //foreach (XYZ xyz in offsetPoints)
            //{
            //    if (fstXYZ is null) 
            //    { 
            //        fstXYZ = xyz; 
            //        continue; 
            //    }

            //    sndXYZ = xyz;

            //    try 
            //    { 
            //        lines.Add(Line.CreateBound(sndXYZ, fstXYZ));
            //    }
            //    catch
            //    {

            //    }
            //    fstXYZ = xyz;
            //}

            return lines;
        }

        public IList<Wall> CreateFinishWall(double offset, double heigth = 0, bool skipNoneSegments = false)
        {
            bool isStructural = false;
            bool flip = false;

            WallType wallType = document.GetElement(new ElementId(100918)) as WallType;
            double halfPastWallWith = wallType.Width / 2;

            ElementId levelIdRoom = room.LevelId;

            Level levelBase = (Level)document.GetElement(levelIdRoom);
            Parameter bottomLevelHeigth = levelBase.get_Parameter(BuiltInParameter.LEVEL_ELEV);

            IList<Wall> newFinishWalls = new List<Wall>();

            IList<SegmentItem> segmentItems = new List<SegmentItem>();

            IList<Face> otherFaces = new List<Face>();

            foreach (Face face in wallFaces)
            {
                BoundarySegment boundarySegment = GetSegmentSubsetFromFace(face);

                if (boundarySegment is null) { otherFaces.Add(face); continue; }

                Curve boundarySegmentCurve = boundarySegment.GetCurve();

                if (boundarySegmentCurve.Length < document.Application.ShortCurveTolerance) 
                { 
                    continue; 
                }

                if (face is PlanarFace && boundarySegmentCurve is Line)
                {
                    segmentItems.Add(new SegmentItem(face as PlanarFace, boundarySegmentCurve));
                }
                else if (face is CylindricalFace && boundarySegmentCurve is Arc)
                {

                }
            }

            GetJoinWallCurves(segmentItems, halfPastWallWith);


            //IList<Wall> walls = GetJoinWallCurves(segmentItems, halfPastWallWith)
            //    .Select(line => Wall.Create(document, line, wallType.Id, levelIdRoom, 3, 0, flip, isStructural))
            //    .ToList();

            //foreach (Face face in wallFaces)
            //{
            //XYZ centerFace = func.GetCenterPointFromFace(face);

            //IList<Curve> curves;

            //BoundarySegment boundarySegment = GetSegmentSubsetFromFace(face, out curves);
            //}

            //    if (skipNoneSegments && (boundarySegment is null)) { continue; }
            //    if (func.SegmentIsModelLine(document, boundarySegment)) { continue; }

            //    FinishWallItem wallItem = GetWallTypeFromBoundarySegment(boundarySegment);

            //    if (wallItem is null) { continue; }
            //    if (wallItem.finishWall is null) { continue; }

            //    WallType wallType = wallItem.finishWall.wallType;

            //    if (wallType is null) { continue; }

            //    ElementId wallTypeId = wallType.Id;

            //    IList<XYZ> allPointsFromCurves = func.GetAllXYZPointFromCurves(curves);

            //    double minZ = allPointsFromCurves.Select(point => point.Z).Min();

            //    double maxZ = allPointsFromCurves.Select(point => point.Z).Max();

            //    XYZ minZPoint = allPointsFromCurves.Where(point => point.Z == minZ).First();

            //    XYZ maxZPoint = allPointsFromCurves.Where(point => point.Z == maxZ).First();

            //    double baseOffsetHeigth = minZ - bottomLevelHeigth.AsDouble();

            //    double topHeigthWall;
            //    if (heigth != 0)
            //    {
            //        double convertHeigth = func.UnitConverter(heigth, true, bottomLevelHeigth.GetUnitTypeId());
            //        topHeigthWall = convertHeigth;
            //    }
            //    else
            //    {
            //        topHeigthWall = maxZ - bottomLevelHeigth.AsDouble();
            //    }

            //    double halfPastWallWith = wallType.Width / 2;

            //    if (face is PlanarFace)
            //    {
            //        Wall newPlanarWall = null;

            //        PlanarFace planarFace = face as PlanarFace;

            //        XYZ faceNormal = planarFace.FaceNormal;
            //        XYZ directionWall = -faceNormal * halfPastWallWith;
            //        Transform translate = Transform.CreateTranslation(directionWall);

            //        IList<Curve> curvesHalfPastWall = curves
            //            .Select(curve => curve.CreateTransformed(translate))
            //            .ToList();

            //        Curve bottomCurve = func.GetBottomCurve(curvesHalfPastWall, minZ);

            //        try
            //        {
            //            if (heigth != 0)
            //            {
            //                //newPlanarWall = Wall.Create(
            //                //    document, bottomCurve, wallTypeId, levelIdRoom, topHeigthWall, baseOffsetHeigth, flip, isStructural);
            //            }
            //            else
            //            {
            //                //newPlanarWall = Wall.Create(
            //                //    document, curvesHalfPastWall, wallTypeId, levelIdRoom, isStructural, directionWall);
            //            }
            //        }
            //        catch (Autodesk.Revit.Exceptions.InvalidOperationException)
            //        {
            //            //newPlanarWall = Wall.Create(
            //            //    document, bottomCurve, wallTypeId, levelIdRoom, topHeigthWall, baseOffsetHeigth, flip, isStructural);
            //        }

            //        if (newPlanarWall is null) { continue; }

            //        bool resultSetParameterWall = SetParametersWall(
            //            newPlanarWall, offset, boundarySegment, wallItem.hasGround, baseOffsetHeigth, topHeigthWall);

            //        if (resultSetParameterWall)
            //        {
            //            newFinishWalls.Add(newPlanarWall);
            //        }
            //    }
            //    else if (face is CylindricalFace)
            //    {
            //        Wall newArcWall = null;

            //        CylindricalFace cylindricalFace = face as CylindricalFace;

            //        Arc arc = func.GetBottomArcFromCylindricalFace(curves);

            //        Arc arcHalfPastWall = func.SetArcFromHalfPastWidth(room, halfPastWallWith, centerFace, arc);

            //        //newArcWall = Wall.Create(
            //        //    document, arcHalfPastWall, wallTypeId, levelIdRoom, topHeigthWall, baseOffsetHeigth, flip, isStructural);

            //        if (newArcWall is null) { continue; }

            //        bool resultSetParameterWall = SetParametersWall(
            //            newArcWall,
            //            offset,
            //            boundarySegment,
            //            wallItem.hasGround,
            //            baseOffsetHeigth,
            //            topHeigthWall
            //        );

            //        if (resultSetParameterWall)
            //        {
            //            newFinishWalls.Add(newArcWall);
            //        }
            //    }
            //    else if (face is RuledFace)
            //    {
            //        RuledFace ruledFace = face as RuledFace;

            //        Wall newSplineWall = null;

            //        double ruledHalfPastWallWith = halfPastWallWith + 0.009;

            //        HermiteSpline bottomHermiteSpline = func.GetBottomHermiteSpline(curves, minZ);

            //        if (bottomHermiteSpline is null) { continue; }

            //        IList<Line> lines = func.SetSplineFromHalfPastWidth(bottomHermiteSpline, ruledHalfPastWallWith);
            //        foreach (Line line in lines)
            //        {
            //            double currentZCoord = line.GetEndPoint(0).Z;
            //            baseOffsetHeigth = currentZCoord - bottomLevelHeigth.AsDouble();

            //            //newSplineWall = Wall.Create(document, line, wallTypeId, levelIdRoom, topHeigthWall, baseOffsetHeigth, flip, isStructural);

            //            if (newSplineWall is null) { continue; }

            //            bool resultSetParameterWall = SetParametersWall(
            //                newSplineWall, offset, boundarySegment, wallItem.hasGround, baseOffsetHeigth, topHeigthWall);

            //            if (resultSetParameterWall)
            //            {
            //                newFinishWalls.Add(newSplineWall);
            //            }
            //        }
            //    }
            //}

            //JoinFinishWalls(newFinishWalls);

            return newFinishWalls;
        }

        //public IList<Wall> CreateFinishWallForHeigth(double offset, double heigth = 0, bool skipNoneSegments = false)
        //{
        //    bool isStructural = false;
        //    bool flip = false;

        //    ElementId levelIdRoom = room.LevelId;

        //    Level levelBase = (Level)document.GetElement(levelIdRoom);
        //    Parameter bottomLevelHeigth = levelBase.get_Parameter(BuiltInParameter.LEVEL_ELEV);

        //    IList<Wall> newFinishWalls = new List<Wall>();

        //    IList<Curve> curves = new List<Curve>();
        //    foreach (Face face in floorFaces)
        //    {
        //        foreach (Curve curve in func.GetCurvesFromFace(face))
        //        {
        //            curves.Add(curve);
        //        }
        //    }
        //    //GetSegmentSubsetFromFace()

        //    return newFinishWalls;
        //}
    }

    class SegmentItem
    {
        public PlanarFace face { get; }
        public Curve segmentCurve { get; }

        public SegmentItem(PlanarFace face, Curve segmentCurve)
        {
            this.face = face;
            this.segmentCurve = segmentCurve;
        }
    }
}
