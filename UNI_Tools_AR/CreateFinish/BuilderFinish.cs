using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;

namespace UNI_Tools_AR.CreateFinish
{
    abstract class BuilderFinish
    {
        public Document document { get; set; }
        public Room room { get; set; }
        public Funcitons func => new Funcitons();

        public SpatialElementBoundaryOptions spatialOptions { get; } =
            new SpatialElementBoundaryOptions()
            {
                StoreFreeBoundaryFaces = true,
                SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish,
            };

        public SpatialElementGeometryCalculator spatialCalculator =>
            new SpatialElementGeometryCalculator(document, spatialOptions);

        public SpatialElementGeometryResults spatialGeometryResults => GetSpatialElementGeometryResults();
        public Options geometryOptions { get; } = new Options() { DetailLevel = ViewDetailLevel.Fine };
        public Solid solid => GetSolid();

        public IList<Face> wallFaces => GetWallFaces();
        public IList<Face> floorFaces => GetFloorFaces();
        public IList<Face> ceilingFaces => GetCeiligFaces();

        public IList<BoundarySegment> curvesForBoundarySegments => GetBoundarySegments();

        public const double minLengthWall = 0.07;

        public SpatialElementGeometryResults GetSpatialElementGeometryResults()
        {
            try
            {
                return spatialCalculator.CalculateSpatialElementGeometry(room);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                return null;
            }
        }

        public Solid GetSolid()
        {
            if (spatialGeometryResults is null)
            {
                GeometryElement geometryElement = room.get_Geometry(geometryOptions);
                IEnumerator<GeometryObject> enumElement = geometryElement.GetEnumerator();
                enumElement.MoveNext();
                return (Solid)enumElement.Current;
            }
            return spatialGeometryResults.GetGeometry();
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
                if (faceNormal.Z < 0) { floorFaces.Add(face); }
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
                if (faceNormal.Z > 0) { ceilingFaces.Add(face); }
            }
            return ceilingFaces;
        }

        private IList<BoundarySegment> GetBoundarySegments()
        {
            IList<BoundarySegment> resultBoundarySegments = new List<BoundarySegment>();

            foreach (IList<BoundarySegment> boundarySegments in room.GetBoundarySegments(spatialOptions))
            {
                foreach (BoundarySegment boundarySegment in boundarySegments)
                {
                    resultBoundarySegments.Add(boundarySegment);
                }
            }
            return resultBoundarySegments;
        }

    }
}
