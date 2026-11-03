#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$repo_root/Tools/EditModeRunner"
python3 patch.py
python3 select_tests.py \
  Assets/Tests/EditMode/Gameplay/Save/BrowserSaveQueueTests.cs \
  Assets/Tests/EditMode/Gameplay/Save/BrowserSaveServiceTests.cs \
  Assets/Tests/EditMode/Gameplay/Save/BrowserSaveFeedbackTests.cs \
  Assets/Tests/EditMode/Gameplay/Save/PhoneIntentGateTests.cs \
  Assets/Tests/EditMode/Gameplay/Save/GameAuditNewGameSaveTests.cs \
  Assets/Tests/EditMode/Gameplay/Save/GameAuditNewGameSaveAdversarialTests.cs \
  Assets/Tests/EditMode/Gameplay/Save/SteamDeckSaveStagingTests.cs \
  Assets/Tests/EditMode/Gameplay/Save/SteamDeckSaveStagingAdversarialTests.cs \
  Assets/Tests/EditMode/Presentation/Input/SaveLoadInputControllerTests.cs \
  Assets/Tests/EditMode/Presentation/UI/BootMenuControllerTests.cs
dotnet build -p:CooSaveUiTests=true -nologo -v q
dotnet bin/Debug/net8.0/EditModeRunner.dll --workers=1 --result=web-port-tests.xml
