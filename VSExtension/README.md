# epicor-editor README

This is an experimental Visual Studio Code Extension which allows you to Edit, Test and Debug Epicor Customizations right from VS Code
# THIS IS IN BETA, DO NOT USE IN PRODUCTION, BACKUP YOUR CUSTOMIZATION BEFORE USING.


## Features

This is an extension which will allow you to Edit, Test and Debug Epicor Customizations right from VS Code. It can be quite handy specially for the code auto complete feature that VS Code Provides.

I would love some testers, and some feedback, open issues in GitHub. Thanks!

Here is a how to video of how it works, how to use it etc.

[![Youtube How To](https://img.youtube.com/vi/JTZqZcwWnv8/0.jpg)](https://youtu.be/JTZqZcwWnv8 "Visual Studio Code Epicor Customization Editor")


##Tool box Demo

[![Youtube Toolbox Demo](https://img.youtube.com/vi/lWd4L-QNZpM/0.jpg)](https://youtu.be/lWd4L-QNZpM "Visual Studio Code Customization Extension ToolBox Feature Demo")




## Requirements

* You will need to have the [OmniSharp Extension] (https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp) Installed to get this benefit to so install that first.

* If you want to be able to debug using DNSpy you will have to download and install [DNSpy] (https://github.com/0xd4d/dnSpy/releases) and extract it into a folder of your choosing

* Please note you will have to download the helper library from the following links (based on your version of Epicor) and unzip it into your client folder

	[10.2.400.X](https://josecgomez.com/files/CustomizationHelper.10.2.400.X.zip?0.41.0)

    [10.2.300.X](https://josecgomez.com/files/CustomizationHelper.10.2.300.X.zip?0.41.0)

    [10.2.200.X](https://josecgomez.com/files/CustomizationHelper.10.2.200.X.zip?0.41.0)

    [10.2.100.X](https://josecgomez.com/files/CustomizationHelper.10.2.100.X.zip?0.41.0)

    [10.1.600.X](https://josecgomez.com/files/CustomizationHelper.10.1.600.X.zip?0.41.0)

    [10.1.500.X](https://josecgomez.com/files/CustomizationHelper.10.1.500.X.zip?0.41.0)

* Whenever there is a new version of the VS Code extension, odds are that there is a new version of the helper library too. So download it again (every time the extension changes). I know its annoying I'm working on a more automated way    

## Extension Settings

This extension contributes the following settings:

* `epicor.clientfolder`: Should point to the folder where your Epicor client is installed
* `epicor.customizationfolder`: Should point to a folder where you'd like all the customization projects downloaded / created in
* `epicor.dnspylocationr`: The folder in which you installed DNSpy 

## Screenshots
Here is a few animations of the extension in Action

![Opening a Customization](images/VSCodeOpen.gif)

![Editing / Running a Customization](images/VSCodeSyncTest.gif)

![Debugging a Customization using DnSpy](images/VSCodeDebug.gif)

![Additional ToolBox Features](images/HammerDrop.gif)


## Known Issues



## Release Notes

### 0.0.9

Initial release 

### 0.7.0
* Added support for 10.2.200

### 0.14.0
* Fixed a bunch of issues, download a new version of the helper lib.

### 0.15.0
* Added support for Dashboard Customizations
* Fixed UD support
* Fixed exported customization format
* Escaped XML conflicting characters

### 0.16.0
* Added support for MES Customization

### 0.17.0
* Fixed issue with versioning added Sync Check to Commit to ensure that no overrides occur accidentally.
* Changed plain text password to encrypted password for extra security

### 0.18.0
* Bug Fix

### 0.19.0
* Versioned Links

### 0.20.0
* Added Download Only Option

### 0.21.0
* Changed to Beta status for initial release

### 0.22.0
* Added support for omnisharp restart

### 0.23.0
* Initial beta release

### 0.24.0
* Added version popup checker alert to the extension.

### 0.27.0
* Added 10.1.500 support

### 0.28.0
* Fixed themeing

### 0.31.0
* Added the toolbox feature which allows a lot of new stuff to happen
* ToolBox will bring up a form which will allow you to launch several epicor screens within the environment
* ToolBox will also allow you to live edit back and fourth the code in the script and in epicor
* ToolBox will allow you to Run and Test your customization
* See new youtube demo above
![ToolBox Demo](images/ToolBoxDemo.gif)

### 0.32.0
* Fixed issue with global companie vs local company customizations with the same Customer ID

### 0.37.0
* Removed Launch, Debut and Edit options, that functionality has moved to the toolbox
* Added support for 10.2.100.X [Thanks Brett!!](https://github.com/bmanners)

### 0.38.0
* Added Object Explorer option to toolbox


### 0.38.0
* Added Data Tools to ToolBox
* Added Code Wiazard to ToolBox
* Added Reference Manager to ToolBox
* Added Two Way Sync with Notification for Epicor Edits to prevent over-writes
* Added SSO Sing on Option

### 0.41.0
* Bug Fixes

### 0.42.0
* Conflict Alert for Epicor Sync

### 0.43.0
* SSO Bug Fix

### 0.44.0
* Added support for 10.2.400.X

### 0.45.0
* Fixed a bunch of github issues and added loggin