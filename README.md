# Jellyfin Newsletter Plugin
<p align='center'>
    <img src='https://github.com/Cloud9Developer/Jellyfin-Newsletter-Plugin/blob/master/logo.png?raw=true'/><br>
</p>
This is my first end-to-end C# project, but I hope you enjoy!

# Description
This plugin automacially scans a users library (default every 4 hours), populates a list of *recently added (not previously scanned)* media, converts that data into HTML format, and sends out emails to a provided list of recipients.

<p align='center'>
    <img src='https://github.com/Cloud9Developer/Jellyfin-Newsletter-Plugin/blob/master/NewsletterExample.png?raw=true'/><br>
</p>

# Current Limitations
1. This plugin uses Imgur's API to upload poster images for newsletter emails to fetch images. Imgur (according to their Documentation) limits uploads to 12,500/day. 
    - HOWEVER, according to some documentation I have just discovered, there is a limit of 500 requests/hour for each user _(IP address)_ hitting the API
    - **This plugin is configured to reference existing Images from previous scans (including current) as to not duplicate requests to Imgur and use up the daily upload limit**
    - Sign up to get an API key in order to use this plugin.
        - Helpful Links:
            - https://dev.to/bearer/how-to-configure-the-imgur-api-2ap9
            - http://siberiancmscustomization.blogspot.com/2020/10/how-to-get-imgur-client-id.html

2. There is no custom formatting to the newsletter (yet). My plan is to add this functionality in a later release, but would like to iron out some finer details before moving on to that.

# File Structure
To ensure proper images are being pulled from Jellyfin's database, ensure you follow the standard Organization Scheme for naming and organizing your files. https://jellyfin.org/docs/general/server/media/books

If this format isn't followed properly, Jellyfin may have issue correctly saving the item's data in the proper database (the database that this plugin uses).

```
Shows
├── Series (2010)
│   ├── Season 00
│   │   ├── Some Special.mkv
│   │   ├── Episode S00E01.mkv
│   │   └── Episode S00E02.mkv
│   ├── Season 01
│   │   ├── Episode S01E01-E02.mkv
│   │   ├── Episode S01E03.mkv
│   │   └── Episode S01E04.mkv
│   └── Season 02
│       ├── Episode S02E01.mkv
│       ├── Episode S02E02.mkv
│       ├── Episode S02E03 Part 1.mkv
│       └── Episode S02E03 Part 2.mkv
└── Series (2018)
    ├── Episode S01E01.mkv
    ├── Episode S01E02.mkv
    ├── Episode S02E01-E02.mkv
    └── Episode S02E03.mkv

Movies
├── Film (1990).mp4
├── Film (1994).mp4
├── Film (2008)
│   └── Film.mkv
└── Film (2010)
    ├── Film-cd1.avi
    └── Film-cd2.avi
```

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
- The address recipients will see on emails as the sender
    - Defaults to JellyfinNewsletter@donotreply.com

### Subject
- The subject of the email

### Library Selection
- Select the item types you want to scan
    - NOTE: this is Item types, not libraries

## Scraper/Scanner Config

### Poster Hosting Type
- The type of poster hosting you want to use
    - Options include:
        - Imgur (Default)
        - Local Hosting from Jellyfin's API  

### Imgur API Key
- Your Imgur API key (Client ID) to upload images to be available in the newsletter

### Hostname
- Your servername/hostname/DNS entry (and Port if applicable) to allow users to access images hosted locally on your server.
    - i.e. https://myDNSentry.com:8096 
        - **NOTE:** do not put a trailing '/' at the end of the url

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
See 'issues' tab in GitHub with the lable 'bug'

# Contribute
If you would like to collaborate/contribute, feel free! Make all PR's to the 'development' branch and please note clearly what was added/fixed, thanks!
