<a href="https://mod.io"><img src="https://static.mod.io/v1/images/branding/modio-color-dark.svg" alt="mod.io" width="360" align="right"/></a>
# Unity Engine Plugin
[![License](https://img.shields.io/badge/license-MIT-brightgreen.svg)](https://github.com/modio/UnityPlugin/blob/master/LICENSE)
[![Discord](https://img.shields.io/discord/389039439487434752.svg?label=Discord&logo=discord&color=7289DA&labelColor=2C2F33)](https://discord.mod.io)
[![Master docs](https://img.shields.io/badge/docs-master-green.svg)](https://github.com/modio/UnityPlugin/wiki)
[![Unity 3D](https://img.shields.io/badge/Unity-2017.3+-lightgrey.svg)](https://unity3d.com/)

Welcome to [mod.io](https://mod.io) Unity Plugin. It allows game developers to easily control the browsing and installation of User-Generated Content in UGC-supported games. The C# interface built on the Unity Engine provides an easy way of connecting to the [mod.io API](https://docs.mod.io). We have a [test environment](https://test.mod.io) available which offers developers a private sandbox to try the Unity Plugin out.

<p align="center"><a href="https://assetstore.unity.com/packages/templates/systems/mod-browser-manager-138866"><img src="https://cdn-images-1.medium.com/max/1600/1*eopj1hgjlJJZ8Q9l8dNVBA.png"></a></p>

## Features
* Platform agnostic (support 1 click mod installs on Steam, Epic Games Store, Discord, GOG, itch.io and even consoles in the future)
* Clientless (it has no other dependencies and works behind the scenes in your game)
* Ready-to-go, fully functional and customizable mod browsing UI
* C# interface built on the Unity Engine for connecting to the [mod.io API](https://docs.mod.io/)
* Powerful search, filtering and tagging of mods
* Player preference and accounty management

## Installation
Requires **Unity 2017.3** or later. Tested on Windows, and MacOS.

There are several ways to install the mod.io Unity Plugin:

### Package Manager
The best way is to install this library as a [Git package](https://docs.unity3d.com/Manual/upm-git.html) using the Package Manager.
First, make sure that you to have Git installed and available in your system's PATH.
After that you need to add the following line to your project's `manifest.json`:
```
"com.modio.unityplugin": "https://github.com/modio/UnityPlugin.git",
```

It should look like this, notice the comma at the end of the line:
```
{
  "dependencies": {
    "com.modio.unityplugin": "https://github.com/modio/UnityPlugin.git",
    "com.unity.package-manager-ui": "1.0.0",
    "com.unity.modules.ai": "1.0.0",
    "com.unity.modules.animation": "1.0.0",
    ...
  }
}
```

If you don't want to use Git, you can download this library as an archive and install it as a [local package](https://docs.unity3d.com/Manual/upm-ui-local.html).

### Asset Store or .unitypackage
The other way is to import a package from the [Asset Store](https://assetstore.unity.com/packages/templates/systems/mod-browser-manager-138866)
or the [Releases page](https://github.com/modio/UnityPlugin/releases).
If you have any previous versions of the plugin installed, it is highly recommended to delete them before importing a newer version.

## Getting started

1. Implement support for user-generated content in your project. Maps, skins, or game modes are often a good place to start.
1. Set up your [game profile on mod.io](https://mod.io/games/add) (or our [private test environment](https://test.mod.io/games/add)) to get your game ID and API key.
1. Add the plugin to your game using the installation instructions above.
1. Drop the _ModBrowser prefab into your menu scene, or adapt the Example Scene for your purposes.
1. Input your ID and API key by selecting "Plugin Settings" on the ModBrowser component inspector, or under the Tools/mod.io/Edit Settings menu item
1. In your code, make a call to `ModManager.GetInstalledModDirectories()` to get a list of mod data your player has installed (read our wiki for [detailed instructions](https://github.com/modio/UnityPlugin/wiki))
1. Setup complete! Join us [on Discord](https://discord.mod.io) if you have questions or need help.

All mods [submitted to mod.io](https://mod.io/mods/add) will be automatically fetched and managed by the plugin, and are instantly downloadable and testable.

## Usage
### Browse Mods
```java
// -- Get as many mods as possible (unfiltered) --
APIClient.GetAllMods(RequestFilter.None,
                     null,
                     (r) => OnModsReceived(r.items),
                     (e) => OnError(e));


// -- Get a specified subset of filtered mods --
RequestFilter filter = new RequestFilter();
filter.sortFieldName = API.GetAllModsFilterFields.dateLive;
filter.isSortAscending = false;
filter.fieldFilters[API.GetAllModsFilterFields.name]
	= new StringLikeFilter() { likeValue = "mod" };

APIPaginationParameters pagination = new APIPaginationParameters()
{
	offset = 20,
	limit = 10,
};

APIClient.GetAllMods(filter
                     pagination
                     (r) => OnModsReceived(r.items),
                     (e) => OnError(e));
```

### User Authentication
```java
// -- Authenticate using external service using wrapper functions --
UserAccountManagement.AuthenticateWithGOGEncryptedAppTicket(ticketData, ticketSize,
                                                            (userProfile) => OnUserAuthenticated(userProfile),
                                                            (e) => OnError(e));



// -- Authenticate via email-flow manually using APIClient --
APIClient.SendSecurityCode("player@email_address.com",
                           (message) => OnSecurityCodeSent(),
                           (e) => OnError(e));

APIClient.GetOAuthToken(securityCodeFromEmail,
                        (token) => OnTokenReceived(token),
                        (e) => OnError(e));

LocalUser.instance = new LocalUser();
LocalUser.OAuthToken = receivedOAuthToken;

APIClient.GetAuthenticatedUser((userProfile) => OnProfileReceived(userProfile),
                               (e) => OnError(e));

LocalUser.Profile = userProfile;
LocalUser.Save();
```

### Manage Subscriptions
```java
// -- Sub/Unsubscribe --
UserAccountManagement.SubscribedToMod(modId);
UserAccountManagement.UnsubscribeFromMod(modId);

// -- Fetch and Store ---
APIClient.GetUserSubscriptions(RequestFilter.None,
                               null,
                               (subscribedMods) => OnSubscriptionsReceived(subscribedMods),
                               (e) => OnError(e));

int[] modIds = Utility.MapProfileIds(subscribedMods);
LocalUser.SubscribedModIds = new List<int>(modIds);

// -- Download, Update, and Install Subscribed Mods --
activeSceneComponent.StartCoroutine(ModManager.DownloadAndUpdateMods_Coroutine(modIds,
                                                                               () => OnCompleted()));
```

### Submit Mods
```java
// -- Changes to a Mod Profile --
EditableModProfile modEdits = EditableModProfile.CreateFromProfile(existingModProfile);
modEdits.name.value = "Updated Mod Name";
modEdits.name.isDirty = true;
modEdits.tags.value = new string[] { "campaign" };
modEdits.tags.isDirty = true;

ModManager.SubmitModChanges(modId,
                            modEdits,
                            (updatedProfile) => OnChangesSubmitted(updatedProfile),
                            (e) => OnError(e));

// -- Upload a new build --
EditableModfile modBuildInformation = new EditableModfile();
modBuildInformation.version.value = "1.2.0";
modBuildInformation.version.isDirty = true;
modBuildInformation.version.changelog = "Changes were made!";
modBuildInformation.version.isDirty = true;
modBuildInformation.version.metadatBlob = "Some game-specific metadata";
modBuildInformation.version.isDirty = true;

ModManager.UploadModBinaryDirectory(modId,
                                    modBuildInformation,
                                    true, // set as the current build
                                    (modfile) => OnUploaded(modfile),
                                    (e) => OnError(e));
```

## Dependencies
The [mod.io](https://mod.io) Unity Plugin requires the functionality of two other open-source Unity plugins to run. These are included as libraries in the UnityPackage in the `Plugins` directory, or in the repository under `third_party`:
* Json.Net for improved Json serialization. ([GitHub Repo](https://github.com/SaladLab/Json.Net.Unity3D) || [Unity Asset Store Page](https://assetstore.unity.com/packages/tools/input-management/json-net-for-unity-11347))
* DotNetZip for Unity to zip and unzip transmitted files. ([GitHub Repo](https://github.com/r2d2rigo/dotnetzip-for-unity))

## Benefits
mod.io offers the same core functionality as Steamworks Workshop (1 click mod installs in-game), plus mod hosting, moderation and all of the critical pieces needed. Where we differ is our approach to modding and the flexibility a REST API offers. For example: 

* Our API is not dependent on a client or SDK, allowing you to run mod.io in many places such as your homepage and launchers
* Designing a good mod browsing UI is hard, our plugin ships with a UI built in to save you a lot of effort and help your mods stand out
* We don’t apply rules globally, so if you want to enable patronage, sales or other experimental features, reach out to discuss
* Our platform is built by the super experienced ModDB.com team and is continually improving for your benefit
* Your community can consume the mod.io API to build modding fan sites or discord bots if they want
* Communicate and interact with your players, using our built-in emailer

## Large studios and Publishers
A private white label option is available to license, if you want a fully featured mod-platform that you can control and host in-house. [Contact us](mailto:developers@mod.io?subject=Whitelabel) to discuss.

## Contributions Welcome
Our Unity plugin is public and open source. Game developers are welcome to utilize it directly, to add support for mods in their games, or fork it for their games customized use. Want to make changes to our plugin? Submit a pull request with your recommended changes to be reviewed.

## Other Repositories
Our aim with [mod.io](https://mod.io), is to provide an open modding API. You are welcome to [view, fork and contribute to our other codebases](https://github.com/modio) in use:

* [Design](https://design.mod.io) is public and open source, the repository can be [found here](https://github.com/modio/WebDesign).
* [API documentation](https://docs.mod.io) is public and open source, the repository can be [found here](https://github.com/modio/APIDocs).
* [Browse engine tools](https://apps.mod.io), plugins and wrappers created by the community, or [share your own](https://apps.mod.io/add).
* [Unreal Engine 4 plugin](https://github.com/modio/UE4Plugin), easily manage the browsing and install of mods in Unreal Engine 4 games
* [Python wrapper](https://github.com/ClementJ18/mod.io), a python wrapper for the mod.io API
* [Rust wrapper](https://github.com/nickelc/modio-rs), rust interface for mod.io
* And more...
