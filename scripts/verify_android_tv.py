#!/usr/bin/env python3
"""Fast source/configuration checks; NOT a Xamarin.Android compilation."""
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
ANDROID = ROOT / "Saplin.CPDT.Android"
UI = ROOT / "Saplin.CPDT.UICore"
errors = []


def check(ok, message):
    print(("PASS " if ok else "FAIL ") + message)
    if not ok:
        errors.append(message)


def read(path):
    if not path.is_file():
        errors.append("Missing " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8-sig")


main = read(ANDROID / "MainActivity.cs")
nav = read(ANDROID / "TvDpadNavigation.cs")
selector = read(UI / "Controls/RollingSelector.cs")
project = read(ANDROID / "Saplin.CPDT.Android.csproj")
manifest = read(ANDROID / "Properties/AndroidManifest.xml")

try:
    xml = ET.fromstring(project)
    ns = {"m": "http://schemas.microsoft.com/developer/msbuild/2003"}
    releases = [
        p for p in xml.findall("m:PropertyGroup", ns)
        if "Release|AnyCPU" in p.attrib.get("Condition", "")
    ]
    check(len(releases) == 1, "Release configuration is present")
    if releases:
        abi_nodes = releases[0].findall("m:AndroidSupportedAbis", ns)
        abis = set((abi_nodes[0].text or "").split(";")) if abi_nodes else set()
        check({"armeabi-v7a", "arm64-v8a"}.issubset(abis),
              "Release includes both ARMv7 and ARM64")
    includes = {n.attrib.get("Include") for n in xml.findall(".//m:Compile", ns)}
    check("TvDpadNavigation.cs" in includes, "D-pad C# file included in Android project")
    refs = xml.findall(".//m:Reference", ns)
    meter = [n for n in refs if n.attrib.get("Include") == "Saplin.StorageSpeedMeter"]
    check(bool(meter) and any(n.attrib.get("Condition", "").find("Release") >= 0
                              for n in meter[0].findall("m:HintPath", ns)),
          "Release disk benchmark assembly reference is configured")
except ET.ParseError as e:
    errors.append("Invalid Android csproj XML: " + str(e))

try:
    root = ET.fromstring(manifest)
    android = "{http://schemas.android.com/apk/res/android}"
    sdk = root.find("uses-sdk")
    check(sdk is not None and int(sdk.attrib[android + "minSdkVersion"]) <= 19,
          "Minimum Android version remains API 19 or lower")
except (ET.ParseError, AttributeError, KeyError, ValueError) as e:
    errors.append("Invalid AndroidManifest.xml: " + str(e))

for key in ("DpadUp", "DpadDown", "DpadLeft", "DpadRight",
            "DpadCenter", "Enter", "NumpadEnter"):
    check("Keycode." + key in nav or "Keycode." + key in main,
          "Remote key " + key + " routed")
check("tvNavigation.Handle(keyCode)" in main, "MainActivity forwards remote keys")
check("Keycode.Back" in nav, "Back key retains Android fallback")
check("selector.Step(" in nav and "public void Step(int direction)" in selector,
      "Left/right selector navigation exists")
check("IsAnyPopupVisible" in nav, "Modal overlay handling is present")
check("ResultsDbViewModel.IsVisible" in nav and "Reset();" in nav,
      "D-pad does not activate controls behind online results")
check("e.RepeatCount > 0" in main, "Held OK cannot activate a disk test repeatedly")

print("\nThis is a source/configuration check only, not a compiled APK.")
if errors:
    print("FAILED checks: " + ", ".join(errors), file=sys.stderr)
    sys.exit(1)
print("All source checks passed.")
