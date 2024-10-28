
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UNI_Tools_AR.CountInsolation
{
    internal class InsolationObject
    {
        private Functions _functions { get; set; }

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
            new ReferenceIntersector(_document.ActiveView as View3D);

        public InsolationObject(Document document, SunAndShadowSettings sunAndShadowObject, Functions functions)
        {
            _functions = functions;
            _document = document;
            _sunAndShadowObject = sunAndShadowObject;
        }

        public IList<XYZ> GetSunTimePoints(XYZ insolationCountPoint)
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
                .Select(gElement => (gElement as Point).Coord - centralPointCurrentSun + insolationCountPoint)
                .ToList();

            XYZ fstArcPoint = sunTimePoints[0];
            XYZ sndArcPoint = sunTimePoints[1];

            XYZ pointInArc = sunTimePoints[(sunTimePoints.Count() / 2)];

            Arc geometryArc = Arc.Create(fstArcPoint, sndArcPoint, pointInArc);

            return geometryArc.Tessellate();
        }

        public IList<XYZ> ReturnNonIntersectionObject(XYZ insolationCountPoint)
        {
            IList<XYZ> result = new List<XYZ>();


            IList<GeometryObject> geometryObjects = new List<GeometryObject> ();
            foreach (XYZ point in GetSunTimePoints(insolationCountPoint))
            {
                ReferenceWithContext refs = referenceIntersector.FindNearest(insolationCountPoint, point);

                if (refs is null)
                {
                    result.Add(point);
                    geometryObjects.Add(Line.CreateBound(insolationCountPoint,point));
                }
            }

            using (Transaction t = new Transaction(_document, "test"))
            {
                t.Start();
                _functions.CreateDirectShape(_document, geometryObjects);
                t.Commit();
            }

            return result;
        }
    }

}
