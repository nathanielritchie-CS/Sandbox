#!/bin/bash
# Multiplayer test harness: Unity Editor = host (Play Mode), headless Windows development builds = clients.
# Why: Multiplayer Play Mode can't be driven by the Pipeline tools (MPPM 2.0.2), so real extra processes stand in for it.
# Needs: Editor open on Isekai/, and the Pipeline runtime enabled for development builds
#        (unity command settings.runtime_pipeline set --settings '{"enable-in-development-builds":true}' --confirm true).
#
#   source tools/mp-harness/harness.sh
#   build_client                  build Isekai/Builds/Client (Editor must be idle: not compiling, not in Play Mode)
#   start_host                    Editor: enter Play Mode and press Host
#   launch_client [n]; join_client [n]
#                                 start headless client n (1 default, 2 = a fresh copy in Builds/Client2) and press Join
#   ev editor|client|client2 '<C# body>'
#                                 run C# in a process; helpers live in prelude.cs (Status, Window, Party, StandAt, KillDummy ...)
#   key <Key> [seconds] [n]       press a key on client n through the real Input System path
#   rt <runtime command ...>      e.g. rt runtime simulate_pointer --x 1 --y 1 --action down   (rt2 for client 2)
#   stop_client [n]; stop_host
# Gotchas: eval in a Player cannot reference project assemblies, so prelude.cs uses reflection; eval results are
#          truncated around 1 KB; every eval costs a few seconds, so use `await Task.Delay` inside one eval for timing.

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$HERE/../.." && pwd)"
PROJ="$ROOT/Isekai"
PROJ_W="$(cygpath -m "$PROJ")"
RUN_DIR="$PROJ/Temp/harness"
RUN_DIR_W="$(cygpath -m "$RUN_DIR")"
mkdir -p "$RUN_DIR"

# cdir [n] -> Windows-style path of client n's build folder
cdir() { if [ "${1:-1}" = 2 ]; then echo "$PROJ_W/Builds/Client2"; else echo "$PROJ_W/Builds/Client"; fi; }

# ev <editor|client|client2> '<c# body>' -> prints the result line (prelude helpers available)
ev() {
  local target=$1 body=$2
  { cat "$HERE/prelude.cs"; printf '%s\n' "$body"; } > "$RUN_DIR/_run_$target.cs"
  local f="$RUN_DIR_W/_run_$target.cs"
  cd "$PROJ" || return 1
  case "$target" in
    editor)  unity command script eval --file "$f" --result-only --timeout 90 2>&1 ;;
    client)  unity command --runtime-path "$(cdir 1)" script eval --file "$f" --result-only --timeout 90 2>&1 ;;
    client2) unity command --runtime-path "$(cdir 2)" script eval --file "$f" --result-only --timeout 90 2>&1 ;;
  esac | grep -E '"result"|"message"|"errorDetails"|error CS|Exception' | cut -c1-900 | head -8
}

# rtn <n> <runtime command args...>; rt = client 1, rt2 = client 2
rtn() { local n=$1; shift; cd "$PROJ" && unity command --runtime-path "$(cdir "$n")" "$@" --result-only 2>&1 | head -c 600; echo; }
rt()  { rtn 1 "$@"; }
rt2() { rtn 2 "$@"; }

# ed <editor command args...>
ed() { cd "$PROJ" && unity command "$@" --result-only 2>&1 | head -c 1500; echo; }

wait_editor() { cd "$PROJ" && unity status --until-ready --project-path "$(cygpath -w "$PROJ")" --format json 2>/dev/null | grep -o '"state": "[a-z]*"' | head -1; }

build_client() {
  cd "$PROJ" || return 1
  unity recompile --format json 2>/dev/null | grep -q '"compilationFailed": true' && { echo "compile errors, fix them first"; return 1; }
  local id
  id=$(unity command build run --target StandaloneWindows64 --output-path "$(cdir 1)/Isekai.exe" --options '["Development","DetailedBuildReport"]' --confirm true --result-only 2>&1 | grep -o '"buildId": "[^"]*"' | cut -d'"' -f4)
  [ -n "$id" ] || { echo "build not queued (Editor idle and out of Play Mode?)"; return 1; }
  local i R
  for i in $(seq 1 60); do
    sleep 10
    R=$(unity command build status --result-only 2>/dev/null)
    if echo "$R" | grep -q "$id" && echo "$R" | grep -q '"platform": *"StandaloneWindows64"'; then
      echo "$R" | python -c "import sys,json; d=json.load(sys.stdin); print({k:d.get(k) for k in ('result','totalWarnings','totalErrors','buildTimeMs')}); [print('  ',w.get('message','')[:200]) for w in d.get('warnings',[])+d.get('errors',[])]"
      return 0
    fi
  done
  echo "timed out waiting for the build"; return 1
}

# start_host: Play Mode in the Editor, then press Host.
start_host() {
  cd "$PROJ" && unity command console clear --result-only >/dev/null 2>&1
  unity command editor.playmode start --result-only --timeout 120 2>&1 | head -1
  local i R
  for i in $(seq 1 40); do
    R=$(ev editor 'return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " " + UnityEditor.EditorApplication.isPlaying;')
    echo "$R" | grep -q "Bootstrap True" && break; sleep 3
  done
  ev editor 'Call(First("Isekai.Core.Events.ConnectionRequestChannel"), "Raise", true, "127.0.0.1", (ushort)7777); await Task.Delay(3000); return Status();'
}

# launch_client [n]: start a headless development player (detached). Client 2 runs from a fresh copy of the build.
launch_client() {
  local n=${1:-1} dir
  if [ "$n" = 2 ]; then
    rm -rf "$PROJ/Builds/Client2" && cp -r "$PROJ/Builds/Client" "$PROJ/Builds/Client2"
    rm -f "$PROJ/Builds/Client2/.unity-pipeline-runtime-port"
    dir="$(cygpath -w "$PROJ")\\Builds\\Client2"
  else
    dir="$(cygpath -w "$PROJ")\\Builds\\Client"
  fi
  powershell.exe -NoProfile -Command "Start-Process -FilePath '$dir\\Isekai.exe' -ArgumentList '-batchmode','-nographics','-logFile','$(cygpath -w "$RUN_DIR")\\client$n.log'" >/dev/null 2>&1
}

# join_client [n]: wait for the player's server, cap fps, add virtual keyboard/mouse, press Join.
join_client() {
  local n=${1:-1} i R target=client
  [ "$n" = 2 ] && target=client2
  for i in $(seq 1 40); do
    R=$(cd "$PROJ" && unity command --runtime-path "$(cdir "$n")" runtime status --result-only 2>&1 | head -c 400)
    echo "$R" | grep -q LoadedLevelName && break; sleep 3
  done
  rtn "$n" runtime set_target_frame_rate --frame-rate 60 >/dev/null
  ev "$target" 'AddVirtualInput(); Call(First("Isekai.Core.Events.ConnectionRequestChannel"), "Raise", false, "127.0.0.1", (ushort)7777); await Task.Delay(5000); return Status();'
}

stop_client() { rtn "${1:-1}" runtime quit >/dev/null; sleep 3; }
stop_host()   { ed editor.playmode stop --timeout 60 | head -1; wait_editor; }

# key <Key> [seconds held] [n]: down, wait, up on client n.
key() { local n=${3:-1}; rtn "$n" runtime simulate_key --key "$1" --action down >/dev/null; sleep "${2:-0.15}"; rtn "$n" runtime simulate_key --key "$1" --action up >/dev/null; }
