using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace UNI_Tools_AR.CreateFinish
{
    internal class WarningSkipper : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor accessor)
        {
            IList<FailureMessageAccessor> failures = accessor.GetFailureMessages();
            foreach (FailureMessageAccessor failureMessageAccessor in failures)
            {
                FailureDefinitionId id = failureMessageAccessor.GetFailureDefinitionId();
                FailureSeverity failureSeverity = accessor.GetSeverity();
                if (failureSeverity == FailureSeverity.Error || failureSeverity == FailureSeverity.Warning)
                {
                    accessor.DeleteWarning(failureMessageAccessor);
                }
            }
            return FailureProcessingResult.Continue;
        }
    }
}
