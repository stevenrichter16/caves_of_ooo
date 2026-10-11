using System;
using System.Collections;
using System.Linq;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class ReferenceGladeNativePlayer
    {
        private int _audioPolishIteration;
        private Action _audioPolishCleanup;
        public void ConfigureAudioPolish(int iteration)
        {
            if(iteration!=1&&iteration!=2)throw new ArgumentOutOfRangeException(nameof(iteration),"No implemented native route for this audio polish iteration.");
            _audioPolishIteration=iteration;
        }
        private string AudioPolishMode=>"audio-polish-"+_audioPolishIteration.ToString("00");
        private string AudioPolishDirectory=>"../Docs/Verification/AudioPolish20/"+_audioPolishIteration.ToString("00");
        private string[] AudioPolishChecks=>_audioPolishIteration==1?AudioPolish01Checks:_audioPolishIteration==2?AudioPolish02Checks:Array.Empty<string>();
        private int AudioPolishMinimumCaptures=>_audioPolishIteration==1||_audioPolishIteration==2?4:int.MaxValue;
        private string AudioPolishCanVerify=>_audioPolishIteration==1?AudioPolish01CanVerify:_audioPolishIteration==2?AudioPolish02CanVerify:"No implemented audio polish route.";
        private string AudioPolishCannotVerify=>_audioPolishIteration==1?AudioPolish01CannotVerify:_audioPolishIteration==2?AudioPolish02CannotVerify:"No live evidence.";
        private bool AudioPolishComplete=>AudioPolishChecks.Length>0
            &&AudioPolishChecks.All(name=>_audit.Count(row=>row=="PASS "+name)==1)
            &&_audit.Count(row=>row=="PASS ordinary_player_and_native_zone")==1
            &&_audit.Count==AudioPolishChecks.Length+1&&_screenshots.Count>=AudioPolishMinimumCaptures;
        private IEnumerator RunAudioPolishAudit()
        {
            try
            {
                switch(_audioPolishIteration)
                {
                    case 1:yield return RunAudioPolish01WorldMap();break;
                    case 2:yield return RunAudioPolish02Bookmarks();break;
                    default:throw new InvalidOperationException("Missing actual-game route for audio polish iteration "+_audioPolishIteration);
                }
            }
            finally{_audioPolishCleanup?.Invoke();_audioPolishCleanup=null;}
        }
    }
}
