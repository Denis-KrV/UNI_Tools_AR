
using Autodesk.Revit.DB;
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

        public XYZ CentralPoint => XYZ.Zero;

        public ReferenceIntersector referenceIntersector =>
            new ReferenceIntersector(_document.ActiveView as View3D) { FindReferencesInRevitLinks = true };

        public InsolationObject(Document document, SunAndShadowSettings sunAndShadowObject, Functions functions)
        {
            _functions = functions;
            _document = document;
            _sunAndShadowObject = sunAndShadowObject;
        }

        public IList<XYZ> GetSunTimePoints()
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


            return _functions.HalfPastPoint(_functions.HalfPastPoint(geometryArc.Tessellate()));
        }

        public IList<SunSegment> ReturnNonIntersectionObject(XYZ insolationCountPoint)
        {
            IList<SunSegment> result = new List<SunSegment>();
            IList<GeometryObject> geometryObjects = new List<GeometryObject> ();
            IList<XYZ> segmentPoint = new List<XYZ>();
            
            XYZ fstPoint = null;
            
            foreach (XYZ point in GetSunTimePoints())
            {
                ReferenceWithContext refs = referenceIntersector
                    .FindNearest(insolationCountPoint, point.Normalize());

                if (refs is null)
                {
                    segmentPoint.Add(point);

                    if (fstPoint is null)
                    {
                        fstPoint = point;
                        continue;
                    }

                    fstPoint = point;
                }
                else
                {
                    if ((fstPoint != null))
                    {
                        fstPoint = null;

                        SunSegment sunSegment = 
                            new SunSegment(segmentPoint, _functions.GetSumAngleInPoints(segmentPoint));

                        result.Add(sunSegment);

                        segmentPoint = new List<XYZ>();
                    }
                }
            }
            
            return result
                .Where(sunSegment => sunSegment.points.Count() > 1)
                .ToList();
        }
    }
    class SunSegment
    {
        public XYZ fstPoint => points.First();
        public XYZ sndPoint => points.Last();

        public IList<XYZ> points;
        public double angle;
        public SunSegment(IList<XYZ> points, double angle)
        { 
            this.points = points; 
            this.angle = angle; 
        }

        public double CountTime()
        {
            double agnleSecond = 0.00417;
            return angle / agnleSecond;
        }
    }
}
