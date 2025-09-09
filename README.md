Hotkey editor for Dawn of War Definitive Edition.

Build using dotnet (e.g. `dotnet publish ".\Dawn of War Definitive Edition Hotkey Editor.csproj" -c Release -r win-x64 --self-contained true`).

**Usage**

Just run the exe from anywhere. It should find your default Profile1 location, but if you have a different setup then you can use file -> Select Profile Folder to find it.

All LUA files in that location will be read, and if they fit the hotkey format can be loaded. You can edit the keys by recording the keys you want to press and changes will be automatically saved.

Then just select the hotkey profile in game, as normal, and everything should work.

You can create your own new profiles with the Make New Profile button. It will duplicate the current profile so pick the one that is closest to what you want. You can also delete these, but bare in mind that Steam cloudshare will recreate the file. As a result, I've made the app delete the contents so that it no longer shows up in the hotkey editor or in-game (this is just an FYI if you are confused why the file exists. This is nothing to do with the hotkey app, and happens even if you create these files manually).
