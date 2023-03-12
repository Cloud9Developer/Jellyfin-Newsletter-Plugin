# Jellyfin Newsletter Plugin

This is my first end-to-end C# project, but I hope you enjoy!


# Current Limitations
1. This plugin uses google's image search API to pull poster images for the newsletter. Google limits their API (for free accounts to 100/day from what I can tell).
    - Sign up to get an API key in order to use this plugin here: https://support.google.com/googleapi/answer/6158862?hl=en
    - You will also need to get a CX key. Follow the instructions here: https://stackoverflow.com/questions/6562125/getting-a-cx-id-for-custom-search-google-api-python

2. This plugin can only process series at this point in time. Series must follow standard naming practices:
    - /PATH/TO/SERIES/My_Series/Season1/MySeries_Name-S1E01.mp4
        - The important part of the above path is that the SeasonX folder is there, and the end of the filename '-S1E01.{ext}' since this plugin parses the filenames to get series name, Episode number and Season
        - The number of digits for Season and Episode above shouldn't matter (i.e. 01, 001, 00001, etc.), but I've only tested with formatting S01E001)

3. There is no custom formatting to the newsletter (yet). My plan is to add this functionality in a later release, but would like to iron out some finer details before moving on to that.

# Installation

The current build file for this plugin is already uploaded here in Jellyfin-Newsletter-Plugin/Jellyfin.Plugin.Newsletters/bin/Debug/net6.0/
Just copy create a folder in your Jellyfin Plugin directory (I named mine Newsletters_alpha) and copy the .dll file to that new folder. 

Once copied, restart Jellyfin and you should see it in your plugins!


# Issues
I expect there to be quite a few issues when people start using this plugin. Please leave a ticket in the Issues on this GitHub page and I will get to it as soon as I can. 
Please be patient with me though, since I did this on the side of my normal job. But I will try to fix any issues that come up to the best of my ability and as fast as I can!
