# Note for Fork
- Added Rabbit.Net source code for One Click Build
- For c# newbie

# BuildBackup

**Note**: This application is no longer actively supported. While parts of it might still work, I don't actively make an effort to make sure it _keeps_ working.

## Description
BuildBackup was created to back up [CASC](https://wowdev.wiki/CASC) data from Blizzard's CDN. Blizzard often removes data for older builds from their CDN, making them unavailable for install. The goal is to have it back up all data needed to install a specific version for a specific application.

In addition to the backup functionality there are also several utilities implemented to dump information/extract data from the CASC filesystem.

## Supported products
Basic data for all [product codes](https://wowdev.wiki/CASC#NGDP_Program_Codes) is supported (when available on CDN), but BuildBackup currently supports full data backups for the following applications:
- World of Warcraft
- Battle.net Agent
- Battle.net App

## Configuration
Files will be saved in the path specified in a ```config.json``` file like :
```
{
	"config":{
		"cacheDir":"/var/www/wow.tools/"
	}
}
```

## Thanks
- WoWDev wiki authors
- Blizzard
