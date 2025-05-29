using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UNI_Tools_AR.CreateFinishWithStair
{
    internal class Functions
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
// The rest of the file content was truncated by the read_file tool
// ...
                }
            }
            return null; // Placeholder due to truncation
        }
    }
} 