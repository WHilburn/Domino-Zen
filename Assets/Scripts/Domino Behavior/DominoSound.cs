using System.Collections.Generic;
using UnityEngine;
public class DominoSound : MonoBehaviour
{
    private AudioSource audioSource;
    private Rigidbody rb;
    public DominoSoundList soundList;
    public float minimumImpactForce = 1f;
    public DominoSoundList dominoClickSounds;
    private bool musicMode = false;
    public float soundCooldown = 0.2f; // Minimum time between sounds
    private static float lastSoundTime = 0f;  // Tracks the last time a sound was played
    private static int songIndex = 0;
    private static SongTitle currentSong = SongTitle.OdeToJoy; // Set the default song
    public int octaveOffset = 1; // Octave offset for the notes

    public enum SongTitle
    { 
        Twinkle, Entertainer, MapleLeafRag,
        MaryHadALittleLamb, HappyBirthday,
        OdeToJoy, JingleBells, FurElise, 
        CanonInD, Beethoven5th, Greensleeves
    }
    private static readonly Dictionary<char, int> noteMap = new Dictionary<char, int>
    {
        { 'C', 0 }, { 'D', 2 }, { 'E', 4 }, { 'F', 5 }, { 'G', 7 }, { 'A', 9 }, { 'B', 11 },
        // { 'C#', 1 }, { 'D#', 3 }, { 'F#', 6 }, { 'G#', 8 }, { 'A#', 10 },
        { '-', -1} //Rest note
    };

    private static readonly Dictionary<SongTitle, string> songLibrary = new Dictionary<SongTitle, string>
    {
        { SongTitle.Twinkle, "CCGGAAG-FFEEDDC-GGFED-GGFED-CCGGAAG-FFEEDDC-" },
        { SongTitle.Entertainer, "EGCAG-CDE-GAC-EAG-" },
        { SongTitle.MapleLeafRag, "EC EGA GEC EGA CEGC EDCA FA CA DFA DBDG BDBD EC EGA GEC EGA" },
        { SongTitle.MaryHadALittleLamb, "E D C D E E E D D D E G G-" },
        { SongTitle.HappyBirthday, "G G A G C B G G A G D C-" },
        { SongTitle.OdeToJoy, "EEFGGFE DCCDEEDD-EEFGGFE DCCDEDCC-DECDEFECDEFEDCDG-EEFGGFEDCCDED-CC-" },
        { SongTitle.JingleBells, "E E E E E E E G C D E F F F F F E E E E D D E D G-" },
        { SongTitle.FurElise, "E D# E D# E B D C A C E A B E G# B C-" },
        { SongTitle.CanonInD, "D A B F# G D G A B F# G A B C# D-" },
        { SongTitle.Beethoven5th, "G G G Eb F F F D G G G Eb F F F D-" },
        { SongTitle.Greensleeves, "E G A B C B A G A B C D E-" },
    };

    void Start()
    {
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();

        // If domino sound list is DominoClickSounds, set source pitch to 2
        if (soundList == dominoClickSounds)
        {
            audioSource.pitch = 2;
        }
        else musicMode = true;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (soundList == null || soundList.sounds.Length == 0) return;

        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce >= minimumImpactForce && Time.time >= lastSoundTime + soundCooldown)
        {
            lastSoundTime = Time.time;
            if (musicMode) PlayMusicNote(impactForce);
            else 
            {
                AudioClip clip = soundList.sounds[Random.Range(0, soundList.sounds.Length)];
                float volume = Mathf.Clamp(impactForce / 20f, 0.1f, 1.0f);
                audioSource.PlayOneShot(clip, volume);
            }

        }
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void PlayMusicNote(float impactForce)
    {
        // Get the current song as a note sequence
        string songNotes = songLibrary[currentSong].Replace(" ", ""); // Remove spaces
        char noteChar = songNotes[songIndex % songNotes.Length]; // Get the current note
        songIndex++; // Move to the next note

        // If the song has completed, switch to the next song
        if (songIndex >= songNotes.Length)
        {
            songIndex = 0; // Reset the index for the next song
            SwitchToNextSong();
        }

        // If the note is a rest, skip to the next note
        if (noteChar == '-')
        {
            return;
        }

        if (noteMap.TryGetValue(noteChar, out int noteIndex) && noteIndex < soundList.sounds.Length)
        {
            noteIndex += octaveOffset * 12; // Adjust for octave offset
            AudioClip clip = soundList.sounds[noteIndex];
            float volume = Mathf.Clamp(impactForce / 20f, 0.1f, 1.0f);
            audioSource.PlayOneShot(clip, volume);
        }
    }

    private void SwitchToNextSong()
    {
        // Get the list of song keys
        SongTitle[] songKeys = (SongTitle[])System.Enum.GetValues(typeof(SongTitle));

        // Find the index of the current song
        int currentIndex = System.Array.IndexOf(songKeys, currentSong);

        // Move to the next song, or loop back to the first song
        int nextIndex = (currentIndex + 1) % songKeys.Length;
        currentSong = songKeys[nextIndex];

        Debug.Log($"Switched to next song: {currentSong}");
    }
}