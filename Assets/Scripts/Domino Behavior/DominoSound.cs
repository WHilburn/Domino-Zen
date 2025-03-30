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
    // private static int lastPlayedNoteIndex = 0;
    public float soundCooldown = 0.2f; // Minimum time between sounds
    private static float lastSoundTime = 0f;  // Tracks the last time a sound was played
    private static int songIndex = 0;
    private static string currentSong = "OdeToJoy"; // Set the song you want
    public int octaveOffset = 1; // Octave offset for the notes
    private static readonly Dictionary<char, int> noteMap = new Dictionary<char, int>
    {
        { 'C', 0 }, { 'D', 2 }, { 'E', 4 }, { 'F', 5 }, { 'G', 7 }, { 'A', 9 }, { 'B', 11 },
        // { 'C#', 1 }, { 'D#', 3 }, { 'F#', 6 }, { 'G#', 8 }, { 'A#', 10 },
        { '-', -1} //Rest note
    };

    private static readonly Dictionary<string, string> songLibrary = new Dictionary<string, string>
    {
        { "Twinkle", "CCGGAAG-FFEEDDC-GGFED-GGFED-CCGGAAG-FFEEDDC-" },
        { "Entertainer", "EGCAG-CDE-GAC-EAG-" },
        { "MapleLeafRag", "EC EGA GEC EGA CEGC EDCA FA CA DFA DBDG BDBD EC EGA GEC EGA" },
        { "MaryHadALittleLamb", "E D C D E E E D D D E G G-" },
        { "HappyBirthday", "G G A G C B" },
        { "OdeToJoy", "EEFGGFE DCCDEEDD-EEFGGFE DCCDEDCC-DECDEFECDEFEDCDG-EEFGGFEDCCDEDCC-" },
        { "JingleBells", "E E E E E E E G C D E" },
        { "FurElise", "E D# E D# E B D C A" },
        { "CanonInD", "D A B F# G D G A B F# G A B C# D" }
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

    private bool PlayMusicNote(float impactForce)
    {
        // Get current song as a note sequence
        string songNotes = songLibrary[currentSong].Replace(" ", ""); // Remove spaces
        char noteChar = songNotes[songIndex % songNotes.Length]; // Get current note
        songIndex++; // Move to next note
                     // If the note is a rest, skip to the next note
        if (noteChar == '-')
        {
            return false;
        }

        if (noteMap.TryGetValue(noteChar, out int noteIndex) && noteIndex < soundList.sounds.Length)
        {
            AudioClip clip = soundList.sounds[noteIndex];
            float volume = Mathf.Clamp(impactForce / 20f, 0.1f, 1.0f);
            audioSource.PlayOneShot(clip, volume);
        }

        return true;
    }
}