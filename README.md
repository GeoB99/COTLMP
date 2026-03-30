# COTLMP
## What is this?
COTLMP (aka Cult of the Lamb Multiplayer) is a game modification of which implements multiplayer functionality for the critically acclaimed game **Cult of the Lamb**, developed by Massive Monster and published by Devolver Digital.

### Mod quality warning
**COTLMP is currently a work-in-progress project and under heavy development!** As such some things may not work well, bugs or glitches might occur and experimental features might change in the future without further premonition!

## Installation Guide
**BepInEx** and the **COTL API** library mod are needed for installation of this mod.

1. You can install BepInEx by [following this guide](https://docs.bepinex.dev/articles/user_guide/installation/index.html). After that follow the instructions on how to setup BepInEx for the game for the first time. **NOTE** that the mod uses [**BepInEx 5.4.23.5**](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.5)!
2. You can get the COTL API mod [from here](https://github.com/xhayper/COTL_API/releases/tag/v0.3.3). **NOTE** that the mod uses **COTL API v0.3.3**! The API library must be installed manually!
3. Once BepInEx is fully configured, extract the **COTLMP** folder to the `Cult of the Lamb\BepInEx\plugins` destination folder. **DO NOT FORGET** to put **COTL_API.dll** into the mod folder otherwise COTLMP won't run!
4. In the `Cult of the Lamb\BepInEx\config` folder path, open the `BepInEx.cfg` file and edit the value of the `Assembly` field to `Assembly-CSharp.dll` and of `Type` to `TwitchManager` respectively! 

This allows BepInEx to properly load the mod and patch all the core methods of the game needed.
The following fields are located down below within `Preloader.Entrypoint` label. [This is how the file should look like](https://raw.githubusercontent.com/GeoB99/COTLMP/refs/heads/master/Media/BepInExFields.png) if you did this step correctly.
5. Simply run the game and the mod should be ready to be loaded!

## Contributing
If you're willing to contribute to the mod project, please check the [contributing guidelines](CONTRIBUTING.md).

## Credits
For a list of notable contributors who have contributed significantly to the mod project, check [CREDITS](CREDITS).

## License
COTLMP is licensed under MIT.

Copyright (C) 2025 George Bișoc (aka GeoB99).
```
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
