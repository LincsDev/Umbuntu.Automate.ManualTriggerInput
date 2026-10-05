export default {
  umbuntuRunWithInput: {
    actionLabel: "Run with input…",
    headline: "Run with input",
    automation: "Automation",
    inputLabel: "Trigger input",
    inputDescription: "Passed to the automation's steps as",
    inputRequired: "Enter the input to run the automation with",
    inputSize: "%0% of %1%",
    inputTooLarge: "The input is too large. The maximum is %0%.",
    run: "Run",
    startedHeadline: "Run started",
    startedMessage: "The automation is running with your input.",
    failedHeadline: "Could not run automation",
    failedMessage: "The server returned %0%.",
    networkError: "The server could not be reached. Check your connection and try again.",
  },
} as const;
