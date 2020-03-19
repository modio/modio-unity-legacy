# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
- [Core] DownloadClient speed monitoring.
- [Core] LocalUser as a central place for the current user.
- [Core] UserDataStorage to manage user-specific data with platform-specific code-paths.
- [Core] Support for itch.io, Occulus, and Xbox Live external authentication.
- [Core] MetadataKVP ArrayToDictionary and DictionaryToArray functions that support duplicate keys.
- [Editor] PluginSettings directory previews.
- [UI] NavigationManager to assist controller support for the UI.
- [UI] Controller supported version of the Mod Browser prefab.
- [UI] Report Mod Dialog.

### Removed
- [Core] UserAuthenticationData - Replaced by LocalUser and UserAccountManagement functionality.

### Changed
- [Core] Subbed and Enabled mod management is now handled through LocalUser and UserAccountManagement rather than ModManager.
- [UI] ViewManager is now a core component for managing the various UI views.
- [Core] WebRequestErrors now log the Response Code in unclear situations.
- [Core] ModIOVersion replaced the SimpleVersion struct. ModIOVersion.Current now represents the plugin version.

### Fixed
- [UI] DownloadView now reactivates correct in OnEnable.
- [UI] ExplorerView now correctly loads default sort.
- [UI] ExplorerView page changes now handle out-of-bounds parameters.
- [UI] InspectorView changelog is now rich-text enabled.
- [UI] FileSize has been correctly labeled in the Inspector View details table.
- [UI] StateToggleDisplays are now grabbed using GetComponents in other components.

## [2.1.1] - 2020-02-12
### Added
- CHANGELOG.md
- UPM Support

### Changed
- Multiple directories and file locations to better support UPM
