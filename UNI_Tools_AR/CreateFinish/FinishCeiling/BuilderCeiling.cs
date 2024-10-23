using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UNI_Tools_AR.CreateFinish.FinishCeiling
{
    internal class BuilderCeiling : BuilderFinish
    {
        public BuilderCeiling(Document document, Room room)
        {
            this.document = document;
            this.room = room;
        }

        public IList<Ceiling> CreateFinishCeilingForRoomGeometry(
            CeilingType ceilingType, double heigthValue = 0, bool hasGround = true
        )
        {
            Level level = document.GetElement(room.LevelId) as Level;

            Parameter levelElev_Par = level.get_Parameter(BuiltInParameter.LEVEL_ELEV);

            double convertHeigthValue = func.UnitConverter(heigthValue, true, levelElev_Par.GetUnitTypeId());

            heigthValue = convertHeigthValue;

            IList<Ceiling> newFinishCeilings = new List<Ceiling>();

            foreach (Face face in ceilingFaces)
            {
                IList<CurveLoop> curveLoops = face.GetEdgesAsCurveLoops();
                IList<Curve> curves = func.GetCurvesFromFace(face);

                double slopeFace = func.SlopeFace(face);
                Line slopeLine = func.GetSlopeLine(face);
                XYZ centerFace = func.GetCenterPointFromFace(face);

                if (func.isInclineFace(face) & (func.SlopeFace(face) != 0.0))
                {
                    IList<Curve> lowerCurves = func.LowerCeilingCurves(curves);
                    
                    CurveLoop lowerCurveLoop = new CurveLoop();
                    foreach (Curve curve in lowerCurves)
                    {
                        lowerCurveLoop.Append(curve);
                    }
                    
                    IList<CurveLoop> newCurveLoops = new List<CurveLoop> { lowerCurveLoop };

                    if (!BoundaryValidation.IsValidHorizontalBoundary(newCurveLoops)) { continue; }

                    try
                    {
                        Ceiling newCeiling = Ceiling.Create(document, newCurveLoops, ceilingType.Id, room.LevelId, slopeLine, slopeFace);
                        newFinishCeilings.Add(newCeiling);   
                    }
                    catch (Autodesk.Revit.Exceptions.InternalException) { }
                }
                else
                {
                    if (!BoundaryValidation.IsValidHorizontalBoundary(curveLoops)) { continue; }

                    try
                    {
                        Ceiling newCeiling = Ceiling.Create(document, curveLoops, ceilingType.Id, room.LevelId);
                        newFinishCeilings.Add(newCeiling);
                    }
                    catch (Autodesk.Revit.Exceptions.InternalException) { }
                }

                foreach (Ceiling newCeiling in newFinishCeilings)
                {
                    Parameter heigthAbovLevel_Par = newCeiling
                        .get_Parameter(BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM);

                    heigthAbovLevel_Par.Set(heigthValue);
                }
            }

            return newFinishCeilings;
        }
    }
}
