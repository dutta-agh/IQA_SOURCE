using IQA_SOURCE.Models.Admin;
using System.Text.Json;

namespace IQA_SOURCE.Services
{
    public class AssessmentSequenceService : IAssessmentSequenceService
    {
        private readonly ILogger<AssessmentSequenceService> _logger;

        // Session key for storing assessment sequence
        private const string SessionKey = "AssessmentSequence";

        public AssessmentSequenceService(ILogger<AssessmentSequenceService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Initializes a new assessment sequence for a user
        /// </summary>
        public void InitializeSequence(string assessmentType, string sessionId, HttpContext httpContext)
        {
            try
            {
                // ✅ Create new sequence
                var sequence = new AssessmentSequence
                {
                    AssessmentType = assessmentType,
                    SessionId = sessionId,
                    CurrentStep = AssessmentSequence.SequenceStep.Index,
                    StartedAt = DateTime.UtcNow,
                    IsCompleted = false,
                    StepData = new Dictionary<string, object>()
                };

                // ✅ Serialize and store in session
                var json = JsonSerializer.Serialize(sequence);
                httpContext.Session.SetString(SessionKey, json);

                _logger.LogInformation($"✅ Assessment sequence initialized - Type: {assessmentType}, SessionId: {sessionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error initializing assessment sequence");
                throw;
            }
        }

        /// <summary>
        /// Retrieves the current assessment sequence from session
        /// </summary>
        public AssessmentSequence GetCurrentSequence(HttpContext httpContext)
        {
            try
            {
                // ✅ Get from session
                var json = httpContext.Session.GetString(SessionKey);
                
                if (string.IsNullOrEmpty(json))
                {
                    _logger.LogWarning("⚠️ No assessment sequence found in session");
                    return null;
                }

                // ✅ Deserialize and return
                var sequence = JsonSerializer.Deserialize<AssessmentSequence>(json);
                _logger.LogInformation($"✅ Retrieved sequence - Current Step: {sequence?.CurrentStep}");
                return sequence;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving assessment sequence");
                return null;
            }
        }

        /// <summary>
        /// Saves data at each step of the assessment
        /// </summary>
        public void SaveStepData(AssessmentSequence.SequenceStep step, Dictionary<string, object> data, HttpContext httpContext)
        {
            try
            {
                var sequence = GetCurrentSequence(httpContext);
                
                if (sequence == null)
                {
                    _logger.LogWarning($"⚠️ Cannot save step data - No sequence found for step: {step}");
                    return;
                }

                // ✅ Save step data
                sequence.StepData[step.ToString()] = data;

                // ✅ Update session
                var json = JsonSerializer.Serialize(sequence);
                httpContext.Session.SetString(SessionKey, json);

                _logger.LogInformation($"✅ Step data saved - Step: {step}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error saving step data for {step}");
                throw;
            }
        }

        /// <summary>
        /// Validates if user can access the requested step
        /// </summary>
        public bool ValidateStep(string assessmentType, AssessmentSequence.SequenceStep requestedStep, HttpContext httpContext)
        {
            try
            {
                var sequence = GetCurrentSequence(httpContext);

                // ✅ If no sequence exists, only Index is allowed
                if (sequence == null)
                {
                    var isAllowed = requestedStep == AssessmentSequence.SequenceStep.Index;
                    _logger.LogInformation($"⚠️ No sequence exists - {(isAllowed ? "Allowing Index access" : "Blocking access to " + requestedStep)}");
                    return isAllowed;
                }

                // ✅ Assessment type must match
                if (sequence.AssessmentType != assessmentType)
                {
                    _logger.LogWarning($"❌ Assessment type mismatch - Stored: {sequence.AssessmentType}, Requested: {assessmentType}");
                    return false;
                }

                // ✅ Can only access current step or next step
                bool isValid = requestedStep <= sequence.CurrentStep + 1;

                if (!isValid)
                {
                    _logger.LogWarning($"❌ Invalid step access - Current: {sequence.CurrentStep}, Requested: {requestedStep}");
                }
                else
                {
                    _logger.LogInformation($"✅ Step validation passed - Current: {sequence.CurrentStep}, Requested: {requestedStep}");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error validating step");
                return false;
            }
        }

        /// <summary>
        /// Marks the assessment as completed and moves to the last step
        /// </summary>
        public void CompleteSequence(HttpContext httpContext)
        {
            try
            {
                var sequence = GetCurrentSequence(httpContext);
                
                if (sequence == null)
                {
                    _logger.LogWarning("⚠️ Cannot complete sequence - No sequence found");
                    return;
                }

                // ✅ Mark as completed
                sequence.IsCompleted = true;
                sequence.CurrentStep = AssessmentSequence.SequenceStep.ImageAssessment;

                // ✅ Update session
                var json = JsonSerializer.Serialize(sequence);
                httpContext.Session.SetString(SessionKey, json);

                _logger.LogInformation($"✅ Assessment sequence completed - SessionId: {sequence.SessionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error completing sequence");
                throw;
            }
        }

        /// <summary>
        /// Resets/clears the sequence from session
        /// </summary>
        public void ResetSequence(HttpContext httpContext)
        {
            try
            {
                // ✅ Remove from session
                httpContext.Session.Remove(SessionKey);
                _logger.LogInformation("✅ Assessment sequence reset");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error resetting sequence");
                throw;
            }
        }
    }
}