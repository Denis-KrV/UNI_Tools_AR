
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UNI_Tools_AR.CountInsolation
{
    internal class InsolationObject
    {
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

        public XYZ CentralPoint => GetCentralPoint();

        public IList<XYZ> SunTimePoint => GetSunTimePoints();

        public InsolationObject(Document document, SunAndShadowSettings sunAndShadowObject)
        {
            _document = document;
            _sunAndShadowObject = sunAndShadowObject;
        }

        private IList<XYZ> GetSunTimePoints()
        {
            return geometry
                .Where(gElement => gElement is Point)
                .Select(gElement => (gElement as Point).Coord)
                .ToList();
        }

        private XYZ GetCentralPoint()
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

            return (fstPoint + sndPoint) / 2;
        }
    }
}
