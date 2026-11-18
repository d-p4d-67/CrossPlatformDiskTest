# CPDT TV Edition: Windows build environment

This fork is a legacy **Xamarin.Forms 5 / Xamarin.Android** application. Android Studio cannot compile its C# solution directly. Xamarin reached end of support in May 2024; current standard Visual Studio installations may not contain Xamarin.Android.

## Check your Windows machine (no changes made)

1. Clone the working branch: `git clone -b feature/android-tv-dpad https://github.com/d-p4d-67/CrossPlatformDiskTest.git`
2. Open PowerShell in the cloned folder.
3. Run `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check-xamarin-windows.ps1`
4. Look for `Xamarin.Android FOUND`, `Android SDK FOUND` and `Java runtime FOUND`. A `MISSING` result means that component needs attention; `UNKNOWN` means the script cannot determine its state.

The script does not download software or modify settings. Share its text output (omit private usernames or directory paths if desired) to determine the next build step.

## Source build notes

- Open `CrossPlatformDiskTest.sln` in Visual Studio **only after** confirming that the legacy Xamarin.Android MSBuild targets are available.
- The Android project uses `TargetFrameworkVersion=v13.0` and an Android manifest with `minSdkVersion=19`, `targetSdkVersion=34`. Installed toolchain/SDK must support the project settings.
- Build **Release|AnyCPU**; the project references prebuilt dependencies from `References/`. Debug configurations also reference sibling repositories, so Release is a better starting point.
- The Release configuration already lists `armeabi-v7a` and `arm64-v8a`; verify the actual generated APK contains native `lib/armeabi-v7a/` and `lib/arm64-v8a/` files before testing.
- An APK must be signed before installation if the packaging task produces an unsigned artifact. Do **not** commit your signing keystore/passwords to GitHub.
- The `Android TV source checks` GitHub Action compiles only the shared `UICore` library and validates source/configuration. It **does not build** the Android APK.
- The `Experimental Android TV APK build` workflow currently requires a legacy Xamarin.Android-capable runner and is manual.

**Current status:** D-pad changes are experimental and have not been compiled in the Android project or tested on hardware. Do not merge into master until that is done.
