# Jellyfin Newsletter Plugin
![Newsletter Logo](https://github.com/Cloud9Developer/Jellyfin-Newsletter-Plugin/blob/master/NewslettersLogo.png?raw=true)

This is my first end-to-end C# project, but I hope you enjoy!

# Description
This plugin automacially scans a users library (default every 4 hours), populates a list of *recently added (not previously scanned)* media, converts that data into HTML format, and sends out emails to a provided list of recipients.

![Newsletter Example](https://github.com/Cloud9Developer/Jellyfin-Newsletter-Plugin/blob/master/NewsletterExample.png?raw=true)

# Current Limitations
1. This plugin uses Imgur's API to upload poster images for newsletter emails to fetch images. Imgur (according to their Documentation) limits uploads to 1,250/day. 
    - **This plugin is configured to reference existing Images from previous scans (including current) as to not duplicate requests to Imgur and use up the daily upload limit**
    - Sign up to get an API key in order to use this plugin.
        - Helpful Links:
            - https://dev.to/bearer/how-to-configure-the-imgur-api-2ap9
            - http://siberiancmscustomization.blogspot.com/2020/10/how-to-get-imgur-client-id.html

2. This plugin can only process series at this point in time.

3. There is no custom formatting to the newsletter (yet). My plan is to add this functionality in a later release, but would like to iron out some finer details before moving on to that.

# Installation

The current build file for this plugin is already uploaded here in `Jellyfin-Newsletter-Plugin/Jellyfin.Plugin.Newsletters/bin/Debug/net6.0/`
Just copy create a folder in your Jellyfin Plugin directory (I named mine Newsletters_alpha) and copy the .dll file to that new folder. 

Once copied, restart Jellyfin and you should see it in your plugins!

*Note:* I am currently working on getting the Manifest.json file working. Once that is completed, users will then be able get updates automatically


# Issues
I expect there to be quite a few issues when people start using this plugin. Please leave a ticket in the Issues on this GitHub page and I will get to it as soon as I can. 
Please be patient with me though, since I did this on the side of my normal job. But I will try to fix any issues that come up to the best of my ability and as fast as I can!
