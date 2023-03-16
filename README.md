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
    - I do plan on adding this functionality in a near-future release

3. There is no custom formatting to the newsletter (yet). My plan is to add this functionality in a later release, but would like to iron out some finer details before moving on to that.

# Testing/Run Frequency

Testing and Frequency can be managed through your Dashboard > Scheduled Tasks

- There are 2 scheduled tasks:
    - Email Newsletter: Which generates and sends out the newsletters via email from the data scanned from the task below
    - Filesystem Scraper:  Which scans your library, parses the data, and gets it ready for the email

# Installation

Manifest is up an running! You can now import the manifest in Jellyfin and this plugin will appear in the Catalog!
- Go to "Plugins" on your "Dashboard"
- Go to the "Repositories" tab
- Click the '+' to add a new Repository
    - Give it a name (i.e. Newsletters)
    - In "Repository URL," put "https://raw.githubusercontent.com/Cloud9Developer/Jellyfin-Newsletter-Plugin/master/manifest.json"
    - Click "Save"
- You should now see Jellyfin Newsletters in Catalog under the Category "Newsletters"
- Once installed, restart Jellyfin to activate the plugin and configure your settings for the plugin

# Configuration

## General Config

### To Addresses:
- Recipients of the newsletter. Add add as many emails as you'd like, separated by commas.
    - All emails will be sent out via BCC

### From Address
- ***Currently not functioning properly***
- The address recipients will see on emails as the sender
    - Defaults to JellyfinNewsletter@donotreply.com

### Subject
- The subject of the email

## Scraper/Scanner Config

### Scraper Config > Imgur API Key
- Your Imgur API key to upload images to be available in the newsletter

## SMTP Config

### Smtp Server Address
- The email server address you want to use. 
    - Defaults to smtp.gmail.com

### Smtp Port
- The port number used by the email server above
    - Defaults to gmail's port (587)

### Smtp Username
- Your username/email to authenticate to the SMTP server above

### Smtp Password
- Your password to authenticate to the SMTP server above
    - I'm not sure about other email servers, but google requires a Dev password to be created.
        - For gmail specific instructions, you can visit https://support.google.com/mail/answer/185833?hl=en for details

# Issues
Please leave a ticket in the Issues on this GitHub page and I will get to it as soon as I can. 
Please be patient with me, since I did this on the side of my normal job. But I will try to fix any issues that come up to the best of my ability and as fast as I can!

## Known Issues
- 'From Address' in setting/config page does nothing right now
