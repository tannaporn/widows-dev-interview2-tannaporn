Build a widows-dev-interview2 Windows desktop app using C#, WPF, and .NET 8.

Workflow
- Read AGENTS.md and inspect existing code first.
- Use the `grill-me` skill, or `grilling` if unavailable, to clarify requirements until you fully understand them.
- Summarize requirements, assumptions, and a step-by-step implementation and testing plan.
- Wait for my explicit approval before starting each step, including edits, installation, builds, and tests.
- Report results after each step and request approval for the next. Ask again before changing the approved scope. Silence is not approval.

Requirements

- Thai UI with month and Gregorian-year selectors.
- Fetch NASA APOD using api_key, start_date, end_date, and thumbs=true.
- Handle leap years, the earliest APOD date (June 16, 1995), and the current date in US Eastern time. Reject future months.
- Show newest entries first, with small thumbnails, titles, and dates.
- Show the selected entry’s image, full details, credits, and expandable, copyable JSON, preserving additional API fields.
- Support images and videos, with HTTPS links to original media and high-resolution images.
- Use a clean navy, blue, and white design. Display listdetails side by side on wide windows and vertically on narrow windows; support a minimum size of 600×640.
- Load asynchronously, support cancellation, and prevent stale requests from overwriting newer results.
- Provide loading, empty, image-fallback, and clear error states.
- Read API keys from NASA_API_KEY, local settings, or a masked input. Never commit or log secrets.
- Handle invalid settings, authentication failures, rate limits, network errors, timeouts, and malformed responses.

Implementation

- Use beginner-friendly code, descriptive names, simple methods, and code-behind.
- Separate models, API services, configuration, and views.
- Avoid unnecessary frameworks and dependencies; preserve existing work.
- Test date handling, response parsing, errors, cancellation, and responsive layouts.
- Provide a Thai README covering setup, build, run, testing, and publishing.
- Report actual verification results and distinguish mocked tests from live NASA checks.

Start with clarification only. Do not implement until I approve.

After finished task use skill code-review