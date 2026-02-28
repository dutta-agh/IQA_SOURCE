// Global application configuration
const APP_CONFIG = {
    SUB_APPLICATION_PATH: '@IQA_SOURCE.Constants.SubApplicationPathJS',
    ASSESSMENT_BASE_URL: '@IQA_SOURCE.Constants.AssessmentBaseUrl',
    
    // Helper function to build API URLs
    buildUrl: function(path) {
        // Remove leading slash if present to avoid double slashes
        const cleanPath = path.startsWith('/') ? path.substring(1) : path;
        return this.SUB_APPLICATION_PATH + '/' + cleanPath;
    },
    
    // Helper for AJAX calls with automatic path prefixing
    ajax: function(options) {
        // Clone options to avoid modifying original
        const ajaxOptions = { ...options };
        
        // Only prefix relative URLs that don't already start with SUB_APPLICATION_PATH or http/https
        if (ajaxOptions.url && 
            !ajaxOptions.url.startsWith('http') && 
            !ajaxOptions.url.startsWith(this.SUB_APPLICATION_PATH) &&
            !ajaxOptions.url.startsWith('/')) {  // Added check for absolute paths
            ajaxOptions.url = this.buildUrl(ajaxOptions.url);
        }
        
        return $.ajax(ajaxOptions);
    }
};

// Backward compatibility - keep as global constants
const SUB_APPLICATION_PATH = APP_CONFIG.SUB_APPLICATION_PATH;
const ASSESSMENT_BASE_URL = APP_CONFIG.ASSESSMENT_BASE_URL;

// Helper function for quick URL building (global)
function buildApiUrl(path) {
    return APP_CONFIG.buildUrl(path);
}

// Helper function to build full assessment URLs for display only
function buildAssessmentUrl(slug) {
    return ASSESSMENT_BASE_URL + SUB_APPLICATION_PATH + '/Assessment/' + slug;
}