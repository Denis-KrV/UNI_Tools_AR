using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System.Collections.Generic;
using System.Linq;

namespace UNI_Tools_AR.CreateFinish
{
    public class WarningResolver : IFailuresPreprocessor
    {
        public Document document { get; set; }
        public WarningResolver(Document document) 
        { 
            this.document = document;
        }

        public FailureProcessingResult PreprocessFailures(FailuresAccessor accessor)
        {
            IList<FailureMessageAccessor> failures = accessor.GetFailureMessages();

            foreach (FailureMessageAccessor failureMessageAccesor in failures)
            {
                if (failureMessageAccesor.HasResolutionOfType(FailureResolutionType.DetachElements))
                {
                    failureMessageAccesor.SetCurrentResolutionType(FailureResolutionType.DetachElements);
                    FailureSeverity failureSeverity = accessor.GetSeverity();

                    if (failureSeverity == FailureSeverity.Error || failureSeverity == FailureSeverity.Warning)
                    {
                        accessor.ResolveFailure(failureMessageAccesor);
                        return FailureProcessingResult.ProceedWithCommit;
                    }
                }
                else if (failureMessageAccesor.HasResolutionOfType(FailureResolutionType.DeleteElements))
                {
                    failureMessageAccesor.SetCurrentResolutionType(FailureResolutionType.DeleteElements);
                    FailureSeverity failureSeverity = accessor.GetSeverity();

                    IList<ElementId> fallingElmentsId = failureMessageAccesor
                        .GetFailingElementIds()
                        .Where(elementId => document.GetElement(elementId) is Room)
                        .Select(elementId => elementId)
                        .ToList();

                    if (fallingElmentsId.Count != 0)
                    {
                        return FailureProcessingResult.ProceedWithRollBack;
                    }

                    if (failureSeverity == FailureSeverity.Error || failureSeverity == FailureSeverity.Warning)
                    {
                        accessor.ResolveFailure(failureMessageAccesor);
                        return FailureProcessingResult.ProceedWithCommit;
                    }
                }
                else
                {
                    accessor.DeleteWarning(failureMessageAccesor);
                }
            }
            return FailureProcessingResult.Continue;
        }
    }
}
