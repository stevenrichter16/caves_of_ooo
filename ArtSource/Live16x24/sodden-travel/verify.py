"""Current Sodden source verifier; original quiet-mire verifier is deck-before/verify.py."""
import sys
from pathlib import Path
sys.dont_write_bytecode=True
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'Tools/Live16x24'))
from build_sodden_peat import verify
if __name__=='__main__':verify()
