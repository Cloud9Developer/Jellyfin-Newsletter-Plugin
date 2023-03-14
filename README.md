# Jellyfin Newsletter Plugin
![Newsletter Logo](https://github.com/Cloud9Developer/Jellyfin-Newsletter-Plugin/blob/master/NewslettersLogo.png?raw=true)

This is my first end-to-end C# project, but I hope you enjoy!

# Description
This plugin automacially scans a users library (default every 4 hours), populates a list of *recently added (not previously scanned)* media, converts that data into HTML format, and sends out emails to a provided list of recipients.

![Newsletter Example](https://github.com/Cloud9Developer/Jellyfin-Newsletter-Plugin/blob/master/NewsletterExample.png?raw=true)

# Current Limitations
1. This plugin uses Imgur's API to upload poster images for newsletter emails to fetch images. Imgur (according to their Documentation) limits uploads to 12,500/day. 
    - HOWEVER, according to some documentation I have just discovered, there is a limit of 500 requests/hour for each user _(IP address)_ hitting the API
    - **This plugin is configured to reference existing Images from previous scans (including current) as to not duplicate requests to Imgur and use up the daily upload limit**
    - Sign up to get an API key in order to use this plugin.
        - Helpful Links:
            - https://dev.to/bearer/how-to-configure-the-imgur-api-2ap9
            - http://siberiancmscustomization.blogspot.com/2020/10/how-to-get-imgur-client-id.html

2. This plugin can only process series at this point in time.

3. There is no custom formatting to the newsletter (yet). My plan is to add this functionality in a later release, but would like to iron out some finer details before moving on to that.

# Installation

Manifest is up an running! You can now import the manifest in Jellyfin and this plugin will appear in the Catalog!
- Go to "Plugins" on your "Dashboard"
- Go to the "Repositories" tab
- Click the '+' to add a new Repository
    - Give it a name (i.e. Newsletters)
    - In "Repository URL," put "https://raw.githubusercontent.com/Cloud9Developer/Jellyfin-Newsletter-Plugin/master/manifest.json"
    - Click "Save"
- You should now see Jellyfin Newsletters in Catalog under the Category "Newsletters!"

# Issues
I expect there to be quite a few issues when people start using this plugin. Please leave a ticket in the Issues on this GitHub page and I will get to it as soon as I can. 
Please be patient with me though, since I did this on the side of my normal job. But I will try to fix any issues that come up to the best of my ability and as fast as I can!

## Known Issues
- When Imgur API reaches it's user limit, all scanned items will have a blank poster image. 
    - This will be fixed in the next release by stopping the current scan, finishing the work on the current scan data, and when the next scheduled scan is run, resume from where it left off
