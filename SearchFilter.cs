using System.Linq;
using Shared.PlayerData;
using Shared.TrackData;
using Shared.TrackSelection;

public class SearchFilter
{
    public SearchFilter(string param_type, string param_data)
    {
        switch (param_type)
        {
            case "": parameter = PARAMETER.TRACK_OR_AUTHOR_NAME; break;
            case "t": parameter = PARAMETER.TRACK_NAME; break;
            case "c": parameter = PARAMETER.AUTHOR_NAME; break;
            case "S": parameter = PARAMETER.SUBTITLE; break;
            case "a": parameter = PARAMETER.ARTIST_NAME; break;
            case "b": parameter = PARAMETER.BPM; break;
            case "i": parameter = PARAMETER.INTENSITY; break;
            case "w": parameter = PARAMETER.CATEGORY; break;
            case "s": parameter = PARAMETER.SCORE; break;
            case "r": parameter = PARAMETER.RANK; break;
            case "f": parameter = PARAMETER.FC; break;
            case "p": parameter = PARAMETER.ATTEMPED; break;
        }

        compString = param_data;

        if (parameter == PARAMETER.CATEGORY || parameter == PARAMETER.ATTEMPED || parameter == PARAMETER.FC)
        {
            float.TryParse(new string(compString.Where(c => char.IsDigit(c)).ToArray()), out compFloat);
        }

        if (parameter == PARAMETER.BPM || parameter == PARAMETER.INTENSITY || parameter == PARAMETER.SCORE)
        {
            int mask = 0;
            foreach (char c in compString)
            {
                if (c == '=') mask |= 1;
                if (c == '>') mask |= 2;
                if (c == '<') mask |= 4;
            }
            compMode = (COMP_MODE)mask;
            if (compMode == COMP_MODE.CONTAINS) compMode = COMP_MODE.EQUALS;

            float.TryParse(new string(compString.Where(c => char.IsDigit(c)).ToArray()), out compFloat);
        }
    }
    public enum PARAMETER
    {
        TRACK_OR_AUTHOR_NAME,
        TRACK_NAME,
        AUTHOR_NAME,
        SUBTITLE,
        ARTIST_NAME,
        BPM,
        INTENSITY,
        LENGTH,
        CATEGORY,
        SCORE,
        RANK,
        FC,
        ATTEMPED,
    }

    public enum COMP_MODE
    {
        CONTAINS,
        EQUALS = 1,
        GREATER_THAN = 2,
        GREATER_THAN_EQUAL = 3,
        LESS_THAN = 4,
        LESS_THAN_EQUAL = 5,
    }

    public PARAMETER parameter = PARAMETER.TRACK_OR_AUTHOR_NAME;
    public COMP_MODE compMode = COMP_MODE.CONTAINS;
    public string compString = "";
    public float compFloat = 0.0f;

    public bool CheckMatch(ITrackMetadata track, CustomTracksSelectionSceneController track_select)
    {
        switch (parameter)
        {
            case PARAMETER.TRACK_OR_AUTHOR_NAME:
                return track.StageCreatorName.ToLower().Contains(compString.ToLower()) || track.TrackName.ToLower().Contains(compString.ToLower());
            case PARAMETER.TRACK_NAME:
                return track.TrackName.ToLower().Contains(compString.ToLower());
            case PARAMETER.AUTHOR_NAME:
                return track.StageCreatorName.ToLower().Contains(compString.ToLower());
            case PARAMETER.SUBTITLE:
                return track.TrackSubtitle.ToLower().Contains(compString.ToLower());
            case PARAMETER.ARTIST_NAME:
                return track.ArtistName.ToLower().Contains(compString.ToLower());
            case PARAMETER.CATEGORY:
                if (compFloat == 0) return track.Category == TrackCategory.UgcLocal;
                return track.Category == TrackCategory.UgcRemote;
            case PARAMETER.BPM:
                return CompValue(track.BeatsPerMinute);
            case PARAMETER.INTENSITY:
                //check actual current diff
                if (track.GetDifficulty(track_select._selectedDifficulty) != null)
                {
                    return CompValue(track.GetDifficulty(track_select._selectedDifficulty).Intensity);
                }
                return false;
            case PARAMETER.SCORE:
                int highScoreForDifficulty = PlayerDataUtil.GetHighScoreForDifficulty(track.LevelId, track_select._selectedDifficulty);
                return CompValue(highScoreForDifficulty);
            case PARAMETER.RANK:
                string c = PlayerDataUtil.GetLetterGradeForDifficulty(track.LevelId, track_select._selectedDifficulty);
                return c.Trim().ToLower() == compString.Trim().ToLower();
            case PARAMETER.ATTEMPED:
                return PlayerDataUtil.HasLevelBeenAttempted(track.LevelId);
            case PARAMETER.FC:
                return PlayerDataUtil.GetIsFullComboForDifficulty(track.LevelId, track_select._selectedDifficulty) == (compFloat == 1.0f);
            case PARAMETER.LENGTH:
                return false;
                //return CompValue(track.TrackLength);
        }
        return false;
    }

    public bool CompValue(float? v)
    {
        if (!v.HasValue) return false;
        switch (compMode)
        {
            case COMP_MODE.EQUALS: return v.Value == compFloat;
            case COMP_MODE.GREATER_THAN: return v.Value > compFloat;
            case COMP_MODE.GREATER_THAN_EQUAL: return v.Value >= compFloat;
            case COMP_MODE.LESS_THAN: return v.Value < compFloat;
            case COMP_MODE.LESS_THAN_EQUAL: return v.Value <= compFloat;
        }
        return false;
    }

}
