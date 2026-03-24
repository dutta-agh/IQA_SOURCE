namespace IQA_SOURCE.Models.Admin
{
    /// <summary>
    /// Represents the state of an assessment sequence for a user
    /// Tracks progress through assessment steps and stores step-specific data
    /// </summary>
    public class AssessmentSequence
    {
        /// <summary>
        /// Enum defining the progression steps in an assessment
        /// </summary>
        public enum SequenceStep
        {
            /// <summary>
            /// Initial step - assessment introduction and consent
            /// </summary>
            Index = 0,

            /// <summary>
            /// Color blindness/vision test step
            /// </summary>
            ColorblindnessTest = 1,

            /// <summary>
            /// Speed and connectivity test step
            /// </summary>
            SpeedTest = 2,

            /// <summary>
            /// Questions/survey step
            /// </summary>
            Questions = 3,

            /// <summary>
            /// Final step - image assessment (Sort or Rating)
            /// </summary>
            ImageAssessment = 4
        }

        /// <summary>
        /// Session key constant for storing assessment sequence in HttpContext.Session
        /// </summary>
        public const string SessionKey = "AssessmentSequence";

        /// <summary>
        /// The type of assessment (e.g., "Sort", "Rating", "image_sort", "image_rating")
        /// </summary>
        public string AssessmentType { get; set; }

        /// <summary>
        /// Unique session identifier for this assessment attempt
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Current step in the assessment sequence (0-4)
        /// </summary>
        public SequenceStep CurrentStep { get; set; }

        /// <summary>
        /// Timestamp when the assessment started (UTC)
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// Dictionary storing data/responses collected at each step
        /// Key: Step name, Value: Step-specific data
        /// </summary>
        public Dictionary<string, object> StepData { get; set; } = new();

        /// <summary>
        /// Whether the assessment has been completed
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Creates a new assessment sequence instance
        /// </summary>
        /// <param name="assessmentType">Type of assessment (Sort, Rating, etc.)</param>
        /// <param name="sessionId">Unique session identifier</param>
        /// <returns>New AssessmentSequence with Index as initial step</returns>
        public static AssessmentSequence CreateNew(string assessmentType, string sessionId)
        {
            return new AssessmentSequence
            {
                AssessmentType = assessmentType,
                SessionId = sessionId,
                CurrentStep = SequenceStep.Index,
                StartedAt = DateTime.UtcNow,
                IsCompleted = false,
                StepData = new Dictionary<string, object>()
            };
        }

        /// <summary>
        /// Advances the sequence to the next step
        /// </summary>
        /// <param name="step">The step to move to</param>
        /// <param name="stepData">Optional data to save at this step</param>
        public void MoveToStep(SequenceStep step, Dictionary<string, object> stepData = null)
        {
            // ✅ Only allow advancing to next step or staying on current
            if (step > CurrentStep + 1)
            {
                throw new InvalidOperationException(
                    $"Cannot skip steps. Current: {CurrentStep}, Requested: {step}");
            }

            CurrentStep = step;

            // ✅ Save step data if provided
            if (stepData != null)
            {
                StepData[step.ToString()] = stepData;
            }
        }

        /// <summary>
        /// Gets the elapsed time since assessment started
        /// </summary>
        /// <returns>TimeSpan representing elapsed time</returns>
        public TimeSpan GetElapsedTime()
        {
            return DateTime.UtcNow - StartedAt;
        }

        /// <summary>
        /// Gets step data for a specific step if it exists
        /// </summary>
        /// <param name="step">The step to retrieve data for</param>
        /// <returns>Step data or null if not found</returns>
        public object GetStepData(SequenceStep step)
        {
            return StepData.TryGetValue(step.ToString(), out var data) ? data : null;
        }

        /// <summary>
        /// Gets step data as a typed object for a specific step
        /// </summary>
        /// <typeparam name="T">Type to deserialize to</typeparam>
        /// <param name="step">The step to retrieve data for</param>
        /// <returns>Deserialized step data or default if not found</returns>
        public T GetStepData<T>(SequenceStep step) where T : class
        {
            if (StepData.TryGetValue(step.ToString(), out var data))
            {
                if (data is T typedData)
                {
                    return typedData;
                }

                // Try deserializing if it's a JSON string
                if (data is string jsonString)
                {
                    try
                    {
                        return System.Text.Json.JsonSerializer.Deserialize<T>(jsonString);
                    }
                    catch
                    {
                        return null;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if all required steps have been completed
        /// </summary>
        /// <returns>True if assessment is fully completed</returns>
        public bool IsFullyCompleted()
        {
            return IsCompleted && CurrentStep == SequenceStep.ImageAssessment;
        }

        /// <summary>
        /// Gets human-readable step name
        /// </summary>
        /// <param name="step">The step to get name for</param>
        /// <returns>Human-readable step name</returns>
        public static string GetStepName(SequenceStep step)
        {
            return step switch
            {
                SequenceStep.Index => "Welcome",
                SequenceStep.ColorblindnessTest => "Color Vision Test",
                SequenceStep.SpeedTest => "Speed Test",
                SequenceStep.Questions => "Survey Questions",
                SequenceStep.ImageAssessment => "Image Assessment",
                _ => "Unknown"
            };
        }
    }
}