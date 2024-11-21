
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UNI_Tools_AR.CountInsolation
{
    internal class InsolationObject
    {
        static private Functions _functions { get; set; }

        private Document _document;
        private SunAndShadowSettings _sunAndShadowObject;

        private Options gOptions => new Options {
            View = _document.ActiveView,
            IncludeNonVisibleObjects = true
        };

        private IList<GeometryObject> geometry => _sunAndShadowObject
            .get_Geometry(gOptions)
            .Select(gElement => gElement)
            .ToList();

        public IList<HermiteSpline> hermiteSplines => geometry
            .Where(gElement => gElement is HermiteSpline)
            .Select(gElement => gElement as HermiteSpline)
            .ToList();

        public IList<XYZ> sunTimePoint { get; set; }
        public XYZ CentralPoint => XYZ.Zero;

        public ReferenceIntersector referenceIntersector => CreateReferenceIntersector();

        public InsolationObject(
            Document document,
            SunAndShadowSettings sunAndShadowObject,
            Functions functions
        )
        {
            _functions = functions;
            _document = document;
            _sunAndShadowObject = sunAndShadowObject;

            sunTimePoint = GetSunTimePoints();
        }

        private ReferenceIntersector CreateReferenceIntersector()
        {
            ElementFilter notSunClassFilter = 
                new ElementClassFilter(typeof(SunAndShadowSettings), true);

            FindReferenceTarget findReferenceTargetAll = FindReferenceTarget.All;
            View3D activeView3D = _document.ActiveView as View3D;

            ReferenceIntersector referenceIntersector =
                new ReferenceIntersector(notSunClassFilter, findReferenceTargetAll, activeView3D);

            referenceIntersector.FindReferencesInRevitLinks = true;

            return referenceIntersector;
        }

        public IList<XYZ> CreateRotatePointsInArc(
            XYZ centerPoint, double radius, XYZ direction, XYZ centerArc
        )
        {
            Transform transformGetCenterPoint = 
                Transform.CreateRotation(centerPoint, Math.PI);

            XYZ fstPoint = transformGetCenterPoint.OfPoint(direction);
            XYZ sndPoint = transformGetCenterPoint.OfPoint(fstPoint);
            XYZ centerPointCurrentRotation = ((fstPoint + sndPoint) / 2); 

            Transform transform = 
                Transform.CreateRotation(centerPoint, Constants.angleRotation);

            XYZ rotate = direction;
            IList<XYZ> resultPoint = new List<XYZ>();
            for (int i = 1; i < Constants.countRotation; i++)
            {
                rotate = transform.OfPoint(rotate);
                XYZ translateInCenterRotationPoint = 
                    (rotate - centerPointCurrentRotation).Normalize();

                XYZ arcPoint = translateInCenterRotationPoint * radius + centerArc;

                if (arcPoint.Z >= 0) 
                {
                    resultPoint.Add(arcPoint);
                }
            }
            return resultPoint;
        }

        private IList<XYZ> GetSunTimePoints()
        {
            IList<Line> geometryLines = geometry
                .Where(gElement => gElement is Line)
                .Select(gElement => gElement as Line)
                .ToList();

            double maxLenght = geometryLines
                .Select(line => line.Length)
                .Max();

            Line maxLenghtLine = geometryLines
                .Where(line => line.Length == maxLenght)
                .First();

            XYZ fstPoint = maxLenghtLine.GetEndPoint(0);
            XYZ sndPoint = maxLenghtLine.GetEndPoint(1);

            XYZ centralPointCurrentSun = (fstPoint + sndPoint) / 2;

            IList<XYZ> sunTimePoints = geometry
                .Where(gElement => gElement is Point)
                .Select(gElement => (gElement as Point).Coord - centralPointCurrentSun)
                .ToList();

            XYZ fstArcPoint = sunTimePoints[0];
            XYZ sndArcPoint = sunTimePoints[1];

            XYZ pointInArc = sunTimePoints[(sunTimePoints.Count() / 2)];

            Arc geometryArc = Arc.Create(fstArcPoint, sndArcPoint, pointInArc);

            XYZ fstPointArc = geometryArc.GetEndPoint(0);
            XYZ sndPointArc = geometryArc.GetEndPoint(1);

            XYZ normalPoint = geometryArc.Normal.Negate();
            XYZ planeNormalPoint = new XYZ(normalPoint.X, normalPoint.Y, 0).Normalize();

            XYZ centerArc = geometryArc.Center;
            double radius = geometryArc.Radius;

            XYZ axis = geometryArc.XDirection;

            IList<XYZ> points = 
                CreateRotatePointsInArc(normalPoint, radius, planeNormalPoint, centerArc);
            
            return points;
        }

        public IList<SunSegment> ReturnNonIntersectionObject(XYZ insolationCountPoint)
        {
            IList<SunSegment> result = new List<SunSegment>();

            IList<XYZ> segmentPoint = new List<XYZ>();

            foreach (XYZ point in sunTimePoint)
            {
                ReferenceWithContext refs = referenceIntersector
                    .FindNearest(insolationCountPoint, point);

                if (refs is null)
                { 
                    segmentPoint.Add(point); 
                }
                else if (segmentPoint.Count > 0)
                {
                    SunSegment sunSegment = new SunSegment(segmentPoint);
                    result.Add(sunSegment);
                    segmentPoint = new List<XYZ>();
                }
            }
            if (segmentPoint.Count > 0)
            {
                SunSegment sunSegment = new SunSegment(segmentPoint);
                result.Add(sunSegment);
            }
            
            return result;
        }
    }
    class SunSegment
    {
        public XYZ fstPoint => points.First();
        public XYZ sndPoint => points.Last();
        public double angle => CountAngle();
        public double time => angle / Constants.angleOneSecond;

        public IList<XYZ> points;
        
        public SunSegment(IList<XYZ> points)
        { 
            this.points = points; 
        }

        private double CountAngle()
        {
            double angle = 0;
            for (int i = 0; i < points.Count(); i++)
            {
                if (i == 0) { continue; }
                XYZ pointFst = points[i-1];
                XYZ pointSnd = points[i];
                angle += pointFst.AngleTo(pointSnd);
            }
            return 180 * angle / Math.PI;
        }
    }
}
