# Simple DLL Loader for BepInEx with SlimVML support

Native loader for BepInEx that naïvely loads DLLs from a given directory and executes their `static void Main()` methods.
Small, simple, configurable and works on all platforms that BepInEx supports (Windows, Linux, macOS) by default.

Supports a few additional [SlimVML](https://github.com/PJninja/InSlimVML) env vars.

Use suggested only for educational purposes on assembly loading. If you need actual proper class loading with support for stable
load ordering, dependency management and automated Unity scene entrypoint handling, write a [normal BepInEx plugin](https://bepinex.github.io/bepinex_docs/v5.4.4/articles/dev_guide/plugin_tutorial/2_plugin_start.html).

**Requires BepInEx 5.4.7 or later**

## Usage

1. Grab `SlimVML.Loader.dll` from releases and put into `BepInEx/patchers` folder.
2. Install SlimVML DLLs like you normally would (i.e. into `SlimVML/Mods`).
3. (Optional) If you're using SlimVML, grab [base DLLs from SlimVML repo](https://github.com/PJninja/InSlimVML/tree/main/InSlimVML/Mods) and put them into `SlimVML/Mods`.
4. Run the game.
5. (Optional) Close the game, and edit loader configuration located at `BepInEx/config/SlimVML.cfg`.