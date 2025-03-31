using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DominoSoundManager : MonoBehaviour
{
    public static DominoSoundManager Instance { get; private set; }
    [Header("Cascade Audio Settings")] //Create a separator in the inspector
    public AudioSource cascadeSource; // Background cascade sound
    public AudioClip cascadeClip;     // Rolling cascade sound
    public float maxVolume = .7f;   // Max volume when many dominoes are moving
    public float velocityScale = 0.01f; // Scale factor for volume adjustment
    public float volumeLerpSpeed = 2f; // Speed at which volume adjusts
    private float targetVolume = 0f; // The volume we want to reach
    public float totalVelocityThreshhold = -100f; // Minimum velocity to play the sound, total must exceed this
    public float minimumVelocity = 0.7f; // Minimum velocity for a piece to contibute to the cascase sound
    private float totalMovement = 0f; // Total movement of all dominoes
    
    [Header("Invididual Domino Sound Settings")]//Create a separator in the inspector
    public float minimumImpactForce = 1f;
    public float soundCooldown = 0.2f;
    private float lastSoundTime = 0f;
    private int songIndex = 0;
    private SongTitle currentSong = SongTitle.OdeToJoy;
    public int octaveOffset = 1;
    public DominoSoundList soundList;
    public DominoSoundList dominoClickSounds;

    public enum SongTitle
    { 
        Twinkle, Entertainer, MapleLeafRag,
        MaryHadALittleLamb, HappyBirthday,
        OdeToJoy, JingleBells, FurElise, 
        CanonInD, Beethoven5th, Greensleeves,
        MoonlightSonata, ClairDeLune, MinuteWaltz,
        ToccataAndFugue, WilliamTellOverture, SwanLake, BachPrelude
    
    }

    private static readonly Dictionary<string, int> noteMap = new Dictionary<string, int>
    {
        { "C", 0 }, { "C#", 1 }, { "Db", 1 },
        { "D", 2 }, { "D#", 3 }, { "Eb", 3 },
        { "E", 4 },
        { "F", 5 }, { "F#", 6 }, { "Gb", 6 },
        { "G", 7 }, { "G#", 8 }, { "Ab", 8 },
        { "A", 9 }, { "A#", 10 }, { "Bb", 10 },
        { "B", 11 },
        { "-", -1 } // Rest note
    };

private static readonly Dictionary<SongTitle, string> songLibrary = new Dictionary<SongTitle, string>
{
    {SongTitle.Twinkle,"CCGGAAG-FFEEDDC-GGFED-GGFED-CCGGAAG-FFEEDDC-"},
    {SongTitle.Entertainer,"EGCAG-CDE-GAC-EAG-"},
    {SongTitle.MapleLeafRag,"ECEGGECEGCEGCEDCFCDFDBDGBDBDECEGGECEGA"},
    {SongTitle.MaryHadALittleLamb,"EDCDEEEDDDEGG-"},
    {SongTitle.HappyBirthday,"GGGCBGGGDC-"},
    {SongTitle.OdeToJoy,"EEFGGFEDCCDEEDD-EEFGGFEDCCDEDCC-DECDEFECDEFEDCDG-EEFGGFEDCCDED-CC-"},
    {SongTitle.JingleBells,"EEEEEEEGCDEFFFFFEEEEDDEDG-"},
    {SongTitle.FurElise,"ED#ED#EBDCCEBEG#BC-"},
    {SongTitle.CanonInD,"DFAF#GAGBF#GBC#D-"},
    {SongTitle.Beethoven5th,"GGGEbFFFDGGGEbFFFD-"},
    {SongTitle.Greensleeves,"EGBCBGBCDE-"},
    {SongTitle.MoonlightSonata, "G#G#G#G#G#G#G#G#-A#A#A#A#A#A#A#A#-BFBF#GF#G-BFBF#GF#G-"}, 
    {SongTitle.ClairDeLune, "DFA#DFA#A#GFG#A#B-"},
    {SongTitle.MinuteWaltz, "EGBDGBEGBEGBDF#GBEGBEGBDGBEGBDGBD-"},
    {SongTitle.ToccataAndFugue, "DADbAC#D-DAAAGF#G#A-GF#G#A-BAGF#G#A-"},
    {SongTitle.WilliamTellOverture, "EDDDEEEDDDEEGFGFEEDDDEEEDDDEEGFGFE-"},
    {SongTitle.SwanLake, "BGAGABGBGAGABG-"},
    {SongTitle.BachPrelude, "CCGGCDEEFFG-"}
};

    void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject);
        // Setup cascade audio
        cascadeSource.clip = cascadeClip;
        cascadeSource.loop = true;
        cascadeSource.volume = 0f;
        cascadeSource.Play();
    }

    void Update()
    {
        // Compute the target volume based on movement
        targetVolume = Mathf.Clamp(totalMovement * velocityScale, 0f, maxVolume);
        // Reset total movement for the next interval
        totalMovement = totalVelocityThreshhold;
        // Smoothly interpolate the actual volume toward the target volume
        cascadeSource.volume = Mathf.Lerp(cascadeSource.volume, targetVolume, Time.deltaTime * volumeLerpSpeed);
    }

    public void UpdateDominoMovement(float velocityMagnitude)
    {
        if (velocityMagnitude > minimumVelocity)
        {
            totalMovement += Mathf.Clamp(velocityMagnitude, 0f, 20f);
        }
    }

    //////////////// Manage Domino Sounds ////////////////
    public void PlayDominoSound(float impactForce, bool isMusicMode, AudioSource source)
    {
        if (impactForce < minimumImpactForce || Time.time < lastSoundTime + soundCooldown)
            return;

        lastSoundTime = Time.time;

        if (isMusicMode)
        {
            PlayMusicNote(impactForce, source);
        }
        else
        {
            AudioClip clip = soundList.sounds[Random.Range(0, soundList.sounds.Length)];
            float volume = Mathf.Clamp(impactForce / 20f, 0.1f, 1.0f);
            source.PlayOneShot(clip, volume);
            source.pitch = 2; // Set pitch to 2 for domino click sounds
        }
    }
    private void PlayMusicNote(float impactForce, AudioSource source)
    {
        // Get the current song as a note sequence
        string songNotes = songLibrary[currentSong].Replace(" ", ""); // Remove spaces
        string[] notes = ParseNotes(songNotes); // Parse notes into an array
        string note = notes[songIndex % notes.Length]; // Get the current note
        songIndex++; // Move to the next note

        // If the song has completed, switch to the next song
        if (songIndex >= notes.Length)
        {
            songIndex = 0; // Reset the index for the next song
            SwitchToNextSong();
        }

        // If the note is a rest, skip to the next note
        if (note == "-")
        {
            return;
        }

        if (noteMap.TryGetValue(note, out int noteIndex) && noteIndex >= 0 && noteIndex < soundList.sounds.Length)
        {
            noteIndex += octaveOffset * 12; // Adjust for octave offset
            AudioClip clip = soundList.sounds[noteIndex];
            float volume = Mathf.Clamp(impactForce / 20f, 0.1f, 1.0f);
            source.PlayOneShot(clip, volume);
        }
    }
    private string[] ParseNotes(string songNotes)
    {
        List<string> parsedNotes = new List<string>();
        for (int i = 0; i < songNotes.Length; i++)
        {
            // Check for sharps (#) or flats (b) after a note
            if (i + 1 < songNotes.Length && (songNotes[i + 1] == '#' || songNotes[i + 1] == 'b'))
            {
                parsedNotes.Add(songNotes[i].ToString() + songNotes[i + 1]); // Combine note and sharp/flat
                i++; // Skip the next character
            }
            else
            {
                parsedNotes.Add(songNotes[i].ToString());
            }
        }
        return parsedNotes.ToArray();
    }

    private void SwitchToNextSong()
    {
        SongTitle[] songKeys = (SongTitle[])System.Enum.GetValues(typeof(SongTitle));
        int currentIndex = System.Array.IndexOf(songKeys, currentSong);
        currentSong = songKeys[(currentIndex + 1) % songKeys.Length];
        Debug.Log("Switched to song: " + currentSong);
    }
}