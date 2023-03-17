# TBD
- Added Test Mail button (tsfoni)

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
