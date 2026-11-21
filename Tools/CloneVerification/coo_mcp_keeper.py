# The Unity MCP server for headless verification runs. Every Unity editor's MCP plugin stops
# the local HTTP server when that editor quits (McpEditorShutdownCleanup), so a finished batch
# run in one verification clone killed the server under the other. The plugin refuses to stop
# a process whose command line names Unity without naming the server package, so this keeper
# (run by the server venv's python, transport passed here rather than on the command line)
# survives those quits. Stop it with: pkill -f coo_mcp_keeper.py
import os, sys
os.chdir('/Users/steven/unity-mcp/Server')
sys.path.insert(0, '/Users/steven/unity-mcp/Server/src')
sys.path.insert(0, '/Users/steven/unity-mcp/Server')
sys.argv = ['server', '--transport', 'http']
from main import main
sys.exit(main())
