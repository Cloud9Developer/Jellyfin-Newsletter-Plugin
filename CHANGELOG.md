# 0.6.1
- Updated html template (hardcoded date)

# 0.6.0
- Added ability to use custom HTML for newsletter emails
- Added buildscript (using dotnet docker) for easier development

# 0.5.1
- Cleaned up logging for easier readibility

# 0.5.0
- Added JF Poster Serving Functionality
    - Allows users to set HOSTNAME/URL to their JF server to serve poster images directly from JF, bypassing any limitations on 3rd parties such as Imgur
- Plugin config page optimization/cleanup

# 0.4.0
- Added movie Functionality

# 0.3.3
- Added Test Mail button (tsfoni)
- Fixed issue where sometimes episode list would cause error due to empty list

# 0.3.2
- Fixed/cleaned newsletter episode formatting/counting and ranges
- Re-org of some logic to catch more errors
    - This will allow scan to continue even after error occurs instead of halting the scan
- Updated Logo
- Updated Template HTML to match what is currently being used
- Cleanup

# 0.3.1
- Fixed/cleaned newsletter episode formatting/counting and ranges
- Updated Logo
- Updated Template HTML to match what is currently being used
- Cleanup

# 0.3.0
- Major overhaul to backend data processing
    - Now implementing SQLite services instead of TXT formatting.
        - ***This will affect current users! Scans prior to this release will not be imported over. I apologize for this inconvenience***
- Added a catch to Imgur's upload limit.
    - Processing will now stop when limit is reached, and resume on the next scan
- Progress bar functionality is now operational in Scheduled Tasks
- Code cleanup
- Merged cleanup/readability submitted by **tsfoni**

# 0.0.5
- Major overhaul to backend file processing
    - Removed requirement/limitation on google image search API
        - Now pulling posters from Jellyfin Server
    - Now pulling episode list from Jellyfin server rather than manually discovering files in filesystem
        - This will allow future improvements, such as Movie scanning

# 0.0.1
- Initial Alpha Release
