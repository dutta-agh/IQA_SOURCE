using IQA_SOURCE.Models.Admin;

namespace IQA_SOURCE.Services
{
    public interface IAssessmentSequenceService
    {
        /// <summary>
        /// Initializes a new assessment sequence for a user
        /// </summary>
        void InitializeSequence(string assessmentType, string sessionId, HttpContext httpContext);

        /// <summary>
        /// Retrieves the current assessment sequence from session
        /// </summary>
        AssessmentSequence GetCurrentSequence(HttpContext httpContext);

        /// <summary>
        /// Saves data at each step of the assessment
        /// </summary>
        void SaveStepData(AssessmentSequence.SequenceStep step, Dictionary<string, object> data, HttpContext httpContext);

        /// <summary>
        /// Validates if user can access the requested step
        /// </summary>
        bool ValidateStep(string assessmentType, AssessmentSequence.SequenceStep step, HttpContext httpContext);

        /// <summary>
        /// Marks the assessment as completed
        /// </summary>
        void CompleteSequence(HttpContext httpContext);

        /// <summary>
        /// Resets/clears the sequence from session
        /// </summary>
        void ResetSequence(HttpContext httpContext);
    }
}