#!/usr/bin/env bash
# Stage only the public Unity example and its small test overlay. No login, build, or upload.
set -euo pipefail
if [[ $# -lt 1 || $# -gt 2 ]]; then
    echo "Usage: $0 NEW_DESTINATION [PINNED_SOURCE_TARBALL]" >&2
    exit 2
fi
here=$(cd -- "$(dirname -- "$0")" && pwd)
base=881f7e197c7b0958621ebd1a05f249bbe254d36f
digest=dcd9b1c1101fd41b212a96a939f54ae0d09fed1e87b5e996c4bf194a584fb0d0
parent=$(cd -- "$(dirname -- "$1")" && pwd)
dest="$parent/$(basename -- "$1")"
[[ ! -e "$dest" && ! -L "$dest" ]] || { echo "Output already exists: $dest" >&2; exit 2; }
tmp=$(mktemp -d "$parent/.hyperxr-unity-stage.XXXXXX")
trap 'rm -rf -- "$tmp"' EXIT
if [[ $# -eq 2 ]]; then
    archive=$2
else
    archive=$tmp/source.tar.gz
    curl --fail --location --retry 2 --max-time 240 \
        "https://codeload.github.com/Unity-Technologies/XR-Interaction-Toolkit-Examples/tar.gz/$base" \
        -o "$archive"
fi
actual=$(sha256sum < "$archive" | cut -d ' ' -f1)
[[ "$actual" == "$digest" ]] || { echo 'Unexpected pinned source archive digest' >&2; exit 2; }
project=$tmp/project
mkdir -p "$project"
tar -xzf "$archive" --no-same-owner --strip-components=1 -C "$project"
grep -Fxq 'm_EditorVersion: 6000.3.0f1' "$project/ProjectSettings/ProjectVersion.txt"
test -s "$project/Assets/XRI_Examples/Scenes/Station_01_Setup_Isolated.unity"
test -s "$project/LICENSE.md"
(cd "$project" && git apply --check "$here/generic-openxr.patch" && git apply "$here/generic-openxr.patch")
mkdir -p "$project/Assets/Editor"
cp "$here/HyperXRAndroidProbeBuild.cs" "$project/Assets/Editor/"
# Do not carry upstream CI definitions into the standalone lab repository.
if [[ -d "$project/.github" ]]; then mv -- "$project/.github" "$tmp/upstream-github"; fi
mv -T --no-clobber -- "$project" "$dest"
[[ ! -e "$project" && -s "$dest/Assets/Editor/HyperXRAndroidProbeBuild.cs" ]]
printf 'Staged Unity sample at %s (not built)\n' "$dest"
