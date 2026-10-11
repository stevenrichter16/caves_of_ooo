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
            if(iteration<1||iteration>10)throw new ArgumentOutOfRangeException(nameof(iteration),"No implemented native route for this audio polish iteration.");
            _audioPolishIteration=iteration;
        }
        private string AudioPolishMode=>"audio-polish-"+_audioPolishIteration.ToString("00");
        private string AudioPolishDirectory=>"../Docs/Verification/AudioPolish20/"+_audioPolishIteration.ToString("00");
        private string[] AudioPolishChecks=>_audioPolishIteration==1?AudioPolish01Checks:_audioPolishIteration==2?AudioPolish02Checks:_audioPolishIteration==3?AudioPolish03Checks:_audioPolishIteration==4?AudioPolish04Checks:_audioPolishIteration==5?AudioPolish05Checks:_audioPolishIteration==6?AudioPolish06Checks:_audioPolishIteration==7?AudioPolish07Checks:_audioPolishIteration==8?AudioPolish08Checks:_audioPolishIteration==9?AudioPolish09Checks:_audioPolishIteration==10?AudioPolish10Checks:Array.Empty<string>();
        private int AudioPolishMinimumCaptures=>_audioPolishIteration>=1&&_audioPolishIteration<=4?4:_audioPolishIteration==5?3:_audioPolishIteration==6?4:_audioPolishIteration==7?3:_audioPolishIteration==8?4:_audioPolishIteration==9?3:_audioPolishIteration==10?3:int.MaxValue;
        private string AudioPolishCanVerify=>_audioPolishIteration==1?AudioPolish01CanVerify:_audioPolishIteration==2?AudioPolish02CanVerify:_audioPolishIteration==3?AudioPolish03CanVerify:_audioPolishIteration==4?AudioPolish04CanVerify:_audioPolishIteration==5?AudioPolish05CanVerify:_audioPolishIteration==6?AudioPolish06CanVerify:_audioPolishIteration==7?AudioPolish07CanVerify:_audioPolishIteration==8?AudioPolish08CanVerify:_audioPolishIteration==9?AudioPolish09CanVerify:_audioPolishIteration==10?AudioPolish10CanVerify:"No implemented audio polish route.";
        private string AudioPolishCannotVerify=>_audioPolishIteration==1?AudioPolish01CannotVerify:_audioPolishIteration==2?AudioPolish02CannotVerify:_audioPolishIteration==3?AudioPolish03CannotVerify:_audioPolishIteration==4?AudioPolish04CannotVerify:_audioPolishIteration==5?AudioPolish05CannotVerify:_audioPolishIteration==6?AudioPolish06CannotVerify:_audioPolishIteration==7?AudioPolish07CannotVerify:_audioPolishIteration==8?AudioPolish08CannotVerify:_audioPolishIteration==9?AudioPolish09CannotVerify:_audioPolishIteration==10?AudioPolish10CannotVerify:"No live evidence.";
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
                    case 3:yield return RunAudioPolish03River();break;
                    case 4:yield return RunAudioPolish04Frozen();break;
                    case 5:yield return RunAudioPolish05Room();break;
                    case 6:yield return RunAudioPolish06AnimalRest();break;
                    case 7:yield return RunAudioPolish07Tails();break;
                    case 8:yield return RunAudioPolish08Brush();break;
                    case 9:yield return RunAudioPolish09();break;
                    case 10:yield return RunAudioPolish10();break;
                    default:throw new InvalidOperationException("Missing actual-game route for audio polish iteration "+_audioPolishIteration);
                }
            }
            finally{_audioPolishCleanup?.Invoke();_audioPolishCleanup=null;}
        }
    }
}
