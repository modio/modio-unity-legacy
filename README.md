<a href="https://mod.io"><img src="https://static.mod.io/v1/images/branding/modio-color-dark.svg" alt="mod.io" width="400"/></a>

# Unity Engine Plugin
[![License](https://img.shields.io/badge/license-MIT-brightgreen.svg)](https://github.com/DBolical/modioUNITY/blob/master/LICENSE)
[![Discord](https://img.shields.io/discord/389039439487434752.svg)](https://discord.mod.io)
[![Master docs](https://img.shields.io/badge/docs-master-green.svg)](https://github.com/DBolical/modioUNITY/wiki)
[![Unity 3D](https://img.shields.io/badge/Unity-2018.2-lightgrey.svg)](https://unity3d.com/)


Welcome to [mod.io](https://mod.io) Unity Plugin. It allows game developers to easily control the browsing and installation of mod files in their games. It provides a C# interface built on the Unity Engine to connect to the [mod.io API](https://docs.mod.io). We have a [test environment](https://test.mod.io) available which offers developers a private sandbox to try the Unity Plugin out.

## Getting started
If you are a game developer, first step is to add mod support to your Unity game. Once mod support is up and running, [create your games profile](https://mod.io/games/add) on mod.io, to get an API key and access to all [functionality mod.io offers](https://apps.mod.io/guides/getting-started).
Next, download the latest [UnityPackage release](https://github.com/DBolical/modioUNITY/releases) and unpack it into your project, then head over to the [GitHub Wiki](https://github.com/DBolical/modioUNITY/wiki) and follow the guides to get it running within your game.

## Dependencies
The [mod.io](https://mod.io) Unity Plugin requires the functionality of two other open-source Unity plugins to run. These are included as libraries in the UnityPackage in the `Plugins` directory, or in the repository under `third_party`:
* Json.Net for improved Json serialization. ([GitHub Repo](https://github.com/SaladLab/Json.Net.Unity3D) || [Unity Asset Store Page](https://assetstore.unity.com/packages/tools/input-management/json-net-for-unity-11347))
* DotNetZip for Unity to zip and unzip transmitted files. ([GitHub Repo](https://github.com/r2d2rigo/dotnetzip-for-unity))

## Contributions Welcome
Our Unity plugin is public and open source. Game developers are welcome to utilize it directly, to add support for mods in their games, or fork it for their games customized use. Want to make changes to our plugin? Submit a pull request with your recommended changes to be reviewed.

## Other Repositories
Our aim with [mod.io](https://mod.io), is to provide an open modding API. You are welcome to view, fork and contribute to our other codebases in use:

* [Design](https://design.mod.io) is public and open source, the repository can be [found here](https://github.com/DBolical/modioDESIGN).
* [SDK](https://sdk.mod.io) is public and open source, the repository with documentation can be [found here](https://github.com/DBolical/modioSDK). Game developers are welcome to utilize it directly, to add support for mods in their games, or extend it to create plugins and wrappers for other engines and codebases.
* [API documentation](https://docs.mod.io) is public and open source, the repository can be [found here](https://github.com/DBolical/modioAPIDOCS).
* [Browse engine tools](https://apps.mod.io), plugins and wrappers created by the community, or [share your own](https://apps.mod.io/add).
